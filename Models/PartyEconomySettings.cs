using System;

namespace WartalesEditor.Models;

public sealed class PartyEconomySettings
{
    public int VolunteerPercentage { get; init; } = 10;
    public int MaximumValour { get; init; } = 5;
    public int RestoredValour { get; init; } = 2;
    public int SaddlebagCapacity { get; init; } = 10;
    public int PonyStartingCapacity { get; init; } = 55;

    public PartyEconomySettings DeepClone() => new()
    {
        VolunteerPercentage = VolunteerPercentage,
        MaximumValour = MaximumValour,
        RestoredValour = RestoredValour,
        SaddlebagCapacity = SaddlebagCapacity,
        PonyStartingCapacity = PonyStartingCapacity
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
                break;
            case ProgressionType.CarryingCapacity:
                ValidateRange(SaddlebagCapacity, 0, 1000, "Saddlebag Capacity Bonus");
                ValidateRange(PonyStartingCapacity, 0, 1000, "Pony Starting Capacity");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    private static void ValidateRange(int value, int minimum, int maximum, string name)
    {
        if (value < minimum || value > maximum)
            throw new ArgumentOutOfRangeException(name, $"{name} must be between {minimum} and {maximum}.");
    }
}
