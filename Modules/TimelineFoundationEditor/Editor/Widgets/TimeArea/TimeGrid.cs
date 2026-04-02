// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.Widgets.Internals;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    partial class TimeGrid : VisualElement
    {
        // [UxmlElement] does no codegen in trunk (6000.2); we have to provide the generated UxmlSerializedData manually.
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData),
                    new UxmlAttributeNames[]
                    {
                        new(nameof(displayStartTime), "display-start-time"), new(nameof(displayEndTime), "display-end-time"),
                    }, true);
            }

#pragma warning disable 649
            [SerializeField] double displayStartTime;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags displayStartTime_UxmlAttributeFlags;
            [SerializeField] double displayEndTime;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags displayEndTime_UxmlAttributeFlags;
#pragma warning restore 649

            public override object CreateInstance() => new TimeGrid();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (TimeGrid)obj;
                if (ShouldWriteAttributeValue(displayStartTime_UxmlAttributeFlags))
                    e.StartTime = displayStartTime;
                if (ShouldWriteAttributeValue(displayEndTime_UxmlAttributeFlags))
                    e.EndTime = displayEndTime;
            }
        }

        const string k_Style = "timeGrid";
        const float k_StrengthMultiplier = .9f;
        static readonly CustomStyleProperty<Color> k_GridColor = new CustomStyleProperty<Color>("--grid-color");
        static readonly StylesheetResource k_Stylesheet = UIResources.StylesheetFactory.Get<TimeGrid>();

        public TimeRange timeRange { get; private set; }

        public double StartTime
        {
            get => (double)timeRange.start;
            set => SetTimeRange(new TimeRange(timeRange.start, value));
        }

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
