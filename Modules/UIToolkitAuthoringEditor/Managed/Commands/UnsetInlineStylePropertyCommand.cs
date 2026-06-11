// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

internal sealed class UnsetInlineStylePropertyCommand : Command<UnsetInlineStylePropertyCommand>
{
    const string CommandUndoName = "Unset inline style property";

    public static UnsetInlineStylePropertyCommand GetPooled(
        object source,
        VisualElement element,
        StylePropertyId stylePropertyId)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.Element = element;
        cmd.StylePropertyId = stylePropertyId;
        return cmd;
    }

    public static void Execute(object source,
        VisualElement element,
        StylePropertyId stylePropertyId)
    {
        using var command = GetPooled(source, element, stylePropertyId);
        UICommandQueue.Execute(command);
    }

    public VisualElement Element { get; private set; }
    public StylePropertyId StylePropertyId { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Styling;

    protected override void Init()
    {
        base.Init();
        Element = null;
        StylePropertyId = default;
    }

    public override bool Validate() =>
        Element != null
        && Element.visualElementAsset != null
        && Element.visualTreeAssetSource != null;

    public override void Prepare(in PrepareContext context)
    {
        var visualTreeAsset = Element.visualTreeAssetSource;
        context.RecordUndo(visualTreeAsset);
        context.RecordUndo(visualTreeAsset.inlineSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        var visualTreeAsset = Element.visualTreeAssetSource;
        var inlineStyleSheet = visualTreeAsset.inlineSheet;
        if (inlineStyleSheet == null)
            return CommandExecutionStatus.Success;

        var vea = Element.visualElementAsset;
        if (vea.ruleIndex < 0)
            return CommandExecutionStatus.Success;

        RemoveBindingCommand.Execute(Source, Element, StylePropertyId);

        var rule = inlineStyleSheet.rules[vea.ruleIndex];
        var property = rule.FindLastProperty(StylePropertyId);
        if (property != null)
        {
            rule.RemoveProperty(property);
            Element.UpdateInlineRule(inlineStyleSheet, rule);
            Element.IncrementVersion(VersionChangeType.StyleSheet | VersionChangeType.Styles);
        }

        return CommandExecutionStatus.Success;
    }
}
