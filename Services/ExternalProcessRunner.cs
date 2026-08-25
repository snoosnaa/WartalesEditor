using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace WartalesEditor.Services;

public sealed class ExternalProcessRunner : IExternalProcessRunner
{
    private static readonly TimeSpan TerminationTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan JobPollInterval = TimeSpan.FromMilliseconds(25);
    private static readonly TimeSpan MaximumProcessTimeout =
        TimeSpan.FromMilliseconds(uint.MaxValue - 1L);

    public async Task<ExternalProcessResult> RunAsync(
        ExternalProcessRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Timeout <= TimeSpan.Zero || request.Timeout > MaximumProcessTimeout)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "The process timeout must be greater than zero and within the supported timer range.");
        }

        if (!OperatingSystem.IsWindows())
        {
            return new ExternalProcessResult
            {
                StartError =
                    "The contained external process could not be started because Windows Job Objects are unavailable."
            };
        }

        ContainedProcessSession session;

        try
        {
            session = ContainedProcessSession.Start(request);
        }
        catch (Exception exception)
        {
            return new ExternalProcessResult
            {
                StartError = exception.Message
            };
        }

        using (session)
        {
            Task<string> standardOutput = session.StandardOutput.ReadToEndAsync();
            Task<string> standardError = session.StandardError.ReadToEndAsync();

            using CancellationTokenSource timeout = new(request.Timeout);
            using CancellationTokenSource linked =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    timeout.Token);

            try
            {
                try
                {
                    await WaitForContainedProcessesToExitAsync(
                        session.JobHandle,
                        linked.Token);
                }
                catch (OperationCanceledException)
                {
                    bool exited =
                        await TerminateAndConfirmJobExitAsync(session.JobHandle);

                    if (!exited)
                    {
                        return new ExternalProcessResult
                        {
                            Started = true,
                            ProcessId = session.ProcessId,
                            TerminationFailed = true,
                            ContainedProcessCount =
                                TryGetActiveProcessCount(session.JobHandle),
                            ExecutionError =
                                "The contained external process tree could not be confirmed stopped."
                        };
                    }

                    return new ExternalProcessResult
                    {
                        Started = true,
                        ProcessId = session.ProcessId,
                        TimedOut = !cancellationToken.IsCancellationRequested,
                        Cancelled = cancellationToken.IsCancellationRequested,
                        ContainedProcessCount = 0,
                        StandardOutput = await ReadOutputSafely(standardOutput),
                        StandardError = await ReadOutputSafely(standardError)
                    };
                }

                return new ExternalProcessResult
                {
                    Started = true,
                    ProcessId = session.ProcessId,
                    ExitCode = session.GetExitCode(),
                    ContainedProcessCount = 0,
                    StandardOutput = await ReadOutputSafely(standardOutput),
                    StandardError = await ReadOutputSafely(standardError)
                };
            }
            catch (Exception exception)
            {
                bool exited =
                    await TerminateAndConfirmJobExitAsync(session.JobHandle);

                return new ExternalProcessResult
                {
                    Started = true,
                    ProcessId = session.ProcessId,
                    TerminationFailed = !exited,
                    ContainedProcessCount =
                        exited ? 0 : TryGetActiveProcessCount(session.JobHandle),
                    ExecutionError = exception.Message
                };
            }
        }
    }

    private static async Task WaitForContainedProcessesToExitAsync(
        SafeFileHandle jobHandle,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            uint activeProcessCount = GetActiveProcessCount(jobHandle);

            if (activeProcessCount == 0)
            {
                return;
            }

            await Task.Delay(JobPollInterval, cancellationToken);
        }
    }

    private static async Task<bool> TerminateAndConfirmJobExitAsync(
        SafeFileHandle jobHandle)
    {
        uint? activeBeforeTermination = TryGetActiveProcessCount(jobHandle);

        if (activeBeforeTermination == 0)
        {
            return true;
        }

        _ = NativeMethods.TerminateJobObject(jobHandle, 1);

        using CancellationTokenSource terminationTimeout = new(TerminationTimeout);

        try
        {
            await WaitForContainedProcessesToExitAsync(
                jobHandle,
                terminationTimeout.Token);
            return true;
        }
        catch
        {
            return TryGetActiveProcessCount(jobHandle) == 0;
        }
    }

    private static uint GetActiveProcessCount(SafeFileHandle jobHandle)
    {
        if (!NativeMethods.QueryInformationJobObject(
                jobHandle,
                NativeMethods.JobObjectInfoType.BasicAccountingInformation,
                out NativeMethods.JobObjectBasicAccountingInformation information,
                (uint)Marshal.SizeOf<NativeMethods.JobObjectBasicAccountingInformation>(),
                IntPtr.Zero))
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "The contained process count could not be queried.");
        }

        return information.ActiveProcesses;
    }

    private static uint? TryGetActiveProcessCount(SafeFileHandle jobHandle)
    {
        try
        {
            return GetActiveProcessCount(jobHandle);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string> ReadOutputSafely(Task<string> outputTask)
    {
        try
        {
            return await outputTask;
        }
        catch
        {
            return string.Empty;
        }
    }

    private sealed class ContainedProcessSession : IDisposable
    {
        private ContainedProcessSession(
            SafeFileHandle jobHandle,
            SafeFileHandle processHandle,
            StreamReader standardOutput,
            StreamReader standardError,
            int processId)
        {
            JobHandle = jobHandle;
            ProcessHandle = processHandle;
            StandardOutput = standardOutput;
            StandardError = standardError;
            ProcessId = processId;
        }

        internal SafeFileHandle JobHandle { get; }
        private SafeFileHandle ProcessHandle { get; }
        internal StreamReader StandardOutput { get; }
        internal StreamReader StandardError { get; }
        internal int ProcessId { get; }

        internal static ContainedProcessSession Start(ExternalProcessRequest request)
        {
            SafeFileHandle? jobHandle = null;
            SafeFileHandle? processHandle = null;
            SafeFileHandle? threadHandle = null;
            SafeFileHandle? standardOutputRead = null;
            SafeFileHandle? standardOutputWrite = null;
            SafeFileHandle? standardErrorRead = null;
            SafeFileHandle? standardErrorWrite = null;
            SafeFileHandle? standardInput = null;
            StreamReader? standardOutput = null;
            StreamReader? standardError = null;
            bool processCreated = false;
            bool processAssignedToJob = false;

            try
            {
                jobHandle = NativeMethods.CreateJobObject(IntPtr.Zero, null);
                ThrowIfInvalid(jobHandle, "The process containment job could not be created.");

                NativeMethods.JobObjectExtendedLimitInformation limitInformation = new();
                limitInformation.BasicLimitInformation.LimitFlags =
                    NativeMethods.JobObjectLimitKillOnJobClose;

                if (!NativeMethods.SetInformationJobObject(
                        jobHandle,
                        NativeMethods.JobObjectInfoType.ExtendedLimitInformation,
                        ref limitInformation,
                        (uint)Marshal.SizeOf<NativeMethods.JobObjectExtendedLimitInformation>()))
                {
                    throw CreateWin32Exception(
                        "The process containment job could not be configured.");
                }

                NativeMethods.SecurityAttributes inheritableSecurity =
                    new()
                    {
                        Length = Marshal.SizeOf<NativeMethods.SecurityAttributes>(),
                        InheritHandle = true
                    };

                CreateOutputPipe(
                    inheritableSecurity,
                    out standardOutputRead,
                    out standardOutputWrite);
                CreateOutputPipe(
                    inheritableSecurity,
                    out standardErrorRead,
                    out standardErrorWrite);

                standardInput = NativeMethods.CreateFile(
                    "NUL",
                    NativeMethods.GenericRead,
                    NativeMethods.FileShareRead | NativeMethods.FileShareWrite,
                    ref inheritableSecurity,
                    NativeMethods.OpenExisting,
                    NativeMethods.FileAttributeNormal,
                    IntPtr.Zero);
                ThrowIfInvalid(
                    standardInput,
                    "The contained process input handle could not be created.");

                NativeMethods.StartupInfo startupInfo =
                    new()
                    {
                        Size = Marshal.SizeOf<NativeMethods.StartupInfo>(),
                        Flags = NativeMethods.StartfUseStdHandles,
                        StandardInput = standardInput.DangerousGetHandle(),
                        StandardOutput = standardOutputWrite.DangerousGetHandle(),
                        StandardError = standardErrorWrite.DangerousGetHandle()
                    };

                StringBuilder commandLine =
                    BuildCommandLine(request.ExecutablePath, request.Arguments);

                if (!NativeMethods.CreateProcess(
                        request.ExecutablePath,
                        commandLine,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        true,
                        NativeMethods.CreateNoWindow | NativeMethods.CreateSuspended,
                        IntPtr.Zero,
                        request.WorkingDirectory,
                        ref startupInfo,
                        out NativeMethods.ProcessInformation processInformation))
                {
                    throw CreateWin32Exception("The external process could not be started.");
                }

                processCreated = true;
                processHandle =
                    new SafeFileHandle(processInformation.Process, ownsHandle: true);
                threadHandle =
                    new SafeFileHandle(processInformation.Thread, ownsHandle: true);

                standardOutputWrite.Dispose();
                standardOutputWrite = null;
                standardErrorWrite.Dispose();
                standardErrorWrite = null;
                standardInput.Dispose();
                standardInput = null;

                if (!NativeMethods.AssignProcessToJobObject(jobHandle, processHandle))
                {
                    throw CreateWin32Exception(
                        "The external process could not be assigned to its containment job.");
                }

                processAssignedToJob = true;

                FileStream standardOutputStream =
                    new(
                        standardOutputRead,
                        FileAccess.Read,
                        bufferSize: 4096,
                        isAsync: false);
                standardOutputRead = null;
                FileStream standardErrorStream =
                    new(
                        standardErrorRead,
                        FileAccess.Read,
                        bufferSize: 4096,
                        isAsync: false);
                standardErrorRead = null;
                standardOutput = new StreamReader(standardOutputStream);
                standardError = new StreamReader(standardErrorStream);

                if (NativeMethods.ResumeThread(threadHandle) == uint.MaxValue)
                {
                    throw CreateWin32Exception(
                        "The contained external process could not be resumed.");
                }

                ContainedProcessSession session =
                    new(
                        jobHandle,
                        processHandle,
                        standardOutput,
                        standardError,
                        unchecked((int)processInformation.ProcessId));

                jobHandle = null;
                processHandle = null;
                standardOutput = null;
                standardError = null;

                return session;
            }
            catch
            {
                if (processCreated)
                {
                    if (processAssignedToJob
                        && jobHandle is not null
                        && !jobHandle.IsInvalid)
                    {
                        _ = NativeMethods.TerminateJobObject(jobHandle, 1);
                    }
                    else if (processHandle is not null && !processHandle.IsInvalid)
                    {
                        _ = NativeMethods.TerminateProcess(processHandle, 1);
                    }

                    if (processHandle is not null && !processHandle.IsInvalid)
                    {
                        _ = NativeMethods.WaitForSingleObject(processHandle, 10_000);
                    }
                }

                throw;
            }
            finally
            {
                threadHandle?.Dispose();
                standardOutput?.Dispose();
                standardError?.Dispose();
                standardOutputRead?.Dispose();
                standardOutputWrite?.Dispose();
                standardErrorRead?.Dispose();
                standardErrorWrite?.Dispose();
                standardInput?.Dispose();
                processHandle?.Dispose();
                jobHandle?.Dispose();
            }
        }

        internal int GetExitCode()
        {
            if (!NativeMethods.GetExitCodeProcess(ProcessHandle, out uint exitCode))
            {
                throw CreateWin32Exception(
                    "The external process exit code could not be read.");
            }

            return unchecked((int)exitCode);
        }

        public void Dispose()
        {
            StandardOutput.Dispose();
            StandardError.Dispose();
            ProcessHandle.Dispose();
            JobHandle.Dispose();
        }

        private static void CreateOutputPipe(
            NativeMethods.SecurityAttributes securityAttributes,
            out SafeFileHandle readHandle,
            out SafeFileHandle writeHandle)
        {
            if (!NativeMethods.CreatePipe(
                    out readHandle,
                    out writeHandle,
                    ref securityAttributes,
                    0))
            {
                throw CreateWin32Exception(
                    "The contained process output pipe could not be created.");
            }

            if (!NativeMethods.SetHandleInformation(
                    readHandle,
                    NativeMethods.HandleFlagInherit,
                    0))
            {
                readHandle.Dispose();
                writeHandle.Dispose();
                throw CreateWin32Exception(
                    "The contained process output pipe could not be secured.");
            }
        }

        private static StringBuilder BuildCommandLine(
            string executablePath,
            IReadOnlyList<string> arguments)
        {
            StringBuilder commandLine = new();
            AppendQuotedArgument(commandLine, executablePath);

            foreach (string argument in arguments)
            {
                commandLine.Append(' ');
                AppendQuotedArgument(commandLine, argument);
            }

            return commandLine;
        }

        private static void AppendQuotedArgument(
            StringBuilder commandLine,
            string argument)
        {
            if (argument.Length > 0
                && argument.IndexOfAny(new[] { ' ', '\t', '\n', '\v', '"' }) < 0)
            {
                commandLine.Append(argument);
                return;
            }

            commandLine.Append('"');
            int backslashCount = 0;

            foreach (char character in argument)
            {
                if (character == '\\')
                {
                    backslashCount++;
                    continue;
                }

                if (character == '"')
                {
                    commandLine.Append('\\', backslashCount * 2 + 1);
                    commandLine.Append('"');
                    backslashCount = 0;
                    continue;
                }

                commandLine.Append('\\', backslashCount);
                backslashCount = 0;
                commandLine.Append(character);
            }

            commandLine.Append('\\', backslashCount * 2);
            commandLine.Append('"');
        }

        private static void ThrowIfInvalid(SafeFileHandle handle, string message)
        {
            if (handle.IsInvalid)
            {
                throw CreateWin32Exception(message);
            }
        }

        private static Win32Exception CreateWin32Exception(string message)
        {
            return new Win32Exception(Marshal.GetLastWin32Error(), message);
        }
    }

    private static class NativeMethods
    {
        internal const uint CreateSuspended = 0x00000004;
        internal const uint CreateNoWindow = 0x08000000;
        internal const uint StartfUseStdHandles = 0x00000100;
        internal const uint HandleFlagInherit = 0x00000001;
        internal const uint JobObjectLimitKillOnJobClose = 0x00002000;
        internal const uint GenericRead = 0x80000000;
        internal const uint FileShareRead = 0x00000001;
        internal const uint FileShareWrite = 0x00000002;
        internal const uint OpenExisting = 3;
        internal const uint FileAttributeNormal = 0x00000080;

        internal enum JobObjectInfoType
        {
            BasicAccountingInformation = 1,
            ExtendedLimitInformation = 9
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SecurityAttributes
        {
            internal int Length;
            internal IntPtr SecurityDescriptor;

            [MarshalAs(UnmanagedType.Bool)]
            internal bool InheritHandle;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct StartupInfo
        {
            internal int Size;
            internal string? Reserved;
            internal string? Desktop;
            internal string? Title;
            internal uint X;
            internal uint Y;
            internal uint XSize;
            internal uint YSize;
            internal uint XCountChars;
            internal uint YCountChars;
            internal uint FillAttribute;
            internal uint Flags;
            internal ushort ShowWindow;
            internal ushort Reserved2Size;
            internal IntPtr Reserved2;
            internal IntPtr StandardInput;
            internal IntPtr StandardOutput;
            internal IntPtr StandardError;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ProcessInformation
        {
            internal IntPtr Process;
            internal IntPtr Thread;
            internal uint ProcessId;
            internal uint ThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobObjectBasicAccountingInformation
        {
            internal long TotalUserTime;
            internal long TotalKernelTime;
            internal long ThisPeriodTotalUserTime;
            internal long ThisPeriodTotalKernelTime;
            internal uint TotalPageFaultCount;
            internal uint TotalProcesses;
            internal uint ActiveProcesses;
            internal uint TotalTerminatedProcesses;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobObjectBasicLimitInformation
        {
            internal long PerProcessUserTimeLimit;
            internal long PerJobUserTimeLimit;
            internal uint LimitFlags;
            internal UIntPtr MinimumWorkingSetSize;
            internal UIntPtr MaximumWorkingSetSize;
            internal uint ActiveProcessLimit;
            internal UIntPtr Affinity;
            internal uint PriorityClass;
            internal uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct IoCounters
        {
            internal ulong ReadOperationCount;
            internal ulong WriteOperationCount;
            internal ulong OtherOperationCount;
            internal ulong ReadTransferCount;
            internal ulong WriteTransferCount;
            internal ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JobObjectExtendedLimitInformation
        {
            internal JobObjectBasicLimitInformation BasicLimitInformation;
            internal IoCounters IoInfo;
            internal UIntPtr ProcessMemoryLimit;
            internal UIntPtr JobMemoryLimit;
            internal UIntPtr PeakProcessMemoryUsed;
            internal UIntPtr PeakJobMemoryUsed;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeFileHandle CreateJobObject(
            IntPtr jobAttributes,
            string? name);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetInformationJobObject(
            SafeFileHandle jobHandle,
            JobObjectInfoType informationClass,
            ref JobObjectExtendedLimitInformation information,
            uint informationLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryInformationJobObject(
            SafeFileHandle jobHandle,
            JobObjectInfoType informationClass,
            out JobObjectBasicAccountingInformation information,
            uint informationLength,
            IntPtr returnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AssignProcessToJobObject(
            SafeFileHandle jobHandle,
            SafeFileHandle processHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool TerminateJobObject(
            SafeFileHandle jobHandle,
            uint exitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreatePipe(
            out SafeFileHandle readPipe,
            out SafeFileHandle writePipe,
            ref SecurityAttributes pipeAttributes,
            uint size);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetHandleInformation(
            SafeFileHandle handle,
            uint mask,
            uint flags);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeFileHandle CreateFile(
            string fileName,
            uint desiredAccess,
            uint shareMode,
            ref SecurityAttributes securityAttributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateProcess(
            string applicationName,
            StringBuilder commandLine,
            IntPtr processAttributes,
            IntPtr threadAttributes,
            [MarshalAs(UnmanagedType.Bool)] bool inheritHandles,
            uint creationFlags,
            IntPtr environment,
            string workingDirectory,
            ref StartupInfo startupInfo,
            out ProcessInformation processInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern uint ResumeThread(SafeFileHandle threadHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetExitCodeProcess(
            SafeFileHandle processHandle,
            out uint exitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool TerminateProcess(
            SafeFileHandle processHandle,
            uint exitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern uint WaitForSingleObject(
            SafeFileHandle handle,
            uint milliseconds);
    }
}
