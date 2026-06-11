// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class RemoveStyleSheetImportCommand : Command<RemoveStyleSheetImportCommand>
{
    const string CommandUndoName = "Remove import from stylesheet";

    public static RemoveStyleSheetImportCommand GetPooled(object source, StyleSheet styleSheet, int index)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.StyleSheet = styleSheet;
        cmd.Index = index;
        return cmd;
    }

    public static void Execute(object source, StyleSheet styleSheet, int index)
    {
        using var command = GetPooled(source, styleSheet, index);
        UICommandQueue.Execute(command);
    }

    public StyleSheet StyleSheet { get; private set; }
    public int Index { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.StylingContext;

    protected override void Init()
    {
        base.Init();
        StyleSheet = null;
        Index = -1;
    }

    public override bool Validate() =>
        StyleSheet != null && Index >= 0 && Index < StyleSheet.imports.Length;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(StyleSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        StyleSheet.RemoveImport(Index);
        return CommandExecutionStatus.Success;
    }
}
