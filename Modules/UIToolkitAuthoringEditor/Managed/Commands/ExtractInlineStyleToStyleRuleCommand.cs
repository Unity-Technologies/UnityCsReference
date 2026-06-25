// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class ExtractInlineStyleToStyleRuleCommand : Command<ExtractInlineStyleToStyleRuleCommand>
{
    public const string CommandUndoName = "Extract inline style to selector";

    public static ExtractInlineStyleToStyleRuleCommand GetPooled(object source, VisualElementAsset vea,
        VisualTreeAsset vta, StyleSheet targetStyleSheet, StyleRule targetRule, string propertyName = null)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.ElementAsset = vea;
        cmd.VisualTreeAsset = vta;
        cmd.TargetStyleSheet = targetStyleSheet;
        cmd.TargetRule = targetRule;
        cmd.PropertyName = propertyName;
        return cmd;
    }

    public static void Execute(object source, VisualElementAsset vea, VisualTreeAsset vta,
        StyleSheet targetStyleSheet, StyleRule targetRule, string propertyName = null)
    {
        using var command = GetPooled(source, vea, vta, targetStyleSheet, targetRule, propertyName);
        UICommandQueue.Execute(command);
    }

    public VisualElementAsset ElementAsset { get; private set; }
    public VisualTreeAsset VisualTreeAsset { get; private set; }
    public StyleSheet TargetStyleSheet { get; private set; }
    public StyleRule TargetRule { get; private set; }
    public string PropertyName { get; private set; }

    public override string UndoName => CommandUndoName;

    protected override void Init()
    {
        ElementAsset = null;
        VisualTreeAsset = null;
        TargetStyleSheet = null;
        TargetRule = null;
        PropertyName = null;
        base.Init();
    }

    public override bool Validate()
    {
        if (ElementAsset == null || VisualTreeAsset == null || VisualTreeAsset.inlineSheet == null)
            return false;
        if (ElementAsset.ruleIndex < 0 || ElementAsset.ruleIndex >= VisualTreeAsset.inlineSheet.rules.Length)
            return false;
        if (TargetStyleSheet == null || TargetRule == null)
            return false;
        if (PropertyName != null)
            return VisualTreeAsset.inlineSheet.rules[ElementAsset.ruleIndex].FindLastProperty(PropertyName) != null;
        return true;
    }

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(VisualTreeAsset.inlineSheet);
        context.RecordUndo(TargetStyleSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        var fromRule = VisualTreeAsset.inlineSheet.rules[ElementAsset.ruleIndex];

        if (PropertyName != null)
        {
            var property = fromRule.FindLastProperty(PropertyName);
            var existing = TargetRule.FindLastProperty(PropertyName);
            if (existing != null)
                TargetRule.RemoveProperty(existing);

            var toProperty = TargetRule.AddProperty(PropertyName);
            StyleSheetUtility.TransferStylePropertyHandles(VisualTreeAsset.inlineSheet, property, TargetStyleSheet, toProperty);
            fromRule.RemoveProperty(property);
        }
        else
        {
            using var _ = ListPool<StyleProperty>.Get(out var properties);
            properties.AddRange(fromRule.properties);

            foreach (var property in properties)
            {
                var existing = TargetRule.FindLastProperty(property.name);
                if (existing != null)
                    TargetRule.RemoveProperty(existing);

                var toProperty = TargetRule.AddProperty(property.name);
                StyleSheetUtility.TransferStylePropertyHandles(VisualTreeAsset.inlineSheet, property, TargetStyleSheet, toProperty);
                fromRule.RemoveProperty(property);
            }
        }

        return CommandExecutionStatus.Success;
    }
}
