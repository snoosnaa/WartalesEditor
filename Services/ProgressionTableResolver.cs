using System;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

internal sealed class ProgressionTableResolver
{
    private const string ConstantSheetName =
        "constant";

    private readonly ProjectMutationService
        projectMutationService;

    public ProgressionTableResolver(
        ProjectMutationService projectMutationService)
    {
        ArgumentNullException.ThrowIfNull(projectMutationService);
        this.projectMutationService = projectMutationService;
    }

    public ProgressionTableBinding Resolve(
        ProjectModel project,
        ProgressionType progressionType)
    {
        ArgumentNullException.ThrowIfNull(project);

        SheetModel sheet =
            projectMutationService.FindSheet(
                project,
                ConstantSheetName);

        EntryModel entry =
            projectMutationService.FindEntry(
                sheet,
                ProgressionScalingService.GetTableId(
                    progressionType));

        PropertyModel[] arrayProperties =
            entry.Properties
                .Where(property =>
                    property.SourceProperty?.Value is JArray)
                .ToArray();

        Trace.WriteLine(
            $"Progression table match: sheet='{sheet.Name}', " +
            $"entry='{entry.Id}', properties=[" +
            string.Join(
                ", ",
                entry.Properties.Select(property =>
                    $"{property.EffectivePropertyPath}:" +
                    $"{property.SourceProperty?.Value.Type}")) +
            "].");

        if (arrayProperties.Length != 1)
        {
            throw new InvalidOperationException(
                $"Progression table '{entry.Id}' must contain exactly " +
                $"one array property, but {arrayProperties.Length} " +
                "were found.");
        }

        PropertyModel arrayProperty =
            arrayProperties[0];

        JArray array =
            (JArray)arrayProperty.SourceProperty!.Value;

        string elementValuePropertyName =
            ResolveElementValuePropertyName(entry, array);

        return new ProgressionTableBinding(
            entry,
            arrayProperty,
            elementValuePropertyName);
    }

    private static string ResolveElementValuePropertyName(
        EntryModel entry,
        JArray array)
    {
        if (array.Count == 0)
        {
            throw new InvalidOperationException(
                $"Progression table '{entry.Id}' must not be empty.");
        }

        string? resolvedName = null;

        for (int index = 0; index < array.Count; index++)
        {
            if (array[index] is not JObject element)
            {
                throw new InvalidOperationException(
                    $"Progression table '{entry.Id}' element {index} " +
                    "must be a JSON object.");
            }

            JProperty[] integerProperties =
                element.Properties()
                    .Where(property =>
                        property.Value.Type == JTokenType.Integer)
                    .ToArray();

            if (integerProperties.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Progression table '{entry.Id}' element {index} " +
                    "must contain exactly one integer member.");
            }

            resolvedName ??= integerProperties[0].Name;

            if (!string.Equals(
                    resolvedName,
                    integerProperties[0].Name,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Progression table '{entry.Id}' uses inconsistent " +
                    "element value members.");
            }
        }

        return resolvedName!;
    }
}
