using System;
using System.Collections;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace WartalesEditor.Converters;

public sealed class FirstValidationErrorConverter
    : IValueConverter
{
    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not IEnumerable errors)
        {
            return null;
        }

        foreach (object? item in errors)
        {
            if (item is ValidationError error)
            {
                return error.ErrorContent;
            }

            break;
        }

        return null;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
