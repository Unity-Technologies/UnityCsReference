// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View.Internals
{
    class TimeTooltipContainer : CanvasOverlay, ITimeTooltipOverlayPositionHandler
    {
        const string k_Style = "tooltipContainer";
        static readonly CustomStyleProperty<bool> k_CenteredStyleProperty = new("--centered");

        readonly List<TimeTooltipOverlay> m_Tooltips = new();
        bool m_Centered;
        DiscreteTime m_Time;
        float m_HalfWidth = 0f;

        public TimeTooltipContainer()
        {
            UIResources.OverlayStylesheet.ApplyTo(this);
            this.AddToTimelineClassList(k_Style);
            RegisterCallback<CustomStyleResolvedEvent>(CustomStyleResolved);
            RegisterCallback<GeometryChangedEvent>(GeometryChanged);
        }

        public DiscreteTime time
        {
            get => m_Time;
            set
            {
                m_Time = value;
                foreach (var overlay in m_Tooltips)
                    overlay.time = value;

                ForceUpdate();
            }
        }

        public float RequestedY { get; set; }

        void CustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            customStyle.TryGetValue(k_CenteredStyleProperty, out m_Centered);
        }

        void GeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.newRect.width > 0)
                m_HalfWidth = evt.newRect.width / 2.0f;
        }

        public void AddTooltip(TimeTooltipOverlay tooltipOverlay)
        {
            m_Tooltips.Add(tooltipOverlay);
            Add(tooltipOverlay);
        }

        public void RemoveTooltip(TimeTooltipOverlay tooltipOverlay)
        {
            m_Tooltips.Remove(tooltipOverlay);
            if (tooltipOverlay.hierarchy.parent == this)
            {
                Remove(tooltipOverlay);
            }
        }

        protected override void Update(ICanvas canvas)
        {
            style.translate = CalculatePosition(canvas);
        }

        Vector3 CalculatePosition(ICanvas canvas)
        {
            float pixel = WorldToLocalX(canvas.TimeToWorldPixel(time));
            float halfWidth = m_Centered ? m_HalfWidth : 0f;

            Vector3 position = resolvedStyle.translate;
            position.x = pixel - halfWidth;
            position.y = RequestedY;
            return position;
        }
    }
}
