namespace WartalesEditor.Models;

public sealed record RainFrequencyPresetOption(
    RainFrequencyPreset Preset,
    string Name,
    decimal Multiplier,
    string Preview);
