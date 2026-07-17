using System;
using WartalesEditor.Helpers;

namespace WartalesEditor.ViewModels;

public sealed class ProfileDetailsViewModel :
    ObservableObject
{
    private string profileName;

    private string description;

    private string author;

    private string profileVersion;

    public ProfileDetailsViewModel(
        string windowTitle,
        string confirmationButtonText,
        string profileName = "",
        string description = "",
        string author = "",
        string profileVersion = "1.0")
    {
        if (string.IsNullOrWhiteSpace(
                windowTitle))
        {
            throw new ArgumentException(
                "A window title is required.",
                nameof(windowTitle));
        }

        if (string.IsNullOrWhiteSpace(
                confirmationButtonText))
        {
            throw new ArgumentException(
                "Confirmation button text is required.",
                nameof(confirmationButtonText));
        }

        WindowTitle =
            windowTitle.Trim();

        ConfirmationButtonText =
            confirmationButtonText.Trim();

        this.profileName =
            profileName ?? string.Empty;

        this.description =
            description ?? string.Empty;

        this.author =
            author ?? string.Empty;

        this.profileVersion =
            string.IsNullOrWhiteSpace(
                profileVersion)
                ? "1.0"
                : profileVersion;
    }

    public string WindowTitle
    {
        get;
    }

    public string ConfirmationButtonText
    {
        get;
    }

    public string ProfileName
    {
        get => profileName;
        set
        {
            if (!SetProperty(
                    ref profileName,
                    value ?? string.Empty))
            {
                return;
            }

            OnPropertyChanged(
                nameof(CanConfirm));

            OnPropertyChanged(
                nameof(ProfileNameValidationMessage));
        }
    }

    public string Description
    {
        get => description;
        set => SetProperty(
            ref description,
            value ?? string.Empty);
    }

    public string Author
    {
        get => author;
        set => SetProperty(
            ref author,
            value ?? string.Empty);
    }

    public string ProfileVersion
    {
        get => profileVersion;
        set
        {
            if (!SetProperty(
                    ref profileVersion,
                    value ?? string.Empty))
            {
                return;
            }

            OnPropertyChanged(
                nameof(CanConfirm));

            OnPropertyChanged(
                nameof(ProfileVersionValidationMessage));
        }
    }

    public bool CanConfirm =>
        !string.IsNullOrWhiteSpace(
            ProfileName)
        &&
        !string.IsNullOrWhiteSpace(
            ProfileVersion);

    public string ProfileNameValidationMessage =>
        string.IsNullOrWhiteSpace(
            ProfileName)
            ? "A profile name is required."
            : string.Empty;

    public string ProfileVersionValidationMessage =>
        string.IsNullOrWhiteSpace(
            ProfileVersion)
            ? "A profile version is required."
            : string.Empty;

    public string NormalizedProfileName =>
        ProfileName.Trim();

    public string NormalizedDescription =>
        Description.Trim();

    public string NormalizedAuthor =>
        Author.Trim();

    public string NormalizedProfileVersion =>
        ProfileVersion.Trim();
}