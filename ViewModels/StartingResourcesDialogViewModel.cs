using System;
using System.Linq;
using WartalesEditor.Helpers;
using WartalesEditor.Models;
using WartalesEditor.Services;

namespace WartalesEditor.ViewModels;

public sealed class StartingResourcesDialogViewModel : ObservableObject
{
    private readonly ProjectModel project;
    private readonly GameplayOperationStateService stateService;
    private int krowns;
    private int bread = 10;
    private int apples = 5;
    private int ironOre;
    private int wood;
    private int cloth;
    private bool isInitialized;
    private bool inputBindingValid = true;
    private string validationMessage = string.Empty;

    public StartingResourcesDialogViewModel(
        ProjectModel project,
        GameplayOperationStateService stateService)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(stateService);
        this.project = project;
        this.stateService = stateService;
        RefreshFromProject();
    }

    public string Title => "Starting Resources";
    public GameplayApplyFeedbackViewModel ApplyFeedback { get; } = new();
    public int MaximumExtra => StartingResourcesSettings.MaximumExtra;

    public int Krowns { get => krowns; set => SetAmount(ref krowns, value, nameof(Krowns)); }
    public int Bread { get => bread; set => SetAmount(ref bread, value, nameof(Bread)); }
    public int Apples { get => apples; set => SetAmount(ref apples, value, nameof(Apples)); }
    public int IronOre { get => ironOre; set => SetAmount(ref ironOre, value, nameof(IronOre)); }
    public int Wood { get => wood; set => SetAmount(ref wood, value, nameof(Wood)); }
    public int Cloth { get => cloth; set => SetAmount(ref cloth, value, nameof(Cloth)); }

    public bool IsInitialized
    {
        get => isInitialized;
        private set
        {
            if (SetProperty(ref isInitialized, value))
            {
                OnPropertyChanged(nameof(NeedsInitialization));
                OnPropertyChanged(nameof(CanApply));
            }
        }
    }

    public bool NeedsInitialization => !IsInitialized;

    public string InitializationText =>
        "Before these extras can be adjusted, the editor needs to remember the current " +
        "starting supplies. This only needs to be done once for this Wartales file.";

    public string ValidationMessage
    {
        get => validationMessage;
        private set
        {
            if (SetProperty(ref validationMessage, value))
            {
                OnPropertyChanged(nameof(CanApply));
            }
        }
    }

    public bool CanApply => IsInitialized && inputBindingValid && string.IsNullOrWhiteSpace(ValidationMessage);

    public void SetInputBindingValid(bool value)
    {
        if (inputBindingValid == value) return;
        inputBindingValid = value;
        if (!value) ApplyFeedback.Clear();
        OnPropertyChanged(nameof(CanApply));
    }

    public string PreviewText
    {
        get
        {
            StartingResourcesSettings settings = CreateSettings();
            string[] values =
            {
                $"+{settings.Krowns:N0} Krowns",
                $"+{settings.Bread:N0} Bread",
                $"+{settings.Apples:N0} Apples",
                $"+{settings.IronOre:N0} Iron Ore",
                $"+{settings.Wood:N0} Wood",
                $"+{settings.Cloth:N0} Cloth",
                "Applied to every supported starting group where required"
            };
            return string.Join(Environment.NewLine, values);
        }
    }

    public StartingResourcesSettings CreateSettings() =>
        new()
        {
            Krowns = Krowns,
            Bread = Bread,
            Apples = Apples,
            IronOre = IronOre,
            Wood = Wood,
            Cloth = Cloth
        };

    public void AddToAllMaterials(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        IronOre = Math.Min(MaximumExtra, checked(IronOre + amount));
        Wood = Math.Min(MaximumExtra, checked(Wood + amount));
        Cloth = Math.Min(MaximumExtra, checked(Cloth + amount));
    }

    public void ClearExtras()
    {
        Krowns = 0;
        Bread = 0;
        Apples = 0;
        IronOre = 0;
        Wood = 0;
        Cloth = 0;
    }

    public void RefreshFromProject(bool useFirstUseDefaults = false)
    {
        stateService.ValidateProjectStates(project);
        GameplayOperationStateModel? state = stateService.FindState(
            project,
            ProgressionType.StartingResources);
        IsInitialized = state?.IsCompatible == true;

        if (IsInitialized && !useFirstUseDefaults)
        {
            StartingResourcesSettings settings = state!.StartingResources!;
            SetAmounts(settings);
            ValidationMessage = string.Empty;
        }
        else if (useFirstUseDefaults)
        {
            SetAmounts(new StartingResourcesSettings { Bread = 10, Apples = 5 });
            IsInitialized = true;
            ValidationMessage = string.Empty;
        }
        else if (state != null)
        {
            ValidationMessage = state.CompatibilityMessage;
        }
        else
        {
            ValidationMessage = project.GameplayOperationStateWarnings.FirstOrDefault() ?? string.Empty;
        }
        NotifyAmountsChanged();
    }

    private void SetAmounts(StartingResourcesSettings settings)
    {
        krowns = settings.Krowns;
        bread = settings.Bread;
        apples = settings.Apples;
        ironOre = settings.IronOre;
        wood = settings.Wood;
        cloth = settings.Cloth;
    }

    private void SetAmount(ref int field, int value, string propertyName)
    {
        if (SetProperty(ref field, value, propertyName))
        {
            ApplyFeedback.Clear();
            ValidateInputs();
            OnPropertyChanged(nameof(PreviewText));
        }
    }

    private void ValidateInputs()
    {
        try
        {
            CreateSettings().Validate();
            ValidationMessage = string.Empty;
        }
        catch (Exception exception)
        {
            ValidationMessage = exception.Message;
        }
    }

    private void NotifyAmountsChanged()
    {
        OnPropertyChanged(nameof(Krowns));
        OnPropertyChanged(nameof(Bread));
        OnPropertyChanged(nameof(Apples));
        OnPropertyChanged(nameof(IronOre));
        OnPropertyChanged(nameof(Wood));
        OnPropertyChanged(nameof(Cloth));
        OnPropertyChanged(nameof(PreviewText));
        OnPropertyChanged(nameof(CanApply));
    }
}
