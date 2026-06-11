// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class RenameStyleRuleCommand : Command<RenameStyleRuleCommand>
{
    const string CommandUndoName = "Rename style rule";

    public static RenameStyleRuleCommand GetPooled(object source, string[] newSelectors, StyleRule toUpdateStyleRule)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.NewSelectors = newSelectors;
        cmd.ToUpdateStyleRule = toUpdateStyleRule;
        return cmd;
    }

    public static void Execute(object source, string[] newSelectors, StyleRule toUpdateStyleRule)
    {
        using var command = GetPooled(source, newSelectors, toUpdateStyleRule);
        UICommandQueue.Execute(command);
    }

    public string[] NewSelectors { get; private set; }
    public StyleRule ToUpdateStyleRule { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.StylingContext;

    protected override void Init()
    {
        base.Init();
        NewSelectors = null;
        ToUpdateStyleRule = null;
    }

    public override bool Validate() => NewSelectors != null && ToUpdateStyleRule != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(ToUpdateStyleRule.styleSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        foreach (var selector in ToUpdateStyleRule.complexSelectors)
            ToUpdateStyleRule.RemoveSelector(selector);

        foreach (var selector in NewSelectors)
        {
            if (!ToUpdateStyleRule.TryAddSelector(selector, out _))
                return CommandExecutionStatus.ExecutionFailed;
        }

        return CommandExecutionStatus.Success;
    }
}
