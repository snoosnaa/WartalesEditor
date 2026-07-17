using System;

namespace WartalesEditor.Models.Profiles;

public sealed class ProfileManagerRequestModel
{
    public ProfileManagerRequestModel(
        ProfileManagerOperation operation,
        ModProfileSummaryModel? profile = null,
        string profileName = "",
        string description = "",
        string author = "",
        string profileVersion = "1.0")
    {
        Operation =
            operation;

        Profile =
            profile;

        ProfileName =
            profileName?.Trim()
            ?? string.Empty;

        Description =
            description?.Trim()
            ?? string.Empty;

        Author =
            author?.Trim()
            ?? string.Empty;

        ProfileVersion =
            profileVersion?.Trim()
            ?? string.Empty;

        Validate();
    }

    public ProfileManagerOperation Operation
    {
        get;
    }

    public ModProfileSummaryModel? Profile
    {
        get;
    }

    public string ProfileName
    {
        get;
    }

    public string Description
    {
        get;
    }

    public string Author
    {
        get;
    }

    public string ProfileVersion
    {
        get;
    }

    private void Validate()
    {
        switch (Operation)
        {
            case ProfileManagerOperation.Create:
                ValidateProfileDetails();
                break;

            case ProfileManagerOperation.Apply:
                ValidateSelectedProfile();
                break;

            case ProfileManagerOperation.Rename:
            case ProfileManagerOperation.Duplicate:
                ValidateSelectedProfile();
                ValidateProfileDetails();
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(Operation),
                    Operation,
                    "The profile manager operation is not supported.");
        }
    }

    private void ValidateSelectedProfile()
    {
        if (Profile == null)
        {
            throw new ArgumentException(
                $"The {Operation} operation requires " +
                "a selected profile.",
                nameof(Profile));
        }
    }

    private void ValidateProfileDetails()
    {
        if (string.IsNullOrWhiteSpace(
                ProfileName))
        {
            throw new ArgumentException(
                $"The {Operation} operation requires " +
                "a profile name.",
                nameof(ProfileName));
        }

        if (string.IsNullOrWhiteSpace(
                ProfileVersion))
        {
            throw new ArgumentException(
                $"The {Operation} operation requires " +
                "a profile version.",
                nameof(ProfileVersion));
        }
    }
}