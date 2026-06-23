// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
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

    public static void Execute(object source, StyleSheet styleSheet, string selectorString)
    {
        using var command = GetPooled(source, styleSheet, selectorString);
        UICommandQueue.Execute(command);
    }

    const string CommandUndoName = "Add style rule";

    public StyleSheet StyleSheet { get; private set; }
    public string SelectorString { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category { get; } = CommandCategory.StylingContext;

    protected override void Init()
    {
        base.Init();
        StyleSheet = null;
        SelectorString = null;
    }

    public override bool Validate() => StyleSheet != null && StyleSheetExtensions.ValidateStyleRule(SelectorString, out _);

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(StyleSheet);
    }


    public override CommandExecutionStatus Execute()
    {
        var styleRule = StyleSheet.AddRule();
        foreach (var selector in StyleSheetExtensions.SplitSelectors(SelectorString))
        {
            if (!styleRule.TryAddSelector(selector, out _))
                return CommandExecutionStatus.ExecutionFailed;
        }
        return CommandExecutionStatus.Success;
    }
}
