// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.Commands.Sequence;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.View.Internals;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View
{
    abstract class TrackHeaderElement : SequenceElement, ITrackHeaderElement
    {
        static readonly TemplateResource k_Template = Internals.UIResources.TemplateFactory.Get<TrackHeaderElement>();

        protected Image m_Icon;
        protected VisualElement m_HeaderTitleContent;

        VisualElement m_CustomControls;
        VisualElement m_ResizableContainer;
        VisualElement m_ExpansionContainer;

        TextField m_TrackNameField;

        TrackElementContext context { get; }

        public Track track => context.track;
        TrackExpansion m_Expansion;
        bool hasExpansion => m_Expansion != null;
        bool m_Attached = false;
        internal TrackExpansion TrackExpansion_ForTesting => m_Expansion;

        protected TrackHeaderElement(TrackElementContext context)
        {
            this.context = context;
            k_Template.CloneInto(this);
            Internals.UIResources.TrackStylesheet.ApplyTo(this);
            this.AddToTimelineClassList("trackHeader");

            m_HeaderTitleContent = this.Q<VisualElement>("headerTitleContent");
            m_Icon = m_HeaderTitleContent.Q<Image>("icon");
            m_TrackNameField = m_HeaderTitleContent.Q<TextField>(classes: "timeline-trackNameField");
            m_TrackNameField.RegisterValueChangedCallback(TitleChanged);
            m_TrackNameField.value = track.name;

            m_CustomControls = this.Q<VisualElement>("customControls");

            m_ExpansionContainer = this.Q<VisualElement>("expansionContainer");
            var expansionResizeHandle = m_ExpansionContainer.Q<VisualElement>("expansion__resizeHandleAnchor");
            expansionResizeHandle.Q<VisualElement>("expansion__resizeHandle").AddManipulator(new TrackExpansionResizeManipulator(this));

            m_ResizableContainer = this.Q<VisualElement>("resizableContainer");
            var resizeHandle = this.Q<VisualElement>("resizeHandleAnchor");
            resizeHandle.Q<VisualElement>("resizeHandle").AddManipulator(new TrackResizeManipulator(this));
        }

        protected override void BindViewModel(ISequenceViewModel viewModel)
        {
            base.BindViewModel(viewModel);
            InitializeAdditionalElementsIfPossible();
        }

        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            switch (evt)
            {
                case AttachToPanelEvent:
                    m_Attached = true;
                    InitializeAdditionalElementsIfPossible();
                    break;
            }
        }

        protected virtual void AddCustomControls() { }

        public bool selected { get; private set; }
        public bool supportsMultiSelect => track.SupportsMultiSelection();

        public UniqueID ID => track.ID;

        void InitializeAdditionalElementsIfPossible()
        {
            if (ViewModel != null && m_Attached)
            {
                InitializeTrackExpansion_Safe();
                AddCustomControls();
                InitializeTrackExpansion();
            }
        }

        void InitializeTrackExpansion_Safe()
        {
            m_Expansion?.RemoveFromHierarchy();

            m_Expansion = CreateTrackExpansion();
            if (m_Expansion == null)
                return;
            m_ExpansionContainer.Add(m_Expansion);
            m_Expansion.SendToBack();
            m_Expansion.Initialize(ViewModel);
        }

        protected virtual TrackExpansion CreateTrackExpansion()
        {
            return null;
        }

        protected virtual void InitializeTrackExpansion() { }

        protected void ShowTrackHeaderExpansion(bool show)
        {
            if (!hasExpansion)
                return;
            m_ExpansionContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            m_Expansion.SetDisplay(show);
        }

        public void SetTrackHeaderExpansionBottom(float positionY)
        {
            if (!hasExpansion)
                return;
            float newHeight = positionY - m_Expansion.worldBound.yMin;
            SetTrackHeaderExpansionHeight(newHeight);
        }

        protected void SetTrackHeaderExpansionHeight(float newHeight)
        {
            float minHeight = m_Expansion.style.minHeight.value.value;
            m_Expansion.style.height = Mathf.Max(minHeight, newHeight);
        }

        public virtual void OnTrackMetadataChanged()
        {
            var metadata = track.GetMetadata<ITrackMetadata>();
            m_TrackNameField?.SetValueWithoutNotify(metadata.name);
            m_Expansion?.OnTrackMetadataChanged(metadata);
        }

        public virtual void OnTrackContentsChanged()
        {
            m_Expansion?.OnTrackContentsChanged();
        }

        public virtual void OnItemContentsChanged()
        {
            m_Expansion?.OnItemContentsChanged();
        }

        public virtual void OnSelectionStateChanged(bool selected)
        {
            this.selected = selected;
            if (selected)
            {
                Select();
            }
            else
            {
                Unselect();
            }
        }

        public abstract void Select();
        public abstract void Unselect();

        protected VisualElement TrackNameField => m_TrackNameField;

        void TitleChanged(ChangeEvent<string> evt)
        {
            ViewModel.Dispatch(new SetTrackName(track, evt.newValue));
            m_TrackNameField.SetValueWithoutNotify(evt.previousValue);
        }

        protected virtual void AddCustomControl(VisualElement control, int preferredIndex = -1)
        {
            if (preferredIndex < 0 || preferredIndex > m_CustomControls.childCount)
            {
                m_CustomControls.Add(control);
            }
            else
            {
                m_CustomControls.Insert(preferredIndex, control);
            }

            m_CustomControls.style.display = DisplayStyle.Flex;
        }

        protected float minimumHeight => m_ResizableContainer.resolvedStyle.minHeight.value;

        int m_HeightExtension = 0;

        protected int heightExtension
        {
            get => m_HeightExtension;
            set
            {
                if (!Mathf.Approximately(m_HeightExtension, value))
                {
                    m_HeightExtension = Math.Max(0, value);
                    VerticalResize();
                }
            }
        }

        protected internal virtual void ExtensionDragged_Internal(Vector3 mousePosition)
        {
            float height = mousePosition.y - worldBound.yMin;
            heightExtension = Mathf.Max(Mathf.RoundToInt(height - minimumHeight));
        }

        protected internal virtual void ExtensionDropped_Internal(Vector3 mousePosition)
        {
            float height = mousePosition.y - worldBound.yMin;
            heightExtension = Mathf.RoundToInt(height - minimumHeight);
        }

        protected virtual void VerticalResize()
        {
            SetHeight(minimumHeight * heightExtension);
        }

        protected void SetHeight(float newHeight)
        {
            m_ResizableContainer.style.height = newHeight < minimumHeight ? minimumHeight : newHeight;
        }
    }
}
