using System.Windows;
using System.Windows.Controls;
using WartalesEditor.Models;

namespace WartalesEditor.Selectors;

public class PropertyEditorTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextTemplate { get; set; }

    public DataTemplate? NumberTemplate { get; set; }

    public DataTemplate? BooleanTemplate { get; set; }

    public DataTemplate? DropdownTemplate { get; set; }

    public DataTemplate? ComplexTemplate { get; set; }

    public DataTemplate? ReadOnlyTemplate { get; set; }

    public override DataTemplate? SelectTemplate(
        object item,
        DependencyObject container)
    {
        if (item is not PropertyModel property)
            return ReadOnlyTemplate;

        return property.EditorType switch
        {
            PropertyEditorType.Text => TextTemplate,
            PropertyEditorType.Number => NumberTemplate,
            PropertyEditorType.Boolean => BooleanTemplate,
            PropertyEditorType.Dropdown => DropdownTemplate,
            PropertyEditorType.Complex => ComplexTemplate,
            PropertyEditorType.ReadOnly => ReadOnlyTemplate,
            _ => ReadOnlyTemplate
        };
    }
}