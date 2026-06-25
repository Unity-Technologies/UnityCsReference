// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class AddStyleRulePropertyCommand : Command<AddStyleRulePropertyCommand>
{
    const string CommandUndoName = "Add style rule property";

    public static AddStyleRulePropertyCommand GetPooled(
        object source,
        StyleSheet styleSheet,
        StyleRule rule,
        string propertyName,
        VariablesInspector.VariableType variableType)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.StyleSheet = styleSheet;
        cmd.Rule = rule;
        cmd.PropertyName = propertyName;
        cmd.VariableType = variableType;
        return cmd;
    }

    public static void Execute(object source,
        StyleSheet styleSheet,
        StyleRule rule,
        string propertyName,
        VariablesInspector.VariableType variableType)
    {
        using var command = GetPooled(source, styleSheet, rule, propertyName, variableType);
        UICommandQueue.Execute(command);
    }

    public StyleSheet StyleSheet { get; private set; }
    public StyleRule Rule { get; private set; }
    public string PropertyName { get; private set; }
    public VariablesInspector.VariableType VariableType { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Styling | CommandCategory.Variables;

    protected override void Init()
    {
        base.Init();
        StyleSheet = null;
        Rule = null;
        PropertyName = null;
        VariableType = default;
    }

    public override bool Validate() => StyleSheet != null && Rule != null && PropertyName != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(StyleSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        var property = Rule.AddProperty(PropertyName);
        ResetStyleRulePropertyValueCommand.SetPropertyToDefaultValue(StyleSheet, property, VariableType);
        return CommandExecutionStatus.Success;
    }
}
