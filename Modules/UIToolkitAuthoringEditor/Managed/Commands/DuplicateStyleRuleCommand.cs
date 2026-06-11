// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class DuplicateStyleRuleCommand : Command<DuplicateStyleRuleCommand>
{
    const string CommandUndoName = "Duplicate style rule";

    public static DuplicateStyleRuleCommand GetPooled(object source, StyleRule[] toDuplicate)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.ToDuplicateStyleRules = toDuplicate;
        return cmd;
    }

    public static void Execute(object source, StyleRule[] toDuplicate)
    {
        using var command = GetPooled(source, toDuplicate);
        UICommandQueue.Execute(command);
    }

    public StyleRule[] ToDuplicateStyleRules { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.StylingContext;

    protected override void Init()
    {
        base.Init();
        ToDuplicateStyleRules = null;
    }

    public override bool Validate()
    {
        if (ToDuplicateStyleRules == null)
            return false;
        foreach (var styleRule in ToDuplicateStyleRules)
        {
            if (styleRule == null || styleRule.styleSheet == null)
                return false;
        }
        return true;
    }

    public override void Prepare(in PrepareContext context)
    {
        using var _ = HashSetPool<StyleSheet>.Get(out var styleSheets);
        foreach (var rule in ToDuplicateStyleRules)
        {
            if (styleSheets.Add(rule.styleSheet))
                context.RecordUndo(rule.styleSheet);
        }
    }

    public override CommandExecutionStatus Execute()
    {
        foreach (var originalRule in ToDuplicateStyleRules)
        {
            var styleSheet = originalRule.styleSheet;
            var ruleIndex = Array.IndexOf(styleSheet.rules, originalRule);
            if (ruleIndex == -1)
                continue;

            var targetIndex = ruleIndex + 1;

            var newRule = styleSheet.AddRuleAtIndex(targetIndex, null);
            StyleSheetExtensions.SwallowStyleRule(styleSheet, newRule, styleSheet, originalRule);
        }

        return CommandExecutionStatus.Success;
    }
}
