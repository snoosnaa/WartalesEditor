using WartalesEditor.Helpers;
using WartalesEditor.Models;

namespace WartalesEditor.ViewModels;

public sealed class QuickBmsExportProgressViewModel :
    ObservableObject
{
    private QuickBmsExportStage stage;

    public event EventHandler? CancellationRequested;

    public QuickBmsExportStage Stage
    {
        get => stage;
        private set
        {
            if (!SetProperty(ref stage, value))
                return;

            OnPropertyChanged(nameof(StageText));
            OnPropertyChanged(nameof(CanCancel));
        }
    }

    public string StageText => Stage switch
    {
        QuickBmsExportStage.Preparing =>
            "Preparing your saved Wartales data...",
        QuickBmsExportStage.Exporting =>
            "Updating the Wartales game package...",
        QuickBmsExportStage.Verifying =>
            "Verifying the exported data...",
        _ => "Export completed."
    };

    public bool CanCancel =>
        Stage == QuickBmsExportStage.Preparing;

    public void SetStage(QuickBmsExportStage value)
    {
        Stage = value;
    }

    public void RequestCancellation()
    {
        if (CanCancel)
            CancellationRequested?.Invoke(this, EventArgs.Empty);
    }
}
