using System;
using System.Linq;
using WartalesEditor.Helpers;
using WartalesEditor.Models;
using WartalesEditor.Services;

namespace WartalesEditor.ViewModels;

public sealed class ProgressionScalingDialogViewModel :
    ObservableObject
{
    private readonly ProjectModel project;

    private readonly ProgressionScalingService
        progressionScalingService;

    private readonly GameplayOperationStateService
        gameplayOperationStateService;

    private int characterPercentage = 100;

    private int professionPercentage = 100;

    private string characterPreviewText = string.Empty;

    private string professionPreviewText = string.Empty;

    private string characterValidationMessage = string.Empty;

    private string professionValidationMessage = string.Empty;

    private bool hasTrustedCharacterBaseline;

    private bool hasTrustedProfessionBaseline;

    public ProgressionScalingDialogViewModel(
        ProjectModel project,
        ProgressionScalingService progressionScalingService)
        : this(
            project,
            progressionScalingService,
            new GameplayOperationStateService())
    {
    }

    public ProgressionScalingDialogViewModel(
        ProjectModel project,
        ProgressionScalingService progressionScalingService,
        GameplayOperationStateService gameplayOperationStateService)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(progressionScalingService);
        ArgumentNullException.ThrowIfNull(gameplayOperationStateService);

        this.project = project;
        this.progressionScalingService = progressionScalingService;
        this.gameplayOperationStateService = gameplayOperationStateService;

        RefreshFromProject();
    }

    public string Title => "XP Progression";

    public int MinimumPercentage =>
        ProgressionScalingService.MinimumPercentage;

    public int MaximumPercentage =>
        ProgressionScalingService.MaximumPercentage;

    public int CharacterPercentage
    {
        get => characterPercentage;
        set
        {
            if (SetProperty(ref characterPercentage, value))
            {
                RefreshCharacterPreview();
            }
        }
    }

    public int ProfessionPercentage
    {
        get => professionPercentage;
        set
        {
            if (SetProperty(ref professionPercentage, value))
            {
                RefreshProfessionPreview();
            }
        }
    }

    public string CharacterPreviewText
    {
        get => characterPreviewText;
        private set => SetProperty(ref characterPreviewText, value);
    }

    public string ProfessionPreviewText
    {
        get => professionPreviewText;
        private set => SetProperty(ref professionPreviewText, value);
    }

    public string CharacterValidationMessage
    {
        get => characterValidationMessage;
        private set
        {
            if (SetProperty(ref characterValidationMessage, value))
            {
                OnPropertyChanged(nameof(CanApplyCharacter));
            }
        }
    }

    public string ProfessionValidationMessage
    {
        get => professionValidationMessage;
        private set
        {
            if (SetProperty(ref professionValidationMessage, value))
            {
                OnPropertyChanged(nameof(CanApplyProfession));
            }
        }
    }

    public bool HasTrustedCharacterBaseline
    {
        get => hasTrustedCharacterBaseline;
        private set
        {
            if (SetProperty(ref hasTrustedCharacterBaseline, value))
            {
                OnPropertyChanged(nameof(CanApplyCharacter));
                OnPropertyChanged(nameof(CanAdoptCharacter));
            }
        }
    }

    public bool HasTrustedProfessionBaseline
    {
        get => hasTrustedProfessionBaseline;
        private set
        {
            if (SetProperty(ref hasTrustedProfessionBaseline, value))
            {
                OnPropertyChanged(nameof(CanApplyProfession));
                OnPropertyChanged(nameof(CanAdoptProfession));
            }
        }
    }

    public bool CanApplyCharacter =>
        HasTrustedCharacterBaseline &&
        string.IsNullOrWhiteSpace(CharacterValidationMessage);

    public bool CanApplyProfession =>
        HasTrustedProfessionBaseline &&
        string.IsNullOrWhiteSpace(ProfessionValidationMessage);

    public bool CanAdoptCharacter =>
        !HasTrustedCharacterBaseline;

    public bool CanAdoptProfession =>
        !HasTrustedProfessionBaseline;

    public void RefreshFromProject()
    {
        gameplayOperationStateService.ValidateProjectStates(project);

        GameplayOperationStateModel? characterState =
            gameplayOperationStateService.FindState(
                project,
                ProgressionType.Character);

        GameplayOperationStateModel? professionState =
            gameplayOperationStateService.FindState(
                project,
                ProgressionType.Profession);

        HasTrustedCharacterBaseline =
            characterState?.IsCompatible == true;

        HasTrustedProfessionBaseline =
            professionState?.IsCompatible == true;

        SetProperty(
            ref characterPercentage,
            HasTrustedCharacterBaseline
                ? characterState!.AppliedPercentage
                : 100,
            nameof(CharacterPercentage));

        SetProperty(
            ref professionPercentage,
            HasTrustedProfessionBaseline
                ? professionState!.AppliedPercentage
                : 100,
            nameof(ProfessionPercentage));

        RefreshCharacterPreview();
        RefreshProfessionPreview();
    }

    private void RefreshCharacterPreview()
    {
        RefreshPreview(
            ProgressionType.Character,
            CharacterPercentage,
            preview => CharacterPreviewText = preview,
            validation => CharacterValidationMessage = validation);
    }

    private void RefreshProfessionPreview()
    {
        RefreshPreview(
            ProgressionType.Profession,
            ProfessionPercentage,
            preview => ProfessionPreviewText = preview,
            validation => ProfessionValidationMessage = validation);
    }

    private void RefreshPreview(
        ProgressionType progressionType,
        int percentage,
        Action<string> setPreview,
        Action<string> setValidation)
    {
        try
        {
            ProgressionScalingPreview preview =
                progressionScalingService.CreatePreview(
                    project,
                    progressionType,
                    percentage);

            string baseline = string.Join(
                ", ",
                preview.BaselineValues.Select(
                    value => value.ToString("N0")));

            string scaled = string.Join(
                ", ",
                preview.ScaledValues.Select(
                    value => value.ToString("N0")));

            setPreview(
                $"100%: {baseline}" +
                Environment.NewLine +
                $"At {percentage}%: {scaled}");

            setValidation(string.Empty);
        }
        catch (Exception exception)
        {
            setPreview(string.Empty);

            string? persistenceWarning =
                project.GameplayOperationStateWarnings
                    .FirstOrDefault();

            setValidation(
                string.IsNullOrWhiteSpace(persistenceWarning)
                    ? exception.Message
                    : persistenceWarning);
        }
    }
}
