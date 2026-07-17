using System;
using System.Collections.Generic;
using System.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Validation;

namespace WartalesEditor.Services.Validation;

public sealed class ValidationWorkflowService
{
    private readonly ValidationService
        validationService;

    public ValidationWorkflowService(
        ValidationService validationService)
    {
        this.validationService =
            validationService
            ?? throw new ArgumentNullException(
                nameof(validationService));
    }

    public ValidationResultModel ValidateProject(
        ProjectModel project)
    {
        return Validate(
            project,
            ValidationPurpose.General);
    }

    public ValidationResultModel ValidateForSave(
        ProjectModel project)
    {
        return Validate(
            project,
            ValidationPurpose.Save);
    }

    public ValidationResultModel
        ValidateForProfileCreation(
            ProjectModel project,
            object? profileSubject = null)
    {
        return Validate(
            project,
            ValidationPurpose.ProfileCreation,
            profileSubject);
    }

    public ValidationResultModel
        ValidateForProfileApplication(
            ProjectModel project,
            object? profileSubject = null)
    {
        return Validate(
            project,
            ValidationPurpose.ProfileApplication,
            profileSubject);
    }

    public ValidationResultModel
        ValidateForSnapshotImport(
            ProjectModel project,
            object? snapshotSubject = null)
    {
        return Validate(
            project,
            ValidationPurpose.SnapshotImport,
            snapshotSubject);
    }

    public ValidationResultModel
        ValidateForSnapshotExport(
            ProjectModel project,
            object? snapshotSubject = null)
    {
        return Validate(
            project,
            ValidationPurpose.SnapshotExport,
            snapshotSubject);
    }

    public ValidationResultModel
        ValidateForContentCreation(
            ProjectModel project,
            object? contentCreationSubject = null)
    {
        return Validate(
            project,
            ValidationPurpose.ContentCreation,
            contentCreationSubject);
    }

    public ValidationResultModel
        ValidateForMergePreview(
            ProjectModel project,
            object? mergeSubject = null)
    {
        return Validate(
            project,
            ValidationPurpose.MergePreview,
            mergeSubject);
    }

    private ValidationResultModel Validate(
        ProjectModel project,
        ValidationPurpose purpose,
        object? subject = null)
    {
        ArgumentNullException.ThrowIfNull(
            project);

        ValidationContext context =
            new(
                project,
                purpose,
                GetModifiedProperties(project),
                subject);

        return validationService.Validate(
            context);
    }

    private static IReadOnlyList<PropertyModel>
        GetModifiedProperties(
            ProjectModel project)
    {
        return project.Sheets
            .SelectMany(sheet =>
                sheet.Entries)
            .SelectMany(entry =>
                entry.Properties)
            .Where(property =>
                property.IsModified)
            .ToList();
    }
}
