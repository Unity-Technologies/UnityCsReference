// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class ReorderStyleRulePropertiesCommand : Command<ReorderStyleRulePropertiesCommand>
{
    const string CommandUndoName = "Reorder style rule properties";

    public static ReorderStyleRulePropertiesCommand GetPooled(object source, StyleSheet styleSheet, StyleRule rule, StyleProperty[] newPropertyOrder)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.StyleSheet = styleSheet;
        cmd.Rule = rule;
        cmd.NewPropertyOrder = newPropertyOrder;
        return cmd;
    }

    public static void Execute(object source, StyleSheet styleSheet, StyleRule rule, StyleProperty[] newPropertyOrder)
    {
        using var command = GetPooled(source, styleSheet, rule, newPropertyOrder);
        UICommandQueue.Execute(command);
    }

    public StyleSheet StyleSheet { get; private set; }
    public StyleRule Rule { get; private set; }
    public StyleProperty[] NewPropertyOrder { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.StylingContext | CommandCategory.Variables;

    protected override void Init()
    {
        base.Init();
        StyleSheet = null;
        Rule = null;
        NewPropertyOrder = null;
    }

    public override bool Validate() => StyleSheet != null && Rule != null && NewPropertyOrder != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(StyleSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        var variableIndices = new List<int>();
        for (var i = 0; i < Rule.properties.Length; i++)
        {
            for (var j = 0; j < NewPropertyOrder.Length; j++)
            {
                if (Rule.properties[i] == NewPropertyOrder[j])
                {
                    variableIndices.Add(i);
                    break;
                }
            }
        }

        for (var i = 0; i < variableIndices.Count && i < NewPropertyOrder.Length; i++)
            Rule.properties[variableIndices[i]] = NewPropertyOrder[i];

        return CommandExecutionStatus.Success;
    }
}
