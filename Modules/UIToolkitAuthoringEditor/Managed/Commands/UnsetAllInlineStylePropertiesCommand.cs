// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class UnsetAllInlineStylePropertiesCommand : Command<UnsetAllInlineStylePropertiesCommand>
{
    const string CommandUndoName = "Unset all inline style properties";

    public static UnsetAllInlineStylePropertiesCommand GetPooled(object source, VisualElement element)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.Element = element;
        return cmd;
    }

    public static void Execute(object source, VisualElement element)
    {
        using var command = GetPooled(source, element);
        UICommandQueue.Execute(command);
    }

    public VisualElement Element { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Styling;

    protected override void Init()
    {
        base.Init();
        Element = null;
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

        var rule = inlineStyleSheet.rules[vea.ruleIndex];
        rule.ClearProperties();

        ClearAllStylePropertyBindingsCommand.Execute(Source, Element);

        Element.UpdateInlineRule(inlineStyleSheet, rule);
        Element.IncrementVersion(VersionChangeType.StyleSheet | VersionChangeType.Styles);

        return CommandExecutionStatus.Success;
    }
}
