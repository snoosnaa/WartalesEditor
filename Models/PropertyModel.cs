using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WartalesEditor.Helpers;
using WartalesEditor.Services;

namespace WartalesEditor.Models;

public class PropertyModel : ObservableObject
{
    private static readonly PropertyDefinitionService definitionService =
        new();

    private static readonly ReferenceDataService referenceDataService =
        ReferenceDataService.Instance;

    private object? value;

    private JToken? originalValue;

    private bool isModified;

    private bool isStructurallyAdded;

    public event EventHandler? ModifiedChanged;

    public event EventHandler<PropertyValueChangedEventArgs>?
        ValueChanged;

    public string SheetName { get; set; } =
        string.Empty;

    public string Name { get; set; } =
        string.Empty;

    public string PropertyPath { get; set; } =
        string.Empty;

    public string EffectivePropertyPath =>
        string.IsNullOrWhiteSpace(PropertyPath)
            ? Name
            : PropertyPath;

    public JProperty? SourceProperty { get; set; }

    public PropertyDefinition Definition =>
        definitionService.GetDefinition(Name);

    public PropertyKind Kind =>
        Definition.Kind;

    public PropertyEditorType EditorType
    {
        get
        {
            if (Definition.EditorType ==
                PropertyEditorType.Dropdown)
            {
                return referenceDataService.HasValues(
                    SheetName,
                    Name)
                    ? PropertyEditorType.Dropdown
                    : GetInferredEditorType();
            }

            if (Definition.EditorType !=
                PropertyEditorType.Text)
            {
                return Definition.EditorType;
            }

            return GetInferredEditorType();
        }
    }

    public bool IsInteger =>
        SourceProperty?.Value.Type ==
        JTokenType.Integer;

    public bool IsDecimal =>
        SourceProperty?.Value.Type ==
        JTokenType.Float;

    public IReadOnlyList<ReferenceValueModel>
        AvailableValues =>
            referenceDataService.GetValues(
                SheetName,
                Name);

    public bool IsEditable =>
        Kind != PropertyKind.Internal
        &&
        EditorType != PropertyEditorType.ReadOnly
        &&
        EditorType != PropertyEditorType.Complex;

    public bool IsReadOnly =>
        !IsEditable;

    public bool IsModified
    {
        get => isModified;
        private set
        {
            if (SetProperty(
                    ref isModified,
                    value))
            {
                OnPropertyChanged(
                    nameof(CanReset));

                ModifiedChanged?.Invoke(
                    this,
                    EventArgs.Empty);
            }
        }
    }

    public bool IsStructurallyAdded
    {
        get => isStructurallyAdded;
        private set
        {
            if (!SetProperty(
                    ref isStructurallyAdded,
                    value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(CanReset));
        }
    }

    public bool CanReset =>
        IsModified
        &&
        !IsReadOnly
        &&
        !IsStructurallyAdded;

    public string OriginalDisplayValue =>
        GetTokenSummaryValue(
            originalValue);

    public string CurrentDisplayValue =>
        GetTokenSummaryValue(
            SourceProperty?.Value);

    public object? Value
    {
        get => value;
        set
        {
            if (!SetProperty(
                    ref this.value,
                    value))
            {
                return;
            }

            ApplyDisplayValue(value);
        }
    }

    public void CaptureOriginalValue()
    {
        IsStructurallyAdded =
            false;

        originalValue =
            SourceProperty?.Value.DeepClone();

        OnPropertyChanged(
            nameof(OriginalDisplayValue));

        UpdateModifiedState();
    }

    public void CaptureNewPropertyBaseline()
    {
        if (SourceProperty == null)
        {
            throw new InvalidOperationException(
                $"Property '{Name}' is not connected " +
                "to a source JSON property.");
        }

        IsStructurallyAdded =
            true;

        originalValue =
            JValue.CreateNull();

        OnPropertyChanged(
            nameof(OriginalDisplayValue));

        UpdateModifiedState();
    }

    public void AcceptCurrentValue()
    {
        IsStructurallyAdded =
            false;

        CaptureOriginalValue();
    }

    public void ResetToOriginal()
    {
        if (originalValue == null ||
            SourceProperty == null)
        {
            return;
        }

        ApplyTokenValue(originalValue);
    }

    public JToken GetOriginalValueSnapshot()
    {
        return originalValue?.DeepClone()
            ?? JValue.CreateNull();
    }

    public JToken GetCurrentValueSnapshot()
    {
        return SourceProperty?.Value.DeepClone()
            ?? JValue.CreateNull();
    }

    public void ApplySnapshotValue(
        JToken snapshotValue)
    {
        ArgumentNullException.ThrowIfNull(
            snapshotValue);

        if (SourceProperty == null)
        {
            throw new InvalidOperationException(
                $"Property '{Name}' is not connected " +
                "to a source JSON property.");
        }

        ApplyTokenValue(snapshotValue);
    }

    internal void ApplyHistoryValue(
        JToken historyValue)
    {
        if (SourceProperty == null)
            return;

        ApplyTokenValue(historyValue);
    }

    private void ApplyDisplayValue(
        object? newValue)
    {
        if (SourceProperty == null ||
            IsReadOnly)
        {
            return;
        }

        JToken previousValue =
            SourceProperty.Value.DeepClone();

        UpdateSourceProperty(newValue);

        JToken currentValue =
            SourceProperty.Value.DeepClone();

        OnPropertyChanged(
            nameof(CurrentDisplayValue));

        UpdateModifiedState();

        RaiseValueChanged(
            previousValue,
            currentValue);
    }

    private void ApplyTokenValue(
        JToken newValue)
    {
        if (SourceProperty == null)
            return;

        JToken previousValue =
            SourceProperty.Value.DeepClone();

        SourceProperty.Value =
            newValue.DeepClone();

        value =
            GetTokenDisplayValue(
                SourceProperty.Value);

        OnPropertyChanged(nameof(Value));

        OnPropertyChanged(
            nameof(CurrentDisplayValue));

        UpdateModifiedState();

        RaiseValueChanged(
            previousValue,
            SourceProperty.Value);
    }

    private void RaiseValueChanged(
        JToken previousValue,
        JToken newValue)
    {
        if (JToken.DeepEquals(
                previousValue,
                newValue))
        {
            return;
        }

        ValueChanged?.Invoke(
            this,
            new PropertyValueChangedEventArgs(
                previousValue,
                newValue));
    }

    private void UpdateModifiedState()
    {
        if (SourceProperty == null ||
            originalValue == null)
        {
            IsModified = false;
            return;
        }

        IsModified =
            !JToken.DeepEquals(
                originalValue,
                SourceProperty.Value);
    }

    private static object?
        GetTokenDisplayValue(
            JToken token)
    {
        return token.Type switch
        {
            JTokenType.Boolean =>
                token.Value<bool>(),

            _ =>
                token.ToString()
        };
    }

    private static string
        GetTokenSummaryValue(
            JToken? token)
    {
        if (token == null)
            return string.Empty;

        return token.Type switch
        {
            JTokenType.Null =>
                "null",

            JTokenType.String =>
                token.Value<string>()
                ?? string.Empty,

            JTokenType.Array =>
                token.ToString(
                    Formatting.None),

            JTokenType.Object =>
                token.ToString(
                    Formatting.None),

            JTokenType.Integer =>
                Convert.ToString(
                    token.Value<long>(),
                    CultureInfo.InvariantCulture)
                ?? string.Empty,

            JTokenType.Float =>
                Convert.ToString(
                    token.Value<double>(),
                    CultureInfo.InvariantCulture)
                ?? string.Empty,

            JTokenType.Boolean =>
                token.Value<bool>()
                    ? "true"
                    : "false",

            _ =>
                token.ToString()
        };
    }

    private PropertyEditorType
        GetInferredEditorType()
    {
        if (SourceProperty == null)
            return PropertyEditorType.Text;

        return SourceProperty.Value.Type switch
        {
            JTokenType.Integer =>
                PropertyEditorType.Number,

            JTokenType.Float =>
                PropertyEditorType.Number,

            JTokenType.Boolean =>
                PropertyEditorType.Boolean,

            JTokenType.Array =>
                PropertyEditorType.Complex,

            JTokenType.Object =>
                PropertyEditorType.Complex,

            _ =>
                PropertyEditorType.Text
        };
    }

    private void UpdateSourceProperty(
        object? newValue)
    {
        if (SourceProperty == null ||
            IsReadOnly)
        {
            return;
        }

        JToken currentValue =
            SourceProperty.Value;

        string textValue =
            newValue?.ToString()
            ?? string.Empty;

        switch (currentValue.Type)
        {
            case JTokenType.String:
                SourceProperty.Value =
                    textValue;
                break;

            case JTokenType.Integer:
                if (long.TryParse(
                        textValue,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out long integerValue))
                {
                    SourceProperty.Value =
                        integerValue;
                }

                break;

            case JTokenType.Float:
                if (double.TryParse(
                        textValue,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double decimalValue))
                {
                    SourceProperty.Value =
                        decimalValue;
                }

                break;

            case JTokenType.Boolean:
                if (newValue is bool directBoolean)
                {
                    SourceProperty.Value =
                        directBoolean;
                }
                else if (bool.TryParse(
                             textValue,
                             out bool parsedBoolean))
                {
                    SourceProperty.Value =
                        parsedBoolean;
                }

                break;

            case JTokenType.Null:
                SourceProperty.Value =
                    textValue;
                break;
        }
    }
}