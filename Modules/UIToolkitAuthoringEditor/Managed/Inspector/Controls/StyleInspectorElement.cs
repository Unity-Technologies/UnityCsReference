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

[UxmlElement]
internal sealed class StyleInspectorElement : VisualElement, IVisualElementChangeProcessor
{
    public static BindingId SelectionProperty = nameof(Element);
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

        public void Dispose()
        {
            m_StyleDiff?.Dispose();
        }
    }

    AuthoringContext m_Context;

    VisualElement m_Element;

    [CreateProperty]
    public VisualElement Element
    {
        get => m_Element;
        set
        {
            if (m_Element == value)
                return;
            ReleaseSelection(m_Element);
            m_Element = value;
            AcquireSelection(m_Element);
            NotifyPropertyChanged(SelectionProperty);
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
                if (m_Element != null)
                    AcquireSelection(m_Element);
                else
                    Refresh();

                break;
            }
            case DetachFromPanelEvent detachFromPanelEvent:
            {
                if (detachFromPanelEvent.originPanel == null)
                    return;
                if (m_Element != null)
                    ReleaseSelection(m_Element);
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
        if (changes.styleChanged.Contains(Element) ||
            changes.bindingContextChanged.Contains(Element) ||
            changes.stylingContextChanged.Contains(Element))
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
        if (m_Context != null)
        {
            m_Context.Refresh(Element);
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
