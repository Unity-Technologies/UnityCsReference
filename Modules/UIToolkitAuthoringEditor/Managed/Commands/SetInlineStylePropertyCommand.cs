// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

internal readonly record struct SetInlineStylePropertyCommand<T>
{
    const string CommandUndoName = "Set inline style property";

    readonly VisualElement Element;
    readonly StylePropertyId StylePropertyId;
    readonly Action<StyleProperty, StyleSheet, T> ValueSetter;
    readonly T Value;

    public SetInlineStylePropertyCommand(
        VisualElement element,
        StylePropertyId stylePropertyId,
        Action<StyleProperty, StyleSheet, T> valueSetter,
        T value)
    {
        Element = element;
        StylePropertyId = stylePropertyId;
        ValueSetter = valueSetter;
        Value = value;
    }

    public void Execute()
    {
        Assert.IsNotNull(Element);
        Assert.IsNotNull(Element.visualElementAsset);
        Assert.IsNotNull(Element.visualTreeAssetSource);

        var visualTreeAsset = Element.visualTreeAssetSource;
        Undo.RegisterCompleteObjectUndo(visualTreeAsset, CommandUndoName);

        var inlineStyleSheet = visualTreeAsset.GetOrCreateInlineStyleSheet();
        Undo.RegisterCompleteObjectUndo(inlineStyleSheet, CommandUndoName);

        var vea = Element.visualElementAsset;
        var rule = GetOrCreateRule(vea, inlineStyleSheet);
        var property = GetOrCreateStyleProperty(rule, StylePropertyId);
        ValueSetter(property, inlineStyleSheet, Value);

        Element.UpdateInlineRule(inlineStyleSheet, rule);
        Element.IncrementVersion(VersionChangeType.StyleSheet | VersionChangeType.Styles);

        EditorUtility.SetDirty(visualTreeAsset);
        EditorUtility.SetDirty(inlineStyleSheet);
        UIElementsUtility.MarkVisualTreeAssetAsChanged(visualTreeAsset);
        if (StageUtility.GetCurrentStage() is VisualElementEditingStage stage)
        {
            stage.PanelElement.FrameUpdate();
        }
    }

    private static StyleRule GetOrCreateRule(VisualElementAsset vea, StyleSheet styleSheet)
    {
        if (vea.ruleIndex >= 0)
            return styleSheet.rules[vea.ruleIndex];

        var ruleIndex = styleSheet.rules.Length;
        var rule = styleSheet.AddRule();
        vea.ruleIndex = ruleIndex;
        return rule;
    }

    private static StyleProperty GetOrCreateStyleProperty(StyleRule rule, StylePropertyId stylePropertyId)
    {
        return rule.FindLastProperty(stylePropertyId) ?? rule.AddProperty(stylePropertyId);
    }
}
