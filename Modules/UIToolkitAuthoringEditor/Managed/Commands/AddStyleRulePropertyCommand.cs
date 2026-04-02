// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct AddStyleRulePropertyCommand
{
    const string CommandUndoName = "Add style rule property";

    readonly StyleSheet StyleSheet;
    readonly StyleRule Rule;
    readonly string PropertyName;
    readonly VariablesInspector.VariableType VariableType;

    public AddStyleRulePropertyCommand(
        StyleSheet styleSheet,
        StyleRule rule,
        string propertyName,
        VariablesInspector.VariableType variableType)
    {
        StyleSheet = styleSheet;
        Rule = rule;
        PropertyName = propertyName;
        VariableType = variableType;
    }

    public void Execute()
    {
        Assert.IsNotNull(StyleSheet);
        Assert.IsNotNull(Rule);
        Assert.IsNotNull(PropertyName);

        Undo.RegisterCompleteObjectUndo(StyleSheet, CommandUndoName);

        var property = Rule.AddProperty(PropertyName);
        ResetStyleRulePropertyValueCommand.SetPropertyToDefaultValue(StyleSheet, property, VariableType);

        EditorUtility.SetDirty(StyleSheet);
    }
}
