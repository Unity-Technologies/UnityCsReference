// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class RemoveStyleSheetCommand : Command<RemoveStyleSheetCommand>
{
    const string CommandUndoName = "Remove style sheet";

    public static RemoveStyleSheetCommand GetPooled(object source, VisualTreeAsset visualTreeAsset, StyleSheet styleSheet)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.VisualTreeAsset = visualTreeAsset;
        cmd.StyleSheet = styleSheet;
        return cmd;
    }

    public static void Execute(object source, VisualTreeAsset visualTreeAsset, StyleSheet styleSheet)
    {
        using var command = GetPooled(source, visualTreeAsset, styleSheet);
        UICommandQueue.Execute(command);
    }

    public VisualTreeAsset VisualTreeAsset { get; private set; }
    public StyleSheet StyleSheet { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.StylingContext | CommandCategory.Hierarchy;

    protected override void Init()
    {
        base.Init();
        VisualTreeAsset = null;
        StyleSheet = null;
    }

    public override bool Validate() => VisualTreeAsset != null && StyleSheet != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(VisualTreeAsset);
    }

    public override CommandExecutionStatus Execute()
    {
        var rootElement = VisualTreeAsset.visualTreeNoAlloc;
        if (rootElement == null || rootElement.stylesheets.Count == 0)
            return CommandExecutionStatus.Success;

        rootElement.stylesheets.Remove(StyleSheet);
        return CommandExecutionStatus.Success;
    }
}
