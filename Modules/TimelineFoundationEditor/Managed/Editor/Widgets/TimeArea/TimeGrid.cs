// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.Widgets.Internals;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    [UxmlElement]
    internal partial class TimeGrid : VisualElement
    {
        const string k_Style = "timeGrid";
        const float k_StrengthMultiplier = .9f;
        static readonly CustomStyleProperty<Color> k_GridColor = new CustomStyleProperty<Color>("--grid-color");
        static readonly StylesheetResource k_Stylesheet = UIResources.StylesheetFactory.Get<TimeGrid>();

        public TimeRange timeRange { get; private set; }

        [UxmlAttribute("display-start-time")]
        public double StartTime
        {
            get => (double)timeRange.start;
            set => SetTimeRange(new TimeRange(timeRange.start, value));
        }

        [UxmlAttribute("display-end-time")]
        public double EndTime
        {
            get => (double)timeRange.end;
            set => SetTimeRange(new TimeRange(value, timeRange.end));
        }

        Color m_GridColor = Color.gray;
        readonly TickHandler m_Handler = new TickHandler();
        List<float> m_TickCache = new List<float>(100);

        public TimeGrid()
        {
            k_Stylesheet.ApplyTo(this);
            this.AddToTimelineClassList(k_Style);

            pickingMode = PickingMode.Ignore;
            focusable = false;
            generateVisualContent += DrawGrid;
            RegisterCallback<CustomStyleResolvedEvent>(CustomStyleResolved);
        }

        public void SetTimeRange(TimeRange range)
        {
            timeRange = range;
            MarkDirtyRepaint();
        }

        public void SetFrameRate(FrameRate frameRate)
        {
            m_Handler.SetTickModulosForFrameRate((float)frameRate.rate);
            MarkDirtyRepaint();
        }

        void CustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            customStyle.TryGetValue(k_GridColor, out m_GridColor);
        }

        void DrawGrid(MeshGenerationContext context)
        {
            Rect rect = layout;
            m_Handler.SetRanges((float)timeRange.start, (float)timeRange.end, worldBound.width);

            for (int level = 0; level < m_Handler.tickLevels; level++)
            {
                float strength = m_Handler.GetStrengthOfLevel(level) * k_StrengthMultiplier;
                if (strength > TickHandler.defaultTickRulerFatThreshold)
                {
                    m_TickCache.Clear();
                    m_Handler.GetTicksAtLevel(level, true, m_TickCache);

                    for (int i = 0; i < m_TickCache.Count; i++)
                    {
                        if (m_TickCache[i] < 0) continue;
                        float pixel = TimeViewUtility.TimeToPixel(new DiscreteTime(m_TickCache[i]), rect.width, timeRange, CanvasTransform.foundationCanvasPixelsBeforeZero);

                        context.DrawVerticalLine(new Vector2(pixel, 0f), rect.height, 1f, m_GridColor);
                    }
                }
            }
        }
    }
}
