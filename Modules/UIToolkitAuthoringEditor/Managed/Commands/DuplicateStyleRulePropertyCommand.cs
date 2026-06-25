// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class DuplicateStyleRulePropertyCommand : Command<DuplicateStyleRulePropertyCommand>
{
    const string CommandUndoName = "Duplicate style property";

    public static DuplicateStyleRulePropertyCommand GetPooled(
        object source,
        StyleSheet styleSheet,
        StyleRule rule,
        StyleProperty sourceProperty,
        string newName)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.StyleSheet = styleSheet;
        cmd.Rule = rule;
        cmd.SourceProperty = sourceProperty;
        cmd.NewName = newName;
        return cmd;
    }

    public static void Execute(object source,
        StyleSheet styleSheet,
        StyleRule rule,
        StyleProperty sourceProperty,
        string newName)
    {
        using var command = GetPooled(source, styleSheet, rule, sourceProperty, newName);
        UICommandQueue.Execute(command);
    }

    public StyleSheet StyleSheet { get; private set; }
    public StyleRule Rule { get; private set; }
    public StyleProperty SourceProperty { get; private set; }
    public string NewName { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Styling | CommandCategory.Variables;

    protected override void Init()
    {
        base.Init();
        StyleSheet = null;
        Rule = null;
        SourceProperty = null;
        NewName = null;
    }

    public override bool Validate() => StyleSheet != null && Rule != null && SourceProperty != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(StyleSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        var newProperty = Rule.AddProperty(NewName);
        StyleSheetUtility.TransferStylePropertyHandles(StyleSheet, SourceProperty, StyleSheet, newProperty);
        return CommandExecutionStatus.Success;
    }
}
