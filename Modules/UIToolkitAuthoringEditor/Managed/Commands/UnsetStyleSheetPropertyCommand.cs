// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

internal sealed class UnsetStyleSheetPropertyCommand : Command<UnsetStyleSheetPropertyCommand>
{
    const string CommandUndoName = "Unset style property";

    public static UnsetStyleSheetPropertyCommand GetPooled(
        object source,
        StyleSheet styleSheet,
        StyleRule rule,
        StylePropertyId stylePropertyId)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.StyleSheet = styleSheet;
        cmd.Rule = rule;
        cmd.StylePropertyId = stylePropertyId;
        return cmd;
    }

    public static void Execute(object source,
        StyleSheet styleSheet,
        StyleRule rule,
        StylePropertyId stylePropertyId)
    {
        using var command = GetPooled(source, styleSheet, rule, stylePropertyId);
        UICommandQueue.Execute(command);
    }

    public StyleSheet StyleSheet { get; private set; }
    public StyleRule Rule { get; private set; }
    public StylePropertyId StylePropertyId { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Styling;

    protected override void Init()
    {
        base.Init();
        StyleSheet = null;
        Rule = null;
        StylePropertyId = default;
    }

    public override bool Validate() => StyleSheet != null && Rule != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(StyleSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        var property = Rule.FindLastProperty(StylePropertyId);
        if (property != null)
            Rule.RemoveProperty(property);

        return CommandExecutionStatus.Success;
    }
}
