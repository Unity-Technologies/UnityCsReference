// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

internal readonly record struct SetStyleSheetPropertyCommand<T>
{
    const string CommandUndoName = "Set style property";

    readonly StyleSheet StyleSheet;
    readonly StyleRule Rule;
    readonly StylePropertyId StylePropertyId;
    readonly Action<StyleProperty, StyleSheet, T> ValueSetter;
    readonly T Value;

    public SetStyleSheetPropertyCommand(
        StyleSheet styleSheet,
        StyleRule rule,
        StylePropertyId stylePropertyId,
        Action<StyleProperty, StyleSheet, T> valueSetter,
        T value)
    {
        StyleSheet = styleSheet;
        Rule = rule;
        StylePropertyId = stylePropertyId;
        ValueSetter = valueSetter;
        Value = value;
    }

    public void Execute()
    {
        Assert.IsNotNull(StyleSheet);
        Assert.IsNotNull(Rule);

        Undo.RegisterCompleteObjectUndo(StyleSheet, CommandUndoName);

        var property = GetOrCreateStyleProperty(Rule, StylePropertyId);
        ValueSetter(property, StyleSheet, Value);

        EditorUtility.SetDirty(StyleSheet);
    }


    private static StyleProperty GetOrCreateStyleProperty(StyleRule rule, StylePropertyId stylePropertyId)
    {
        return rule.FindLastProperty(stylePropertyId) ?? rule.AddProperty(stylePropertyId);
    }
}
