// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class ExtractInlineStylesToNewClassCommand : Command<ExtractInlineStylesToNewClassCommand>
{
    public const string CommandUndoName = "Extract inline styles to new class";

    public static ExtractInlineStylesToNewClassCommand GetPooled(object source, VisualElementAsset vea, VisualTreeAsset vta, StyleSheet ss, string className)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.ElementAsset = vea;
        cmd.VisualTreeAsset = vta;
        cmd.MainStyleSheet = ss;
        cmd.ClassName = className;
        return cmd;
    }

    public static void Execute(object source, VisualElementAsset vea, VisualTreeAsset vta, StyleSheet ss,
        string className)
    {
        using var command = GetPooled(source, vea, vta, ss, className);
        UICommandQueue.Execute(command);
    }

    public VisualElementAsset ElementAsset { get; private set; }
    public VisualTreeAsset VisualTreeAsset { get; private set; }
    public StyleSheet MainStyleSheet { get; private set; }
    public string ClassName { get; private set; }

    public override string UndoName => CommandUndoName;

    protected override void Init()
    {
        ElementAsset = null;
        VisualTreeAsset = null;
        MainStyleSheet = null;
        ClassName = null;
        base.Init();
    }

    public override bool Validate()
    {
        return ElementAsset != null &&
               VisualTreeAsset != null &&
               VisualTreeAsset.inlineSheet != null &&
               ElementAsset.ruleIndex >= 0 &&
               ElementAsset.ruleIndex < VisualTreeAsset.inlineSheet.rules.Length &&
               MainStyleSheet != null &&
               !string.IsNullOrEmpty(ClassName);
    }

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(VisualTreeAsset);
        context.RecordUndo(VisualTreeAsset.inlineSheet);
        context.RecordUndo(MainStyleSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        var selectorString = "." + ClassName;

        var newRule = MainStyleSheet.AddRule();
        newRule.AddSelector(selectorString);

        var fromRule = VisualTreeAsset.inlineSheet.rules[ElementAsset.ruleIndex];
        TransferInlineStylePropertiesToRule(MainStyleSheet, newRule, VisualTreeAsset.inlineSheet, fromRule);

        ElementAsset.AddStyleClass(ClassName);
        return CommandExecutionStatus.Success;
    }

    static void TransferInlineStylePropertiesToRule(
        StyleSheet mainStyleSheet,
        StyleRule newRule,
        StyleSheet inlineSheet,
        StyleRule fromRule)
    {
        using var _ = ListPool<StyleProperty>.Get(out var properties);
        properties.AddRange(fromRule.properties);

        foreach (var property in properties)
        {
            var existing = newRule.FindLastProperty(property.name);
            if (existing != null)
                newRule.RemoveProperty(existing);

            var toProperty = newRule.AddProperty(property.name);
            StyleSheetUtility.TransferStylePropertyHandles(inlineSheet, property, mainStyleSheet, toProperty);
            fromRule.RemoveProperty(property);
        }
    }
}
