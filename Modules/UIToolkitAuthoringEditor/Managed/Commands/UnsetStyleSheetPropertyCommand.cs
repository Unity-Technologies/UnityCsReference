// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

internal readonly record struct UnsetStyleSheetPropertyCommand
{
    const string CommandUndoName = "Unset style property";

    readonly StyleSheet StyleSheet;
    readonly StyleRule Rule;
    readonly StylePropertyId StylePropertyId;

    public UnsetStyleSheetPropertyCommand(
        StyleSheet styleSheet,
        StyleRule rule,
        StylePropertyId stylePropertyId)
    {
        StyleSheet = styleSheet;
        Rule = rule;
        StylePropertyId = stylePropertyId;
    }

    public void Execute()
    {
        Assert.IsNotNull(StyleSheet);
        Assert.IsNotNull(Rule);

        Undo.RegisterCompleteObjectUndo(StyleSheet, CommandUndoName);

        var property = Rule.FindLastProperty(StylePropertyId);
        if (property != null)
        {
            Rule.RemoveProperty(property);
            EditorUtility.SetDirty(StyleSheet);
        }
    }
}
