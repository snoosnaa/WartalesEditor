using System;
using System.Collections.Generic;
using WartalesEditor.Helpers;
using WartalesEditor.Models;
using WartalesEditor.Services;

namespace WartalesEditor.ViewModels;

public sealed class PartyEconomyDialogViewModel : ObservableObject
{
    private readonly ProjectModel project;
    private readonly PartyEconomyService service;
    private int volunteerPercentage;
    private int maximumValour;
    private int restoredValour;
    private int saddlebagCapacity;
    private int ponyStartingCapacity;
    private int tentTier1Valour;
    private int tentTier2Valour;
    private int tentTier3Valour;
    private int hitchingPostTier1Base;
    private int hitchingPostTier2Base;
    private int hitchingPostTier3Base;
    private int hitchingPostTier1Trait;
    private int hitchingPostTier2Trait;
    private int hitchingPostTier3Trait;
    private string? selectedTentPreset;
    private string? selectedHitchingPostPreset;
    private bool customExpandedValuesConfirmed;
    private bool inputBindingValid = true;
    private string validationMessage = string.Empty;

    public PartyEconomyDialogViewModel(
        ProjectModel project,
        PartyEconomyService service,
        ProgressionType operationType)
    {
        this.project = project ?? throw new ArgumentNullException(nameof(project));
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        OperationType = operationType;
        RefreshFromProject();
    }

    public ProgressionType OperationType { get; }
    public bool IsVolunteer => OperationType == ProgressionType.VolunteerWages;
    public bool IsValour => OperationType == ProgressionType.ValourPoints;
    public bool IsCarrying => OperationType == ProgressionType.CarryingCapacity;
    public string Title => IsVolunteer ? "Volunteer Trait" : IsValour ? "Valour Points" : "Carrying Capacity";
    public GameplayApplyFeedbackViewModel ApplyFeedback { get; } = new();

    public int VolunteerPercentage { get => volunteerPercentage; set => SetValue(ref volunteerPercentage, value, nameof(VolunteerPercentage)); }
    public int MaximumValour { get => maximumValour; set => SetValue(ref maximumValour, value, nameof(MaximumValour)); }
    public int RestoredValour { get => restoredValour; set => SetValue(ref restoredValour, value, nameof(RestoredValour)); }
    public int SaddlebagCapacity { get => saddlebagCapacity; set => SetValue(ref saddlebagCapacity, value, nameof(SaddlebagCapacity)); }
    public int PonyStartingCapacity { get => ponyStartingCapacity; set => SetValue(ref ponyStartingCapacity, value, nameof(PonyStartingCapacity)); }
    public IReadOnlyList<string> TentPresets { get; } = new[] { "Vanilla", "Increased" };
    public IReadOnlyList<string> HitchingPostPresets { get; } = new[] { "Vanilla", "Increased" };

    public string? SelectedTentPreset
    {
        get => selectedTentPreset;
        set
        {
            if (!SetProperty(ref selectedTentPreset, value) || value == null) return;
            ApplyFeedback.Clear();
            (tentTier1Valour, tentTier2Valour, tentTier3Valour) =
                value == "Increased" ? (2, 3, 4) : (1, 2, 3);
            customExpandedValuesConfirmed = true;
            Validate();
            OnPropertyChanged(nameof(PreviewText));
        }
    }

    public string? SelectedHitchingPostPreset
    {
        get => selectedHitchingPostPreset;
        set
        {
            if (!SetProperty(ref selectedHitchingPostPreset, value) || value == null) return;
            ApplyFeedback.Clear();
            (hitchingPostTier1Base, hitchingPostTier2Base, hitchingPostTier3Base,
             hitchingPostTier1Trait, hitchingPostTier2Trait, hitchingPostTier3Trait) =
                value == "Increased"
                    ? (20, 40, 60, 0, 20, 30)
                    : (10, 10, 10, 0, 5, 10);
            customExpandedValuesConfirmed = true;
            Validate();
            OnPropertyChanged(nameof(PreviewText));
        }
    }

    public string ValidationMessage
    {
        get => validationMessage;
        private set
        {
            if (SetProperty(ref validationMessage, value))
                OnPropertyChanged(nameof(CanApply));
        }
    }

    public bool CanApply =>
        inputBindingValid &&
        string.IsNullOrWhiteSpace(ValidationMessage);

    public string PreviewText => OperationType switch
    {
        ProgressionType.VolunteerWages when VolunteerPercentage == 0 =>
            "The Volunteer trait provides no wage reduction.",
        ProgressionType.VolunteerWages when VolunteerPercentage == 100 =>
            "Volunteer companions require no Krowns during wage payments.",
        ProgressionType.VolunteerWages =>
            $"{VolunteerPercentage}% wage reduction for companions with Volunteer.",
        ProgressionType.ValourPoints =>
            $"Base maximum: {MaximumValour} Valour{Environment.NewLine}" +
            $"Restored after each rest: {RestoredValour} Valour{Environment.NewLine}" +
            $"Tent bonus by tier: {tentTier1Valour} / {tentTier2Valour} / {tentTier3Valour} maximum Valour",
        ProgressionType.CarryingCapacity =>
            $"Saddlebags add {SaddlebagCapacity} carrying capacity when equipped.{Environment.NewLine}" +
            $"Ponies begin with {PonyStartingCapacity} carrying capacity.{Environment.NewLine}" +
            $"Hitching Post base bonus by tier: {hitchingPostTier1Base} / {hitchingPostTier2Base} / {hitchingPostTier3Base}{Environment.NewLine}" +
            $"Additional Draught Pony bonus by tier: {hitchingPostTier1Trait} / {hitchingPostTier2Trait} / {hitchingPostTier3Trait}",
        _ => string.Empty
    };

    public PartyEconomySettings CreateSettings() => new()
    {
        VolunteerPercentage = VolunteerPercentage,
        MaximumValour = MaximumValour,
        RestoredValour = RestoredValour,
        SaddlebagCapacity = SaddlebagCapacity,
        PonyStartingCapacity = PonyStartingCapacity,
        TentTier1Valour = tentTier1Valour,
        TentTier2Valour = tentTier2Valour,
        TentTier3Valour = tentTier3Valour,
        HitchingPostTier1Base = hitchingPostTier1Base,
        HitchingPostTier2Base = hitchingPostTier2Base,
        HitchingPostTier3Base = hitchingPostTier3Base,
        HitchingPostTier1Trait = hitchingPostTier1Trait,
        HitchingPostTier2Trait = hitchingPostTier2Trait,
        HitchingPostTier3Trait = hitchingPostTier3Trait
    };

    public void ResetToGameDefaults()
    {
        ApplyFeedback.Clear();
        Assign(
            service.GetBaselineSettings(project, OperationType),
            true);
        Validate();
        NotifyAll();
    }

    public void SetNoWages()
    {
        if (!IsVolunteer) return;
        VolunteerPercentage = 100;
    }

    public void RefreshFromProject()
    {
        Assign(service.GetSettings(project, OperationType), false);
        Validate();
        NotifyAll();
    }

    public void SetInputBindingValid(bool value)
    {
        if (inputBindingValid == value) return;
        inputBindingValid = value;
        if (!value) ApplyFeedback.Clear();
        OnPropertyChanged(nameof(CanApply));
    }

    private void Assign(
        PartyEconomySettings settings,
        bool confirmCustomExpandedValues)
    {
        volunteerPercentage = settings.VolunteerPercentage;
        maximumValour = settings.MaximumValour;
        restoredValour = settings.RestoredValour;
        saddlebagCapacity = settings.SaddlebagCapacity;
        ponyStartingCapacity = settings.PonyStartingCapacity;
        tentTier1Valour = settings.TentTier1Valour;
        tentTier2Valour = settings.TentTier2Valour;
        tentTier3Valour = settings.TentTier3Valour;
        hitchingPostTier1Base = settings.HitchingPostTier1Base;
        hitchingPostTier2Base = settings.HitchingPostTier2Base;
        hitchingPostTier3Base = settings.HitchingPostTier3Base;
        hitchingPostTier1Trait = settings.HitchingPostTier1Trait;
        hitchingPostTier2Trait = settings.HitchingPostTier2Trait;
        hitchingPostTier3Trait = settings.HitchingPostTier3Trait;
        selectedTentPreset =
            (tentTier1Valour, tentTier2Valour, tentTier3Valour) switch
            {
                (1, 2, 3) => "Vanilla",
                (2, 3, 4) => "Increased",
                _ => null
            };
        selectedHitchingPostPreset =
            (hitchingPostTier1Base, hitchingPostTier2Base, hitchingPostTier3Base,
             hitchingPostTier1Trait, hitchingPostTier2Trait, hitchingPostTier3Trait) switch
            {
                (10, 10, 10, 0, 5, 10) => "Vanilla",
                (20, 40, 60, 0, 20, 30) => "Increased",
                _ => null
            };
        customExpandedValuesConfirmed = confirmCustomExpandedValues;
    }

    private void SetValue(ref int field, int value, string name)
    {
        if (!SetProperty(ref field, value, name)) return;
        ApplyFeedback.Clear();
        Validate();
        OnPropertyChanged(nameof(PreviewText));
    }

    private void Validate()
    {
        try
        {
            CreateSettings().Validate(OperationType);
            ValidationMessage = OperationType switch
            {
                ProgressionType.ValourPoints
                    when selectedTentPreset == null &&
                         !customExpandedValuesConfirmed =>
                    "Current Tent bonuses are custom. Choose Vanilla or Increased before applying.",
                ProgressionType.CarryingCapacity
                    when selectedHitchingPostPreset == null &&
                         !customExpandedValuesConfirmed =>
                    "Current Hitching Post bonuses are custom. Choose Vanilla or Increased before applying.",
                _ => string.Empty
            };
        }
        catch (Exception exception)
        {
            ValidationMessage = exception.Message;
        }
    }

    private void NotifyAll()
    {
        OnPropertyChanged(nameof(VolunteerPercentage));
        OnPropertyChanged(nameof(MaximumValour));
        OnPropertyChanged(nameof(RestoredValour));
        OnPropertyChanged(nameof(SaddlebagCapacity));
        OnPropertyChanged(nameof(PonyStartingCapacity));
        OnPropertyChanged(nameof(SelectedTentPreset));
        OnPropertyChanged(nameof(SelectedHitchingPostPreset));
        OnPropertyChanged(nameof(PreviewText));
        OnPropertyChanged(nameof(CanApply));
    }
}
