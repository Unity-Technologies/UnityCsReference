// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

internal readonly record struct RemoveBindingCommand
{
    const string CommandUndoName = "Remove style property binding";

    readonly VisualElement Element;
    readonly StylePropertyId StylePropertyId;
    readonly BindingId BindingId;

    public RemoveBindingCommand(VisualElement visualElement, StylePropertyId styleProperty)
    {
        Element = visualElement;
        StylePropertyId = styleProperty;

        var ussName = StylePropertyUtil.stylePropertyIdToPropertyName[StylePropertyId];
        var csharpName = StylePropertyUtil.ussNameToCSharpName.GetValueOrDefault(ussName, ussName);

        BindingId = $"style.{csharpName}";
    }

    public void Execute()
    {
        var visualElementAsset = Element.visualElementAsset;
        var visualTreeAsset = Element.visualElementAsset.visualTreeAsset;

        Assert.IsNotNull(Element);
        Assert.IsNotNull(visualElementAsset);
        Assert.IsNotNull(visualTreeAsset);

        var hasBinding = visualElementAsset.FindUxmlBinding(BindingId) != null;

        if (hasBinding)
        {
            Undo.RegisterCompleteObjectUndo(visualTreeAsset, CommandUndoName);

            visualElementAsset.RemoveBinding(BindingId);
            Element.ClearBinding(BindingId);

            // Clean up the serializedData
            var uxmlSerializedDataDescription = UxmlSerializedDataRegistry.GetDescription(Element.fullTypeName);
            var attribute = uxmlSerializedDataDescription.FindAttributeWithUxmlName("Bindings");
            attribute?.SyncSerializedData(Element, visualElementAsset.serializedData);

            ResetInlineStyle();

            Undo.RegisterCompleteObjectUndo(visualTreeAsset, CommandUndoName);

            Element.IncrementVersion(VersionChangeType.Bindings);
            EditorUtility.SetDirty(visualTreeAsset);
            UIElementsUtility.MarkVisualTreeAssetAsChanged(visualTreeAsset);
        }
    }

    internal void ResetInlineStyle()
    {
        StyleDebug.SetInlineKeyword(Element.style, StylePropertyId, StyleKeyword.Null);

        var vea = Element.visualElementAsset;
        var vta = Element.visualElementAsset.visualTreeAsset;

        if (vea.ruleIndex < 0)
            return;

        var inlineStyleSheet = vta.GetOrCreateInlineStyleSheet();
        var rule = inlineStyleSheet.rules[vea.ruleIndex];
        Element.UpdateInlineRule(inlineStyleSheet, rule);
        Element.IncrementVersion(VersionChangeType.Styles);
    }
}
