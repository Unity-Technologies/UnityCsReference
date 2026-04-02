// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

internal readonly record struct ClearAllStylePropertyBindingsCommand
{
    const string CommandUndoName = "Clear all style property bindings";

    readonly VisualElement Element;

    public ClearAllStylePropertyBindingsCommand(VisualElement visualElement)
    {
        Element = visualElement;
    }

    public void Execute()
    {
        var visualElementAsset = Element.visualElementAsset;
        var visualTreeAsset = Element.visualElementAsset.visualTreeAsset;

        Assert.IsNotNull(Element);
        Assert.IsNotNull(visualElementAsset);
        Assert.IsNotNull(visualTreeAsset);

        using var styleBindings = ListPool<string>.Get(out var bindingIds);
        visualElementAsset.FindAllStyleBindings(bindingIds);

        if (bindingIds.Count > 0)
        {
            Undo.RegisterCompleteObjectUndo(visualTreeAsset, CommandUndoName);

            foreach (var bindingId in bindingIds)
            {
                visualElementAsset.RemoveBinding(bindingId);
                Element.ClearBinding(bindingId);
            }

            // Clean up the serializedData
            var uxmlSerializedDataDescription = UxmlSerializedDataRegistry.GetDescription(Element.fullTypeName);
            var attribute = uxmlSerializedDataDescription.FindAttributeWithUxmlName("Bindings");
            attribute?.SyncSerializedData(Element, visualElementAsset.serializedData);

            ResetAllInlineStyles(bindingIds);

            Element.IncrementVersion(VersionChangeType.Bindings | VersionChangeType.Styles);
            EditorUtility.SetDirty(visualTreeAsset);
            UIElementsUtility.MarkVisualTreeAssetAsChanged(visualTreeAsset);
        }
    }

    internal void ResetAllInlineStyles(List<string> bindingIds)
    {
        var vea = Element.visualElementAsset;
        var vta = Element.visualElementAsset.visualTreeAsset;
        if (vea.ruleIndex < 0)
            return;

        var style = Element.style;

        foreach (var bindingId in bindingIds)
        {
            if (bindingId.StartsWith("style."))
            {
                var csharpName = bindingId.Substring(6);
                StylePropertyUtil.cSharpNameToUssName.TryGetValue(csharpName, out var styleName);
                var id = StyleDebug.GetStylePropertyIdFromName(styleName);
                StyleDebug.SetInlineKeyword(style, id, StyleKeyword.Null);
            }
        }

        var inlineStyleSheet = vta.GetOrCreateInlineStyleSheet();
        var rule = inlineStyleSheet.rules[vea.ruleIndex];
        Element.UpdateInlineRule(inlineStyleSheet, rule);
    }
}
