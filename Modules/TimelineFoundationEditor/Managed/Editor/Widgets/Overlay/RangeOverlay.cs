// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Timeline.Foundation.Time;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    class RangeOverlay : CanvasOverlay
    {
        const string k_Name = "rangeOverlay";
        const string k_Style = "rangeOverlay";

        TimeRange m_Range;

        public TimeRange range
        {
            get => m_Range;
            set
            {
                m_Range = value;
                ForceUpdate();
            }
        }

        public RangeOverlay()
        {
            this.StretchToParentSize();
            UIResources.OverlayStylesheet.ApplyTo(this);
            this.AddToTimelineClassList(k_Style);
            name = k_Name;
        }

        protected override void Update(ICanvas canvas)
        {
            float left = WorldToLocalX(canvas.TimeToWorldPixel(range.start));
            float right = WorldToLocalX(canvas.TimeToWorldPixel(range.end));


            style.translate = new Vector2(left, resolvedStyle.translate.y);
            style.width = right - left;
        }
    }
}
