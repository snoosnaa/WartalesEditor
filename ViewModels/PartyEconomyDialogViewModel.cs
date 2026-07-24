using System;
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

    public int VolunteerPercentage { get => volunteerPercentage; set => SetValue(ref volunteerPercentage, value, nameof(VolunteerPercentage)); }
    public int MaximumValour { get => maximumValour; set => SetValue(ref maximumValour, value, nameof(MaximumValour)); }
    public int RestoredValour { get => restoredValour; set => SetValue(ref restoredValour, value, nameof(RestoredValour)); }
    public int SaddlebagCapacity { get => saddlebagCapacity; set => SetValue(ref saddlebagCapacity, value, nameof(SaddlebagCapacity)); }
    public int PonyStartingCapacity { get => ponyStartingCapacity; set => SetValue(ref ponyStartingCapacity, value, nameof(PonyStartingCapacity)); }

    public string ValidationMessage
    {
        get => validationMessage;
        private set
        {
            if (SetProperty(ref validationMessage, value))
                OnPropertyChanged(nameof(CanApply));
        }
    }

    public bool CanApply => inputBindingValid && string.IsNullOrWhiteSpace(ValidationMessage);

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
            $"Restored after each rest: {RestoredValour} Valour",
        ProgressionType.CarryingCapacity =>
            $"Saddlebags add {SaddlebagCapacity} carrying capacity when equipped.{Environment.NewLine}" +
            $"Ponies begin with {PonyStartingCapacity} carrying capacity.",
        _ => string.Empty
    };

    public PartyEconomySettings CreateSettings() => new()
    {
        VolunteerPercentage = VolunteerPercentage,
        MaximumValour = MaximumValour,
        RestoredValour = RestoredValour,
        SaddlebagCapacity = SaddlebagCapacity,
        PonyStartingCapacity = PonyStartingCapacity
    };

    public void ResetToGameDefaults()
    {
        Assign(service.GetBaselineSettings(project, OperationType));
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
        Assign(service.GetSettings(project, OperationType));
        Validate();
        NotifyAll();
    }

    public void SetInputBindingValid(bool value)
    {
        inputBindingValid = value;
        OnPropertyChanged(nameof(CanApply));
    }

    private void Assign(PartyEconomySettings settings)
    {
        volunteerPercentage = settings.VolunteerPercentage;
        maximumValour = settings.MaximumValour;
        restoredValour = settings.RestoredValour;
        saddlebagCapacity = settings.SaddlebagCapacity;
        ponyStartingCapacity = settings.PonyStartingCapacity;
    }

    private void SetValue(ref int field, int value, string name)
    {
        if (!SetProperty(ref field, value, name)) return;
        Validate();
        OnPropertyChanged(nameof(PreviewText));
    }

    private void Validate()
    {
        try
        {
            CreateSettings().Validate(OperationType);
            ValidationMessage = string.Empty;
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
        OnPropertyChanged(nameof(PreviewText));
        OnPropertyChanged(nameof(CanApply));
    }
}
