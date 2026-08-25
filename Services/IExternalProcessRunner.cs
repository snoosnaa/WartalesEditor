namespace WartalesEditor.Services;

public sealed class ExternalProcessRequest
{
    public required string ExecutablePath { get; init; }

    public required IReadOnlyList<string> Arguments { get; init; }

    public required string WorkingDirectory { get; init; }

    public required TimeSpan Timeout { get; init; }
}

public sealed class ExternalProcessResult
{
    public bool Started { get; init; }

    public bool TimedOut { get; init; }

    public bool Cancelled { get; init; }

    public bool TerminationFailed { get; init; }

    public uint? ContainedProcessCount { get; init; }

    public int ProcessId { get; init; }

    public int ExitCode { get; init; } = -1;

    public string StandardOutput { get; init; } = string.Empty;

    public string StandardError { get; init; } = string.Empty;

    public string StartError { get; init; } = string.Empty;

    public string ExecutionError { get; init; } = string.Empty;
}

public interface IExternalProcessRunner
{
    Task<ExternalProcessResult> RunAsync(
        ExternalProcessRequest request,
        CancellationToken cancellationToken = default);
}
