// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct RemoveStyleRulePropertyCommand
{
    const string CommandUndoName = "Remove style rule property";

    readonly StyleSheet StyleSheet;
    readonly StyleRule Rule;
    readonly StyleProperty Property;

    public RemoveStyleRulePropertyCommand(StyleSheet styleSheet, StyleRule rule, StyleProperty property)
    {
        StyleSheet = styleSheet;
        Rule = rule;
        Property = property;
    }

    public void Execute()
    {
        Assert.IsNotNull(StyleSheet);
        Assert.IsNotNull(Rule);
        Assert.IsNotNull(Property);

        Undo.RegisterCompleteObjectUndo(StyleSheet, CommandUndoName);

        Rule.RemoveProperty(Property);

        EditorUtility.SetDirty(StyleSheet);
    }
}
