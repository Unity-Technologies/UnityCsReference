// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

internal readonly record struct ResetStyleRulePropertyValueCommand
{
    const string CommandUndoName = "Reset style rule property value";

    readonly StyleSheet StyleSheet;
    readonly StyleProperty Property;
    readonly VariablesInspector.VariableType Type;

    public ResetStyleRulePropertyValueCommand(
        StyleSheet styleSheet,
        StyleProperty property,
        VariablesInspector.VariableType type)
    {
        StyleSheet = styleSheet;
        Property = property;
        Type = type;
    }

    public void Execute()
    {
        Assert.IsNotNull(StyleSheet);
        Assert.IsNotNull(Property);

        Undo.RegisterCompleteObjectUndo(StyleSheet, CommandUndoName);
        SetPropertyToDefaultValue(StyleSheet, Property, Type);
        EditorUtility.SetDirty(StyleSheet);
    }

    internal static void SetPropertyToDefaultValue(StyleSheet styleSheet, StyleProperty property, VariablesInspector.VariableType type)
    {
        switch (type)
        {
            case VariablesInspector.VariableType.Float:
                property.SetFloat(styleSheet, float.Parse(GetTextFieldDefaultValue(type)));
                break;
            case VariablesInspector.VariableType.Color:
                property.SetColor(styleSheet, Color.black);
                break;
            case VariablesInspector.VariableType.AssetReference:
                property.SetAssetReference(styleSheet, new UnityEngine.Object());
                break;
            case VariablesInspector.VariableType.Length:
                property.SetLength(styleSheet, new Length(0, LengthUnit.Pixel));
                break;
            case VariablesInspector.VariableType.Angle:
                property.SetRotate(styleSheet, new Angle(0, AngleUnit.Degree));
                break;
            case VariablesInspector.VariableType.Time:
                property.SetDimension(styleSheet, new Dimension(0, Dimension.Unit.Second));
                break;
            case VariablesInspector.VariableType.Keyword:
                property.SetKeyword(styleSheet, StyleValueKeyword.Auto);
                break;
            case VariablesInspector.VariableType.String:
                property.SetString(styleSheet, GetTextFieldDefaultValue(type));
                break;
            case VariablesInspector.VariableType.Enum:
                property.SetEnum(styleSheet, (Enum)StyleValueKeyword.Auto);
                break;
        }
    }

    static string GetTextFieldDefaultValue(VariablesInspector.VariableType type)
    {
        return (type) switch
        {
            VariablesInspector.VariableType.Float => "0",
            VariablesInspector.VariableType.String => "String",
            _ => ""
        };
    }
}
