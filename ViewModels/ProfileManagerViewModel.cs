using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using WartalesEditor.Helpers;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Services;

namespace WartalesEditor.ViewModels;

public sealed class ProfileManagerViewModel :
    ObservableObject
{
    private const string ProfileFileFilter =
        "Wartales Profile (*.wtprofile)|*.wtprofile|" +
        "All Files (*.*)|*.*";

    private readonly ModProfileLibraryService
        profileLibraryService;

    private readonly IFileDialogService
        fileDialogService;

    private readonly IMessageDialogService
        messageDialogService;

    private readonly Func<
        ProfileDetailsViewModel,
        bool?> showProfileDetailsDialog;

    private ModProfileSummaryModel? selectedProfile;

    private bool canApplyToCurrentProject;

    private string status =
        "Ready";

    public event EventHandler<ProfileManagerRequestModel>?
        OperationRequested;

    public ProfileManagerViewModel(
        ModProfileLibraryService profileLibraryService,
        IFileDialogService fileDialogService,
        IMessageDialogService messageDialogService,
        Func<ProfileDetailsViewModel, bool?>
            showProfileDetailsDialog)
    {
        this.profileLibraryService =
            profileLibraryService
            ?? throw new ArgumentNullException(
                nameof(profileLibraryService));

        this.fileDialogService =
            fileDialogService
            ?? throw new ArgumentNullException(
                nameof(fileDialogService));

        this.messageDialogService =
            messageDialogService
            ?? throw new ArgumentNullException(
                nameof(messageDialogService));

        this.showProfileDetailsDialog =
            showProfileDetailsDialog
            ?? throw new ArgumentNullException(
                nameof(showProfileDetailsDialog));

        Profiles =
            new ObservableCollection<
                ModProfileSummaryModel>();

        RefreshCommand =
            new RelayCommand(
                _ => Refresh());

        CreateCommand =
            new RelayCommand(
                _ => CreateProfile(),
                _ => CanApplyToCurrentProject);

        RenameCommand =
            new RelayCommand(
                _ => RenameProfile(),
                _ => HasSelectedProfile);

        DuplicateCommand =
            new RelayCommand(
                _ => DuplicateProfile(),
                _ => HasSelectedProfile);

        ApplyCommand =
            new RelayCommand(
                _ => RaiseApplyRequested(),
                _ => CanApply);

        ImportCommand =
            new RelayCommand(
                _ => ImportProfile());

        ExportCommand =
            new RelayCommand(
                _ => ExportProfile(),
                _ => HasSelectedProfile);

        DeleteCommand =
            new RelayCommand(
                _ => DeleteProfile(),
                _ => HasSelectedProfile);

        Refresh();

        RefreshCommandStates();
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
                nameof(CanApply));

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

            RefreshCommandStates();
        }
    }

    public bool HasProfiles =>
        Profiles.Count > 0;

    public bool HasSelectedProfile =>
        SelectedProfile != null;

    public bool CanApplyToCurrentProject
    {
        get => canApplyToCurrentProject;
        set
        {
            if (!SetProperty(
                    ref canApplyToCurrentProject,
                    value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(CanApply));

            RefreshCommandStates();
        }
    }

    public bool CanApply =>
        HasSelectedProfile &&
        CanApplyToCurrentProject;

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
                $"Changes: {SelectedProfile.EffectiveChangeCount:N0}";
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

    public RelayCommand CreateCommand
    {
        get;
    }

    public RelayCommand RenameCommand
    {
        get;
    }

    public RelayCommand DuplicateCommand
    {
        get;
    }

    public RelayCommand ApplyCommand
    {
        get;
    }

    public RelayCommand ImportCommand
    {
        get;
    }

    public RelayCommand ExportCommand
    {
        get;
    }

    public RelayCommand DeleteCommand
    {
        get;
    }

    public void Refresh()
    {
        RefreshProfiles(
            SelectedProfile?.FilePath);
    }

    public void RefreshAndSelect(
        string? filePath)
    {
        RefreshProfiles(
            filePath);
    }

    private void RefreshProfiles(
        string? preferredFilePath)
    {
        try
        {
            var profiles =
                profileLibraryService
                    .GetProfiles();

            SelectedProfile =
                null;

            Profiles.Clear();

            foreach (ModProfileSummaryModel profile
                     in profiles)
            {
                Profiles.Add(profile);
            }

            SelectedProfile =
                FindProfileByPath(
                    preferredFilePath)
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

            RefreshCommandStates();
        }
        catch (Exception exception)
        {
            Profiles.Clear();
            SelectedProfile = null;

            OnPropertyChanged(nameof(Header));
            OnPropertyChanged(nameof(HasProfiles));

            Status =
                "Profile library refresh failed.";

            RefreshCommandStates();

            messageDialogService.ShowError(
                $"The profile library could not be refreshed." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                exception.Message,
                "Profile Manager");
        }
    }

    private void CreateProfile()
    {
        if (!CanApplyToCurrentProject)
        {
            return;
        }

        ProfileDetailsViewModel details =
            new(
                "Create Profile",
                "Create");

        if (showProfileDetailsDialog(
                details) != true)
        {
            return;
        }

        RaiseOperationRequested(
            new ProfileManagerRequestModel(
                ProfileManagerOperation.Create,
                profileName:
                    details.NormalizedProfileName,
                description:
                    details.NormalizedDescription,
                author:
                    details.NormalizedAuthor,
                profileVersion:
                    details.NormalizedProfileVersion));
    }

    private void RenameProfile()
    {
        ModProfileSummaryModel? profile =
            SelectedProfile;

        if (profile == null)
        {
            return;
        }

        ProfileDetailsViewModel details =
            new(
                "Rename Profile",
                "Rename",
                profile.Name,
                profile.Description,
                profile.Author,
                profile.ProfileVersion);

        if (showProfileDetailsDialog(
                details) != true)
        {
            return;
        }

        RaiseOperationRequested(
            new ProfileManagerRequestModel(
                ProfileManagerOperation.Rename,
                profile,
                details.NormalizedProfileName,
                details.NormalizedDescription,
                details.NormalizedAuthor,
                details.NormalizedProfileVersion));
    }

    private void DuplicateProfile()
    {
        ModProfileSummaryModel? profile =
            SelectedProfile;

        if (profile == null)
        {
            return;
        }

        ProfileDetailsViewModel details =
            new(
                "Duplicate Profile",
                "Duplicate",
                $"{profile.Name} Copy",
                profile.Description,
                profile.Author,
                profile.ProfileVersion);

        if (showProfileDetailsDialog(
                details) != true)
        {
            return;
        }

        RaiseOperationRequested(
            new ProfileManagerRequestModel(
                ProfileManagerOperation.Duplicate,
                profile,
                details.NormalizedProfileName,
                details.NormalizedDescription,
                details.NormalizedAuthor,
                details.NormalizedProfileVersion));
    }

    private void RaiseApplyRequested()
    {
        if (!CanApply)
        {
            return;
        }

        ModProfileSummaryModel? profile =
            SelectedProfile;

        if (profile == null)
        {
            return;
        }

        RaiseOperationRequested(
            new ProfileManagerRequestModel(
                ProfileManagerOperation.Apply,
                profile));
    }

    private void RaiseOperationRequested(
        ProfileManagerRequestModel request)
    {
        OperationRequested?.Invoke(
            this,
            request);
    }

    private void ImportProfile()
    {
        string? sourceFile =
            fileDialogService.ShowOpenFileDialog(
                ProfileFileFilter);

        if (string.IsNullOrWhiteSpace(
                sourceFile))
        {
            return;
        }

        try
        {
            ModProfileSummaryModel importedProfile =
                profileLibraryService.ImportProfile(
                    sourceFile);

            Refresh();

            SelectedProfile =
                FindProfileByPath(
                    importedProfile.FilePath)
                ?? FindProfileByFileName(
                    importedProfile.FileName)
                ?? SelectedProfile;

            Status =
                $"Imported profile: " +
                $"{importedProfile.Name}";

            messageDialogService.ShowInformation(
                $"The profile was imported successfully." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"Profile: {importedProfile.Name}" +
                $"{Environment.NewLine}" +
                $"File: {importedProfile.FileName}",
                "Import Profile");
        }
        catch (Exception exception)
        {
            Status =
                "Profile import failed.";

            messageDialogService.ShowError(
                $"The profile could not be imported." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                exception.Message,
                "Import Profile");
        }
    }

    private void ExportProfile()
    {
        ModProfileSummaryModel? profile =
            SelectedProfile;

        if (profile == null)
        {
            return;
        }

        string? destinationFile =
            fileDialogService.ShowSaveFileDialog(
                ProfileFileFilter,
                profile.FileName);

        if (string.IsNullOrWhiteSpace(
                destinationFile))
        {
            return;
        }

        try
        {
            profileLibraryService.ExportProfile(
                profile,
                destinationFile);

            Status =
                $"Exported profile: {profile.Name}";

            messageDialogService.ShowInformation(
                $"The profile was exported successfully." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"Profile: {profile.Name}" +
                $"{Environment.NewLine}" +
                $"File: {Path.GetFileName(destinationFile)}",
                "Export Profile");
        }
        catch (Exception exception)
        {
            Status =
                "Profile export failed.";

            messageDialogService.ShowError(
                $"The profile could not be exported." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                exception.Message,
                "Export Profile");
        }
    }

    private void DeleteProfile()
    {
        ModProfileSummaryModel? profile =
            SelectedProfile;

        if (profile == null)
        {
            return;
        }

        bool confirmed =
            messageDialogService.ShowConfirmation(
                $"Delete the selected profile?" +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"Profile: {profile.Name}" +
                $"{Environment.NewLine}" +
                $"File: {profile.FileName}" +
                $"{Environment.NewLine}{Environment.NewLine}" +
                "This action cannot be undone.",
                "Delete Profile");

        if (!confirmed)
        {
            return;
        }

        try
        {
            string deletedProfileName =
                profile.Name;

            string deletedFilePath =
                profile.FilePath;

            profileLibraryService.DeleteProfile(
                profile);

            RemoveProfileFromCollection(
                deletedFilePath);

            Status =
                $"Deleted profile: " +
                $"{deletedProfileName}";

            messageDialogService.ShowInformation(
                $"The profile was deleted successfully." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"Profile: {deletedProfileName}",
                "Delete Profile");
        }
        catch (Exception exception)
        {
            Status =
                "Profile deletion failed.";

            messageDialogService.ShowError(
                $"The profile could not be deleted." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                exception.Message,
                "Delete Profile");
        }
    }

    private void RemoveProfileFromCollection(
        string deletedFilePath)
    {
        ModProfileSummaryModel? profileToRemove =
            FindProfileByPath(
                deletedFilePath);

        if (profileToRemove != null)
        {
            Profiles.Remove(
                profileToRemove);
        }

        SelectedProfile =
            Profiles.FirstOrDefault();

        OnPropertyChanged(nameof(Header));
        OnPropertyChanged(nameof(HasProfiles));

        RefreshCommandStates();
    }

    private ModProfileSummaryModel?
        FindProfileByPath(
            string? filePath)
    {
        if (string.IsNullOrWhiteSpace(
                filePath))
        {
            return null;
        }

        return Profiles.FirstOrDefault(
            profile =>
                string.Equals(
                    profile.FilePath,
                    filePath,
                    StringComparison.OrdinalIgnoreCase));
    }

    private ModProfileSummaryModel?
        FindProfileByFileName(
            string? fileName)
    {
        if (string.IsNullOrWhiteSpace(
                fileName))
        {
            return null;
        }

        return Profiles.FirstOrDefault(
            profile =>
                string.Equals(
                    profile.FileName,
                    fileName,
                    StringComparison.OrdinalIgnoreCase));
    }

    private void RefreshCommandStates()
    {
        RefreshCommand.NotifyCanExecuteChanged();
        CreateCommand.NotifyCanExecuteChanged();
        RenameCommand.NotifyCanExecuteChanged();
        DuplicateCommand.NotifyCanExecuteChanged();
        ApplyCommand.NotifyCanExecuteChanged();
        ImportCommand.NotifyCanExecuteChanged();
        ExportCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
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
