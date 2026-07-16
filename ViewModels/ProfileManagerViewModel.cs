using System;
using System.Collections.ObjectModel;
using System.Linq;
using WartalesEditor.Helpers;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Services;

namespace WartalesEditor.ViewModels;

public sealed class ProfileManagerViewModel :
    ObservableObject
{
    private readonly ModProfileLibraryService
        profileLibraryService;

    private readonly IMessageDialogService
        messageDialogService;

    private ModProfileSummaryModel? selectedProfile;

    private string status =
        "Ready";

    public ProfileManagerViewModel(
        ModProfileLibraryService profileLibraryService,
        IMessageDialogService messageDialogService)
    {
        this.profileLibraryService =
            profileLibraryService
            ?? throw new ArgumentNullException(
                nameof(profileLibraryService));

        this.messageDialogService =
            messageDialogService
            ?? throw new ArgumentNullException(
                nameof(messageDialogService));

        Profiles =
            new ObservableCollection<
                ModProfileSummaryModel>();

        RefreshCommand =
            new RelayCommand(
                _ => Refresh());

        Refresh();
    }

    public ObservableCollection<ModProfileSummaryModel>
        Profiles
    {
        get;
    }

    public ModProfileSummaryModel? SelectedProfile
    {
        get => selectedProfile;
        set
        {
            if (!SetProperty(
                    ref selectedProfile,
                    value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(HasSelectedProfile));

            OnPropertyChanged(
                nameof(SelectedProfileHeader));

            OnPropertyChanged(
                nameof(SelectedProfileAuthor));

            OnPropertyChanged(
                nameof(SelectedProfileVersion));

            OnPropertyChanged(
                nameof(SelectedProfileDescription));

            OnPropertyChanged(
                nameof(SelectedProfileCreatedAt));

            OnPropertyChanged(
                nameof(SelectedProfileModifiedAt));

            OnPropertyChanged(
                nameof(SelectedProfileChangeSummary));

            OnPropertyChanged(
                nameof(SelectedProfileFileName));

            OnPropertyChanged(
                nameof(SelectedProfileTags));
        }
    }

    public bool HasProfiles =>
        Profiles.Count > 0;

    public bool HasSelectedProfile =>
        SelectedProfile != null;

    public string Header =>
        Profiles.Count == 1
            ? "1 Mod Profile"
            : $"{Profiles.Count:N0} Mod Profiles";

    public string Status
    {
        get => status;
        private set => SetProperty(
            ref status,
            value);
    }

    public string SelectedProfileHeader =>
        SelectedProfile?.Name
        ?? "No profile selected";

    public string SelectedProfileAuthor =>
        string.IsNullOrWhiteSpace(
            SelectedProfile?.Author)
            ? "Not specified"
            : SelectedProfile.Author;

    public string SelectedProfileVersion =>
        string.IsNullOrWhiteSpace(
            SelectedProfile?.ProfileVersion)
            ? "Not specified"
            : SelectedProfile.ProfileVersion;

    public string SelectedProfileDescription =>
        string.IsNullOrWhiteSpace(
            SelectedProfile?.Description)
            ? "No description has been provided."
            : SelectedProfile.Description;

    public string SelectedProfileCreatedAt =>
        SelectedProfile == null
            ? string.Empty
            : SelectedProfile.CreatedAtUtc
                .ToLocalTime()
                .ToString("g");

    public string SelectedProfileModifiedAt =>
        SelectedProfile == null
            ? string.Empty
            : SelectedProfile.ModifiedAtUtc
                .ToLocalTime()
                .ToString("g");

    public string SelectedProfileChangeSummary
    {
        get
        {
            if (SelectedProfile == null)
            {
                return string.Empty;
            }

            return
                $"{SelectedProfile.CategoryCount:N0} " +
                $"{GetSingularOrPlural(
                    SelectedProfile.CategoryCount,
                    "category",
                    "categories")}, " +
                $"{SelectedProfile.SettingCount:N0} " +
                $"{GetSingularOrPlural(
                    SelectedProfile.SettingCount,
                    "setting",
                    "settings")}, " +
                $"{SelectedProfile.PropertyCount:N0} " +
                $"{GetSingularOrPlural(
                    SelectedProfile.PropertyCount,
                    "property",
                    "properties")}";
        }
    }

    public string SelectedProfileFileName =>
        SelectedProfile?.FileName
        ?? string.Empty;

    public string SelectedProfileTags
    {
        get
        {
            if (SelectedProfile == null ||
                SelectedProfile.Tags.Count == 0)
            {
                return "None";
            }

            return string.Join(
                ", ",
                SelectedProfile.Tags);
        }
    }

    public RelayCommand RefreshCommand
    {
        get;
    }

    public void Refresh()
    {
        ModProfileSummaryModel?
            previousSelection =
                SelectedProfile;

        try
        {
            var profiles =
                profileLibraryService
                    .GetProfiles();

            Profiles.Clear();

            foreach (ModProfileSummaryModel profile
                     in profiles)
            {
                Profiles.Add(profile);
            }

            SelectedProfile =
                FindMatchingProfile(
                    previousSelection)
                ?? Profiles.FirstOrDefault();

            OnPropertyChanged(nameof(Header));
            OnPropertyChanged(nameof(HasProfiles));

            Status =
                Profiles.Count == 0
                    ? "No profiles were found in the profile library."
                    : $"Loaded {Profiles.Count:N0} " +
                      $"{GetSingularOrPlural(
                          Profiles.Count,
                          "profile",
                          "profiles")}.";
        }
        catch (Exception exception)
        {
            Profiles.Clear();
            SelectedProfile = null;

            OnPropertyChanged(nameof(Header));
            OnPropertyChanged(nameof(HasProfiles));

            Status =
                "Profile library refresh failed.";

            messageDialogService.ShowError(
                $"The profile library could not be refreshed." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                exception.Message,
                "Profile Manager");
        }
    }

    private ModProfileSummaryModel?
        FindMatchingProfile(
            ModProfileSummaryModel? previousSelection)
    {
        if (previousSelection == null)
        {
            return null;
        }

        return Profiles.FirstOrDefault(
            profile =>
                string.Equals(
                    profile.FilePath,
                    previousSelection.FilePath,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static string GetSingularOrPlural(
        int count,
        string singular,
        string plural)
    {
        return count == 1
            ? singular
            : plural;
    }
}
