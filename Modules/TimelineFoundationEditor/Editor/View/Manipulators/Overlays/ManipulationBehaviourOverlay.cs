// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace Unity.Timeline.Foundation.View
{
    abstract class ManipulationBehaviourOverlay : CanvasOverlay
    {
        const string k_Style = "editModeOverlay";

        static readonly CustomStyleProperty<float> k_IndicatorHeightRatioProperty = new CustomStyleProperty<float>("--indicator-height-ratio");
        static readonly CustomStyleProperty<float> k_IndicatorsWidthProperty = new CustomStyleProperty<float>("--indicator-width");
        static readonly CustomStyleProperty<Color> k_IndicatorsColorProperty = new CustomStyleProperty<Color>("--indicator-color");

        public Action<Cursor> cursorChanged;

        private protected SequenceTreeView sequenceTreeView { get; private set; }

        Cursor? m_Cursor;

        public Cursor? cursor
        {
            get => m_Cursor;
            protected set
            {
                if (m_Cursor == value || value == null)
                    return;
                m_Cursor = value;
                cursorChanged?.Invoke(m_Cursor.Value);
            }
        }

        float m_IndicatorsWidth;
        float m_IndicatorHeightRatio;
        Color m_IndicatorColor;
        List<Rect> m_Indicators;
        Dictionary<UniqueID, Rect> m_IndicatorsRect;

        protected ManipulationBehaviourOverlay()
        {
            Internals.UIResources.OverlayStylesheet.ApplyTo(this);
            this.AddToTimelineClassList(k_Style);
            m_Indicators = new List<Rect>();
            m_IndicatorsRect = new Dictionary<UniqueID, Rect>();
            generateVisualContent += GenerateVisualContent;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            Hide();
        }

        protected void AddStartIndicatorAtTime(DiscreteTime time, UniqueID trackID) => AddIndicatorAtTime(time, trackID, m_IndicatorsWidth);
        protected void AddEndIndicatorAtTime(DiscreteTime time, UniqueID trackID) => AddIndicatorAtTime(time, trackID, 0f);

        protected VisualElement GetItemElement(UniqueID id) => sequenceTreeView.ContentLookup.GetItemElement(id);

        protected virtual void GenerateVisualContent(MeshGenerationContext context)
        {
            foreach (Rect indicator in m_Indicators)
                context.DrawRect(indicator, m_IndicatorColor, fill: true);
        }

        protected override void Update(ICanvas canvas)
        {
            m_Indicators.Clear();
            base.Update(canvas);
        }

        protected void ClearIndicators()
        {
            m_Indicators.Clear();
        }

        public void BuildIndicatorsList(IEnumerable<Track> tracks)
        {
            m_IndicatorsRect.Clear();
            foreach (var track in tracks)
            {
                TrackElement element = sequenceTreeView.ContentLookup.GetTrackElement(track);
                m_IndicatorsRect[element.ID] = new Rect(this.WorldToLocal(element.worldBound.position),
                    new Vector2(m_IndicatorsWidth, element.worldBound.height));
            }
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            evt.customStyle.TryGetValue(k_IndicatorsWidthProperty, out m_IndicatorsWidth);
            evt.customStyle.TryGetValue(k_IndicatorHeightRatioProperty, out m_IndicatorHeightRatio);
            evt.customStyle.TryGetValue(k_IndicatorsColorProperty, out m_IndicatorColor);
        }

        void AddIndicatorAtTime(DiscreteTime time, UniqueID trackID, float offset)
        {
            if (!m_IndicatorsRect.TryGetValue(trackID, out Rect indicator))
                return;

            indicator.x = WorldToLocalX(Canvas.TimeToWorldPixel(time)) - offset;
            float trackHeight = indicator.height;
            indicator.height *= m_IndicatorHeightRatio;
            indicator.y += (trackHeight - indicator.height) * 0.5f;
            m_Indicators.Add(indicator);
        }

        void OnAttachToPanel(AttachToPanelEvent evt) => sequenceTreeView = GetFirstAncestorOfType<SequenceTreeView>();
        void OnDetachFromPanel(DetachFromPanelEvent evt) => sequenceTreeView = null;
    }
}
