// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class RenameStyleRulePropertyCommand : Command<RenameStyleRulePropertyCommand>
{
    const string CommandUndoName = "Rename style rule property";

    public static RenameStyleRulePropertyCommand GetPooled(object source, StyleSheet styleSheet, StyleProperty property, string newName)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.StyleSheet = styleSheet;
        cmd.Property = property;
        cmd.NewName = newName;
        return cmd;
    }

    public static void Execute(object source, StyleSheet styleSheet, StyleProperty property, string newName)
    {
        using var command = GetPooled(source, styleSheet, property, newName);
        UICommandQueue.Execute(command);
    }

    public StyleSheet StyleSheet { get; private set; }
    public StyleProperty Property { get; private set; }
    public string NewName { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.StylingContext;

    protected override void Init()
    {
        base.Init();
        StyleSheet = null;
        Property = null;
        NewName = null;
    }

    public override bool Validate() => StyleSheet != null && Property != null && NewName != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(StyleSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        Property.name = NewName;
        return CommandExecutionStatus.Success;
    }
}
