// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    class TimeIndicator : Overlay
    {
        static readonly CustomStyleProperty<Color> k_LineColorProperty = new CustomStyleProperty<Color>("--line-color");
        static readonly StylesheetResource k_Stylesheet = UIResources.StylesheetFactory.Get<TimeIndicator>();
        const string k_StyleClassName = "timeIndicator";

        public TimeIndicator()
        {
            k_Stylesheet.ApplyTo(this);
            this.AddToTimelineClassList(k_StyleClassName);

            generateVisualContent += GenerateVisualContent;
        }

        float m_Pixel;

        DiscreteTime m_Time;
        DiscreteTime m_MaxTime;

        public DiscreteTime time
        {
            get => m_Time;
            set
            {
                if (m_Time != value)
                {
                    m_Time = value;
                    UpdatePixelPosition();
                }
            }
        }

        public DiscreteTime maxValue
        {
            get => m_MaxTime;
            set
            {
                if (m_MaxTime != value)
                {
                    m_MaxTime = value;
                    UpdatePixelPosition();
                }
            }
        }

        public void UpdatePixelPosition()
        {
            float layoutWidth = parent.layout.width;
            if (!float.IsNaN(layoutWidth) && layoutWidth > 0)
            {
                m_Pixel = TimeViewUtility.TimeToPixel(
                    time,
                    layoutWidth,
                    new TimeRange(DiscreteTime.Zero, new DiscreteTime(maxValue)));
                MarkDirtyRepaint();
            }
        }

        void GenerateVisualContent(MeshGenerationContext context)
        {
            Rect bounds = layout;
            var linePos = new Vector2(m_Pixel, bounds.y);

            customStyle.TryGetValue(k_LineColorProperty, out Color color);
            context.DrawVerticalLine(linePos, bounds.height, 1, color);
        }
    }
}
