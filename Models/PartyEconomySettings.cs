using System;

namespace WartalesEditor.Models;

public sealed class PartyEconomySettings
{
    public int VolunteerPercentage { get; init; } = 10;
    public int MaximumValour { get; init; } = 5;
    public int RestoredValour { get; init; } = 2;
    public int SaddlebagCapacity { get; init; } = 10;
    public int PonyStartingCapacity { get; init; } = 55;
    public int TentTier1Valour { get; init; } = 1;
    public int TentTier2Valour { get; init; } = 2;
    public int TentTier3Valour { get; init; } = 3;
    public int HitchingPostTier1Base { get; init; } = 10;
    public int HitchingPostTier2Base { get; init; } = 10;
    public int HitchingPostTier3Base { get; init; } = 10;
    public int HitchingPostTier1Trait { get; init; }
    public int HitchingPostTier2Trait { get; init; } = 5;
    public int HitchingPostTier3Trait { get; init; } = 10;

    public PartyEconomySettings DeepClone() => new()
    {
        VolunteerPercentage = VolunteerPercentage,
        MaximumValour = MaximumValour,
        RestoredValour = RestoredValour,
        SaddlebagCapacity = SaddlebagCapacity,
        PonyStartingCapacity = PonyStartingCapacity,
        TentTier1Valour = TentTier1Valour,
        TentTier2Valour = TentTier2Valour,
        TentTier3Valour = TentTier3Valour,
        HitchingPostTier1Base = HitchingPostTier1Base,
        HitchingPostTier2Base = HitchingPostTier2Base,
        HitchingPostTier3Base = HitchingPostTier3Base,
        HitchingPostTier1Trait = HitchingPostTier1Trait,
        HitchingPostTier2Trait = HitchingPostTier2Trait,
        HitchingPostTier3Trait = HitchingPostTier3Trait
    };

    public void Validate(ProgressionType type)
    {
        switch (type)
        {
            case ProgressionType.VolunteerWages:
                ValidateRange(VolunteerPercentage, 0, 100, "Volunteer Wage Reduction");
                break;
            case ProgressionType.ValourPoints:
                ValidateRange(MaximumValour, 1, 100, "Maximum Valour");
                ValidateRange(RestoredValour, 0, 100, "Valour Restored After Rest");
                ValidateTierProgression(
                    TentTier1Valour,
                    TentTier2Valour,
                    TentTier3Valour,
                    "Tent Valour");
                break;
            case ProgressionType.CarryingCapacity:
                ValidateRange(SaddlebagCapacity, 0, 1000, "Saddlebag Capacity Bonus");
                ValidateRange(PonyStartingCapacity, 0, 1000, "Pony Starting Capacity");
                ValidateTierProgression(
                    HitchingPostTier1Base,
                    HitchingPostTier2Base,
                    HitchingPostTier3Base,
                    "Hitching Post base capacity");
                ValidateTierProgression(
                    HitchingPostTier1Trait,
                    HitchingPostTier2Trait,
                    HitchingPostTier3Trait,
                    "Hitching Post trait capacity");
                if (HitchingPostTier1Trait != 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(HitchingPostTier1Trait),
                        "Tier 1 Hitching Posts do not provide the additional trait bonus.");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    private static void ValidateTierProgression(
        int tier1,
        int tier2,
        int tier3,
        string name)
    {
        ValidateRange(tier1, 0, 1000, $"{name} Tier 1");
        ValidateRange(tier2, 0, 1000, $"{name} Tier 2");
        ValidateRange(tier3, 0, 1000, $"{name} Tier 3");
        if (tier1 > tier2 || tier2 > tier3)
            throw new ArgumentOutOfRangeException(
                name,
                $"{name} must not decrease at higher tiers.");
    }

    private static void ValidateRange(int value, int minimum, int maximum, string name)
    {
        if (value < minimum || value > maximum)
            throw new ArgumentOutOfRangeException(name, $"{name} must be between {minimum} and {maximum}.");
    }
}
