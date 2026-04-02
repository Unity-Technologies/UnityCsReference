// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.View.Internals;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View
{
    abstract class TrackElement : CanvasElement, ITrackContentElement
    {
        const string k_ClassName = "track";
        const string k_ItemContainerName = "itemContainer";
        const string k_MarkerContainerName = "markerContainer";
        const string k_ExpansionContainerName = "expansionContainer";

        static readonly TemplateResource k_Template = Internals.UIResources.TemplateFactory.Get<TrackElement>();

        protected Color m_TrackColor;
        TrackExpansion m_Expansion;
        bool hasExpansion => m_Expansion != null;
        TrackElementContext context { get; }

        VisualElement expansionContainer { get; }
        VisualElement itemsContainer { get; }
        VisualElement m_MarkerContainer;
        bool m_Attached = false;
        ClipConnectorsOverlay clipConnectorsOverlay { get; }

        public Track track => context.track;
        public ISequenceViewModel viewModel => context.viewModel;

        protected TrackElement(TrackElementContext context)
        {
            this.context = context;
            k_Template.CloneInto(this);
            Internals.UIResources.TrackStylesheet.ApplyTo(this);

            itemsContainer = this.Q<VisualElement>(k_ItemContainerName);
            m_MarkerContainer = this.Q<VisualElement>(k_MarkerContainerName);
            m_MarkerContainer.focusable = false;
            expansionContainer = this.Q<VisualElement>(k_ExpansionContainerName);

            clipConnectorsOverlay = this.Q<ClipConnectorsOverlay>();
            clipConnectorsOverlay.Initialize(track);

            this.AddToTimelineClassList(k_ClassName);
        }

        protected override void BindViewModel(ISequenceViewModel viewModel)
        {
            base.BindViewModel(viewModel);
            InitializeAdditionalElementsIfPossible();
            CreateAdditionalContent();
            InitializeTrackExpansion_Safe();
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

        void InitializeAdditionalElementsIfPossible()
        {
            if (ViewModel != null && m_Attached)
            {
                InitializeTrackExpansion();
            }
        }

        void InitializeTrackExpansion_Safe()
        {
            m_Expansion?.RemoveFromHierarchy();
            m_Expansion = CreateTrackExpansion();
            if (m_Expansion == null)
                return;

            m_Expansion.Initialize(ViewModel);
            expansionContainer.Add(m_Expansion);
        }

        protected void ShowTrackExpansion(bool show)
        {
            if (!hasExpansion)
                return;
            expansionContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            m_Expansion.SetDisplay(show);
        }

        protected void SetTrackExpansionHeight(float height)
        {
            if (!hasExpansion)
                return;
            m_Expansion.style.height = height;
        }

        protected virtual void CreateAdditionalContent() { }

        protected virtual TrackExpansion CreateTrackExpansion()
        {
            return null;
        }

        protected virtual void InitializeTrackExpansion() { }

        /// <summary>
        /// Use this method to place elements on the track when the canvas changes.
        /// </summary>
        /// <param name="canvasTransform">The canvas transformation data</param>
        public override void PositionInCanvas(CanvasTransform canvasTransform)
        {
            m_Expansion?.OnCanvasTransformChanged(canvasTransform);
        }

        public virtual void OnTrackMetadataChanged()
        {
            m_Expansion?.OnTrackMetadataChanged(track.GetGenericMetadata());
        }

        public virtual void OnTrackContentsChanged()
        {
            clipConnectorsOverlay.MarkDirtyRepaint();
            m_Expansion?.OnTrackContentsChanged();
        }

        public virtual void OnItemContentsChanged()
        {
            m_Expansion?.OnItemContentsChanged();
        }

        public bool supportsMultiSelect => track.SupportsMultiSelection();

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

        public bool selected { get; protected set; }

        public UniqueID ID => track.ID;

        public VisualElement GetItemsContainer()
        {
            return itemsContainer;
        }

        public VisualElement GetMarkersContainer()
        {
            return m_MarkerContainer;
        }
    }
}
