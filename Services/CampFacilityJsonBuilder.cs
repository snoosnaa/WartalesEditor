using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace WartalesEditor.Services;

public sealed class CampFacilityJsonBuilder
{
    private const string IconFile =
        "ui/Icons/WM_Icons.png";

    private const int IconSize =
        42;

    private const string WorkshopTool =
        "Workshop";

    private const string KnowledgeItem =
        "Knowledge";

    private const string TinkererToolGroup =
        "TinkererTool";

    private static readonly CampFacilityDefinition
        AnvilDefinition =
            new(
                ItemId:
                    "Anvil",
                Model:
                    "content/elements/Dioramas/Activities/" +
                    "Anvil.prefab",
                Activity:
                    "Forge",
                Animation:
                    "Forge",
                CampWidth:
                    2,
                CampHeight:
                    2,
                ToolCapacity:
                    2,
                Width:
                    2,
                Height:
                    2,
                Capacity:
                    1,
                Tier:
                    1,
                IconX:
                    4,
                IconY:
                    0,
                Ingredients:
                [
                    new CampFacilityIngredient(
                        ItemId:
                            "IronOre",
                        Quantity:
                            12)
                ],
                Bonuses:
                [
                    "BlacksmithQuality",
                    "BlacksmithResources"
                ]);

    private static readonly CampFacilityDefinition
        ApothecaryDefinition =
            new(
                ItemId:
                    "ApothecaryTable",
                Model:
                    "content/elements/Dioramas/Activities/" +
                    "ApothecaryTable.prefab",
                Activity:
                    "Alchemy",
                Animation:
                    "Workshop",
                CampWidth:
                    2,
                CampHeight:
                    4,
                ToolCapacity:
                    2,
                Width:
                    2,
                Height:
                    4,
                Capacity:
                    1,
                Tier:
                    1,
                IconX:
                    0,
                IconY:
                    2,
                Ingredients:
                [
                    new CampFacilityIngredient(
                        ItemId:
                            "Wood",
                        Quantity:
                            4),

                    new CampFacilityIngredient(
                        ItemId:
                            "Rope",
                        Quantity:
                            4)
                ],
                Bonuses:
                [
                    "AlchemyResources"
                ]);

    public JObject BuildAnvilProps(
        JObject existingProps)
    {
        return BuildProps(
            existingProps,
            AnvilDefinition);
    }

    public JObject BuildAnvilTool()
    {
        return BuildTool(
            AnvilDefinition);
    }

    public JObject BuildAnvilIcon()
    {
        return BuildIcon(
            AnvilDefinition);
    }

    public JObject BuildAnvilCraftEntry()
    {
        return BuildCraftEntry(
            AnvilDefinition);
    }

    public JObject BuildApothecaryProps(
        JObject existingProps)
    {
        return BuildProps(
            existingProps,
            ApothecaryDefinition);
    }

    public JObject BuildApothecaryTool()
    {
        return BuildTool(
            ApothecaryDefinition);
    }

    public JObject BuildApothecaryIcon()
    {
        return BuildIcon(
            ApothecaryDefinition);
    }

    public JObject BuildApothecaryCraftEntry()
    {
        return BuildCraftEntry(
            ApothecaryDefinition);
    }

    public int GetEffectivePropertyChangeCount()
    {
        return GetEffectivePropertyChangeCount(
                   AnvilDefinition)
               +
               GetEffectivePropertyChangeCount(
                   ApothecaryDefinition);
    }

    private static int GetEffectivePropertyChangeCount(
        CampFacilityDefinition definition)
    {
        JObject cleanProps =
            new()
            {
                ["activity"] = definition.Activity,
                ["hideInCheatMenu"] = true
            };

        return CountChangedMembers(
                   cleanProps,
                   BuildProps(
                       cleanProps,
                       definition))
               + BuildTool(definition)
                   .Properties()
                   .Count()
               + BuildIcon(definition)
                   .Properties()
                   .Count();
    }

    private static int CountChangedMembers(
        JObject baseline,
        JObject result)
    {
        int count = 0;

        foreach (JProperty property in result.Properties())
        {
            if (!JToken.DeepEquals(
                    baseline[property.Name],
                    property.Value))
            {
                count++;
            }
        }

        return count;
    }

    private static JObject BuildProps(
        JObject existingProps,
        CampFacilityDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(
            existingProps);

        ArgumentNullException.ThrowIfNull(
            definition);

        JObject props =
            (JObject)existingProps.DeepClone();

        props["model"] =
            definition.Model;

        props["activity"] =
            definition.Activity;

        props["hideInCheatMenu"] =
            true;

        props["bonuses"] =
            BuildBonuses(
                definition.Bonuses);

        return props;
    }

    private static JObject BuildTool(
        CampFacilityDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(
            definition);

        return new JObject
        {
            ["campWidth"] =
                definition.CampWidth,

            ["campHeight"] =
                definition.CampHeight,

            ["toolCapacity"] =
                definition.ToolCapacity,

            ["width"] =
                definition.Width,

            ["height"] =
                definition.Height,

            ["capacity"] =
                definition.Capacity,

            ["animation"] =
                definition.Animation,

            ["hideHandEquipment"] =
                true,

            ["tier"] =
                definition.Tier,

            ["bonusesIfAssigned"] =
                BuildBonuses(
                    definition.Bonuses)
        };
    }

    private static JObject BuildIcon(
        CampFacilityDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(
            definition);

        return new JObject
        {
            ["file"] =
                IconFile,

            ["size"] =
                IconSize,

            ["x"] =
                definition.IconX,

            ["y"] =
                definition.IconY
        };
    }

    private static JObject BuildCraftEntry(
        CampFacilityDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(
            definition);

        return new JObject
        {
            ["item"] =
                definition.ItemId,

            ["tool"] =
                WorkshopTool,

            ["recipe"] =
                BuildIngredients(
                    definition.Ingredients),

            ["props"] =
                new JObject(),

            ["learnCost"] =
                new JArray
                {
                    new JObject
                    {
                        ["qty"] =
                            1,

                        ["item"] =
                            KnowledgeItem
                    }
                },

            ["jobLevel"] =
                1,

            ["group"] =
                TinkererToolGroup
        };
    }

    private static JArray BuildIngredients(
        IReadOnlyList<CampFacilityIngredient>
            ingredients)
    {
        ArgumentNullException.ThrowIfNull(
            ingredients);

        JArray result =
            new();

        foreach (CampFacilityIngredient ingredient
                 in ingredients)
        {
            result.Add(
                new JObject
                {
                    ["qty"] =
                        ingredient.Quantity,

                    ["item"] =
                        ingredient.ItemId
                });
        }

        return result;
    }

    private static JArray BuildBonuses(
        IReadOnlyList<string> bonuses)
    {
        ArgumentNullException.ThrowIfNull(
            bonuses);

        JArray result =
            new();

        foreach (string bonus in bonuses)
        {
            result.Add(
                new JObject
                {
                    ["bonus"] =
                        bonus
                });
        }

        return result;
    }

    private sealed record CampFacilityDefinition(
        string ItemId,
        string Model,
        string Activity,
        string Animation,
        int CampWidth,
        int CampHeight,
        int ToolCapacity,
        int Width,
        int Height,
        int Capacity,
        int Tier,
        int IconX,
        int IconY,
        IReadOnlyList<CampFacilityIngredient>
            Ingredients,
        IReadOnlyList<string> Bonuses);

    private sealed record CampFacilityIngredient(
        string ItemId,
        int Quantity);
}
