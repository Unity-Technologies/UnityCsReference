// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class SetStyleSheetImportCommand : Command<SetStyleSheetImportCommand>
{
    const string CommandUndoName = "Change stylesheet import";

    public static SetStyleSheetImportCommand GetPooled(object source, StyleSheet styleSheet, int index, StyleSheet importedStyleSheet)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.StyleSheet = styleSheet;
        cmd.Index = index;
        cmd.ImportedStyleSheet = importedStyleSheet;
        return cmd;
    }

    public static void Execute(object source, StyleSheet styleSheet, int index, StyleSheet importedStyleSheet)
    {
        using var command = GetPooled(source, styleSheet, index, importedStyleSheet);
        UICommandQueue.Execute(command);
    }

    public StyleSheet StyleSheet { get; private set; }
    public int Index { get; private set; }
    public StyleSheet ImportedStyleSheet { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.StylingContext;

    protected override void Init()
    {
        base.Init();
        StyleSheet = null;
        Index = -1;
        ImportedStyleSheet = null;
    }

    public override bool Validate() => StyleSheet != null && ImportedStyleSheet != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(StyleSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        StyleSheet.SetStyleSheetImportAtIndex(Index, ImportedStyleSheet);
        return CommandExecutionStatus.Success;
    }
}
