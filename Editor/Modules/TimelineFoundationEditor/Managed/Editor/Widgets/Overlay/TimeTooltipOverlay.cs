// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    /// <summary>
    /// <c>TimeTooltipOverlay</c> objects placed inside a VisualElement that implements ITimeTooltipOverlayPositionHandler will not attempt to position themselves.
    /// </summary>
    interface ITimeTooltipOverlayPositionHandler { }

    class TimeTooltipOverlay : TooltipOverlay
    {
        const string k_Style = "timeTooltipOverlay";
        const string k_Name = "timeTooltipOverlay";

        static readonly CustomStyleProperty<bool> k_CenteredStyleProperty = new("--centered");

        bool m_Centered;
        bool m_ShouldUpdateLabel;
        DiscreteTime m_Time;
        bool m_HandlesPosition = true;
        float m_HalfWidth = 0f;

        public DiscreteTime time
        {
            get => m_Time;
            set
            {
                m_Time = value;
                m_ShouldUpdateLabel = true;
                ForceUpdate();
                m_ShouldUpdateLabel = false;
            }
        }

        bool handlesPosition
        {
            get => m_HandlesPosition;
            set
            {
                if (m_HandlesPosition != value)
                {
                    m_HandlesPosition = value;
                    style.position = value ? Position.Absolute : Position.Relative;
                }
            }
        }

        public TimeTooltipOverlay()
        {
            this.AddToTimelineClassList(k_Style);
            name = k_Name;

            RegisterCallback<CustomStyleResolvedEvent>(CustomStyleResolved);
            RegisterCallback<GeometryChangedEvent>(GeometryChanged);
        }

        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);
            if (evt is AttachToPanelEvent)
            {
                if (parent is ITimeTooltipOverlayPositionHandler)
                    handlesPosition = false;
            }
            else if (evt is DetachFromPanelEvent)
            {
                handlesPosition = true;
            }
        }

        void CustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            customStyle.TryGetValue(k_CenteredStyleProperty, out m_Centered);
        }

        void GeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.newRect.width > 0)
                m_HalfWidth = evt.newRect.width / 2.0f;
        }


        protected override void Update(ICanvas canvas)
        {
            if (m_ShouldUpdateLabel)
                labelText = GetTimeString(canvas);

            if (handlesPosition)
                style.translate = CalculatePosition(canvas);
        }

        protected virtual Vector3 CalculatePosition(ICanvas canvas)
        {
            float pixel = WorldToLocalX(canvas.TimeToWorldPixel(time));
            float halfWidth = m_Centered ? m_HalfWidth : 0f;

            Vector3 position = resolvedStyle.translate;
            position.x = pixel - halfWidth;
            return position;
        }

        protected virtual string GetTimeString(ICanvas canvas) => canvas.timeConverter.ToTimeString(time);
    }
}
