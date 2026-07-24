using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Operations;

namespace WartalesEditor.Services.Operations;

public sealed class ProgressionXpOperationValidator :
    IProjectOperationValidator
{
    private const string ConstantSheetName =
        "constant";

    private readonly ProgressionScalingService
        progressionScalingService;

    public ProgressionXpOperationValidator()
        : this(
            new ProgressionScalingService(
                new ProjectMutationService()))
    {
    }

    public ProgressionXpOperationValidator(
        ProgressionScalingService progressionScalingService)
    {
        ArgumentNullException.ThrowIfNull(
            progressionScalingService);

        this.progressionScalingService =
            progressionScalingService;
    }

    public OperationValidationResult Validate(
        IProjectOperation operation,
        ProjectModel project,
        ProjectMutationResult mutationResult)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(mutationResult);

        if (!TryGetOperationDetails(
                operation,
                out ProgressionType progressionType,
                out int percentage))
        {
            return OperationValidationResult.Failure(
                $"{nameof(ProgressionXpOperationValidator)} cannot " +
                $"validate operation type '{operation.GetType().Name}'.");
        }

        List<string> errors =
            new();

        if (percentage < ProgressionScalingService.MinimumPercentage ||
            percentage > ProgressionScalingService.MaximumPercentage)
        {
            errors.Add(
                $"Percentage must be between " +
                $"{ProgressionScalingService.MinimumPercentage}% and " +
                $"{ProgressionScalingService.MaximumPercentage}%.");
        }

        string tableId =
            ProgressionScalingService.GetTableId(
                progressionType);

        ProgressionTableBinding? binding =
            null;

        try
        {
            binding =
                progressionScalingService.ResolveProgressionTable(
                    project,
                    progressionType);

            ValidateProgressionValues(
                binding,
                progressionType,
                errors);
        }
        catch (InvalidOperationException exception)
        {
            errors.Add(exception.Message);
        }

        ValidateMutationConnections(
            mutationResult,
            tableId,
            binding?.ArrayPropertyPath,
            errors);

        return errors.Count == 0
            ? OperationValidationResult.Success()
            : OperationValidationResult.Failure(errors);
    }

    private static void ValidateProgressionValues(
        ProgressionTableBinding binding,
        ProgressionType progressionType,
        ICollection<string> errors)
    {
        IReadOnlyList<long> values;

        try
        {
            values = binding.ReadValues(
                binding.ArrayProperty.GetCurrentValueSnapshot());
        }
        catch (InvalidOperationException exception)
        {
            errors.Add(exception.Message);
            return;
        }

        long? previousValue = null;

        for (int index = 0;
             index < values.Count;
             index++)
        {
            long value = values[index];

            bool isCharacterInitialZero =
                progressionType == ProgressionType.Character &&
                index == 0;

            if (isCharacterInitialZero)
            {
                if (value != 0)
                {
                    errors.Add(
                        "Character XP progression must begin with zero.");
                }
            }
            else if (progressionType == ProgressionType.Character &&
                     value <= 0)
            {
                errors.Add(
                    $"Progression table '{binding.Entry.Id}' contains " +
                    $"a zero or negative XP requirement at index {index}.");
            }

            if (previousValue.HasValue &&
                value <= previousValue.Value)
            {
                errors.Add(
                    $"Progression table '{binding.Entry.Id}' is not " +
                    $"strictly increasing at index {index}.");
            }

            previousValue = value;
        }
    }

    private static void ValidateMutationConnections(
        ProjectMutationResult mutationResult,
        string tableId,
        string? arrayPropertyPath,
        ICollection<string> errors)
    {
        if (mutationResult.CreatedEntries.Count > 0 ||
            mutationResult.CreatedProperties.Count > 0 ||
            mutationResult.CreatedJsonPropertyRollbackRecords.Count > 0)
        {
            errors.Add(
                "XP requirement scaling unexpectedly created project structure.");
        }

        foreach (PropertyModel property in
                 mutationResult.UpdatedProperties)
        {
            if (property.SourceProperty == null)
            {
                errors.Add(
                    $"Changed property '{property.EffectivePropertyPath}' " +
                    "is not connected to source JSON.");
                continue;
            }

            if (!string.Equals(
                    property.SheetName,
                    ConstantSheetName,
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(arrayPropertyPath) ||
                !string.Equals(
                    property.EffectivePropertyPath,
                    arrayPropertyPath,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    FindEntryId(property),
                    tableId,
                    StringComparison.Ordinal))
            {
                errors.Add(
                    "XP requirement scaling modified a property outside " +
                    $"'constant/{tableId}/{arrayPropertyPath}'.");
            }
        }
    }

    private static string FindEntryId(
        PropertyModel property)
    {
        JToken? current =
            property.SourceProperty?.Parent;

        while (current != null)
        {
            if (current is JObject sourceObject)
            {
                string? entryId =
                    sourceObject["id"]?.Value<string>();

                if (!string.IsNullOrWhiteSpace(entryId))
                {
                    return entryId;
                }
            }

            current = current.Parent;
        }

        return string.Empty;
    }

    private static bool TryGetOperationDetails(
        IProjectOperation operation,
        out ProgressionType progressionType,
        out int percentage)
    {
        switch (operation)
        {
            case CharacterXpRequirementsOperation characterOperation:
                progressionType = ProgressionType.Character;
                percentage = characterOperation.Percentage;
                return true;

            case ProfessionXpRequirementsOperation professionOperation:
                progressionType = ProgressionType.Profession;
                percentage = professionOperation.Percentage;
                return true;

            default:
                progressionType = default;
                percentage = default;
                return false;
        }
    }
}
