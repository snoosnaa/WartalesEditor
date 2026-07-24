namespace WartalesEditor.Models;

public sealed record OverworldMovementPresetOption(
    OverworldMovementPreset Preset,
    string Name,
    int WalkSpeed,
    int RunSpeed);
