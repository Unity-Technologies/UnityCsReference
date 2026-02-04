// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct StyleInspectorTarget
{
    public readonly VisualElement Element;
    public readonly StyleRule Rule;
    public readonly StyleSheet Sheet;
    public readonly StyleDiff.ContextType Type;

    public StyleInspectorTarget(VisualElement element)
    {
        Element = element;
        Rule = null;
        Sheet = null;
        Type = StyleDiff.ContextType.VisualElement;
    }

    public StyleInspectorTarget(VisualElement element, StyleSheet sheet, StyleRule rule)
    {
        Element = element;
        Rule = rule;
        Sheet = sheet;
        Type = StyleDiff.ContextType.StyleSheet;
    }

    public bool IsValid()
    {
        switch (Type)
        {
            case StyleDiff.ContextType.None:
                return false;
            case StyleDiff.ContextType.VisualElement:
                return Element != null;
            case StyleDiff.ContextType.StyleSheet:
                return Element != null && Rule != null && Sheet != null;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

[UxmlElement]
internal sealed class StyleInspectorElement : VisualElement, IVisualElementChangeProcessor
{
    public static BindingId TargetProperty = nameof(Target);
    public static BindingId ContentAssetProperty = nameof(ContentAsset);
    public static BindingId IsReadOnlyProperty = nameof(IsReadOnly);

    public const string UssClassName = "unity-style-inspector";
    public const string InspectorFlexColumnModeClassName = UssClassName + "--flex-column";
    public const string InspectorFlexColumnReverseModeClassName = UssClassName + "--flex-column-reverse";
    public const string InspectorFlexRowModeClassName = UssClassName + "--flex-row";
    public const string InspectorFlexRowReverseModeClassName = UssClassName + "--flex-row-reverse";

    [Serializable]
    public new class UxmlSerializedData : VisualElement.UxmlSerializedData
    {
        /// <summary>
        /// This is used by the code generator when a custom control is using the <see cref="UxmlElementAttribute"/>. You should not need to call it.
        /// </summary>
        [Conditional("UNITY_EDITOR"), RegisterUxmlCache]
        public new static void Register()
        {
            UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
            {
                new(nameof(ContentAsset), "content-asset"),
            }, true);
        }

#pragma warning disable 649

        [SerializeField] VisualTreeAsset ContentAsset;

        [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags ContentAsset_UxmlAttributeFlags;
#pragma warning restore 649

        [ExcludeFromDocs]
        public override object CreateInstance()
        {
            return new StyleInspectorElement();
        }

        [ExcludeFromDocs]
        public override void Deserialize(object obj)
        {
            var e = (StyleInspectorElement)obj;
            if (ShouldWriteAttributeValue(ContentAsset_UxmlAttributeFlags))
                e.ContentAsset = ContentAsset;
        }
    }

    public class AuthoringContext : IDisposable
    {
        StyleDiff m_StyleDiff = new ();
        StyleDiffAdditionalDataFlags m_StyleDiffFlags = StyleDiffAdditionalDataFlags.All;
        bool m_IsReadonly = true;

        public StyleDiff StyleDiff => m_StyleDiff;

        public bool IsReadOnly
        {
            get => m_IsReadonly;
            set => m_IsReadonly = value;
        }

        public void Refresh(VisualElement element)
        {
            m_StyleDiff.RefreshElement(element, m_StyleDiffFlags);
        }

        public void RefreshRule(VisualElement element, StyleSheet styleSheet, StyleRule rule)
        {
            m_StyleDiff.RefreshRule(element, styleSheet, rule, m_StyleDiffFlags);
        }

        public void Dispose()
        {
            m_StyleDiff?.Dispose();
        }
    }

    AuthoringContext m_Context;

    StyleInspectorTarget m_Target;

    [CreateProperty]
    public StyleInspectorTarget Target
    {
        get => m_Target;
        set
        {
            if (m_Target == value)
                return;
            ReleaseSelection(m_Target.Element);
            m_Target = value;
            AcquireSelection(m_Target.Element);
            NotifyPropertyChanged(TargetProperty);
        }
    }

    private VisualTreeAsset m_ContentAsset;

    [UxmlAttribute]
    public VisualTreeAsset ContentAsset
    {
        get => m_ContentAsset;
        set
        {
            if (m_ContentAsset == value)
                return;
            m_ContentAsset = value;

            RegenerateContent();
            NotifyPropertyChanged(ContentAssetProperty);
        }
    }

    private bool m_IsReadOnly = true;

    [CreateProperty]
    public bool IsReadOnly
    {
        get => m_IsReadOnly;
        set
        {
            if (m_IsReadOnly == value)
                return;
            m_IsReadOnly = value;
            if (m_Context != null)
                m_Context.IsReadOnly = value;
            NotifyPropertyChanged(IsReadOnlyProperty);
        }
    }

    private VisualElement m_ContentContainer;

    public override VisualElement contentContainer { get; }

    public StyleInspectorElement()
        :this(null)
    {
    }

    public StyleInspectorElement(VisualTreeAsset content)
    {
        AddToClassList(UssClassName);
        m_ContentContainer = new VisualElement();
        hierarchy.Add(m_ContentContainer);

        contentContainer = m_ContentContainer;
        ContentAsset = content;

        BindAdvancedTextUI();
    }

    [EventInterest(typeof(AttachToPanelEvent), typeof(DetachFromPanelEvent))]
    protected override void HandleEventBubbleUp(EventBase evt)
    {
        switch (evt)
        {
            case AttachToPanelEvent attachToPanelEvent:
            {
                if (attachToPanelEvent.destinationPanel == null)
                    return;
                m_Context = new AuthoringContext
                {
                    IsReadOnly = IsReadOnly
                };
                dataSource = m_Context;
                if (Target.Element != null)
                    AcquireSelection(Target.Element);
                else
                    Refresh();

                break;
            }
            case DetachFromPanelEvent detachFromPanelEvent:
            {
                if (detachFromPanelEvent.originPanel == null)
                    return;
                if (Target.Element != null)
                    ReleaseSelection(Target.Element);
                dataSource = null;
                m_Context.Dispose();
                UnbindAdvancedTextUI();
                break;
            }
        }
        base.HandleEventBubbleUp(evt);
    }

    private void ReleaseSelection(VisualElement element)
    {
        if (element?.panel is Panel targetPanel)
            targetPanel.UnregisterChangeProcessor(this);
    }

    private void AcquireSelection(VisualElement element)
    {
        if (element?.panel is Panel targetPanel)
        {
            targetPanel.RegisterChangeProcessor(this);
            Refresh();
        }
    }

    public void BeginProcessing(BaseVisualElementPanel targetPanel)
    {
        // Intentionally left empty.
    }

    public void ProcessChanges(BaseVisualElementPanel targetPanel, AuthoringChanges changes)
    {
        if (changes.styleChanged.Contains(Target.Element) ||
            changes.bindingContextChanged.Contains(Target.Element) ||
            changes.stylingContextChanged.Contains(Target.Element))
        {
            Refresh();
        }
    }

    public void EndProcessing(BaseVisualElementPanel targetPanel)
    {
        // Intentionally left empty.
    }

    private void Refresh()
    {
        if (m_Context != null && m_Target.IsValid())
        {
            switch (m_Target.Type)
            {
                case StyleDiff.ContextType.VisualElement:
                    m_Context.Refresh(m_Target.Element);
                    break;
                case StyleDiff.ContextType.StyleSheet:
                    m_Context.RefreshRule(m_Target.Element, m_Target.Sheet, m_Target.Rule);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            UpdateFlexColumnGlobalState(m_Context.StyleDiff.flexDirection.computedValue);
            UpdateAdvancedTextHelpBox(UIToolkitProjectSettings.enableAdvancedText);
        }
        else
        {
            UpdateFlexColumnGlobalState(FlexDirection.Column);
        }
    }

    private void UpdateFlexColumnGlobalState(FlexDirection newDirection)
    {
        EnableInClassList(InspectorFlexColumnModeClassName, newDirection == FlexDirection.Column);
        EnableInClassList(InspectorFlexColumnReverseModeClassName, newDirection == FlexDirection.ColumnReverse);
        EnableInClassList(InspectorFlexRowModeClassName, newDirection == FlexDirection.Row);
        EnableInClassList(InspectorFlexRowReverseModeClassName, newDirection == FlexDirection.RowReverse);
    }

    void BindAdvancedTextUI()
    {
        UIToolkitProjectSettings.onEnableAdvancedTextChanged += UpdateAdvancedTextHelpBox;
    }

    void UnbindAdvancedTextUI()
    {
        UIToolkitProjectSettings.onEnableAdvancedTextChanged -= UpdateAdvancedTextHelpBox;
    }

    internal void UpdateAdvancedTextHelpBox(bool enable)
    {
        var atgWarningRow = contentContainer.Q<OverrideRow>("atg-warning-row");
        var autoSizeWarningRow = contentContainer.Q<OverrideRow>("autosize-warning-row");

        if (atgWarningRow == null || autoSizeWarningRow == null)
            return;

        var isAdvanced = m_Context.StyleDiff.unityTextGenerator.computedValue == TextGeneratorType.Advanced;

        var showAtgWarning = isAdvanced && !enable;
        atgWarningRow.style.display = showAtgWarning ? DisplayStyle.Flex : DisplayStyle.None;

        var bestFit = m_Context.StyleDiff.unityTextAutoSize.computedValue.mode == TextAutoSizeMode.BestFit;
        var showAutoSizeWarning = !isAdvanced && bestFit;
        autoSizeWarningRow.style.display = showAutoSizeWarning ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void RegenerateContent()
    {
        m_ContentContainer.Clear();
        ContentAsset?.CloneTree(m_ContentContainer);
    }
}
