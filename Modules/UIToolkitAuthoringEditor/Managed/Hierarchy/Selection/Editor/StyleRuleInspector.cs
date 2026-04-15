// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class StyleRuleInspector : VisualElement
{
    class __SelectorElement : VisualElement;

    [Serializable]
    public new class UxmlSerializedData : VisualElement.UxmlSerializedData
    {
        /// <summary>
        /// This is used by the code generator when a custom control is using the <see cref="UxmlElementAttribute"/>. You should not need to call it.
        /// </summary>
        [Conditional("UNITY_EDITOR"), RegisterUxmlCache]
        public new static void Register()
        {
            UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), Array.Empty<UxmlAttributeNames>(), true);
        }

        public override object CreateInstance()
        {
            return new StyleRuleInspector();
        }
    }

    public static readonly BindingId StyleRuleProperty = nameof(StyleRule);
    public static readonly BindingId IsReadOnlyProperty = nameof(IsReadOnly);

    public const string UssClass = "unity-style-rule-inspector";
    public const string StyleInspectorClass = UssClass + "__style-inspector";
    public const string ReadOnlyStyleInspectorClass = "unity-visual-element-inspector__style-inspector--readonly";
    public const string HeaderUssClass = "unity-style-rule-inspector__header";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/StyleRuleInspector.uxml";
    private const string k_StyleSheetDark = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorDark.uss";
    private const string k_StyleSheetLight = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorLight.uss";

    internal const string VariablesFoldoutName = "style-inspector__style-section__variables";

    private readonly StyleInspectorElement m_StyleInspector;
    private StyleInspectorDefaultContent m_StyleInspectorDefaultContent;
    private readonly StyleRuleHeader m_Header;
    private VariablesInspector m_VariablesSection;

    private StyleRule m_StyleRule;
    private bool m_IsReadOnly;

    private PanelElement m_PanelElement;
    private __SelectorElement m_Element;

    [CreateProperty]
    public StyleRule StyleRule
    {
        get => m_StyleRule;
        set
        {
            m_StyleRule = value;

            if (m_StyleRule == null)
            {
                m_StyleInspector.Target = default;
                m_Header.Rule = null;
                m_VariablesSection?.Refresh(null);
            }
            else
            {
                if (m_Element != null)
                {
                    SetSelectorElementInlineStyles();
                }

                m_StyleInspector.Target = new StyleInspectorTarget(m_Element, m_StyleRule.styleSheet, m_StyleRule);
                m_Header.Rule = m_StyleRule;
                m_VariablesSection?.Refresh(m_StyleRule);
            }

            NotifyPropertyChanged(StyleRuleProperty);
        }
    }

    [CreateProperty]
    public bool IsReadOnly
    {
        get => m_IsReadOnly;
        set
        {
            if (m_IsReadOnly == value && m_StyleInspector.IsReadOnly == value)
                return;
            m_IsReadOnly = value;
            m_StyleInspector.IsReadOnly = value;
            m_StyleInspector.EnableInClassList(ReadOnlyStyleInspectorClass, value);
            NotifyPropertyChanged(IsReadOnlyProperty);
        }
    }

    public StyleRuleInspector()
    {
        AddToClassList(UssClass);

        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        vta.CloneTree(this);

        var styleSheetPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var styleSheet = EditorGUIUtility.Load(styleSheetPath) as StyleSheet;
        styleSheets.Add(styleSheet);
        m_StyleInspector = this.Q<StyleInspectorElement>(className:StyleInspectorClass);
        IsReadOnly = false;

        m_Header = this.Q<StyleRuleHeader>(className: HeaderUssClass);
    }

    protected override void HandleEventBubbleUp(EventBase evt)
    {
        switch (evt)
        {
            case AttachToPanelEvent attachToPanelEvent:
            {
                if (attachToPanelEvent.destinationPanel == null)
                    return;
                m_PanelElement = new PanelElement();
                m_PanelElement.CreateSubPanel();
                m_Element = new __SelectorElement();
                m_PanelElement.SubPanel.visualTree.Add(m_Element);

                SetSelectorElementInlineStyles();

                m_StyleInspector.contentContainer.Add(m_StyleInspectorDefaultContent = StyleInspectorDefaultContent.Get());
                InitializeVariablesSection();

                m_StyleInspector.Target = new StyleInspectorTarget(m_Element, m_StyleRule?.styleSheet, m_StyleRule);
                m_Header.Rule = m_StyleRule;
                break;
            }
            case DetachFromPanelEvent detachFromPanelEvent:
            {
                if (detachFromPanelEvent.originPanel == null)
                    return;
                if (m_StyleInspectorDefaultContent != null)
                    m_StyleInspectorDefaultContent.contentWasGenerated -= OnDefaultContentGenerated;
                m_StyleInspectorDefaultContent?.RemoveFromHierarchy();
                StyleInspectorDefaultContent.Release(m_StyleInspectorDefaultContent);
                m_StyleInspectorDefaultContent = null;

                m_VariablesSection = null;

                m_StyleInspector.Target = default;

                m_PanelElement.DestroySubPanel();
                m_Element = null;
                m_PanelElement = null;
                break;
            }
        }
        base.HandleEventBubbleUp(evt);
    }

    void InitializeVariablesSection()
    {
        m_VariablesSection = m_StyleInspectorDefaultContent.Q<VariablesInspector>();
        if (m_VariablesSection != null)
        {
            m_VariablesSection.Refresh(m_StyleRule);
            return;
        }

        m_StyleInspectorDefaultContent.contentWasGenerated += OnDefaultContentGenerated;
    }

    void OnDefaultContentGenerated(StyleInspectorDefaultContent content)
    {
        content.contentWasGenerated -= OnDefaultContentGenerated;

        m_VariablesSection = content.Q<VariablesInspector>();
        if (m_VariablesSection == null)
            return;

        m_VariablesSection.Refresh(m_StyleRule);
    }

    void SetSelectorElementInlineStyles()
    {
        if (m_StyleRule == null || m_StyleRule.styleSheet == null || m_Element == null)
            return;

        m_Element.styleSheets.Clear();
        m_Element.styleSheets.Add(m_StyleRule.styleSheet);

        m_Element.SetInlineRule(m_StyleRule.styleSheet, m_StyleRule);

        m_Element.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet);

        // Force immediate style resolution to update the element's variableContext
        m_PanelElement.SubPanel.ApplyStyles();
    }
}
