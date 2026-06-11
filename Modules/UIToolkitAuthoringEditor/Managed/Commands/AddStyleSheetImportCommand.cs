// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class AddStyleSheetImportCommand : Command<AddStyleSheetImportCommand>
{
    const string CommandUndoName = "Add import to stylesheet";

    public static AddStyleSheetImportCommand GetPooled(object source, StyleSheet styleSheet)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.StyleSheet = styleSheet;
        return cmd;
    }

    public static void Execute(object source, StyleSheet styleSheet)
    {
        using var command = GetPooled(source, styleSheet);
        UICommandQueue.Execute(command);
    }

    public StyleSheet StyleSheet { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.StylingContext;

    protected override void Init()
    {
        base.Init();
        StyleSheet = null;
    }

    public override bool Validate() => StyleSheet != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(StyleSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        StyleSheet.AddImportAtIndex(-1, new StyleSheet.ImportStruct());
        return CommandExecutionStatus.Success;
    }
}
