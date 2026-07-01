// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class RenameStyleRulePropertyCommand : Command<RenameStyleRulePropertyCommand>
{
    const string CommandUndoName = "Rename style rule property";

    public static RenameStyleRulePropertyCommand GetPooled(object source, StyleSheet styleSheet, StyleProperty property, string newName, VisualTreeAsset visualTreeAsset = null)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.StyleSheet = styleSheet;
        cmd.Property = property;
        cmd.NewName = newName;
        cmd.VisualTreeAsset = visualTreeAsset;
        return cmd;
    }

    public static void Execute(object source, StyleSheet styleSheet, StyleProperty property, string newName, VisualTreeAsset visualTreeAsset = null)
    {
        using var command = GetPooled(source, styleSheet, property, newName, visualTreeAsset);
        UICommandQueue.Execute(command);
    }

    public StyleSheet StyleSheet { get; private set; }
    public StyleProperty Property { get; private set; }
    public string NewName { get; private set; }
    public VisualTreeAsset VisualTreeAsset { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.StylingContext | CommandCategory.Variables;

    protected override void Init()
    {
        base.Init();
        StyleSheet = null;
        Property = null;
        NewName = null;
        VisualTreeAsset = null;
    }

    public override bool Validate() => StyleSheet != null && Property != null && NewName != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(StyleSheet);
        context.RecordUndo(VisualTreeAsset);
    }

    public override CommandExecutionStatus Execute()
    {
        Property.SetName(StyleSheet, NewName);
        return CommandExecutionStatus.Success;
    }
}
