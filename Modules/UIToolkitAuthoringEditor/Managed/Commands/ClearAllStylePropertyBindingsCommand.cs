// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

internal sealed class ClearAllStylePropertyBindingsCommand : Command<ClearAllStylePropertyBindingsCommand>
{
    const string CommandUndoName = "Clear all style property bindings";

    public static ClearAllStylePropertyBindingsCommand GetPooled(object source, VisualElement visualElement)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.Element = visualElement;
        return cmd;
    }

    public static void Execute(object source, VisualElement visualElement)
    {
        using var command = GetPooled(source, visualElement);
        UICommandQueue.Execute(command);
    }

    public VisualElement Element { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Attributes;

    protected override void Init()
    {
        base.Init();
        Element = null;
    }

    public override bool Validate() =>
        Element != null
        && Element.visualElementAsset != null
        && Element.visualElementAsset.visualTreeAsset != null;

    public override void Prepare(in PrepareContext context)
    {
        // Undo is registered only when there are bindings to clear; we record optimistically.
        context.RecordUndo(Element.visualElementAsset.visualTreeAsset);
    }

    public override CommandExecutionStatus Execute()
    {
        var visualElementAsset = Element.visualElementAsset;

        using var styleBindings = ListPool<string>.Get(out var bindingIds);
        visualElementAsset.FindAllStyleBindings(bindingIds);

        if (bindingIds.Count == 0)
            return CommandExecutionStatus.Success;

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
        return CommandExecutionStatus.Success;
    }

    void ResetAllInlineStyles(List<string> bindingIds)
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
