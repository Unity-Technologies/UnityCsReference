// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct ReorderStyleRulePropertiesCommand
{
    const string CommandUndoName = "Reorder style rule properties";

    readonly StyleSheet StyleSheet;
    readonly StyleRule Rule;
    readonly StyleProperty[] NewPropertyOrder;

    public ReorderStyleRulePropertiesCommand(StyleSheet styleSheet, StyleRule rule, StyleProperty[] newPropertyOrder)
    {
        StyleSheet = styleSheet;
        Rule = rule;
        NewPropertyOrder = newPropertyOrder;
    }

    public void Execute()
    {
        Assert.IsNotNull(StyleSheet);
        Assert.IsNotNull(Rule);
        Assert.IsNotNull(NewPropertyOrder);

        Undo.RegisterCompleteObjectUndo(StyleSheet, CommandUndoName);

        var variableIndices = new List<int>();
        for (var i = 0; i < Rule.properties.Length; i++)
        {
            for (var j = 0; j < NewPropertyOrder.Length; j++)
            {
                if (Rule.properties[i] == NewPropertyOrder[j])
                {
                    variableIndices.Add(i);
                    break;
                }
            }
        }

        for (var i = 0; i < variableIndices.Count && i < NewPropertyOrder.Length; i++)
            Rule.properties[variableIndices[i]] = NewPropertyOrder[i];

        EditorUtility.SetDirty(StyleSheet);
    }
}
