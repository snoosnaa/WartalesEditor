using System;

namespace WartalesEditor.Models;

public sealed class StartingResourcesSettings
{
    public const int MaximumExtra = 1_000_000;

    public int Krowns { get; init; }
    public int Bread { get; init; }
    public int Apples { get; init; }
    public int IronOre { get; init; }
    public int Wood { get; init; }
    public int Cloth { get; init; }

    public StartingResourcesSettings DeepClone() =>
        new()
        {
            Krowns = Krowns,
            Bread = Bread,
            Apples = Apples,
            IronOre = IronOre,
            Wood = Wood,
            Cloth = Cloth
        };

    public void Validate()
    {
        ValidateValue(Krowns, nameof(Krowns));
        ValidateValue(Bread, nameof(Bread));
        ValidateValue(Apples, nameof(Apples));
        ValidateValue(IronOre, nameof(IronOre));
        ValidateValue(Wood, nameof(Wood));
        ValidateValue(Cloth, nameof(Cloth));
    }

    private static void ValidateValue(int value, string name)
    {
        if (value < 0 || value > MaximumExtra)
        {
            throw new ArgumentOutOfRangeException(
                name,
                value,
                $"Extra amounts must be between 0 and {MaximumExtra:N0}.");
        }
    }
}
