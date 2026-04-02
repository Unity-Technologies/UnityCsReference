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

internal readonly record struct WriteStyleRulePropertyValueCommand<T>
{
    const string CommandUndoName = "Write style rule property value";

    readonly StyleSheet StyleSheet;
    readonly StyleProperty Property;
    readonly VariablesInspector.VariableType Type;
    readonly T Value;

    public WriteStyleRulePropertyValueCommand(
        StyleSheet styleSheet,
        StyleProperty property,
        VariablesInspector.VariableType type,
        T value)
    {
        StyleSheet = styleSheet;
        Property = property;
        Type = type;
        Value = value;
    }

    public void Execute()
    {
        Assert.IsNotNull(StyleSheet);
        Assert.IsNotNull(Property);

        Undo.RegisterCompleteObjectUndo(StyleSheet, CommandUndoName);

        switch (Type)
        {
            case VariablesInspector.VariableType.Float:
                StyleSheet.WriteFloat(ref Property.values[0], (float)(object)Value);
                break;
            case VariablesInspector.VariableType.Color:
                StyleSheet.WriteColor(ref Property.values[0], (Color)(object)Value);
                break;
            case VariablesInspector.VariableType.Length:
                StyleSheet.WriteLength(ref Property.values[0], (Length)(object)Value);
                break;
            case VariablesInspector.VariableType.Angle:
                StyleSheet.WriteAngle(ref Property.values[0], (Angle)(object)Value);
                break;
            case VariablesInspector.VariableType.Time:
                StyleSheet.WriteTimeValue(ref Property.values[0], (TimeValue)(object)Value);
                break;
            case VariablesInspector.VariableType.AssetReference:
                StyleSheet.WriteAssetReference(ref Property.values[0], (UnityEngine.Object)(object)Value);
                break;
            case VariablesInspector.VariableType.Keyword:
                StyleSheet.WriteKeyword(ref Property.values[0], (StyleValueKeyword)(object)Value);
                break;
            case VariablesInspector.VariableType.String:
                StyleSheet.WriteString(ref Property.values[0], (string)(object)Value);
                break;
            case VariablesInspector.VariableType.Enum:
                StyleSheet.WriteEnumAsString(ref Property.values[0], (string)(object)Value);
                break;
        }

        EditorUtility.SetDirty(StyleSheet);
    }
}
