using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public static class GameplayPresetCatalog
{
    private static readonly IReadOnlyDictionary<ProgressionType, GameplayPresetDefinition>
        definitions = CreateDefinitions().ToDictionary(x => x.OperationType);

    public static bool IsSupported(ProgressionType type) =>
        definitions.ContainsKey(type);

    public static GameplayPresetDefinition Get(ProgressionType type) =>
        definitions.TryGetValue(type, out GameplayPresetDefinition? definition)
            ? definition
            : throw new ArgumentOutOfRangeException(
                nameof(type), type, "The gameplay preset tool is not supported.");

    private static IReadOnlyList<GameplayPresetDefinition> CreateDefinitions() =>
        new[]
        {
            Definition(
                ProgressionType.DeliciousMealChance,
                "Delicious Meal Chance",
                "Changes the Delicious Meal chance for Tier 2 and Tier 3 Cooking Pots. Tier 1 does not provide this bonus.",
                new[]
                {
                    Array("item", "CookingPotT2", "props.bonuses", "bonus", "PerfectRecipe"),
                    Array("item", "CookingPotT3", "props.bonuses", "bonus", "PerfectRecipe")
                },
                Preset("Vanilla", "Vanilla", "T2 15%; T3 30%", "Restores the normal Delicious Meal chances.", 15, 30),
                Preset("Improved", "Improved", "T2 25%; T3 45%", "Improves the Delicious Meal chance at both supported tiers.", 25, 45),
                Preset("High", "High", "T2 35%; T3 55%", "Provides a high Delicious Meal chance at both supported tiers.", 35, 55),
                Preset("Guaranteed", "Guaranteed", "T2 50%; T3 100%", "Tier 2 succeeds half the time and Tier 3 is guaranteed.", 50, 100)),

            Definition(
                ProgressionType.ForgingAssistance,
                "Forging Assistance",
                "Gives you more time to react while the forge is in the perfect-heat state. The forging activity remains active.",
                new[]
                {
                    Scalar("constant", "ForgeDurationPerfectHeatMin", "value"),
                    Scalar("constant", "ForgeDurationPerfectHeatMax", "value")
                },
                Preset("Vanilla", "Vanilla", "Normal reaction window", "Uses the normal perfect-heat timing window.", 0.25, 0.25),
                Preset("Easier", "Easier", "2× reaction window", "Provides twice the normal perfect-heat reaction time.", 0.50, 0.50),
                Preset("Easy", "Easy", "3.2× reaction window", "Provides a generous perfect-heat reaction window.", 0.80, 0.80),
                Preset("VeryEasy", "Very Easy", "4.8× reaction window", "Provides the most forgiving supported reaction window.", 1.20, 1.20)),

            Definition(
                ProgressionType.MiningWoodcuttingTiming,
                "Mining & Woodcutting",
                "Slows the shared timing indicator for Mining and Woodcutting while keeping both activities playable.",
                new[]
                {
                    Scalar(
                        "constant", "MiningSpeedCircleMin", "value",
                        GameplayPresetValueSemantics.BaselineScaled),
                    Scalar(
                        "constant", "MiningSpeedCircleMax", "value",
                        GameplayPresetValueSemantics.BaselineScaled)
                },
                Preset("Vanilla", "Vanilla", "100% timing speed", "The timing indicator moves at normal speed.", 1.0, 1.4),
                Preset("Easier", "Easier", "80% timing speed", "The timing indicator moves at 80% of normal speed.", 0.8, 1.12),
                Preset("Easy", "Easy", "60% timing speed", "The timing indicator moves at 60% of normal speed, giving you more time to react.", 0.6, 0.84),
                Preset("VeryEasy", "Very Easy", "40% timing speed", "The timing indicator moves at 40% of normal speed.", 0.4, 0.56)),

            Definition(
                ProgressionType.FishingSpeed,
                "Fishing Speed",
                "Shortens the fishing control phase. The fishing minigame remains active.",
                new[] { Scalar("constant", "FishingDurationControl", "value") },
                Preset("Vanilla", "Vanilla", "Normal control phase", "Uses the normal fishing control duration.", 6),
                Preset("Faster", "Faster", "Shorter control phase", "Fishing control completes sooner.", 4),
                Preset("Fast", "Fast", "Much shorter control phase", "Fishing control completes quickly.", 2),
                Preset("VeryFast", "Very Fast", "Shortest supported control phase", "Fishing control completes very quickly while remaining playable.", 1)),

            Definition(
                ProgressionType.LockpickingTolerance,
                "Lockpicking",
                "Increases the smallest valid lockpicking zone, making harder locks more forgiving while keeping the minigame active.",
                new[]
                {
                    Scalar("constant", "LockpickMinRangeRatio", "value"),
                    Scalar("constant", "LockpickMaxRangeRatio", "value")
                },
                Preset("Vanilla", "Vanilla", "Normal minimum lock zone", "Uses the normal valid-zone range.", 0.025, 0.20),
                Preset("Easier", "Easier", "Larger minimum lock zone", "Difficult locks provide a more forgiving valid zone.", 0.05, 0.20),
                Preset("Easy", "Easy", "Generous minimum lock zone", "Difficult locks provide a generous valid zone.", 0.10, 0.20),
                Preset("VeryEasy", "Very Easy", "Most forgiving supported zone", "Difficult locks remain interactive with a much larger minimum valid zone.", 0.15, 0.20)),

            Definition(
                ProgressionType.NinePuzzleAssistance,
                "Nine Puzzle Assistance",
                "Starts the Nine Puzzle with fewer shuffle steps and more correctly placed tiles. You still complete the puzzle.",
                new[]
                {
                    Scalar("constant", "NinePuzzle_Start_MinShuffleMoves", "value"),
                    Scalar("constant", "NinePuzzle_Start_MaxWellPlaceTiles", "value")
                },
                Preset("Vanilla", "Vanilla", "12 shuffle steps; 4 placed tiles", "Uses the normal puzzle setup.", 12, 4),
                Preset("Easier", "Easier", "8 shuffle steps; 5 placed tiles", "Starts with fewer shuffled moves and one more tile correctly placed.", 8, 5),
                Preset("Easy", "Easy", "4 shuffle steps; 6 placed tiles", "Starts close to completion while preserving the puzzle.", 4, 6),
                Preset("VeryEasy", "Very Easy", "0 shuffle steps; up to 8 placed tiles", "Uses the easiest supported starting layout without an instant-win mode.", 0, 8)),

            Definition(
                ProgressionType.RunStaminaRecovery,
                "Run Stamina Recovery",
                "Recovers overworld running stamina faster in both normal and fully exhausted states.",
                new[]
                {
                    Scalar("constant", "RunStaminaRecovery", "value"),
                    Scalar("constant", "RunStaminaLowRecovery", "value")
                },
                Preset("Vanilla", "Vanilla", "Normal recovery", "Uses normal running-stamina recovery.", 1.2, 1.5),
                Preset("Faster", "Faster", "Faster recovery", "Both normal and exhausted recovery complete sooner.", 0.9, 1.2),
                Preset("Fast", "Fast", "Fast recovery", "Both normal and exhausted recovery complete much sooner.", 0.6, 0.9),
                Preset("VeryFast", "Very Fast", "Very fast recovery", "Uses the fastest supported recovery for both states.", 0.3, 0.6)),

            DefinitionWithNote(
                ProgressionType.BattleCameraZoom,
                "Battle Camera Zoom",
                "Changes maximum zoom distance during battles. It does not affect overworld travel.",
                "Visual note: At farther zoom distances, some units may appear blurry. They become clear again when you zoom back in. This is a visual effect only.",
                new[]
                {
                    Scalar(
                        "constant", "CameraMinDistance", "value",
                        GameplayPresetValueSemantics.PreserveBaseline),
                    Scalar("constant", "CameraMaxDistance", "value")
                },
                Preset("Vanilla", "Vanilla", "Normal battle zoom", "Keeps the battle camera between 30 and 40 distance.", 30, 40),
                Preset("Extended", "Extended", "Maximum distance 44", "Lets the battle camera zoom moderately farther out.", 30, 44),
                Preset("Far", "Far", "Maximum distance 48", "Lets the battle camera zoom far out.", 30, 48),
                Preset("VeryFar", "Very Far", "Maximum distance 50", "Uses the farthest supported battle zoom.", 30, 50)),

            Definition(
                ProgressionType.CampfireExpansion,
                "Campfire Expansion",
                "Makes every campfire tier physically larger and increases assignment capacity at higher tiers.",
                CampfireTargets(),
                Preset("Vanilla", "Vanilla", "4 × 4; capacity 4 / 4 / 4", "Restores normal campfire dimensions and capacity.", CampfireValues(4, 4, 4, 4)),
                Preset("Expanded", "Expanded", "6 × 6; capacity 4 / 8 / 12", "All tiers become 6 × 6. Tier 1 keeps 4 assignments; Tiers 2 and 3 support 8 and 12.", CampfireValues(6, 4, 8, 12))),

            Definition(
                ProgressionType.CookingPotFoodReduction,
                "Cooking Pot Food Reduction",
                "Changes daily troop food saved by an assigned cook. Cooking Pot assignment capacity is unchanged.",
                TierBonusTargets(new[] { "CookingPot", "CookingPotT2", "CookingPotT3" }, "tool.bonusesIfAssigned", "FoodReduction"),
                Preset("Vanilla", "Vanilla", "T1 2; T2 4; T3 6 food", "Restores normal food saved per tier.", 2, 4, 6),
                Preset("Improved", "Improved", "T1 3; T2 6; T3 9 food", "An assigned cook saves 3, 6, or 9 food by Cooking Pot tier.", 3, 6, 9),
                Preset("Strong", "Strong", "T1 4; T2 8; T3 12 food", "An assigned cook saves 4, 8, or 12 food by Cooking Pot tier.", 4, 8, 12),
                Preset("VeryStrong", "Very Strong", "T1 6; T2 12; T3 18 food", "An assigned cook saves 6, 12, or 18 food by Cooking Pot tier.", 6, 12, 18)),

            Definition(
                ProgressionType.WorkshopMaterials,
                "Workshop Materials",
                "Changes Raw Materials produced per rest by an assigned Tinkerer.",
                TierBonusTargets(new[] { "Workshop", "WorkshopT2", "WorkshopT3" }, "tool.bonusesIfAssigned", "RawMaterialOnRest"),
                Preset("Vanilla", "Vanilla", "T1 2; T2 2; T3 2 materials", "Restores normal Raw Materials production.", 2, 2, 2),
                Preset("Improved", "Improved", "T1 2; T2 3; T3 4 materials", "Produces 2, 3, or 4 Raw Materials per rest by Workshop tier.", 2, 3, 4),
                Preset("High", "High", "T1 3; T2 4; T3 5 materials", "Produces 3, 4, or 5 Raw Materials per rest by Workshop tier.", 3, 4, 5),
                Preset("VeryHigh", "Very High", "T1 4; T2 5; T3 6 materials", "Produces 4, 5, or 6 Raw Materials per rest by Workshop tier.", 4, 5, 6)),

            Definition(
                ProgressionType.VendorRefresh,
                "Vendor Refresh",
                "Speeds merchant inventory replenishment while preserving the differences between merchant categories.",
                new[]
                {
                    Scalar(
                        "constant", "MerchantRefillPerDaySlow", "value",
                        GameplayPresetValueSemantics.BaselineScaled),
                    Scalar(
                        "constant", "MerchantRefillPerDayNormal", "value",
                        GameplayPresetValueSemantics.BaselineScaled),
                    Scalar(
                        "constant", "MerchantRefillPerDayFast", "value",
                        GameplayPresetValueSemantics.BaselineScaled),
                    Scalar("constant", "MerchantFullRefillDays", "value")
                },
                Preset("Vanilla", "Vanilla", "1× rates; full refill about 15 days", "Uses normal merchant restocking rates.", 0.3, 1.0, 3.0, 15),
                Preset("Faster", "Faster", "2× rates; full refill about 10 days", "Merchant stock replenishes at twice the normal rates.", 0.6, 2.0, 6.0, 10),
                Preset("Fast", "Fast", "3× rates; full refill about 7 days", "Merchant stock replenishes at three times the normal rates.", 0.9, 3.0, 9.0, 7),
                Preset("VeryFast", "Very Fast", "5× rates; full refill about 3 days", "Merchant stock replenishes at five times the normal rates.", 1.5, 5.0, 15.0, 3)),

            Definition(
                ProgressionType.ResourceReplenishment,
                "Resource Replenishment",
                "Increase how quickly renewable world resources become available again.",
                new[]
                {
                    Scalar(
                        "constant", "GatherRefillSlow", "value",
                        GameplayPresetValueSemantics.BaselineScaled),
                    Scalar(
                        "constant", "GatherRefillNormal", "value",
                        GameplayPresetValueSemantics.BaselineScaled),
                    Scalar(
                        "constant", "GatherRefillFast", "value",
                        GameplayPresetValueSemantics.BaselineScaled)
                },
                Preset("Vanilla", "Vanilla", "1× replenishment", "Uses the resource replenishment speed captured from the current game data.", 0.15, 0.3, 1.0),
                Preset("Faster", "Faster", "2× replenishment", "Renewable world resources replenish at twice their normal rates.", 0.3, 0.6, 2.0),
                Preset("Fast", "Fast", "3× replenishment", "Renewable world resources replenish at three times their normal rates.", 0.45, 0.9, 3.0),
                Preset("VeryFast", "Very Fast", "5× replenishment", "Renewable world resources replenish at five times their normal rates.", 0.75, 1.5, 5.0)),

            Definition(
                ProgressionType.RubySapphireValue,
                "Ruby & Sapphire Value",
                "Changes the base economy value of Ruby and Sapphire. Other valuables are unchanged.",
                new[]
                {
                    Scalar("item", "Ruby", "price"),
                    Scalar("item", "Sapphire", "price")
                },
                Preset("Vanilla", "Vanilla", "Ruby 40; Sapphire 40", "Restores the normal base values.", 40, 40),
                Preset("Higher", "Higher", "Ruby 100; Sapphire 100", "Sets both gems to a base value of 100.", 100, 100),
                Preset("High", "High", "Ruby 150; Sapphire 150", "Sets both gems to a base value of 150.", 150, 150),
                Preset("VeryHigh", "Very High", "Ruby 200; Sapphire 200", "Sets both gems to a base value of 200.", 200, 200)),

            Definition(
                ProgressionType.TimeBetweenRests,
                "Time Between Rests",
                "Changes approximately how many in-game travel hours pass before fatigue requires another rest.",
                new[] { Scalar("constant", "TirednessAmountHours", "value") },
                Preset("Vanilla", "Vanilla", "About 24 travel hours", "Uses the normal time between rests.", 24),
                Preset("Longer", "Longer", "About 48 travel hours", "Approximately doubles travel time before another rest is required.", 48),
                Preset("Extended", "Extended", "About 72 travel hours", "Allows approximately 72 travel hours between rests.", 72),
                Preset("VeryLong", "Very Long", "About 96 travel hours", "Allows approximately 96 travel hours between rests.", 96))
        };

    private static GameplayPresetDefinition Definition(
        ProgressionType type,
        string title,
        string description,
        IReadOnlyList<GameplayTargetDefinition> targets,
        params GameplayPresetOption[] presets) =>
        new(type, title, description, null, targets, presets);

    private static GameplayPresetDefinition DefinitionWithNote(
        ProgressionType type,
        string title,
        string description,
        string informationalNote,
        IReadOnlyList<GameplayTargetDefinition> targets,
        params GameplayPresetOption[] presets) =>
        new(type, title, description, informationalNote, targets, presets);

    private static GameplayPresetOption Preset(
        string key,
        string name,
        string valueSummary,
        string description,
        params object[] values) =>
        new(key, name, valueSummary, description, new JArray(values));

    private static GameplayTargetDefinition Scalar(
        string sheet,
        string entry,
        string path,
        GameplayPresetValueSemantics valueSemantics =
            GameplayPresetValueSemantics.Absolute) =>
        new(sheet, entry, path, null, null, valueSemantics);

    private static GameplayTargetDefinition Array(
        string sheet,
        string entry,
        string path,
        string discriminator,
        string identity) =>
        new(
            sheet,
            entry,
            path,
            discriminator,
            identity,
            GameplayPresetValueSemantics.Absolute);

    private static IReadOnlyList<GameplayTargetDefinition> TierBonusTargets(
        IEnumerable<string> entries,
        string path,
        string bonus) =>
        entries.Select(entry => Array("item", entry, path, "bonus", bonus)).ToArray();

    private static IReadOnlyList<GameplayTargetDefinition> CampfireTargets()
    {
        List<GameplayTargetDefinition> targets = new();
        foreach (string entry in new[] { "Firecamp", "FirecampT2", "FirecampT3" })
        {
            foreach (string path in new[]
                     {
                         "tool.campWidth", "tool.width", "tool.campHeight",
                         "tool.height", "tool.toolCapacity", "tool.capacity"
                     })
                targets.Add(Scalar("item", entry, path));
        }
        return targets;
    }

    private static object[] CampfireValues(
        int dimension,
        int tier1Capacity,
        int tier2Capacity,
        int tier3Capacity)
    {
        List<object> values = new();
        foreach (int capacity in new[] { tier1Capacity, tier2Capacity, tier3Capacity })
        {
            values.AddRange(new object[]
            {
                dimension, dimension, dimension, dimension, capacity, capacity
            });
        }
        return values.ToArray();
    }
}

public sealed class GameplayPresetDefinition
{
    internal GameplayPresetDefinition(
        ProgressionType operationType,
        string title,
        string description,
        string? informationalNote,
        IReadOnlyList<GameplayTargetDefinition> targets,
        IReadOnlyList<GameplayPresetOption> presets)
    {
        OperationType = operationType;
        Title = title;
        Description = description;
        InformationalNote = informationalNote;
        Targets = targets;
        Presets = presets;
    }

    public ProgressionType OperationType { get; }
    public string Title { get; }
    public string Description { get; }
    public string? InformationalNote { get; }
    public bool HasInformationalNote =>
        !string.IsNullOrWhiteSpace(InformationalNote);
    public IReadOnlyList<GameplayPresetOption> Presets { get; }
    internal IReadOnlyList<GameplayTargetDefinition> Targets { get; }
}

internal sealed record GameplayTargetDefinition(
    string Sheet,
    string Entry,
    string Path,
    string? Discriminator,
    string? Identity,
    GameplayPresetValueSemantics ValueSemantics);

internal enum GameplayPresetValueSemantics
{
    Absolute,
    BaselineScaled,
    PreserveBaseline
}
