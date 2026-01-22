// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

internal readonly record struct UnsetInlineStylePropertyCommand
{
    const string CommandUndoName = "Unset inline style property";

    readonly VisualElement Element;
    readonly StylePropertyId StylePropertyId;

    public UnsetInlineStylePropertyCommand(
        VisualElement element,
        StylePropertyId stylePropertyId)
    {
        Element = element;
        StylePropertyId = stylePropertyId;
    }

    public void Execute()
    {
        Assert.IsNotNull(Element);
        Assert.IsNotNull(Element.visualElementAsset);
        Assert.IsNotNull(Element.visualTreeAssetSource);

        var visualTreeAsset = Element.visualTreeAssetSource;
        Undo.RegisterCompleteObjectUndo(visualTreeAsset, CommandUndoName);

        var inlineStyleSheet = visualTreeAsset.inlineSheet;
        if (inlineStyleSheet == null)
            return;

        Undo.RegisterCompleteObjectUndo(inlineStyleSheet, CommandUndoName);

        var vea = Element.visualElementAsset;
        if (vea.ruleIndex < 0)
            return;

        var removeBindingCommand = new RemoveBindingCommand(Element, StylePropertyId);
        removeBindingCommand.Execute();

        var rule = inlineStyleSheet.rules[vea.ruleIndex];
        var property = rule.FindLastProperty(StylePropertyId);
        if (property != null)
        {
            rule.RemoveProperty(property);
            Element.UpdateInlineRule(inlineStyleSheet, rule);
            Element.IncrementVersion(VersionChangeType.StyleSheet | VersionChangeType.Styles);

            EditorUtility.SetDirty(visualTreeAsset);
            EditorUtility.SetDirty(inlineStyleSheet);
        }
    }
}
