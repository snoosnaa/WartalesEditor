using System;
using System.Collections.Generic;

namespace WartalesEditor.Models;

public enum PropertyKind
{
    Gameplay,
    Reference,
    Complex,
    Internal
}

public enum PropertyEditorType
{
    Text,
    Number,
    Boolean,
    Dropdown,
    Complex,
    ReadOnly
}

public class PropertyDefinition
{
    public string Name { get; init; } = "";

    public PropertyKind Kind { get; init; } =
        PropertyKind.Gameplay;

    public PropertyEditorType EditorType { get; init; } =
        PropertyEditorType.Text;

    public string Description { get; init; } = "";

    public IReadOnlyList<string> AllowedValues { get; init; } =
        Array.Empty<string>();

    public double? MinimumValue { get; init; }

    public double? MaximumValue { get; init; }

    public bool IsEditable =>
        EditorType != PropertyEditorType.ReadOnly &&
        EditorType != PropertyEditorType.Complex;
}