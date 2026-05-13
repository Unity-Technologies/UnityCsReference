// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

internal readonly record struct SetVariableCommand
{
    const string CommandUndoName = "Set variable";

    readonly StyleSheet StyleSheet;
    readonly StyleRule Rule;
    readonly StylePropertyId StylePropertyId;
    readonly string VariableName;

    public SetVariableCommand(
        StyleSheet styleSheet,
        StyleRule rule,
        StylePropertyId stylePropertyId,
        string variableName)
    {
        StyleSheet = styleSheet;
        Rule = rule;
        StylePropertyId = stylePropertyId;
        VariableName = variableName;
    }

    public void Execute()
    {
        Assert.IsNotNull(StyleSheet);
        Assert.IsNotNull(Rule);

        Undo.RegisterCompleteObjectUndo(StyleSheet, CommandUndoName);

        var property = Rule.FindLastProperty(StylePropertyId) ?? Rule.AddProperty(StylePropertyId);
        property.SetVariableReference(StyleSheet, VariableName);

        EditorUtility.SetDirty(StyleSheet);
    }
}
