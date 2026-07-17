using System;
using System.Collections.Generic;
using WartalesEditor.Models.Validation;

namespace WartalesEditor.Services.Validation.Rules;

public sealed class ProjectSerializationValidationRule
    : IValidationRule
{
    private readonly JsonDataService
        jsonDataService;

    public ProjectSerializationValidationRule(
        JsonDataService jsonDataService)
    {
        this.jsonDataService =
            jsonDataService
            ?? throw new ArgumentNullException(
                nameof(jsonDataService));
    }

    public string RuleId =>
        ValidationRuleIds.ProjectSerialization;

    public bool AppliesTo(
        ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Purpose
            is ValidationPurpose.General
            or ValidationPurpose.Save
            or ValidationPurpose.ProfileCreation
            or ValidationPurpose.ContentCreation
            or ValidationPurpose.MergePreview;
    }

    public IEnumerable<ValidationIssueModel> Validate(
        ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (jsonDataService.TrySerializeProject(
                context.Project,
                out _,
                out string errorMessage))
        {
            yield break;
        }

        yield return new ValidationIssueModel(
            RuleId,
            ValidationSeverity.Error,
            ValidationCategory.ProjectStructure,
            "The project cannot be serialized safely." +
            Environment.NewLine +
            errorMessage);
    }
}
