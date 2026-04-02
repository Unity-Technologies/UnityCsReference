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
    partial class TimeArea : VisualElement
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
                        new(nameof(displayStartTime), "display-start-time"),
                        new(nameof(displayEndTime), "display-end-time"),
                        new(nameof(allowNegativeTicks),  "allow-negative-ticks"),
                    }, true);
            }

#pragma warning disable 649
            [SerializeField] double displayStartTime;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags displayStartTime_UxmlAttributeFlags;
            [SerializeField] double displayEndTime;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags displayEndTime_UxmlAttributeFlags;
            [SerializeField] bool allowNegativeTicks;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags allowNegativeTicks_UxmlAttributeFlags;
#pragma warning restore 649

            public override object CreateInstance() => new TimeArea();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (TimeArea)obj;
                if (ShouldWriteAttributeValue(displayStartTime_UxmlAttributeFlags))
                    e.StartTime = displayStartTime;
                if (ShouldWriteAttributeValue(displayEndTime_UxmlAttributeFlags))
                    e.EndTime = displayEndTime;
                if (ShouldWriteAttributeValue(allowNegativeTicks_UxmlAttributeFlags))
                    e.AllowNegativeTicks = allowNegativeTicks;
            }
        }

        static readonly CustomStyleProperty<int> k_AttrTickRulerDistLabel = new CustomStyleProperty<int>("--tick-label-padding");
        static readonly CustomStyleProperty<float> k_AttrTickRulerHeightMax = new CustomStyleProperty<float>("--tick-max-height");
        static readonly CustomStyleProperty<Color> k_AttrLineColor = new CustomStyleProperty<Color>("--line-color");

        const string k_Style = "timeArea";
        const string k_LabelStyle = "timeAreaLabel";
        const int k_DefaultTickRulerDist = 40; // min distance between ruler tick mark labels
        const float k_DefaultTickRulerHeightMax = 0.7f; // height of the ruler tick marks when they are highest

        static readonly Color k_DefaultLineColor = new Color(1.0f, 1.0f, 1.0f, 0.2f);
        static readonly StylesheetResource k_Stylesheet = UIResources.StylesheetFactory.Get<TimeArea>();

        public TimeRange DisplayRange => m_DisplayRange;

        public FrameRate FrameRate
        {
            get => m_FrameRate;
            set
            {
                if (m_FrameRate != value)
                {
                    m_FrameRate = value;
                    var floatFrameRate = (float)value.rate;
                    m_TickHandler.SetTickModulosForFrameRate(floatFrameRate);
                    m_LabelTickHandler.SetTickModulosForFrameRate(floatFrameRate);
                    CreateLabels();
                }
            }
        }

        public TimeTransform DisplayRangeTransform
        {
            get => m_DisplayRangeTransform;
            set
            {
                if (m_DisplayRangeTransform == value)
                    return;
                m_DisplayRangeTransform = value;
                CreateLabels();
                MarkDirtyRepaint();
            }
        }

        public TimeFormat TimeFormat
        {
            get => m_TimeFormat;
            set
            {
                if (m_TimeFormat != value)
                {
                    m_TimeFormat = value;
                    CreateLabels();
                }
            }
        }

        public double StartTime
        {
            get => (double)DisplayRange.start;
            set => SetDisplayRangeWithoutNotify(new TimeRange(DisplayRange.start, value));
        }

        public double EndTime
        {
            get => (float)DisplayRange.end;
            set => SetDisplayRangeWithoutNotify(new TimeRange(value, DisplayRange.end));
        }

        public bool AllowNegativeTicks
        {
            get => m_AllowNegativeTicks;
            set => m_AllowNegativeTicks = value;
        }

        TimeRange m_DisplayRange;
        TimeTransform m_DisplayRangeTransform = TimeTransform.Identity;
        FrameRate m_FrameRate;
        TimeFormat m_TimeFormat;
        bool m_AllowNegativeTicks = true;

        int m_TickRulerLabelDistance = k_DefaultTickRulerDist;
        float m_TickRulerHeightMax = k_DefaultTickRulerHeightMax;
        Color m_BaseColor = k_DefaultLineColor;
        readonly Color m_LineColor = Color.white;

        readonly TickHandler m_TickHandler;
        readonly TickHandler m_LabelTickHandler;
        List<float> m_TickCache = new (1000);
        List<float> m_LabelTickCache = new (1000);
        List<Label> m_LabelCache = new (100);
        List<(Rect, float)> m_LineRectsCache = new (1000);

        public TimeArea()
        {
            UIResources.CommonStylesheet.ApplyTo(this);
            k_Stylesheet.ApplyTo(this);
            this.AddToTimelineClassList(k_Style);

            m_TickHandler = new TickHandler();
            m_LabelTickHandler = new TickHandler();

            generateVisualContent = GenerateVisualContent;

            this.AddManipulator(new ZoomManipulator());
            this.AddManipulator(new PanManipulator());

            RegisterCallback<ZoomEvent>(OnZoomEvent);
            RegisterCallback<PanEvent>(OnPanEvent);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            FrameRate = FrameRate.k_60Fps;
        }

        public void SetDisplayRange(TimeRange displayRange)
        {
            TimeRange previous = m_DisplayRange;
            SetDisplayRangeWithoutNotify(displayRange);
            DisplayRangeChangeEvent.Send(this, previous, displayRange);
        }

        public void SetDisplayRangeWithoutNotify(TimeRange displayRange)
        {
            if (DisplayRange != displayRange)
            {
                m_DisplayRange = displayRange;
                CreateLabels();
                MarkDirtyRepaint();
            }
        }

        void OnGeometryChanged(GeometryChangedEvent e)
        {
            CreateLabels();
        }

        void OnZoomEvent(ZoomEvent evt)
        {
            SetDisplayRange(evt.ApplyToTimeRange(DisplayRange));
        }

        void OnPanEvent(PanEvent evt)
        {
            SetDisplayRange(evt.ApplyToTimeRange(DisplayRange));
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            m_TickRulerLabelDistance = e.customStyle.TryGetValue(k_AttrTickRulerDistLabel, out int labelDistance) ? labelDistance : k_DefaultTickRulerDist;
            m_TickRulerHeightMax = e.customStyle.TryGetValue(k_AttrTickRulerHeightMax, out float heightMax) ? heightMax : k_DefaultTickRulerHeightMax;
            m_BaseColor = e.customStyle.TryGetValue(k_AttrLineColor, out Color lineColor) ? lineColor : k_DefaultLineColor;
        }

        void GenerateVisualContent(MeshGenerationContext context)
        {
            m_TickHandler.SetRanges((float)DisplayRange.start, (float)DisplayRange.end, worldBound.width);
            int vertexOffset = 0;
            var lineRects = CalculateLineRects((float)FrameRate.rate);

            int numVerticalLines = lineRects.Count;
            int numVertices = numVerticalLines * 4;
            int numIndices = numVerticalLines * 6;

            MeshWriteData mesh = context.Allocate(numVertices, numIndices);

            foreach ((Rect lineRect, float strength) in lineRects)
            {
                var color = m_LineColor;
                color.a = strength / TickHandler.defaultTickRulerFatThreshold;
                mesh.DrawRect(lineRect, color * m_BaseColor, vertexOffset);
                vertexOffset += 4;
            }
        }

        List<(Rect rect, float strength)> CalculateLineRects(float frameRate)
        {
            m_LineRectsCache.Clear();
            for (int tickLevel = 0; tickLevel < m_TickHandler.tickLevels; tickLevel++)
            {
                float strength = m_TickHandler.GetStrengthOfLevel(tickLevel) * .9f;
                if (strength < 0.01f)
                    continue;

                float height = layout.height * Mathf.Min(1, strength) * m_TickRulerHeightMax;
                float y0 = contentRect.yMax - height;

                m_TickCache.Clear();
                m_TickHandler.GetTicksAtLevel(tickLevel, true, m_TickCache, m_AllowNegativeTicks);

                for (int i = 0; i < m_TickCache.Count; i++)
                {
                    float tick = m_TickCache[i];
                    if (DisplayRange.Intersects(new DiscreteTime(tick)))
                    {
                        float frame = Mathf.Round(tick * frameRate);
                        float x0 = TimeViewUtility.TimeToPixel(frame / frameRate, layout.width, DisplayRange, CanvasTransform.foundationCanvasPixelsBeforeZero);
                        m_LineRectsCache.Add((new Rect(x0, y0, 1.0f, height), strength));
                    }
                }
            }

            return m_LineRectsCache;
        }

        void CreateLabels()
        {
            if (DisplayRange == TimeRange.Empty)
                return;

            const float kLabelOffsetY = 3.0f;
            m_LabelTickHandler.SetRanges((float)DisplayRange.start, (float)DisplayRange.end, worldBound.width);
            int labelLevel = m_LabelTickHandler.GetLevelWithMinSeparation(m_TickRulerLabelDistance);
            m_LabelTickCache.Clear();

            if (labelLevel < 0)
                return;

            m_LabelTickHandler.GetTicksAtLevel(labelLevel, false, m_LabelTickCache, m_AllowNegativeTicks);

            int numLabels = m_LabelTickCache.Count;

            for (int i = 0; i < numLabels; ++i)
            {
                int frame = Mathf.RoundToInt(m_LabelTickCache[i] * (float)FrameRate.rate);
                Label label;
                if (i < m_LabelCache.Count)
                {
                    label = m_LabelCache[i];
                    if (IndexOf(label) < 0)
                        Add(label);
                }
                else
                {
                    label = new Label { pickingMode = PickingMode.Ignore };
                    m_LabelCache.Add(label);
                    label.AddToTimelineClassList(k_LabelStyle);
                    Add(label);
                }

                float labelPos = TimeViewUtility.FrameToPixel(frame, (float)FrameRate.rate, layout.width, DisplayRange, CanvasTransform.foundationCanvasPixelsBeforeZero);

                label.text = TimeFormat.ToTimeString((double)DisplayRangeTransform.Transform(new DiscreteTime(frame / FrameRate.rate)),
                    FrameRate, TimeFormat == TimeFormat.Frames ? "f0" : "f2");
                label.style.left = labelPos + 3;
                label.style.top = contentRect.yMin + kLabelOffsetY;
            }

            for (int i = numLabels; i < m_LabelCache.Count; i++)
                m_LabelCache[i].RemoveFromHierarchy();

            MarkDirtyRepaint();
        }
    }
}
