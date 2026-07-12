using System.Collections.Generic;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public class PropertyDefinitionService
{
    private readonly Dictionary<string, PropertyDefinition> definitions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // ==========================
            // Gameplay
            // ==========================

            {
                "price",
                new PropertyDefinition
                {
                    Name = "price",
                    Kind = PropertyKind.Gameplay,
                    Description = "Purchase price of the item."
                }
            },

            {
                "weight",
                new PropertyDefinition
                {
                    Name = "weight",
                    Kind = PropertyKind.Gameplay,
                    Description = "Weight carried in inventory."
                }
            },

            {
                "rarity",
                new PropertyDefinition
                {
                    Name = "rarity",
                    Kind = PropertyKind.Gameplay,
                    EditorType = PropertyEditorType.Dropdown,
                    Description = "Item rarity."
                }
            },

            {
                "weaponType",
                new PropertyDefinition
                {
                    Name = "weaponType",
                    Kind = PropertyKind.Reference,
                    EditorType = PropertyEditorType.Dropdown,
                    Description = "Weapon type."
                }
            },

            // ==========================
            // Internal
            // ==========================

            {
                "id",
                new PropertyDefinition
                {
                    Name = "id",
                    Kind = PropertyKind.Internal,
                    EditorType = PropertyEditorType.ReadOnly,
                    Description = "Internal identifier."
                }
            },

            {
                "name",
                new PropertyDefinition
                {
                    Name = "name",
                    Kind = PropertyKind.Internal,
                    EditorType = PropertyEditorType.ReadOnly,
                    Description = "Localization key."
                }
            },

            {
                "desc",
                new PropertyDefinition
                {
                    Name = "desc",
                    Kind = PropertyKind.Internal,
                    EditorType = PropertyEditorType.ReadOnly,
                    Description = "Localization description key."
                }
            },

            {
                "icon",
                new PropertyDefinition
                {
                    Name = "icon",
                    Kind = PropertyKind.Internal,
                    EditorType = PropertyEditorType.ReadOnly,
                    Description = "Internal icon reference."
                }
            },

            {
                "iconeDone",
                new PropertyDefinition
                {
                    Name = "iconeDone",
                    Kind = PropertyKind.Internal,
                    EditorType = PropertyEditorType.Boolean,
                    Description = "Internal completion flag."
                }
            }
        };

    public PropertyDefinition GetDefinition(string propertyName)
    {
        if (definitions.TryGetValue(propertyName, out PropertyDefinition? definition))
        {
            return definition;
        }

        return new PropertyDefinition
        {
            Name = propertyName
        };
    }
}