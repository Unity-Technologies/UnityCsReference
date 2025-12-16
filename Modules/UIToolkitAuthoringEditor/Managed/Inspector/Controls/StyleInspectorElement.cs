// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
internal sealed class StyleInspectorElement : VisualElement, IVisualElementChangeProcessor
{
    public static BindingId SelectionProperty = nameof(Element);
    public static BindingId ContentAssetProperty = nameof(ContentAsset);

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
    StyleDiff m_StyleDiff;
    StyleDiffAdditionalDataFlags m_StyleDiffFlags;

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

    private VisualElement m_ContentContainer;

    public override VisualElement contentContainer { get; }

    public StyleInspectorElement()
        :this(null)
    {
    }

    public StyleInspectorElement(VisualTreeAsset content)
    {
        m_StyleDiffFlags = StyleDiffAdditionalDataFlags.All;

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
                m_StyleDiff = new StyleDiff();
                dataSource = m_StyleDiff;
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
                m_StyleDiff.Dispose();
                m_StyleDiff = null;
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
        if (m_StyleDiff != null)
        {
            m_StyleDiff.RefreshElement(Element, m_StyleDiffFlags);
            UpdateFlexColumnGlobalState(m_StyleDiff.flexDirection.computedValue);
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

    private void RegenerateContent()
    {
        m_ContentContainer.Clear();
        ContentAsset?.CloneTree(m_ContentContainer);
    }
}
