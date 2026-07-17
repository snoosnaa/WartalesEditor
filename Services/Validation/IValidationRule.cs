using System.Collections.Generic;
using WartalesEditor.Models.Validation;

namespace WartalesEditor.Services.Validation;

public interface IValidationRule
{
    string RuleId { get; }

    bool AppliesTo(
        ValidationContext context);

    IEnumerable<ValidationIssueModel> Validate(
        ValidationContext context);
}
