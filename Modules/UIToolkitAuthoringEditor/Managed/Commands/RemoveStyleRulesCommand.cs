// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class RemoveStyleRulesCommand : Command<RemoveStyleRulesCommand>
{
    const string CommandUndoName = "Remove style rules";

    public static RemoveStyleRulesCommand GetPooled(object source, StyleRule[] toRemoveStyleRules)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.ToRemoveStyleRules = toRemoveStyleRules;
        return cmd;
    }

    public static void Execute(object source, StyleRule[] toRemoveStyleRules)
    {
        using var command = GetPooled(source, toRemoveStyleRules);
        UICommandQueue.Execute(command);
    }

    public StyleRule[] ToRemoveStyleRules { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.StylingContext;

    protected override void Init()
    {
        base.Init();
        ToRemoveStyleRules = null;
    }

    public override bool Validate()
    {
        if (ToRemoveStyleRules == null || ToRemoveStyleRules.Length == 0)
            return false;
        foreach (var rule in ToRemoveStyleRules)
        {
            if (rule.styleSheet == null)
                return false;
        }
        return true;
    }

    public override void Prepare(in PrepareContext context)
    {
        using var _ = HashSetPool<StyleSheet>.Get(out var set);
        foreach (var rule in ToRemoveStyleRules)
        {
            var styleSheet = rule.styleSheet;
            if (styleSheet && set.Add(styleSheet))
                context.RecordUndo(styleSheet);
        }
    }

    public override CommandExecutionStatus Execute()
    {
        foreach (var rule in ToRemoveStyleRules)
            rule.styleSheet.RemoveRule(rule);

        return CommandExecutionStatus.Success;
    }
}
