// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [System.Serializable]
    class TimeArea : ZoomableArea
    {
        [SerializeField] private TickHandler m_HTicks;

        public TickHandler hTicks
        {
            get { return m_HTicks; }
            set { m_HTicks = value; }
        }

        [SerializeField] private TickHandler m_VTicks;

        public TickHandler vTicks
        {
            get { return m_VTicks; }
            set { m_VTicks = value; }
        }

        internal const int kTickRulerDistMin = 3
        ;     // min distance between ruler tick marks before they disappear completely

        internal const int kTickRulerDistFull = 80; // distance between ruler tick marks where they gain full strength
        internal const int kTickRulerDistLabel = 40; // min distance between ruler tick mark labels
        internal const float kTickRulerHeightMax = 0.7f; // height of the ruler tick marks when they are highest

        internal const float kTickRulerFatThreshold = 0.5f
        ;     // size of ruler tick marks at which they begin getting fatter

        public enum TimeFormat
        {
            None, // Unformatted time
            TimeFrame, // Time:Frame
            Frame // Integer frame
        };

        class Styles2
        {
            public GUIStyle timelineTick = "AnimationTimelineTick";
            public GUIStyle labelTickMarks = "CurveEditorLabelTickMarks";
            public GUIStyle playhead = "AnimationPlayHead";
        }

        static Styles2 timeAreaStyles;

        static void InitStyles()
        {
            if (timeAreaStyles == null)
                timeAreaStyles = new Styles2();
        }

        public TimeArea(bool minimalGUI) : base(minimalGUI)
        {
            float[] modulos = new float[]
            {
                0.0000001f, 0.0000005f, 0.000001f, 0.000005f, 0.00001f, 0.00005f, 0.0001f, 0.0005f,
                0.001f, 0.005f, 0.01f, 0.05f, 0.1f, 0.5f, 1, 5, 10, 50, 100, 500,
                1000, 5000, 10000, 50000, 100000, 500000, 1000000, 5000000, 10000000
            };
            hTicks = new TickHandler();
            hTicks.SetTickModulos(modulos);
            vTicks = new TickHandler();
            vTicks.SetTickModulos(modulos);
        }

        public void SetTickMarkerRanges()
        {
            hTicks.SetRanges(shownArea.xMin, shownArea.xMax, drawRect.xMin, drawRect.xMax);
            vTicks.SetRanges(shownArea.yMin, shownArea.yMax, drawRect.yMin, drawRect.yMax);
        }

        public void DrawMajorTicks(Rect position, float frameRate)
        {
            GUI.BeginGroup(position);
            if (Event.current.type != EventType.Repaint)
            {
                GUI.EndGroup();
                return;
            }
            InitStyles();

            HandleUtility.ApplyWireMaterial();

            SetTickMarkerRanges();
            hTicks.SetTickStrengths(kTickRulerDistMin, kTickRulerDistFull, true);

            Color tickColor = timeAreaStyles.timelineTick.normal.textColor;
            tickColor.a = 0.1f;

            if (Application.platform == RuntimePlatform.WindowsEditor)
                GL.Begin(GL.QUADS);
            else
                GL.Begin(GL.LINES);

            // Draw tick markers of various sizes
            Rect theShowArea = shownArea;
            for (int l = 0; l < hTicks.tickLevels; l++)
            {
                float strength = hTicks.GetStrengthOfLevel(l) * .9f;
                if (strength > kTickRulerFatThreshold)
                {
                    float[] ticks = hTicks.GetTicksAtLevel(l, true);
                    for (int i = 0; i < ticks.Length; i++)
                    {
                        if (ticks[i] < 0) continue;
                        int frame = Mathf.RoundToInt(ticks[i] * frameRate);
                        float x = FrameToPixel(frame, frameRate, position, theShowArea);
                        // Draw line
                        DrawVerticalLineFast(x, 0.0f, position.height, tickColor);
                    }
                }
            }

            GL.End();
            GUI.EndGroup();
        }

        public void TimeRuler(Rect position, float frameRate)
        {
            TimeRuler(position, frameRate, true, false, 1f, TimeFormat.TimeFrame);
        }

        public void TimeRuler(Rect position, float frameRate, bool labels, bool useEntireHeight, float alpha)
        {
            TimeRuler(position, frameRate, labels, useEntireHeight, alpha, TimeFormat.TimeFrame);
        }

        public void TimeRuler(Rect position, float frameRate, bool labels, bool useEntireHeight, float alpha,
            TimeFormat timeFormat)
        {
            Color backupCol = GUI.color;
            GUI.BeginGroup(position);
            InitStyles();

            HandleUtility.ApplyWireMaterial();

            Color tempBackgroundColor = GUI.backgroundColor;

            SetTickMarkerRanges();
            hTicks.SetTickStrengths(kTickRulerDistMin, kTickRulerDistFull, true);

            Color baseColor = timeAreaStyles.timelineTick.normal.textColor;
            baseColor.a = 0.75f * alpha;

            if (Event.current.type == EventType.Repaint)
            {
                if (Application.platform == RuntimePlatform.WindowsEditor)
                    GL.Begin(GL.QUADS);
                else
                    GL.Begin(GL.LINES);

                // Draw tick markers of various sizes

                Rect cachedShowArea = shownArea;
                for (int l = 0; l < hTicks.tickLevels; l++)
                {
                    float strength = hTicks.GetStrengthOfLevel(l) * .9f;
                    float[] ticks = hTicks.GetTicksAtLevel(l, true);
                    for (int i = 0; i < ticks.Length; i++)
                    {
                        if (ticks[i] < hRangeMin || ticks[i] > hRangeMax)
                            continue;
                        int frame = Mathf.RoundToInt(ticks[i] * frameRate);

                        float height = useEntireHeight
                            ? position.height
                            : position.height * Mathf.Min(1, strength) * kTickRulerHeightMax;
                        float x = FrameToPixel(frame, frameRate, position, cachedShowArea);

                        // Draw line
                        DrawVerticalLineFast(x, position.height - height + 0.5f, position.height - 0.5f,
                            new Color(1, 1, 1, strength / kTickRulerFatThreshold) * baseColor);
                    }
                }

                GL.End();
            }

            if (labels)
            {
                // Draw tick labels
                int labelLevel = hTicks.GetLevelWithMinSeparation(kTickRulerDistLabel);
                float[] labelTicks = hTicks.GetTicksAtLevel(labelLevel, false);
                for (int i = 0; i < labelTicks.Length; i++)
                {
                    if (labelTicks[i] < hRangeMin || labelTicks[i] > hRangeMax)
                        continue;

                    int frame = Mathf.RoundToInt(labelTicks[i] * frameRate);
                    // Important to take floor of positions of GUI stuff to get pixel correct alignment of
                    // stuff drawn with both GUI and Handles/GL. Otherwise things are off by one pixel half the time.

                    float labelpos = Mathf.Floor(FrameToPixel(frame, frameRate, position));
                    string label = FormatTime(labelTicks[i], frameRate, timeFormat);
                    GUI.Label(new Rect(labelpos + 3, -3, 40, 20), label, timeAreaStyles.timelineTick);
                }
            }
            GUI.EndGroup();

            GUI.backgroundColor = tempBackgroundColor;
            GUI.color = backupCol;
        }

        public static void DrawPlayhead(float x, float yMin, float yMax, float thickness, float alpha)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            InitStyles();
            float halfThickness = thickness * 0.5f;
            Color lineColor = timeAreaStyles.playhead.normal.textColor.AlphaMultiplied(alpha);
            if (thickness > 1f)
            {
                Rect labelRect = Rect.MinMaxRect(x - halfThickness, yMin, x + halfThickness, yMax);
                EditorGUI.DrawRect(labelRect, lineColor);
            }
            else
            {
                DrawVerticalLine(x, yMin, yMax, lineColor);
            }
        }

        public static void DrawVerticalLine(float x, float minY, float maxY, Color color)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Color backupCol = Handles.color;

            HandleUtility.ApplyWireMaterial();
            if (Application.platform == RuntimePlatform.WindowsEditor)
                GL.Begin(GL.QUADS);
            else
                GL.Begin(GL.LINES);
            DrawVerticalLineFast(x, minY, maxY, color);
            GL.End();

            Handles.color = backupCol;
        }

        public static void DrawVerticalLineFast(float x, float minY, float maxY, Color color)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                GL.Color(color);
                GL.Vertex(new Vector3(x - 0.5f, minY, 0));
                GL.Vertex(new Vector3(x + 0.5f, minY, 0));
                GL.Vertex(new Vector3(x + 0.5f, maxY, 0));
                GL.Vertex(new Vector3(x - 0.5f, maxY, 0));
            }
            else
            {
                GL.Color(color);
                GL.Vertex(new Vector3(x, minY, 0));
                GL.Vertex(new Vector3(x, maxY, 0));
            }
        }

        public enum TimeRulerDragMode
        {
            None,
            Start,
            End,
            Dragging,
            Cancel
        }

        static float s_OriginalTime;
        static float s_PickOffset;

        public TimeRulerDragMode BrowseRuler(Rect position, ref float time, float frameRate, bool pickAnywhere,
            GUIStyle thumbStyle)
        {
            int id = GUIUtility.GetControlID(3126789, FocusType.Passive);
            return BrowseRuler(position, id, ref time, frameRate, pickAnywhere, thumbStyle);
        }

        public TimeRulerDragMode BrowseRuler(Rect position, int id, ref float time, float frameRate, bool pickAnywhere,
            GUIStyle thumbStyle)
        {
            Event evt = Event.current;
            Rect pickRect = position;
            if (time != -1)
            {
                pickRect.x = Mathf.Round(TimeToPixel(time, position)) - thumbStyle.overflow.left;
                pickRect.width = thumbStyle.fixedWidth + thumbStyle.overflow.horizontal;
            }

            switch (evt.GetTypeForControl(id))
            {
                case EventType.Repaint:
                    if (time != -1)
                    {
                        bool hover = position.Contains(evt.mousePosition);
                        pickRect.x += thumbStyle.overflow.left;
                        thumbStyle.Draw(pickRect, id == GUIUtility.hotControl, hover || id == GUIUtility.hotControl,
                            false, false);
                    }
                    break;
                case EventType.MouseDown:
                    if (pickRect.Contains(evt.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        s_PickOffset = evt.mousePosition.x - TimeToPixel(time, position);
                        evt.Use();
                        return TimeRulerDragMode.Start;
                    }
                    else if (pickAnywhere && position.Contains(evt.mousePosition))
                    {
                        GUIUtility.hotControl = id;

                        float newT = SnapTimeToWholeFPS(PixelToTime(evt.mousePosition.x, position), frameRate);
                        s_OriginalTime = time;
                        if (newT != time)
                            GUI.changed = true;
                        time = newT;
                        s_PickOffset = 0;
                        evt.Use();
                        return TimeRulerDragMode.Start;
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        float newT = SnapTimeToWholeFPS(PixelToTime(evt.mousePosition.x - s_PickOffset, position),
                                frameRate);
                        if (newT != time)
                            GUI.changed = true;
                        time = newT;

                        evt.Use();
                        return TimeRulerDragMode.Dragging;
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                        return TimeRulerDragMode.End;
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.hotControl == id && evt.keyCode == KeyCode.Escape)
                    {
                        if (time != s_OriginalTime)
                            GUI.changed = true;
                        time = s_OriginalTime;

                        GUIUtility.hotControl = 0;
                        evt.Use();
                        return TimeRulerDragMode.Cancel;
                    }
                    break;
            }
            return TimeRulerDragMode.None;
        }

        private float FrameToPixel(float i, float frameRate, Rect rect, Rect theShownArea)
        {
            return (i - theShownArea.xMin * frameRate) * rect.width / (theShownArea.width * frameRate);
        }

        public float FrameToPixel(float i, float frameRate, Rect rect)
        {
            return FrameToPixel(i, frameRate, rect, shownArea);
        }

        public float TimeField(Rect rect, int id, float time, float frameRate, TimeFormat timeFormat)
        {
            if (timeFormat == TimeFormat.None)
            {
                float newTime = EditorGUI.DoFloatField(
                        EditorGUI.s_RecycledEditor,
                        rect,
                        new Rect(0, 0, 0, 0),
                        id,
                        time,
                        EditorGUI.kFloatFieldFormatString,
                        EditorStyles.numberField,
                        false);

                return SnapTimeToWholeFPS(newTime, frameRate);
            }

            if (timeFormat == TimeFormat.Frame)
            {
                int frame = Mathf.RoundToInt(time * frameRate);

                int newFrame = EditorGUI.DoIntField(
                        EditorGUI.s_RecycledEditor,
                        rect,
                        new Rect(0, 0, 0, 0),
                        id,
                        frame,
                        EditorGUI.kIntFieldFormatString,
                        EditorStyles.numberField,
                        false,
                        0f);

                return (float)newFrame / frameRate;
            }
            else // if (timeFormat == TimeFormat.TimeFrame)
            {
                string str = FormatTime(time, frameRate, TimeFormat.TimeFrame);

                string allowedCharacters = "0123456789.,:";

                bool changed;
                str = EditorGUI.DoTextField(EditorGUI.s_RecycledEditor, id, rect, str, EditorStyles.numberField,
                        allowedCharacters, out changed, false, false, false);

                if (changed)
                {
                    if (GUIUtility.keyboardControl == id)
                    {
                        GUI.changed = true;

                        // Make sure that comma & period are interchangable.
                        str = str.Replace(',', '.');

                        // format is time:frame
                        int index = str.IndexOf(':');
                        if (index >= 0)
                        {
                            string timeStr = str.Substring(0, index);
                            string frameStr = str.Substring(index + 1);

                            int timeValue, frameValue;
                            if (int.TryParse(timeStr, out timeValue) && int.TryParse(frameStr, out frameValue))
                            {
                                float newTime = (float)timeValue + (float)frameValue / frameRate;
                                return newTime;
                            }
                        }
                        // format is floating time value.
                        else
                        {
                            float newTime;
                            if (float.TryParse(str, out newTime))
                            {
                                return SnapTimeToWholeFPS(newTime, frameRate);
                            }
                        }
                    }
                }
            }

            return time;
        }

        public float ValueField(Rect rect, int id, float value)
        {
            float newValue = EditorGUI.DoFloatField(
                    EditorGUI.s_RecycledEditor,
                    rect,
                    new Rect(0, 0, 0, 0),
                    id,
                    value,
                    EditorGUI.kFloatFieldFormatString,
                    EditorStyles.numberField,
                    false);

            return newValue;
        }

        public string FormatTime(float time, float frameRate, TimeFormat timeFormat)
        {
            if (timeFormat == TimeFormat.None)
            {
                int hDecimals;
                if (frameRate != 0)
                    hDecimals = MathUtils.GetNumberOfDecimalsForMinimumDifference(1 / frameRate);
                else
                    hDecimals = MathUtils.GetNumberOfDecimalsForMinimumDifference(shownArea.width / drawRect.width);

                return time.ToString("N" + hDecimals);
            }

            int frame = Mathf.RoundToInt(time * frameRate);

            if (timeFormat == TimeFormat.TimeFrame)
            {
                int frameDigits = frameRate != 0 ? ((int)frameRate - 1).ToString().Length : 1;
                string sign = string.Empty;
                if (frame < 0)
                {
                    sign = "-";
                    frame = -frame;
                }
                return sign + (frame / (int)frameRate).ToString() + ":" +
                    (frame % frameRate).ToString().PadLeft(frameDigits, '0');
            }
            else
            {
                return frame.ToString();
            }
        }

        public string FormatValue(float value)
        {
            int vDecimals = MathUtils.GetNumberOfDecimalsForMinimumDifference(shownArea.height / drawRect.height);
            return value.ToString("N" + vDecimals);
        }

        public float SnapTimeToWholeFPS(float time, float frameRate)
        {
            if (frameRate == 0)
                return time;
            return Mathf.Round(time * frameRate) / frameRate;
        }

        public void DrawTimeOnSlider(float time, Color c, float maxTime, float leftSidePadding = 0, float rightSidePadding = 0)
        {
            const float maxTimeFudgeFactor = 3;
            if (!hSlider)
                return;

            if (styles.horizontalScrollbar == null)
                styles.InitGUIStyles(false, true);

            var inMin = TimeToPixel(0, rect); // Assume 0 minTime
            var inMax = TimeToPixel(maxTime, rect);
            var outMin = TimeToPixel(shownAreaInsideMargins.xMin, rect) + styles.horizontalScrollbarLeftButton.fixedWidth + leftSidePadding;
            var outMax = TimeToPixel(shownAreaInsideMargins.xMax, rect) - (styles.horizontalScrollbarRightButton.fixedWidth + rightSidePadding);
            var x = (TimeToPixel(time, rect) - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
            if (x > rect.xMax - (styles.horizontalScrollbarLeftButton.fixedWidth + leftSidePadding + maxTimeFudgeFactor))
                return;
            var inset = styles.sliderWidth - styles.visualSliderWidth;
            var otherInset = (vSlider && hSlider) ? inset : 0;
            var hRangeSliderRect = new Rect(drawRect.x + 1, drawRect.yMax - inset, drawRect.width - otherInset, styles.sliderWidth);

            var p1 = new Vector2(x, hRangeSliderRect.yMin);
            var p2 = new Vector2(x, hRangeSliderRect.yMax);

            var lineRect = Rect.MinMaxRect(p1.x - 0.5f, p1.y, p2.x + 0.5f, p2.y);
            EditorGUI.DrawRect(lineRect, c);
        }
    }
}
