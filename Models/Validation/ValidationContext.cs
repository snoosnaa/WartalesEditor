using System;
using System.Collections.Generic;
using System.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Models.Validation;

public sealed class ValidationContext
{
    public ValidationContext(
        ProjectModel project,
        ValidationPurpose purpose,
        IEnumerable<PropertyModel>? modifiedProperties = null,
        object? subject = null)
    {
        Project =
            project
            ?? throw new ArgumentNullException(
                nameof(project));

        Purpose = purpose;

        ModifiedProperties =
            modifiedProperties == null
                ? Array.Empty<PropertyModel>()
                : modifiedProperties
                    .Where(property =>
                        property != null)
                    .Distinct()
                    .ToList();

        Subject = subject;
    }

    public ProjectModel Project { get; }

    public ValidationPurpose Purpose { get; }

    public IReadOnlyList<PropertyModel>
        ModifiedProperties
    {
        get;
    }

    public object? Subject { get; }

    public bool HasModifiedProperties =>
        ModifiedProperties.Count > 0;

    public TSubject? GetSubject<TSubject>()
        where TSubject : class
    {
        return Subject as TSubject;
    }
}