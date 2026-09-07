using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Profiles;

namespace WartalesEditor.Services;

public sealed class ProfileOperationIntentRegistry
{
    private readonly IReadOnlyDictionary<string, Registration>
        registrationsById;

    private readonly IReadOnlyDictionary<ProgressionType, Registration>
        registrationsByType;

    public ProfileOperationIntentRegistry()
    {
        Registration[] registrations = CreateRegistrations();

        registrationsById = registrations.ToDictionary(
            registration => registration.OperationId,
            StringComparer.Ordinal);

        registrationsByType = registrations
            .Where(registration => registration.OperationType.HasValue)
            .ToDictionary(
                registration => registration.OperationType!.Value);

        OperationIds = registrationsById.Keys
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<string> OperationIds { get; }

    public void ValidateRequest(
        ProfileOperationRequestModel request,
        int profileFormatVersion)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.FormatVersion !=
            ProfileOperationRequestModel.CurrentFormatVersion)
        {
            throw new InvalidOperationException(
                $"Gameplay-tool request '{request.OperationId}' uses " +
                $"unsupported format version '{request.FormatVersion}'.");
        }

        if (string.IsNullOrWhiteSpace(request.OperationId))
        {
            throw new InvalidOperationException(
                "A gameplay-tool request has no operation ID.");
        }

        if (!registrationsById.TryGetValue(
                request.OperationId,
                out Registration? registration))
        {
            throw new InvalidOperationException(
                $"The profile requests an unsupported gameplay tool " +
                $"'{request.OperationId}'.");
        }

        if (profileFormatVersion <
                ModProfileFormat.ProfileOperationIntentVersion &&
            !registration.IsLegacyExplicitRequest)
        {
            throw new InvalidOperationException(
                $"Gameplay-tool intent '{request.OperationId}' requires " +
                $"profile format version " +
                $"'{ModProfileFormat.ProfileOperationIntentVersion}'.");
        }

        registration.ValidateSettings(request.Settings);
    }

    public bool TryProjectLegacyIntent(
        GameplayOperationStateModel state,
        out ProfileOperationRequestModel? intent,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(state);
        intent = null;
        error = string.Empty;

        if (!registrationsByType.TryGetValue(
                state.OperationType,
                out Registration? registration) ||
            registration.ProjectState == null)
        {
            error =
                $"Gameplay state '{state.OperationType}' has no supported " +
                "profile-intent projection.";
            return false;
        }

        try
        {
            if (state.FormatVersion !=
                GameplayOperationStateModel.CurrentFormatVersion)
            {
                throw new InvalidOperationException(
                    $"Gameplay state '{state.OperationType}' uses " +
                    $"unsupported format version '{state.FormatVersion}'.");
            }

            JObject? settings = registration.ProjectState(state);
            if (settings == null)
            {
                return true;
            }
            ProfileOperationRequestModel candidate = new()
            {
                OperationId = registration.OperationId,
                Settings = settings
            };

            registration.ValidateSettings(candidate.Settings);
            intent = candidate;
            return true;
        }
        catch (Exception exception)
            when (exception is InvalidOperationException
                  or ArgumentException
                  or OverflowException
                  or FormatException)
        {
            error =
                $"Gameplay state '{state.OperationType}' cannot be " +
                $"projected as profile intent: {exception.Message}";
            return false;
        }
    }

    public string GetOperationId(ProgressionType operationType)
    {
        if (!registrationsByType.TryGetValue(
                operationType,
                out Registration? registration))
        {
            throw new InvalidOperationException(
                $"Gameplay state '{operationType}' has no registered " +
                "profile operation ID.");
        }

        return registration.OperationId;
    }

    public ProgressionType? GetOperationType(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        if (!registrationsById.TryGetValue(
                operationId,
                out Registration? registration))
        {
            throw new InvalidOperationException(
                $"Profile operation '{operationId}' is not registered.");
        }

        return registration.OperationType;
    }

    internal void ValidateStateMatchesRequest(
        GameplayOperationStateModel state,
        ProfileOperationRequestModel request)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(
            request,
            ModProfileFormat.ProfileOperationIntentVersion);

        if (!registrationsById.TryGetValue(
                request.OperationId,
                out Registration? registration) ||
            registration.OperationType != state.OperationType ||
            registration.ProjectState == null)
        {
            throw new InvalidOperationException(
                "The exact-source gameplay state does not belong to " +
                "the requested profile operation.");
        }

        JObject? stateSettings = registration.ProjectState(state);
        if (stateSettings == null ||
            !JToken.DeepEquals(stateSettings, request.Settings))
        {
            throw new InvalidOperationException(
                "The exact-source gameplay state settings do not match " +
                "the requested profile operation settings.");
        }
    }

    private static Registration[] CreateRegistrations()
    {
        List<Registration> registrations = new()
        {
            Percentage(
                ProfileOperationIds.CharacterXp,
                ProgressionType.Character,
                ProgressionScalingService.ValidatePercentage),
            Percentage(
                ProfileOperationIds.ProfessionXp,
                ProgressionType.Profession,
                ProgressionScalingService.ValidatePercentage),
            new(
                ProfileOperationIds.StartingResources,
                ProgressionType.StartingResources,
                ValidateStartingResources,
                ProjectStartingResources),
            Party(
                ProfileOperationIds.VolunteerWages,
                ProgressionType.VolunteerWages),
            Party(
                ProfileOperationIds.ValourPoints,
                ProgressionType.ValourPoints),
            Party(
                ProfileOperationIds.CarryingCapacity,
                ProgressionType.CarryingCapacity),
            new(
                ProfileOperationIds.OverworldMovementSpeed,
                ProgressionType.OverworldMovementSpeed,
                ValidateMovementPreset,
                ProjectPreset),
            new(
                ProfileOperationIds.RainFrequency,
                ProgressionType.RainFrequency,
                ValidateRainPreset,
                ProjectPreset),
            GenericPreset(
                ProfileOperationIds.DeliciousMealChance,
                ProgressionType.DeliciousMealChance),
            GenericPreset(
                ProfileOperationIds.ForgingAssistance,
                ProgressionType.ForgingAssistance),
            GenericPreset(
                ProfileOperationIds.MiningWoodcuttingTiming,
                ProgressionType.MiningWoodcuttingTiming),
            GenericPreset(
                ProfileOperationIds.FishingSpeed,
                ProgressionType.FishingSpeed),
            GenericPreset(
                ProfileOperationIds.LockpickingTolerance,
                ProgressionType.LockpickingTolerance),
            GenericPreset(
                ProfileOperationIds.NinePuzzleAssistance,
                ProgressionType.NinePuzzleAssistance),
            GenericPreset(
                ProfileOperationIds.RunStaminaRecovery,
                ProgressionType.RunStaminaRecovery),
            GenericPreset(
                ProfileOperationIds.BattleCameraZoom,
                ProgressionType.BattleCameraZoom),
            GenericPreset(
                ProfileOperationIds.CampfireExpansion,
                ProgressionType.CampfireExpansion),
            GenericPreset(
                ProfileOperationIds.CookingPotFoodReduction,
                ProgressionType.CookingPotFoodReduction),
            GenericPreset(
                ProfileOperationIds.WorkshopMaterials,
                ProgressionType.WorkshopMaterials),
            GenericPreset(
                ProfileOperationIds.VendorRefresh,
                ProgressionType.VendorRefresh),
            GenericPreset(
                ProfileOperationIds.RubySapphireValue,
                ProgressionType.RubySapphireValue),
            GenericPreset(
                ProfileOperationIds.TimeBetweenRests,
                ProgressionType.TimeBetweenRests),
            GenericPreset(
                ProfileOperationIds.ResourceReplenishment,
                ProgressionType.ResourceReplenishment),
            GenericPreset(
                ProfileOperationIds.LecternKnowledgeGain,
                ProgressionType.LecternKnowledgeGain),
            GenericPreset(
                ProfileOperationIds.PositiveRandomTraits,
                ProgressionType.PositiveRandomTraits),
            new(
                ProfileOperationIds.RandomTraitExclusions,
                ProgressionType.RandomTraitExclusions,
                ValidateRandomTraitSelections,
                ProjectRandomTraitSelections),
            Percentage(
                ProfileOperationIds.RequestBoardRewards,
                ProgressionType.RequestBoardRewards,
                RequestBoardRewardsService.ValidateProfilePercentage,
                isLegacyExplicitRequest: true),
            Parameterless(
                ProfileOperationIds.AddCampFacilities),
            Parameterless(
                ProfileOperationIds.UpgradeAllEquipment)
        };

        return registrations.ToArray();
    }

    private static Registration Percentage(
        string operationId,
        ProgressionType operationType,
        Action<int> validatePercentage,
        bool isLegacyExplicitRequest = false) =>
        new(
            operationId,
            operationType,
            settings =>
            {
                ValidateExactProperties(settings, "percentage");
                int percentage = RequiredInt(settings!, "percentage");
                validatePercentage(percentage);
            },
            state =>
            {
                int percentage = operationType ==
                    ProgressionType.RequestBoardRewards
                        ? RequiredInt(
                            state.GameplaySettings,
                            "percentage")
                        : state.AppliedPercentage;

                return operationType ==
                           ProgressionType.RequestBoardRewards &&
                       percentage == 100
                    ? null
                    : new JObject
                    {
                        ["percentage"] = percentage
                    };
            },
            IsLegacyExplicitRequest: isLegacyExplicitRequest);

    private static Registration Party(
        string operationId,
        ProgressionType operationType) =>
        new(
            operationId,
            operationType,
            settings => ValidatePartySettings(settings, operationType),
            state =>
            {
                JObject settings = state.GameplaySettings != null
                    ? (JObject)state.GameplaySettings.DeepClone()
                    : throw new InvalidOperationException(
                        "The saved Party Economy settings are missing.");
                ValidatePartySettings(settings, operationType);
                return settings;
            });

    private static Registration GenericPreset(
        string operationId,
        ProgressionType operationType) =>
        new(
            operationId,
            operationType,
            settings => ValidateGenericPreset(settings, operationType),
            ProjectPreset);

    private static Registration Parameterless(string operationId) =>
        new(
            operationId,
            null,
            ValidateParameterless,
            null,
            IsLegacyExplicitRequest: true);

    private static JObject ProjectStartingResources(
        GameplayOperationStateModel state)
    {
        StartingResourcesSettings settings = state.StartingResources
            ?? throw new InvalidOperationException(
                "The saved Starting Resources settings are missing.");

        settings.Validate();
        return new JObject
        {
            ["krowns"] = settings.Krowns,
            ["bread"] = settings.Bread,
            ["apples"] = settings.Apples,
            ["ironOre"] = settings.IronOre,
            ["wood"] = settings.Wood,
            ["cloth"] = settings.Cloth
        };
    }

    private static JObject? ProjectPreset(
        GameplayOperationStateModel state)
    {
        string preset = RequiredString(
            state.GameplaySettings,
            "preset");

        if ((state.OperationType is
                 ProgressionType.OverworldMovementSpeed or
                 ProgressionType.RainFrequency) &&
            string.Equals(
                preset,
                "PreviousValues",
                StringComparison.Ordinal))
        {
            return null;
        }

        return new JObject
        {
            ["preset"] = preset
        };
    }

    private static JObject ProjectRandomTraitSelections(
        GameplayOperationStateModel state)
    {
        if (state.BaselineArray.Count == 0)
        {
            throw new InvalidOperationException(
                "The saved random-trait candidate set is empty.");
        }

        if (state.GameplaySettings?["allowedTraitIds"] is not JArray allowed)
        {
            throw new InvalidOperationException(
                "The saved allowed-trait selection is missing.");
        }

        HashSet<string> allowedIds = ReadUniqueStrings(
            allowed,
            "allowedTraitIds");
        HashSet<string> candidateIds = new(StringComparer.Ordinal);
        JArray traits = new();

        foreach (JToken token in state.BaselineArray)
        {
            if (token is not JObject candidate)
            {
                throw new InvalidOperationException(
                    "A saved random-trait candidate is not an object.");
            }

            string id = RequiredString(candidate, "id");
            if (!candidateIds.Add(id))
            {
                throw new InvalidOperationException(
                    $"Random trait '{id}' appears more than once.");
            }

            int personalityOrdinal = RequiredInt(candidate, "personality");
            if (!Enum.IsDefined(
                    typeof(RandomTraitPersonality),
                    personalityOrdinal))
            {
                throw new InvalidOperationException(
                    $"Random trait '{id}' has an invalid personality.");
            }

            string group = RequiredString(candidate, "group");
            traits.Add(new JObject
            {
                ["id"] = id,
                ["personality"] =
                    ((RandomTraitPersonality)personalityOrdinal).ToString(),
                ["group"] = group,
                ["allowed"] = allowedIds.Contains(id)
            });
        }

        if (!allowedIds.IsSubsetOf(candidateIds))
        {
            throw new InvalidOperationException(
                "The saved allowed-trait selection references an unknown " +
                "candidate.");
        }

        return new JObject
        {
            ["traits"] = new JArray(
                traits.OfType<JObject>()
                    .OrderBy(
                        trait => trait.Value<string>("id"),
                        StringComparer.Ordinal))
        };
    }

    private static void ValidateStartingResources(JObject? settings)
    {
        ValidateExactProperties(
            settings,
            "krowns",
            "bread",
            "apples",
            "ironOre",
            "wood",
            "cloth");

        StartingResourcesSettings values = new()
        {
            Krowns = RequiredInt(settings!, "krowns"),
            Bread = RequiredInt(settings, "bread"),
            Apples = RequiredInt(settings, "apples"),
            IronOre = RequiredInt(settings, "ironOre"),
            Wood = RequiredInt(settings, "wood"),
            Cloth = RequiredInt(settings, "cloth")
        };
        values.Validate();
    }

    private static void ValidatePartySettings(
        JObject? settings,
        ProgressionType operationType)
    {
        PartyEconomySettings values;
        switch (operationType)
        {
            case ProgressionType.VolunteerWages:
                ValidateExactProperties(
                    settings,
                    "volunteerPercentage");
                values = new PartyEconomySettings
                {
                    VolunteerPercentage = RequiredInt(
                        settings!,
                        "volunteerPercentage")
                };
                break;

            case ProgressionType.ValourPoints:
                ValidateExactProperties(
                    settings,
                    "maximumValour",
                    "restoredValour",
                    "tentTier1Valour",
                    "tentTier2Valour",
                    "tentTier3Valour");
                values = new PartyEconomySettings
                {
                    MaximumValour = RequiredInt(settings!, "maximumValour"),
                    RestoredValour = RequiredInt(settings, "restoredValour"),
                    TentTier1Valour = RequiredInt(settings, "tentTier1Valour"),
                    TentTier2Valour = RequiredInt(settings, "tentTier2Valour"),
                    TentTier3Valour = RequiredInt(settings, "tentTier3Valour")
                };
                break;

            case ProgressionType.CarryingCapacity:
                ValidateExactProperties(
                    settings,
                    "saddlebagCapacity",
                    "ponyStartingCapacity",
                    "hitchingPostTier1Base",
                    "hitchingPostTier2Base",
                    "hitchingPostTier3Base",
                    "hitchingPostTier1Trait",
                    "hitchingPostTier2Trait",
                    "hitchingPostTier3Trait");
                values = new PartyEconomySettings
                {
                    SaddlebagCapacity = RequiredInt(settings!, "saddlebagCapacity"),
                    PonyStartingCapacity = RequiredInt(settings, "ponyStartingCapacity"),
                    HitchingPostTier1Base = RequiredInt(settings, "hitchingPostTier1Base"),
                    HitchingPostTier2Base = RequiredInt(settings, "hitchingPostTier2Base"),
                    HitchingPostTier3Base = RequiredInt(settings, "hitchingPostTier3Base"),
                    HitchingPostTier1Trait = RequiredInt(settings, "hitchingPostTier1Trait"),
                    HitchingPostTier2Trait = RequiredInt(settings, "hitchingPostTier2Trait"),
                    HitchingPostTier3Trait = RequiredInt(settings, "hitchingPostTier3Trait")
                };
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(operationType));
        }

        values.Validate(operationType);
    }

    private static void ValidateMovementPreset(JObject? settings)
    {
        string preset = ValidatePresetShape(settings);
        if (!Enum.TryParse(
                preset,
                ignoreCase: false,
                out OverworldMovementPreset parsed) ||
            parsed is OverworldMovementPreset.Custom or
                OverworldMovementPreset.Unavailable ||
            !OverworldMovementSpeedService.Presets.Any(option =>
                option.Preset == parsed))
        {
            throw new InvalidOperationException(
                $"Movement preset '{preset}' is not supported.");
        }
    }

    private static void ValidateRainPreset(JObject? settings)
    {
        string preset = ValidatePresetShape(settings);
        if (!Enum.TryParse(
                preset,
                ignoreCase: false,
                out RainFrequencyPreset parsed) ||
            parsed is RainFrequencyPreset.Custom or
                RainFrequencyPreset.Unavailable ||
            !RainFrequencyService.Presets.Any(option =>
                option.Preset == parsed))
        {
            throw new InvalidOperationException(
                $"Rain preset '{preset}' is not supported.");
        }
    }

    private static void ValidateGenericPreset(
        JObject? settings,
        ProgressionType operationType)
    {
        string preset = ValidatePresetShape(settings);
        if (!GameplayPresetCatalog.Get(operationType).Presets.Any(option =>
                string.Equals(
                    option.Key,
                    preset,
                    StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Preset '{preset}' is not supported for " +
                $"'{operationType}'.");
        }
    }

    private static string ValidatePresetShape(JObject? settings)
    {
        ValidateExactProperties(settings, "preset");
        return RequiredString(settings!, "preset");
    }

    private static void ValidateRandomTraitSelections(JObject? settings)
    {
        ValidateExactProperties(settings, "traits");
        if (settings!["traits"] is not JArray traits || traits.Count == 0)
        {
            throw new InvalidOperationException(
                "Random Trait Exclusions requires semantic trait selections.");
        }

        HashSet<string> ids = new(StringComparer.Ordinal);
        string? previousId = null;
        foreach (JToken token in traits)
        {
            if (token is not JObject trait)
            {
                throw new InvalidOperationException(
                    "A random-trait selection is not an object.");
            }

            ValidateExactProperties(
                trait,
                "id",
                "personality",
                "group",
                "allowed");
            string id = RequiredString(trait, "id");
            string personality = RequiredString(trait, "personality");
            _ = RequiredString(trait, "group");
            if (trait["allowed"]?.Type != JTokenType.Boolean)
            {
                throw new InvalidOperationException(
                    $"Random trait '{id}' has no valid allowed state.");
            }

            if (!Enum.TryParse(
                    personality,
                    ignoreCase: false,
                    out RandomTraitPersonality parsed) ||
                !Enum.IsDefined(parsed))
            {
                throw new InvalidOperationException(
                    $"Random trait '{id}' has an invalid personality.");
            }

            if (!ids.Add(id))
            {
                throw new InvalidOperationException(
                    $"Random trait '{id}' appears more than once.");
            }

            if (previousId != null &&
                StringComparer.Ordinal.Compare(previousId, id) >= 0)
            {
                throw new InvalidOperationException(
                    "Random-trait selections must be ordered by stable ID.");
            }

            previousId = id;
        }
    }

    private static void ValidateParameterless(JObject? settings)
    {
        if (settings != null && settings.HasValues)
        {
            throw new InvalidOperationException(
                "This gameplay-tool request does not support settings.");
        }
    }

    private static void ValidateExactProperties(
        JObject? value,
        params string[] propertyNames)
    {
        if (value == null)
        {
            throw new InvalidOperationException(
                "The gameplay-tool request settings are missing.");
        }

        HashSet<string> expected = propertyNames.ToHashSet(
            StringComparer.Ordinal);
        HashSet<string> actual = value.Properties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        if (!actual.SetEquals(expected))
        {
            throw new InvalidOperationException(
                "The gameplay-tool request contains missing or " +
                "unsupported settings.");
        }
    }

    private static int RequiredInt(JObject? value, string propertyName)
    {
        if (value?[propertyName]?.Type != JTokenType.Integer)
        {
            throw new InvalidOperationException(
                $"Setting '{propertyName}' must be an integer.");
        }

        return value[propertyName]!.Value<int>();
    }

    private static string RequiredString(
        JObject? value,
        string propertyName)
    {
        if (value?[propertyName]?.Type != JTokenType.String)
        {
            throw new InvalidOperationException(
                $"Setting '{propertyName}' must be text.");
        }

        string result = value[propertyName]!.Value<string>() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(result))
        {
            throw new InvalidOperationException(
                $"Setting '{propertyName}' cannot be empty.");
        }

        return result;
    }

    private static HashSet<string> ReadUniqueStrings(
        JArray values,
        string propertyName)
    {
        HashSet<string> result = new(StringComparer.Ordinal);
        foreach (JToken value in values)
        {
            if (value.Type != JTokenType.String ||
                string.IsNullOrWhiteSpace(value.Value<string>()) ||
                !result.Add(value.Value<string>()!))
            {
                throw new InvalidOperationException(
                    $"Setting '{propertyName}' contains an invalid or " +
                    "duplicate ID.");
            }
        }

        return result;
    }

    private sealed record Registration(
        string OperationId,
        ProgressionType? OperationType,
        Action<JObject?> ValidateSettings,
        Func<GameplayOperationStateModel, JObject?>? ProjectState,
        bool IsLegacyExplicitRequest = false);
}
