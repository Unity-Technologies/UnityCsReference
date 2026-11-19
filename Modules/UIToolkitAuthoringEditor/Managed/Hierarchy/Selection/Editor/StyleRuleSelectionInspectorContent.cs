// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// The header and the inspector of a <see cref="StyleRuleSelection"/>.
/// </summary>
sealed class StyleRuleSelectionInspectorContent : UISelectionInspectorContent
{
    readonly StyleRuleHeader m_Header;
    readonly StyleRuleInspector m_Inspector;

    public override VisualElement Header => m_Header;

    public override UIInspector Inspector => m_Inspector;

    public new StyleRuleSelection Target
    {
        get => (StyleRuleSelection)base.Target;
        set => base.Target = value;
    }

    /// <param name="context">The context of the rules the content shows, which supplies the canvas theme.</param>
    public StyleRuleSelectionInspectorContent(AuthoringContext context)
    {
        m_Header = new StyleRuleHeader { name = "Header" };
        m_Inspector = new StyleRuleInspector { Context = context };
        m_Inspector.InitializeSearchField(m_Header.SearchField);
    }

    protected override StyleInspectorAnimationRecordingContext CreateRecordingContext()
        => StyleInspectorAnimationRecordingContext.TryCreateForRule(Target.StyleRule);

    protected override void OnTargetChanged(UISelectionObject previous)
    {
        var target = Target;
        if (target == null)
        {
            m_Header.ClearBinding(StyleRuleHeader.RuleProperty);
            m_Header.ClearBinding(StyleRuleHeader.IsReadOnlyProperty);
            m_Inspector.ClearBinding(StyleRuleInspector.StyleRuleProperty);
            m_Inspector.ClearBinding(StyleRuleInspector.IsReadOnlyProperty);
            m_Header.Rule = null;
            m_Inspector.StyleRule = null;
            return;
        }

        m_Header.Rule = target.StyleRule;
        m_Header.IsReadOnly = target.IsReadOnly;
        m_Inspector.StyleRule = target.StyleRule;
        m_Inspector.IsReadOnly = target.IsReadOnly;

        m_Header.SetBinding(StyleRuleHeader.RuleProperty, CreateBinding(target, StyleRuleSelection.StyleRuleProperty));
        m_Header.SetBinding(StyleRuleHeader.IsReadOnlyProperty, CreateBinding(target, UISelectionObject.IsReadOnlyProperty));
        m_Inspector.SetBinding(StyleRuleInspector.StyleRuleProperty, CreateBinding(target, StyleRuleSelection.StyleRuleProperty));
        m_Inspector.SetBinding(StyleRuleInspector.IsReadOnlyProperty, CreateBinding(target, UISelectionObject.IsReadOnlyProperty));
    }

    static DataBinding CreateBinding(StyleRuleSelection target, BindingId property) => new()
    {
        dataSource = target,
        dataSourcePath = property,
        bindingMode = BindingMode.ToTarget,
    };
}
