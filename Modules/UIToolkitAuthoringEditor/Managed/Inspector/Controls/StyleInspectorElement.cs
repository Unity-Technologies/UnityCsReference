// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

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
internal sealed partial class StyleInspectorElement : VisualElement, IVisualElementChangeProcessor
{
    public static BindingId TargetProperty = nameof(Target);
    public static BindingId ContentAssetProperty = nameof(ContentAsset);
    public static BindingId IsReadOnlyProperty = nameof(IsReadOnly);

    public const string UssClassName = "unity-style-inspector";
    public const string InspectorFlexColumnModeClassName = UssClassName + "--flex-column";
    public const string InspectorFlexColumnReverseModeClassName = UssClassName + "--flex-column-reverse";
    public const string InspectorFlexRowModeClassName = UssClassName + "--flex-row";
    public const string InspectorFlexRowReverseModeClassName = UssClassName + "--flex-row-reverse";

    public class AuthoringContext : IDisposable, INotifyBindablePropertyChanged, IDataSourceViewHashProvider
    {
        StyleDiff m_StyleDiff;
        StyleDiffAdditionalDataFlags m_StyleDiffFlags = StyleDiffAdditionalDataFlags.All;
        bool m_IsReadonly = true;
        StyleInspectorAnimationRecordingContext m_AnimationController;

        public StyleDiff StyleDiff => m_StyleDiff;

        /// <summary>
        /// When non-null, style field changes in VisualElement mode are routed to the animation recording pipeline,
        /// and field enabled state reflects which longhands are recordable for the active clip.
        /// Must be set before any ProcessChange runs — <see cref="AnimationMode.InAnimationRecording"/> must agree.
        /// </summary>
        internal StyleInspectorAnimationRecordingContext AnimationController
        {
            get => m_AnimationController;
            set
            {
                if (ReferenceEquals(m_AnimationController, value))
                    return;
                m_AnimationController = value;
                RefreshAllProperties();
            }
        }

        public bool IsReadOnly
        {
            get => m_IsReadonly;
            set
            {
                if (m_IsReadonly == value)
                    return;
                m_IsReadonly = value;
                RefreshAllProperties();
            }
        }

        public AuthoringContext()
        {
            m_StyleDiff = new StyleDiff();
            m_StyleDiff.propertyChanged += PropagateProperty;
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
            if (m_StyleDiff != null)
            {
                m_StyleDiff.Dispose();
                m_StyleDiff.propertyChanged -= PropagateProperty;
            }
        }

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        public long GetViewHashCode() => m_StyleDiff.GetViewHashCode();

        void PropagateProperty(object sender, BindablePropertyChangedEventArgs e)
        {
            propertyChanged?.Invoke(this, e);
        }

        public void RefreshAllProperties()
        {
            m_StyleDiff.MarkAllPropertiesDirty();
        }
    }

    AuthoringContext m_Context;
    AuthoringVariableEditingContext m_VariableEditingContext;
    StyleInspectorAnimationRecordingContext m_AnimationController;

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

    /// <summary>
    /// The variable editing context for this inspector. Available after the element is attached to a panel.
    /// </summary>
    internal IVariableEditingContext VariableEditingContext => m_VariableEditingContext;

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
                    IsReadOnly = IsReadOnly,
                    AnimationController = m_AnimationController
                };
                m_VariableEditingContext = new AuthoringVariableEditingContext(this);
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
                m_Context = null;
                m_VariableEditingContext = null;
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

    /// <summary>
    /// Sets the animation recording controller on the <see cref="AuthoringContext"/>.
    /// Called by <see cref="VisualElementInspector.RefreshRecordingState"/> when the controller
    /// is updated by <see cref="VisualElementSelectionEditor"/>.
    /// The value is kept so it survives detach/reattach cycles and is applied
    /// when the context is created in <see cref="HandleEventBubbleUp"/>.
    /// </summary>
    internal void SetAnimationController(StyleInspectorAnimationRecordingContext controller)
    {
        m_AnimationController = controller;
        if (m_Context != null)
            m_Context.AnimationController = controller;
    }

    /// <summary>
    /// Triggers a refresh of the style diff and inspector UI.
    /// </summary>
    internal void Refresh()
    {
        if (m_Context != null && m_Target.IsValid())
        {
            switch (m_Target.Type)
            {
                case StyleDiff.ContextType.VisualElement:
                    m_Context.Refresh(m_Target.Element);
                    break;
                case StyleDiff.ContextType.StyleSheet:
                    m_Context.AnimationController = null;
                    m_Context.RefreshRule(m_Target.Element, m_Target.Sheet, m_Target.Rule);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            UpdateFlexColumnGlobalState(m_Context.StyleDiff.flexDirection.computedValue);
            UpdateTextGeneratorHelpBoxes();
        }
        else
        {
            if (m_Context != null)
                m_Context.AnimationController = null;
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

    internal void UpdateTextGeneratorHelpBoxes()
    {
        var standardGeneratorWarningRow = contentContainer.Q<OverrideRow>("standard-generator-warning-row");
        var autoSizeWarningRow = contentContainer.Q<OverrideRow>("autosize-warning-row");

        var isAdvanced = m_Context.StyleDiff.unityTextGenerator.computedValue == TextGeneratorType.Advanced;

        if (standardGeneratorWarningRow != null)
            standardGeneratorWarningRow.style.display = !isAdvanced ? DisplayStyle.Flex : DisplayStyle.None;

        if (autoSizeWarningRow != null)
        {
            var bestFit = m_Context.StyleDiff.unityTextAutoSize.computedValue.mode == TextAutoSizeMode.BestFit;
            var showAutoSizeWarning = !isAdvanced && bestFit;
            autoSizeWarningRow.style.display = showAutoSizeWarning ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void RegenerateContent()
    {
        m_ContentContainer.Clear();
        ContentAsset?.CloneTree(m_ContentContainer);
    }
}
