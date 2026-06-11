// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class UnsetAllStyleSheetPropertiesCommand : Command<UnsetAllStyleSheetPropertiesCommand>
{
    const string CommandUndoName = "Unset all style properties";

    public static UnsetAllStyleSheetPropertiesCommand GetPooled(object source, StyleSheet styleSheet, StyleRule rule)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.StyleSheet = styleSheet;
        cmd.Rule = rule;
        return cmd;
    }

    public static void Execute(object source, StyleSheet styleSheet, StyleRule rule)
    {
        using var command = GetPooled(source, styleSheet, rule);
        UICommandQueue.Execute(command);
    }

    public StyleSheet StyleSheet { get; private set; }
    public StyleRule Rule { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Styling;

    protected override void Init()
    {
        base.Init();
        StyleSheet = null;
        Rule = null;
    }

    public override bool Validate() => StyleSheet != null && Rule != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(StyleSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        Rule.ClearProperties();
        return CommandExecutionStatus.Success;
    }
}
