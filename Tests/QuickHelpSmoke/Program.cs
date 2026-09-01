using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;
using WartalesEditor;
using WartalesEditor.Models;
using WartalesEditor.Services;
using WartalesEditor.ViewModels;
using WartalesEditor.Views;

int checks = 0;
Exception? uiFailure = null;

Thread uiThread = new(() =>
{
    try
    {
        RunQuickHelpSmoke();
    }
    catch (Exception exception)
    {
        uiFailure = exception;
    }
});

uiThread.SetApartmentState(ApartmentState.STA);
uiThread.Start();
uiThread.Join();

if (uiFailure != null)
{
    throw new InvalidOperationException(
        "FAILED: Quick Help WPF/STA smoke coverage",
        uiFailure);
}

Console.WriteLine(
    $"Quick Help smoke checks passed: {checks}");

void RunQuickHelpSmoke()
{
    App application = new()
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown
    };
    application.InitializeComponent();

    MainWindow mainWindow = new()
    {
        WindowStartupLocation = WindowStartupLocation.Manual,
        Left = 80,
        Top = 80,
        ShowInTaskbar = false
    };
    application.MainWindow = mainWindow;
    mainWindow.Show();
    DrainDispatcher();

    MainViewModel viewModel = mainWindow.ViewModel;
    TestMessageDialogService messages = new();
    viewModel.UseMessageDialogServiceForTesting(messages);

    Button quickHelpButton =
        (Button?)mainWindow.FindName("QuickHelpButton")
        ?? throw new InvalidOperationException(
            "Quick Help button was not found.");

    Check(
        string.Equals(
            quickHelpButton.Content?.ToString(),
            "Quick Help",
            StringComparison.Ordinal),
        "Quick Help button exists with the approved label");
    Check(
        quickHelpButton.IsVisible &&
        quickHelpButton.IsEnabled,
        "Quick Help button is visible and enabled with no project");
    Check(
        quickHelpButton.Parent is Grid selectorGrid &&
        selectorGrid.ColumnDefinitions.Count == 2 &&
        Grid.GetColumn(quickHelpButton) == 1,
        "Quick Help occupies the right selector-grid column");
    Check(
        viewModel.Project == null &&
        viewModel.ShowQuickHelpCommand.CanExecute(null),
        "Quick Help command can execute with no project");

    string initialStatus = viewModel.Status;
    bool initialUndo = viewModel.UndoCommand.CanExecute(null);
    bool initialRedo = viewModel.RedoCommand.CanExecute(null);

    viewModel.ShowQuickHelpCommand.Execute(null);
    DrainDispatcher();

    QuickHelpWindow firstWindow =
        application.Windows
            .OfType<QuickHelpWindow>()
            .Single();

    Check(
        viewModel.IsQuickHelpWindowOpen &&
        firstWindow.IsVisible,
        "Quick Help opens and is tracked");
    Check(
        ReferenceEquals(firstWindow.Owner, mainWindow) &&
        mainWindow.IsEnabled,
        "Quick Help is owned and modeless");
    Check(
        firstWindow.Width == 720 &&
        firstWindow.Height == 440 &&
        firstWindow.MinWidth == 620 &&
        firstWindow.MinHeight == 360 &&
        firstWindow.ResizeMode == ResizeMode.CanResizeWithGrip &&
        firstWindow.SizeToContent == SizeToContent.Manual &&
        !firstWindow.ShowInTaskbar,
        "Quick Help uses the approved sizing and utility-window contract");

    TabControl tabs =
        (TabControl?)firstWindow.FindName("QuickHelpTabs")
        ?? throw new InvalidOperationException(
            "Quick Help tabs were not found.");
    string[] expectedHeaders =
    {
        "Import",
        "Gameplay Tools",
        "Profiles",
        "Restore Previous Values",
        "Export"
    };
    string[] actualHeaders = tabs.Items
        .Cast<TabItem>()
        .Select(item => item.Header?.ToString() ?? string.Empty)
        .ToArray();

    Check(
        tabs.Items.Count == 5,
        "Quick Help contains exactly five tabs");
    Check(
        actualHeaders.SequenceEqual(expectedHeaders),
        "Quick Help tab labels and order are exact");
    Check(
        tabs.SelectedIndex == 0,
        "Import is the default tab");

    string[][] expectedContent =
    {
        new[]
        {
            "Import the current Wartales game data into the editor.",
            "1. Close Wartales.",
            "2. Set up QuickBMS and the Shiro Games PAK script if you have not already.",
            "3. Click Import From Wartales.",
            "4. Confirm replacement if prompted.",
            "5. Wait for data.cdb to open in the editor."
        },
        new[]
        {
            "Use Gameplay Tools for guided gameplay changes.",
            "1. Open or import a CDB and select Gameplay Tools.",
            "2. Choose the tool you want to use.",
            "3. Select the available preset or option.",
            "4. Review the current setting and preview.",
            "5. Click Apply.",
            "6. Open Review Changes if you want to inspect the result."
        },
        new[]
        {
            "Profiles preserve a reusable personal mod configuration.",
            "1. Open or import the CDB you want to work with.",
            "2. If it is fresh or unmodded, apply your existing Profile if desired.",
            "3. Make or adjust your changes.",
            "4. Save the edited CDB.",
            "5. Create a new Profile or update your existing Profile to preserve the setup.",
            "Saving the CDB saves the edited game data. Creating or updating a Profile saves your reusable mod setup."
        },
        new[]
        {
            "Restore Previous Values returns a Gameplay Tool to the values it had before you first changed it with that tool.",
            "1. Open the same Gameplay Tool you previously changed.",
            "2. Click Restore Previous Values.",
            "3. Review the restored result in the tool or Review Changes.",
            "These are your previous values, not necessarily Wartales defaults."
        },
        new[]
        {
            "Export your saved CDB back to Wartales.",
            "1. Finish and review your changes.",
            "2. Save the edited CDB.",
            "3. Close Wartales.",
            "4. Click Export Back to Wartales.",
            "5. Confirm the warning.",
            "6. Wait for writing and verification to finish.",
            "Export writes your saved CDB back to Wartales and verifies the result."
        }
    };

    for (int index = 0; index < expectedContent.Length; index++)
    {
        string content = GetTabText(
            (TabItem)tabs.Items[index]);
        int previousPosition = -1;
        bool ordered = true;

        foreach (string expected in expectedContent[index])
        {
            int position = content.IndexOf(
                expected,
                previousPosition + 1,
                StringComparison.Ordinal);
            if (position < 0)
            {
                ordered = false;
                break;
            }

            previousPosition = position;
        }

        Check(
            ordered,
            $"{expectedHeaders[index]} content and numbered order are exact");
    }

    Check(
        !GetTabText((TabItem)tabs.Items[3]).Contains(
            "Click Apply",
            StringComparison.Ordinal),
        "Restore Previous Values does not instruct a second Apply");

    const string footerReminderText =
        "For more detailed instructions, see the User Guide.";
    TextBlock footerReminder =
        (TextBlock?)firstWindow.FindName(
            "UserGuideFooterReminder")
        ?? throw new InvalidOperationException(
            "Quick Help footer reminder was not found.");
    Grid footer =
        (Grid?)firstWindow.FindName(
            "QuickHelpFooter")
        ?? throw new InvalidOperationException(
            "Quick Help footer was not found.");
    StackPanel footerButtons =
        (StackPanel?)firstWindow.FindName(
            "QuickHelpFooterButtons")
        ?? throw new InvalidOperationException(
            "Quick Help footer buttons were not found.");

    Check(
        tabs.Items.Cast<TabItem>().All(
            tab => !GetTabText(tab).Contains(
                "User Guide",
                StringComparison.Ordinal)),
        "No tab contains a repeated User Guide reminder");

    List<string> windowText = new();
    CollectText(
        firstWindow,
        windowText);
    Check(
        windowText.Count(
            text => string.Equals(
                text,
                footerReminderText,
                StringComparison.Ordinal)) == 1,
        "Exactly one common User Guide reminder is displayed");
    Check(
        footerReminder.Text == footerReminderText &&
        ReferenceEquals(footerReminder.Parent, footer) &&
        Grid.GetRow(footer) == 6 &&
        Grid.GetColumn(footerReminder) == 0 &&
        footerReminder.HorizontalAlignment ==
            HorizontalAlignment.Left &&
        footerReminder.VerticalAlignment ==
            VerticalAlignment.Center &&
        footerReminder.TextWrapping == TextWrapping.Wrap,
        "The approved reminder is left-aligned in the footer row");
    Check(
        ReferenceEquals(footerButtons.Parent, footer) &&
        Grid.GetColumn(footerButtons) == 1 &&
        footerButtons.HorizontalAlignment ==
            HorizontalAlignment.Right &&
        footerButtons.Children
            .OfType<Button>()
            .Select(button => button.Content?.ToString())
            .SequenceEqual(
                new[]
                {
                    "Open User Guide",
                    "Close"
                }),
        "Footer buttons remain right-aligned in the approved order");
    Check(
        FooterFits(
            footer,
            footerReminder,
            footerButtons),
        "Footer reminder and buttons do not overlap at the default size");

    firstWindow.Width = firstWindow.MinWidth;
    firstWindow.Height = firstWindow.MinHeight;
    DrainDispatcher();
    Check(
        FooterFits(
            footer,
            footerReminder,
            footerButtons) &&
        footerReminder.ActualHeight <=
            footerButtons.ActualHeight,
        "Footer remains on one balanced line without overlap at minimum size");

    firstWindow.WindowState = WindowState.Minimized;
    viewModel.ShowQuickHelpCommand.Execute(null);
    DrainDispatcher();
    Check(
        ReferenceEquals(
            firstWindow,
            application.Windows.OfType<QuickHelpWindow>().Single()) &&
        firstWindow.WindowState == WindowState.Normal,
        "Repeated open restores and focuses the existing instance");
    Check(
        application.Windows.OfType<QuickHelpWindow>().Count() == 1,
        "Repeated open retains exactly one window");

    Check(
        viewModel.Project == null &&
        viewModel.Status == initialStatus &&
        viewModel.UndoCommand.CanExecute(null) == initialUndo &&
        viewModel.RedoCommand.CanExecute(null) == initialRedo,
        "No-project Quick Help use is state-neutral");

    VerifyUserGuideBehavior(
        firstWindow,
        viewModel,
        messages);

    firstWindow.Close();
    DrainDispatcher();
    Check(
        !viewModel.IsQuickHelpWindowOpen &&
        !application.Windows.OfType<QuickHelpWindow>().Any(),
        "Close clears tracking and the visible instance");

    viewModel.ShowQuickHelpCommand.Execute(null);
    DrainDispatcher();
    QuickHelpWindow secondWindow =
        application.Windows.OfType<QuickHelpWindow>().Single();
    Check(
        !ReferenceEquals(firstWindow, secondWindow) &&
        viewModel.IsQuickHelpWindowOpen,
        "Reopen creates one fresh window");

    ProjectModel modifiedProject =
        CreateModifiedProject();
    viewModel.Project = modifiedProject;
    DrainDispatcher();

    string jsonBefore =
        modifiedProject.RootDocument.ToString();
    int stateCountBefore =
        modifiedProject.GameplayOperationStates.Count;
    int historicalStateCountBefore =
        modifiedProject.HistoricalGameplayOperationStates.Count;
    bool propertyModifiedBefore =
        modifiedProject.Sheets[0].Entries[0].Properties[0].IsModified;
    string projectStatusBefore =
        viewModel.Status;
    bool undoBefore =
        viewModel.UndoCommand.CanExecute(null);
    bool redoBefore =
        viewModel.RedoCommand.CanExecute(null);

    viewModel.ShowQuickHelpCommand.Execute(null);
    DrainDispatcher();
    Check(
        ReferenceEquals(
            secondWindow,
            application.Windows.OfType<QuickHelpWindow>().Single()) &&
        secondWindow.IsVisible,
        "Quick Help remains usable with a modified project");
    Check(
        modifiedProject.RootDocument.ToString() == jsonBefore &&
        modifiedProject.Sheets[0].Entries[0].Properties[0].IsModified ==
            propertyModifiedBefore &&
        modifiedProject.IsModified,
        "Quick Help does not mutate project data or modified state");
    Check(
        modifiedProject.GameplayOperationStates.Count == stateCountBefore &&
        modifiedProject.HistoricalGameplayOperationStates.Count ==
            historicalStateCountBefore,
        "Quick Help does not change gameplay-operation state");
    Check(
        viewModel.Status == projectStatusBefore &&
        viewModel.UndoCommand.CanExecute(null) == undoBefore &&
        viewModel.RedoCommand.CanExecute(null) == redoBefore,
        "Quick Help does not change status or edit history");
    Check(
        modifiedProject.UpdateCompatibilityReport == null,
        "Quick Help does not create a compatibility report");

    ProjectModel replacementProject = new();
    viewModel.Project = replacementProject;
    DrainDispatcher();
    Check(
        ReferenceEquals(
            secondWindow,
            application.Windows.OfType<QuickHelpWindow>().Single()) &&
        secondWindow.IsVisible,
        "Project replacement leaves Quick Help open and usable");

    Button closeButton =
        FindButton(secondWindow, "Close");
    Check(
        closeButton.IsCancel &&
        !closeButton.IsDefault,
        "Close supplies the Escape contract without a default Enter action");
    closeButton.RaiseEvent(
        new RoutedEventArgs(Button.ClickEvent));
    DrainDispatcher();
    Check(
        !viewModel.IsQuickHelpWindowOpen,
        "The in-window Close action clears lifecycle tracking");

    viewModel.Project = null;
    viewModel.ShowQuickHelpCommand.Execute(null);
    DrainDispatcher();
    QuickHelpWindow shutdownWindow =
        application.Windows.OfType<QuickHelpWindow>().Single();
    mainWindow.Close();
    DrainDispatcher();
    Check(
        !shutdownWindow.IsVisible &&
        !viewModel.IsQuickHelpWindowOpen &&
        !application.Windows.OfType<QuickHelpWindow>().Any(),
        "MainWindow shutdown closes and cleans the owned Quick Help window");

    application.Shutdown();
}

void VerifyUserGuideBehavior(
    QuickHelpWindow window,
    MainViewModel viewModel,
    TestMessageDialogService messages)
{
    string guidePath =
        Path.Combine(
            AppContext.BaseDirectory,
            "USER-GUIDE.pdf");
    byte[]? priorContents =
        File.Exists(guidePath)
            ? File.ReadAllBytes(guidePath)
            : null;

    try
    {
        if (File.Exists(guidePath))
        {
            File.Delete(guidePath);
        }

        int launchCount = 0;
        viewModel.UseUserGuideProcessStarterForTesting(
            _ => launchCount++);
        messages.Clear();
        FindButton(window, "Open User Guide").RaiseEvent(
            new RoutedEventArgs(Button.ClickEvent));
        Check(
            launchCount == 0 &&
            messages.LastErrorTitle == "Open User Guide" &&
            messages.LastErrorMessage ==
                "The User Guide could not be found." +
                Environment.NewLine + Environment.NewLine +
                "Make sure USER-GUIDE.pdf is in the Wartales Editor folder.",
            "Missing User Guide produces the approved controlled message");

        File.WriteAllText(
            guidePath,
            "Quick Help smoke PDF fixture");
        ProcessStartInfo? captured = null;
        viewModel.UseUserGuideProcessStarterForTesting(
            startInfo => captured = startInfo);
        messages.Clear();
        FindButton(window, "Open User Guide").RaiseEvent(
            new RoutedEventArgs(Button.ClickEvent));
        Check(
            captured != null &&
            string.Equals(
                captured.FileName,
                guidePath,
                StringComparison.Ordinal) &&
            string.Equals(
                Path.GetDirectoryName(captured.FileName),
                AppContext.BaseDirectory.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase),
            "User Guide resolves exactly from AppContext.BaseDirectory");
        Check(
            captured?.UseShellExecute == true,
            "User Guide reaches the Windows shell-launch seam with UseShellExecute true");
        Check(
            captured != null &&
            !captured.FileName.StartsWith("http:", StringComparison.OrdinalIgnoreCase) &&
            !captured.FileName.StartsWith("https:", StringComparison.OrdinalIgnoreCase),
            "User Guide launch has no URL or network fallback");

        viewModel.UseUserGuideProcessStarterForTesting(
            _ => throw new InvalidOperationException(
                "test launch failure"));
        messages.Clear();
        FindButton(window, "Open User Guide").RaiseEvent(
            new RoutedEventArgs(Button.ClickEvent));
        Check(
            messages.LastErrorTitle == "Open User Guide" &&
            messages.LastErrorMessage ==
                "The User Guide was found, but Windows could not open it." +
                Environment.NewLine + Environment.NewLine +
                "Make sure a PDF reader or default PDF application is available." +
                Environment.NewLine + Environment.NewLine +
                "Details: test launch failure",
            "Shell launch failure produces the approved controlled error");
    }
    finally
    {
        if (priorContents == null)
        {
            if (File.Exists(guidePath))
            {
                File.Delete(guidePath);
            }
        }
        else
        {
            File.WriteAllBytes(
                guidePath,
                priorContents);
        }
    }
}

ProjectModel CreateModifiedProject()
{
    JObject root = JObject.Parse(
        "{\"sheets\":[{\"name\":\"constant\",\"lines\":[{\"id\":\"QuickHelpState\",\"value\":1}]}]}");
    JObject sourceEntry =
        (JObject)root["sheets"]![0]!["lines"]![0]!;
    JProperty sourceProperty =
        sourceEntry.Property("value")!;
    PropertyModel property = new()
    {
        SheetName = "constant",
        Name = "value",
        PropertyPath = "value",
        SourceProperty = sourceProperty
    };
    property.InitializeValueFromSource();
    property.CaptureOriginalValue();

    EntryModel entry = new()
    {
        Id = "QuickHelpState",
        Name = "QuickHelpState",
        DisplayName = "QuickHelpState",
        SourceEntry = sourceEntry
    };
    entry.Properties.Add(property);

    SheetModel sheet = new()
    {
        Name = "constant",
        SourceSheet = (JObject)root["sheets"]![0]!
    };
    sheet.Entries.Add(entry);

    ProjectModel project = new()
    {
        RootDocument = root,
        OriginalJson = root.ToString()
    };
    project.Sheets.Add(sheet);

    property.Value = 2;
    return project;
}

string GetTabText(TabItem tab)
{
    List<string> lines = new();
    CollectText(
        tab.Content as DependencyObject,
        lines);
    return string.Join(
        Environment.NewLine,
        lines);
}

void CollectText(
    DependencyObject? root,
    ICollection<string> lines)
{
    if (root == null)
    {
        return;
    }

    if (root is TextBlock textBlock)
    {
        string text = new TextRange(
                textBlock.ContentStart,
                textBlock.ContentEnd)
            .Text
            .Trim();
        if (!string.IsNullOrEmpty(text))
        {
            lines.Add(text);
        }

        return;
    }

    foreach (object child in LogicalTreeHelper.GetChildren(root))
    {
        if (child is DependencyObject dependencyObject)
        {
            CollectText(
                dependencyObject,
                lines);
        }
    }
}

Button FindButton(
    DependencyObject root,
    string content)
{
    if (root is Button button &&
        string.Equals(
            button.Content?.ToString(),
            content,
            StringComparison.Ordinal))
    {
        return button;
    }

    int childCount =
        VisualTreeHelper.GetChildrenCount(root);
    for (int index = 0; index < childCount; index++)
    {
        DependencyObject child =
            VisualTreeHelper.GetChild(root, index);
        try
        {
            return FindButton(
                child,
                content);
        }
        catch (InvalidOperationException)
        {
        }
    }

    throw new InvalidOperationException(
        $"Button '{content}' was not found.");
}

bool FooterFits(
    Grid footer,
    FrameworkElement reminder,
    FrameworkElement buttons)
{
    Point reminderPosition =
        reminder.TranslatePoint(
            new Point(0, 0),
            footer);
    Point buttonPosition =
        buttons.TranslatePoint(
            new Point(0, 0),
            footer);

    return reminder.ActualWidth > 0 &&
        buttons.ActualWidth > 0 &&
        reminderPosition.X + reminder.ActualWidth <=
            buttonPosition.X + 0.01 &&
        buttonPosition.X + buttons.ActualWidth <=
            footer.ActualWidth + 0.01;
}

void DrainDispatcher()
{
    DispatcherFrame frame = new();
    Dispatcher.CurrentDispatcher.BeginInvoke(
        DispatcherPriority.ApplicationIdle,
        new Action(() => frame.Continue = false));
    Dispatcher.PushFrame(frame);
}

void Check(
    bool condition,
    string description)
{
    if (!condition)
    {
        throw new InvalidOperationException(
            $"FAILED: {description}");
    }

    checks++;
}

sealed class TestMessageDialogService :
    IMessageDialogService
{
    public string LastErrorMessage { get; private set; } =
        string.Empty;

    public string LastErrorTitle { get; private set; } =
        string.Empty;

    public void Clear()
    {
        LastErrorMessage = string.Empty;
        LastErrorTitle = string.Empty;
    }

    public void ShowInformation(
        string message,
        string title)
    {
    }

    public void ShowWarning(
        string message,
        string title)
    {
    }

    public void ShowError(
        string message,
        string title)
    {
        LastErrorMessage = message;
        LastErrorTitle = title;
    }

    public bool ShowConfirmation(
        string message,
        string title) =>
        false;

    public UnsavedChangesResult ShowUnsavedChanges(
        string message,
        string title) =>
        UnsavedChangesResult.Cancel;
}
