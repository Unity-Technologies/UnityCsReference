// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class VisualElementInspector : VisualElement, IDisposable
{
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
            return new VisualElementInspector();
        }
    }

    public static readonly BindingId ElementProperty = nameof(Element);
    public static readonly BindingId EditFlagsProperty = nameof(EditFlags);

    public const string UssClass = "unity-visual-element-inspector";
    public const string HeaderUssClass = UssClass + "__header";
    public const string OpenInBuilderUssClass = UssClass + "__open-in-builder-button";
    public const string BindingsSectionViewClass = UssClass + "__bindings-section";
    public const string StyleInspectorClass = UssClass + "__style-inspector";
    public const string AttributesInspectorClass = UssClass + "__attributes-inspector";
    public const string ReadonlyAttributesInspectorClass = AttributesInspectorClass + "--readonly";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/VisualElementInspector.uxml";
    private const string k_StyleSheetDark = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorDark.uss";
    private const string k_StyleSheetLight = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorLight.uss";

    private VisualElement m_Element;
    private VisualElementEditFlags m_EditFlags;

    private readonly VisualElementHeader m_Header;
    private readonly OpenInBuilderElement m_OpenInBuilder;
    private readonly VisualElementBindingsInspectorElement m_BindingsInspector;
    private readonly VisualElementAttributesInspectorElement m_AttributesInspector;
    private readonly StyleInspectorElement m_StyleInspector;
    private StyleInspectorDefaultContent m_StyleInspectorDefaultContent;
    private VariablesInspector m_VariablesSection;

    [CreateProperty]
    public VisualElement Element
    {
        get => m_Element;
        set
        {
            if (m_Element == value)
                return;
            m_Element = value;

            m_Header.Element = m_Element;

            if (m_Element == null)
            {
                m_OpenInBuilder.VisualTreeAsset = null;
                m_StyleInspector.Target = new StyleInspectorTarget(null);
                m_AttributesInspector.Target = null;
                m_VariablesSection?.Refresh(null);
            }
            else
            {
                var visualTreeAsset = m_Element.visualTreeAssetSource
                    ? m_Element.visualTreeAssetSource
                    : m_Element.GetFirstAncestorWhere(ve => ve.visualTreeAssetSource)?.visualTreeAssetSource;
                m_OpenInBuilder.VisualTreeAsset = visualTreeAsset;
                m_StyleInspector.Target = new StyleInspectorTarget(m_Element);
                m_AttributesInspector.Target = m_Element;
                m_VariablesSection?.Refresh(GetInlineStyleRule());
            }
            NotifyPropertyChanged(ElementProperty);
        }
    }

    [CreateProperty]
    public VisualElementEditFlags EditFlags
    {
        get => m_EditFlags;
        set
        {
            if (m_EditFlags == value)
                return;
            SetEditFlags(value);
            NotifyPropertyChanged(EditFlagsProperty);
        }
    }

    public VisualElementInspector()
    {
        AddToClassList(UssClass);

        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        vta.CloneTree(this);

        var styleSheetPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var styleSheet = EditorGUIUtility.Load(styleSheetPath) as StyleSheet;
        styleSheets.Add(styleSheet);

        m_Header = this.Q<VisualElementHeader>(className:HeaderUssClass);
        m_OpenInBuilder = this.Q<OpenInBuilderElement>(className:OpenInBuilderUssClass);

        // Attributes
        m_AttributesInspector = this.Q<VisualElementAttributesInspectorElement>();

        // Bindings
        m_BindingsInspector = this.Q<VisualElementBindingsInspectorElement>();
        m_BindingsInspector.ShareContext(m_AttributesInspector);

        // Styles
        m_StyleInspector = this.Q<StyleInspectorElement>(className:StyleInspectorClass);

        SetEditFlags(VisualElementEditFlags.None);
    }

    void SetEditFlags(VisualElementEditFlags editFlags)
    {
        m_EditFlags = editFlags;

        switch (m_EditFlags)
        {
            // Complete read-only.
            case VisualElementEditFlags.None:
                m_Header.SetEnabled(false);
                m_AttributesInspector.IsReadOnly = true;
                m_AttributesInspector.EnableInClassList(ReadonlyAttributesInspectorClass, true);
                m_OpenInBuilder.style.display = DisplayStyle.Flex;
                m_StyleInspector.IsReadOnly = true;
                if (m_VariablesSection != null)
                    m_VariablesSection.enabledSelf = false;
                break;
            // Attribute overrides
            case VisualElementEditFlags.Attributes:
                m_Header.SetEnabled(false);
                m_AttributesInspector.IsReadOnly = false;
                m_AttributesInspector.EnableInClassList(ReadonlyAttributesInspectorClass, false);
                m_OpenInBuilder.style.display = DisplayStyle.None;
                m_StyleInspector.IsReadOnly = true;
                if (m_VariablesSection != null)
                    m_VariablesSection.enabledSelf = false;
                break;
            // Should almost never get here.
            case VisualElementEditFlags.Styles:
                m_Header.SetEnabled(false);
                m_AttributesInspector.IsReadOnly = true;
                m_AttributesInspector.EnableInClassList(ReadonlyAttributesInspectorClass, true);
                m_OpenInBuilder.style.display = DisplayStyle.None;
                m_StyleInspector.IsReadOnly = false;
                if (m_VariablesSection != null)
                    m_VariablesSection.enabledSelf = true;
                break;
            // Full editing
            case VisualElementEditFlags.Attributes | VisualElementEditFlags.Styles:
                m_Header.SetEnabled(true);
                m_AttributesInspector.IsReadOnly = false;
                m_AttributesInspector.EnableInClassList(ReadonlyAttributesInspectorClass, false);
                m_OpenInBuilder.style.display = DisplayStyle.None;
                m_StyleInspector.IsReadOnly = false;
                if (m_VariablesSection != null)
                    m_VariablesSection.enabledSelf = true;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override void HandleEventBubbleUp(EventBase evt)
    {
        switch (evt)
        {
            case AttachToPanelEvent attachToPanelEvent:
            {
                if (attachToPanelEvent.destinationPanel == null)
                    return;
                m_StyleInspector.contentContainer.Add(m_StyleInspectorDefaultContent = StyleInspectorDefaultContent.Get());
                InitializeVariablesSection();
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
            m_VariablesSection.enabledSelf = (EditFlags & VisualElementEditFlags.Styles) == VisualElementEditFlags.Styles;
            m_VariablesSection.Refresh(GetInlineStyleRule(), GetOrCreateInlineStyleRule);
            return;
        }

        m_StyleInspectorDefaultContent.contentWasGenerated += OnDefaultContentGenerated;
    }

    void OnDefaultContentGenerated(StyleInspectorDefaultContent content)
    {
        content.contentWasGenerated -= OnDefaultContentGenerated;

        m_VariablesSection = content.Q<VariablesInspector>();
        if (m_VariablesSection != null)
        {
            m_VariablesSection.enabledSelf = (EditFlags & VisualElementEditFlags.Styles) == VisualElementEditFlags.Styles;
            m_VariablesSection.Refresh(GetInlineStyleRule(), GetOrCreateInlineStyleRule);
        }
    }

    StyleRule GetInlineStyleRule()
    {
        if (m_Element?.visualTreeAssetSource?.inlineSheet == null)
            return null;

        var index = m_Element?.visualElementAsset?.ruleIndex;
        if (index == null || index < 0)
            return null;

        var styleSheet = m_Element.visualTreeAssetSource.inlineSheet;
        if (styleSheet.rules.Length <= index)
            return null;

        return styleSheet.rules[index.Value];
    }

    StyleRule GetOrCreateInlineStyleRule()
    {
        if (m_Element?.visualElementAsset == null || m_Element.visualTreeAssetSource == null)
            return null;

        var vta = m_Element.visualTreeAssetSource;
        var vea = m_Element.visualElementAsset;
        return vta.GetOrCreateInlineStyleRule(vea);
    }

    public void Dispose()
    {
        m_AttributesInspector.Context.Dispose();
    }
}
