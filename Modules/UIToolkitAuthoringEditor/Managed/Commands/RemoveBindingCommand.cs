// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

internal sealed class RemoveBindingCommand : Command<RemoveBindingCommand>
{
    const string CommandUndoName = "Remove style property binding";

    public static RemoveBindingCommand GetPooled(object source, VisualElement visualElement, StylePropertyId styleProperty)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.Element = visualElement;
        cmd.StylePropertyId = styleProperty;

        var ussName = StylePropertyUtil.stylePropertyIdToPropertyName[styleProperty];
        var csharpName = StylePropertyUtil.ussNameToCSharpName.GetValueOrDefault(ussName, ussName);
        cmd.BindingId = $"style.{csharpName}";
        return cmd;
    }

    public static void Execute(object source, VisualElement visualElement, StylePropertyId styleProperty)
    {
        using var command = GetPooled(source, visualElement, styleProperty);
        UICommandQueue.Execute(command);
    }

    public static RemoveBindingCommand GetPooled(object source, VisualElement visualElement, string bindingPath)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.Element = visualElement;
        cmd.BindingId = bindingPath;
        cmd.StylePropertyId = StylePropertyId.Unknown;
        return cmd;
    }

    public static void Execute(object source, VisualElement visualElement, string bindingPath)
    {
        using var command = GetPooled(source, visualElement, bindingPath);
        UICommandQueue.Execute(command);
    }

    public VisualElement Element { get; private set; }
    public StylePropertyId StylePropertyId { get; private set; }
    public BindingId BindingId { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Attributes;

    protected override void Init()
    {
        base.Init();
        Element = null;
        StylePropertyId = StylePropertyId.Unknown;
        BindingId = default;
    }

    public override bool Validate() =>
        Element != null
        && Element.visualElementAsset != null
        && Element.visualElementAsset.visualTreeAsset != null;

    public override void Prepare(in PrepareContext context)
    {
        // Optimistically record; Execute is a no-op when no binding exists.
        context.RecordUndo(Element.visualElementAsset.visualTreeAsset);
    }

    public override CommandExecutionStatus Execute()
    {
        var visualElementAsset = Element.visualElementAsset;
        var visualTreeAsset = visualElementAsset.visualTreeAsset;

        var hasBinding = visualElementAsset.FindUxmlBinding(BindingId) != null;
        if (!hasBinding)
            return CommandExecutionStatus.Success;

        visualElementAsset.RemoveBinding(BindingId);
        Element.ClearBinding(BindingId);

        // Clean up the serializedData
        var uxmlSerializedDataDescription = UxmlSerializedDataRegistry.GetDescription(Element.fullTypeName);
        var attribute = uxmlSerializedDataDescription.FindAttributeWithUxmlName("Bindings");
        attribute?.SyncSerializedData(Element, visualElementAsset.serializedData);

        if (StylePropertyId != StylePropertyId.Unknown)
            ResetInlineStyle();

        Element.IncrementVersion(VersionChangeType.Bindings);
        return CommandExecutionStatus.Success;
    }

    void ResetInlineStyle()
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
