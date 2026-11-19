// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets.Internals
{
    class TimeMinimap : OverlayManager
    {
        const string k_ClassName = "timeMinimap";
        UQueryState<TimeIndicator> m_TimeIndicators;

        public TimeMinimap()
        {
            this.StretchToParentSize();
            TimeRangeScroller.stylesheetResource.ApplyTo(this);
            this.AddToTimelineClassList(k_ClassName);
            m_TimeIndicators = this.Query<TimeIndicator>().Build();

            style.overflow = Overflow.Visible;

            RegisterCallback<CustomStyleResolvedEvent>(CustomStyleResolved);
        }

        static readonly CustomStyleProperty<float> k_RangeHandleWidth = new("--timeline-rangeHandle-width");
        const int k_FallBackLeft = 10;
        void CustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (customStyle.TryGetValue(k_RangeHandleWidth, out float w))
            {
                style.left = w;
            }
            else
            {
                style.left = k_FallBackLeft;
            }
        }

        public void SetMaxValue(float max)
        {
            var maxValue = new DiscreteTime(max);
            foreach (var overlay in m_TimeIndicators)
            {
                overlay.maxValue = maxValue;
            }
        }

        protected override void HandleEventBubbleUp(EventBase evt)
        {
            if (evt is GeometryChangedEvent)
            {
                foreach (var overlay in m_TimeIndicators)
                    overlay.UpdatePixelPosition();
            }
        }
    }
}
