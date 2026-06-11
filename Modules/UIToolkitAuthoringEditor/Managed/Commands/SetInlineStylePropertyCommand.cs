// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

internal sealed class SetInlineStylePropertyCommand<T> : Command<SetInlineStylePropertyCommand<T>>
{
    const string CommandUndoName = "Set inline style property";

    public static SetInlineStylePropertyCommand<T> GetPooled(
        object source,
        VisualElement element,
        StylePropertyId stylePropertyId,
        Action<StyleProperty, StyleSheet, T> valueSetter,
        T value)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.Element = element;
        cmd.StylePropertyId = stylePropertyId;
        cmd.ValueSetter = valueSetter;
        cmd.Value = value;
        return cmd;
    }

    public static void Execute(
        object source,
        VisualElement element,
        StylePropertyId stylePropertyId,
        Action<StyleProperty, StyleSheet, T> valueSetter,
        T value)
    {
        using var command = GetPooled(source, element, stylePropertyId, valueSetter, value);
        UICommandQueue.Execute(command);
    }

    public VisualElement Element { get; private set; }
    public StylePropertyId StylePropertyId { get; private set; }
    public Action<StyleProperty, StyleSheet, T> ValueSetter { get; private set; }
    public T Value { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Styling;

    protected override void Init()
    {
        base.Init();
        Element = null;
        StylePropertyId = default;
        ValueSetter = null;
        Value = default;
    }

    public override bool Validate() =>
        Element != null
        && Element.visualElementAsset != null
        && Element.visualTreeAssetSource != null;

    public override void Prepare(in PrepareContext context)
    {
        var visualTreeAsset = Element.visualTreeAssetSource;
        context.RecordUndo(visualTreeAsset);
        context.RecordUndo(visualTreeAsset.GetOrCreateInlineStyleSheet());
    }

    public override CommandExecutionStatus Execute()
    {
        var visualTreeAsset = Element.visualTreeAssetSource;
        var inlineStyleSheet = visualTreeAsset.GetOrCreateInlineStyleSheet();

        var vea = Element.visualElementAsset;
        var rule = GetOrCreateRule(vea, inlineStyleSheet);
        var property = GetOrCreateStyleProperty(rule, StylePropertyId);
        ValueSetter(property, inlineStyleSheet, Value);

        Element.UpdateInlineRule(inlineStyleSheet, rule);
        Element.IncrementVersion(VersionChangeType.StyleSheet | VersionChangeType.Styles);

        return CommandExecutionStatus.Success;
    }

    static StyleRule GetOrCreateRule(VisualElementAsset vea, StyleSheet styleSheet)
    {
        if (vea.ruleIndex >= 0)
            return styleSheet.rules[vea.ruleIndex];

        var ruleIndex = styleSheet.rules.Length;
        var rule = styleSheet.AddRule();
        vea.ruleIndex = ruleIndex;
        return rule;
    }

    static StyleProperty GetOrCreateStyleProperty(StyleRule rule, StylePropertyId stylePropertyId)
    {
        return rule.FindLastProperty(stylePropertyId) ?? rule.AddProperty(stylePropertyId);
    }
}
