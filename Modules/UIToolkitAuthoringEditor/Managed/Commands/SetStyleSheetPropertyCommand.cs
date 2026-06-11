// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

internal sealed class SetStyleSheetPropertyCommand<T> : Command<SetStyleSheetPropertyCommand<T>>
{
    const string CommandUndoName = "Set style property";

    public static SetStyleSheetPropertyCommand<T> GetPooled(
        object source,
        StyleSheet styleSheet,
        StyleRule rule,
        StylePropertyId stylePropertyId,
        Action<StyleProperty, StyleSheet, T> valueSetter,
        T value)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.StyleSheet = styleSheet;
        cmd.Rule = rule;
        cmd.StylePropertyId = stylePropertyId;
        cmd.ValueSetter = valueSetter;
        cmd.Value = value;
        return cmd;
    }

    public static void Execute(
        object source,
        StyleSheet styleSheet,
        StyleRule rule,
        StylePropertyId stylePropertyId,
        Action<StyleProperty, StyleSheet, T> valueSetter,
        T value)
    {
        using var command = GetPooled(source, styleSheet, rule, stylePropertyId, valueSetter, value);
        UICommandQueue.Execute(command);
    }

    public StyleSheet StyleSheet { get; private set; }
    public StyleRule Rule { get; private set; }
    public StylePropertyId StylePropertyId { get; private set; }
    public Action<StyleProperty, StyleSheet, T> ValueSetter { get; private set; }
    public T Value { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Styling;

    protected override void Init()
    {
        base.Init();
        StyleSheet = null;
        Rule = null;
        StylePropertyId = default;
        ValueSetter = null;
        Value = default;
    }

    public override bool Validate() => StyleSheet != null && Rule != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(StyleSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        var property = GetOrCreateStyleProperty(Rule, StylePropertyId);
        ValueSetter(property, StyleSheet, Value);
        return CommandExecutionStatus.Success;
    }

    static StyleProperty GetOrCreateStyleProperty(StyleRule rule, StylePropertyId stylePropertyId)
    {
        return rule.FindLastProperty(stylePropertyId) ?? rule.AddProperty(stylePropertyId);
    }
}
