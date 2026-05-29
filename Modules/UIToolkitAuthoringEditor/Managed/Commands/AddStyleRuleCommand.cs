// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class AddStyleRuleCommand : Command<AddStyleRuleCommand>
{
    public static AddStyleRuleCommand GetPooled(object source, StyleSheet styleSheet, string selectorString)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.StyleSheet = styleSheet;
        cmd.SelectorString = selectorString;
        return cmd;
    }

    const string CommandUndoName = "Add style rule";

    public StyleSheet StyleSheet { get; private set; }
    public string SelectorString { get; private set; }

    public override string UndoName => CommandUndoName;

    protected override void Init()
    {
        base.Init();
        StyleSheet = null;
        SelectorString = null;
    }

    public override bool Validate() => StyleSheet != null && !string.IsNullOrWhiteSpace(SelectorString);

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(StyleSheet);
    }


    public override CommandExecutionStatus Execute()
    {
        var styleRule = StyleSheet.AddRule();
        styleRule.AddSelector(SelectorString);
        return CommandExecutionStatus.Success;
    }
}
