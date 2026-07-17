using System;
using WartalesEditor.Models.Validation;
using WartalesEditor.Services.Validation.Rules;

namespace WartalesEditor.Services.Validation;

public sealed class ValidationService
{
    private readonly ValidationPipeline
        validationPipeline;

    public ValidationService(
        JsonDataService jsonDataService)
    {
        ArgumentNullException.ThrowIfNull(
            jsonDataService);

        validationPipeline =
            new ValidationPipeline(
                new IValidationRule[]
                {
                    new ProjectStructureValidationRule(),
                    new EntryIdentityValidationRule(),
                    new PropertyIdentityValidationRule(),
                    new PropertySourceConnectionValidationRule(),
                    new ModifiedPropertyTokenTypeValidationRule(),
                    new ProjectSerializationValidationRule(
                        jsonDataService)
                });
    }

    public ValidationResultModel Validate(
        ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        return validationPipeline.Run(
            context);
    }
}
