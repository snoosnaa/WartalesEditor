using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Validation;

namespace WartalesEditor.Services.Validation.Rules;

public sealed class ModifiedPropertyTokenTypeValidationRule
    : IValidationRule
{
    public string RuleId =>
        ValidationRuleIds.ModifiedPropertyTokenType;

    public bool AppliesTo(
        ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.HasModifiedProperties;
    }

    public IEnumerable<ValidationIssueModel> Validate(
        ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (PropertyModel property in
                 context.ModifiedProperties)
        {
            if (property.SourceProperty == null ||
                property.IsStructurallyAdded)
            {
                continue;
            }

            JToken originalValue =
                property.GetOriginalValueSnapshot();

            JToken currentValue =
                property.GetCurrentValueSnapshot();

            if (AreCompatibleTokenTypes(
                    originalValue.Type,
                    currentValue.Type))
            {
                continue;
            }

            yield return new ValidationIssueModel(
                RuleId,
                ValidationSeverity.Error,
                ValidationCategory.OriginalState,
                $"The property changed JSON type from " +
                $"'{originalValue.Type}' to " +
                $"'{currentValue.Type}'.",
                property.SheetName,
                propertyName:
                    property.Name,
                originalValue:
                    property.OriginalDisplayValue,
                currentValue:
                    property.CurrentDisplayValue);
        }
    }

    private static bool AreCompatibleTokenTypes(
        JTokenType originalType,
        JTokenType currentType)
    {
        if (originalType == currentType)
        {
            return true;
        }

        return IsNumericType(originalType)
            &&
            IsNumericType(currentType);
    }

    private static bool IsNumericType(
        JTokenType tokenType)
    {
        return tokenType
            is JTokenType.Integer
            or JTokenType.Float;
    }
}