// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

internal readonly record struct RemoveVariableCommand
{
    const string CommandUndoName = "Remove variable";

    readonly StyleSheet StyleSheet;
    readonly StyleRule Rule;
    readonly StylePropertyId StylePropertyId;

    public RemoveVariableCommand(
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
        Assert.IsNotNull(Rule);

        if (StyleSheet != null)
            Undo.RegisterCompleteObjectUndo(StyleSheet, CommandUndoName);

        var property = Rule.FindLastProperty(StylePropertyId);
        if (property != null)
        {
            Rule.RemoveProperty(property);

            if (StyleSheet != null)
                EditorUtility.SetDirty(StyleSheet);
        }
    }
}
