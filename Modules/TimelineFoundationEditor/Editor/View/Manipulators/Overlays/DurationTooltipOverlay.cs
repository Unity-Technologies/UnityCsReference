// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using Unity.Timeline.Foundation.Widgets;
using UnityEditor;
using UnityEngine;

namespace Unity.Timeline.Foundation.View.Internals
{
    class DurationTooltipOverlay : TimeTooltipOverlay
    {
        static readonly string k_DurationText = L10n.Tr("Duration:");

        DiscreteTime m_Duration;

        public DurationTooltipOverlay()
        {
            UIResources.OverlayStylesheet.ApplyTo(this);
            this.AddToTimelineClassList("durationTooltipOverlay");
        }

        public void SetValues(DiscreteTime time, DiscreteTime duration)
        {
            m_Duration = duration;
            this.time = time; // Forces the label to update
        }

        public DiscreteTime initialDuration { get; set; }

        public float RequestedY { get; set; }

        protected override string GetTimeString(ICanvas canvas) => $"{k_DurationText} {canvas.timeConverter.ToTimeStringWithDelta(m_Duration, m_Duration - initialDuration)}";

        protected override Vector3 CalculatePosition(ICanvas canvas)
        {
            Vector3 position = base.CalculatePosition(canvas);
            position.y = RequestedY;
            return position;
        }
    }
}
