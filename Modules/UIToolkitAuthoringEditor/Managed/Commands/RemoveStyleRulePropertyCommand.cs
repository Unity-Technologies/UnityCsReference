// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class RemoveStyleRulePropertyCommand : Command<RemoveStyleRulePropertyCommand>
{
    const string CommandUndoName = "Remove style rule property";

    public static RemoveStyleRulePropertyCommand GetPooled(object source, StyleSheet styleSheet, StyleRule rule, StyleProperty property)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.StyleSheet = styleSheet;
        cmd.Rule = rule;
        cmd.Property = property;
        return cmd;
    }

    public static void Execute(object source, StyleSheet styleSheet, StyleRule rule, StyleProperty property)
    {
        using var command = GetPooled(source, styleSheet, rule, property);
        UICommandQueue.Execute(command);
    }

    public StyleSheet StyleSheet { get; private set; }
    public StyleRule Rule { get; private set; }
    public StyleProperty Property { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.StylingContext | CommandCategory.Variables;

    protected override void Init()
    {
        base.Init();
        StyleSheet = null;
        Rule = null;
        Property = null;
    }

    public override bool Validate() => StyleSheet != null && Rule != null && Property != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(StyleSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        Rule.RemoveProperty(Property);
        return CommandExecutionStatus.Success;
    }
}
