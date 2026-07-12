using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using WartalesEditor.Models;

namespace WartalesEditor.Validation;

public class PropertyValidationRule : ValidationRule
{
    public override ValidationResult Validate(
        object value,
        CultureInfo cultureInfo)
    {
        if (value is not BindingExpression bindingExpression ||
            bindingExpression.DataItem is not PropertyModel property)
        {
            return ValidationResult.ValidResult;
        }

        string text = string.Empty;

        if (bindingExpression.Target is TextBox textBox)
        {
            text = textBox.Text;
        }

        if (property.IsInteger)
        {
            return long.TryParse(
                text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out _)
                    ? ValidationResult.ValidResult
                    : new ValidationResult(
                        false,
                        "Please enter a whole number.");
        }

        if (property.IsDecimal)
        {
            return double.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out _)
                    ? ValidationResult.ValidResult
                    : new ValidationResult(
                        false,
                        "Please enter a valid decimal number.");
        }

        return ValidationResult.ValidResult;
    }
}