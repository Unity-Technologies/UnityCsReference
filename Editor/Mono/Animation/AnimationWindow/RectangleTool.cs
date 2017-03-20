// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    internal class RectangleTool
    {
        private TimeArea m_TimeArea;
        private Styles m_Styles;
        private bool m_RippleTimeClutch;

        internal enum ToolCoord
        {
            BottomLeft,
            Bottom,
            BottomRight,

            Left,
            Center,
            Right,

            TopLeft,
            Top,
            TopRight
        }

        internal class Styles
        {
            public GUIStyle rectangleToolHBarLeft = "RectangleToolHBarLeft";
            public GUIStyle rectangleToolHBarRight = "RectangleToolHBarRight";
            public GUIStyle rectangleToolHBar = "RectangleToolHBar";

            public GUIStyle rectangleToolVBarBottom = "RectangleToolVBarBottom";
            public GUIStyle rectangleToolVBarTop = "RectangleToolVBarTop";
            public GUIStyle rectangleToolVBar = "RectangleToolVBar";

            public GUIStyle rectangleToolSelection = "RectangleToolSelection";
            public GUIStyle rectangleToolHighlight = "RectangleToolHighlight";

            public GUIStyle rectangleToolScaleLeft = "RectangleToolScaleLeft";
            public GUIStyle rectangleToolScaleRight = "RectangleToolScaleRight";
            public GUIStyle rectangleToolScaleBottom = "RectangleToolScaleBottom";
            public GUIStyle rectangleToolScaleTop = "RectangleToolScaleTop";

            public GUIStyle dopesheetScaleLeft = "DopesheetScaleLeft";
            public GUIStyle dopesheetScaleRight = "DopesheetScaleRight";

            public GUIStyle dragLabel = "ProfilerBadge";
        }

        public TimeArea timeArea { get { return m_TimeArea; } }

        public Styles styles { get { return m_Styles; } }

        public bool rippleTimeClutch { get { return m_RippleTimeClutch; } }

        public Rect contentRect
        {
            get
            {
                return new Rect(0, 0, m_TimeArea.drawRect.width, m_TimeArea.drawRect.height);
            }
        }

        public virtual void Initialize(TimeArea timeArea)
        {
            m_TimeArea = timeArea;

            if (m_Styles == null)
                m_Styles = new Styles();
        }

        public Vector2 ToolCoordToPosition(ToolCoord coord, Bounds bounds)
        {
            switch (coord)
            {
                case ToolCoord.BottomLeft:
                    return bounds.min;
                case ToolCoord.Bottom:
                    return new Vector2(bounds.center.x, bounds.min.y);
                case ToolCoord.BottomRight:
                    return new Vector2(bounds.max.x, bounds.min.y);

                case ToolCoord.Left:
                    return new Vector2(bounds.min.x, bounds.center.y);
                case ToolCoord.Center:
                    return bounds.center;
                case ToolCoord.Right:
                    return new Vector2(bounds.max.x, bounds.center.y);

                case ToolCoord.TopLeft:
                    return new Vector2(bounds.min.x, bounds.max.y);
                case ToolCoord.Top:
                    return new Vector2(bounds.center.x, bounds.max.y);
                case ToolCoord.TopRight:
                    return bounds.max;
            }

            return Vector2.zero;
        }

        public bool CalculateScaleTimeMatrix(float fromTime, float toTime, float offsetTime, float pivotTime, float frameRate, out Matrix4x4 transform, out bool flipKeys)
        {
            transform = Matrix4x4.identity;
            flipKeys = false;

            float thresholdTime = (Mathf.Approximately(frameRate, 0f)) ? 0.001f : 1f / frameRate;

            float dtNum = toTime - pivotTime;
            float dtDenum = fromTime - pivotTime;

            // Scale handle overlaps pivot, discard operation.
            if ((Mathf.Abs(dtNum) - offsetTime) < 0f)
                return false;

            dtNum = (Mathf.Sign(dtNum) == Mathf.Sign(dtDenum)) ? dtNum - offsetTime : dtNum + offsetTime;

            if (Mathf.Approximately(dtDenum, 0f))
            {
                transform.SetTRS(new Vector3(dtNum, 0f, 0f), Quaternion.identity, Vector3.one);
                flipKeys = false;

                return true;
            }

            if (Mathf.Abs(dtNum) < thresholdTime)
                dtNum = (dtNum < 0f) ? -thresholdTime : thresholdTime;

            float scaleTime = dtNum / dtDenum;

            transform.SetTRS(new Vector3(pivotTime, 0f, 0f), Quaternion.identity, Vector3.one);
            transform = transform * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scaleTime, 1f, 1f));
            transform = transform * Matrix4x4.TRS(new Vector3(-pivotTime, 0f), Quaternion.identity, Vector3.one);

            flipKeys = scaleTime < 0f;

            return true;
        }

        public bool CalculateScaleValueMatrix(float fromValue, float toValue, float offsetValue, float pivotValue, out Matrix4x4 transform, out bool flipKeys)
        {
            transform = Matrix4x4.identity;
            flipKeys = false;

            float thresholdValue = 0.001f;

            float dvNum = toValue - pivotValue;
            float dvDenum = fromValue - pivotValue;

            // Scale handle overlaps pivot, discard operation.
            if ((Mathf.Abs(dvNum) - offsetValue) < 0f)
                return false;

            dvNum = (Mathf.Sign(dvNum) == Mathf.Sign(dvDenum)) ? dvNum - offsetValue : dvNum + offsetValue;

            if (Mathf.Approximately(dvDenum, 0f))
            {
                transform.SetTRS(new Vector3(0f, dvNum, 0f), Quaternion.identity, Vector3.one);
                flipKeys = false;

                return true;
            }

            if (Mathf.Abs(dvNum) < thresholdValue)
                dvNum = (dvNum < 0f) ? -thresholdValue : thresholdValue;

            float scaleValue = dvNum / dvDenum;

            transform.SetTRS(new Vector3(0f, pivotValue, 0f), Quaternion.identity, Vector3.one);
            transform = transform * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, scaleValue, 1f));
            transform = transform * Matrix4x4.TRS(new Vector3(0f, -pivotValue, 0f), Quaternion.identity, Vector3.one);

            flipKeys = scaleValue < 0f;

            return true;
        }

        public float PixelToTime(float pixelTime, float frameRate)
        {
            float width = contentRect.width;
            float visibleTimeSpan = m_TimeArea.shownArea.xMax - m_TimeArea.shownArea.xMin;
            float minVisibleTime = m_TimeArea.shownArea.xMin;

            float time = ((pixelTime / width) * visibleTimeSpan + minVisibleTime);
            if (frameRate != 0f)
                time = Mathf.Round(time * frameRate) / frameRate;

            return time;
        }

        public float PixelToValue(float pixelValue)
        {
            float height = contentRect.height;

            float pixelPerValue = m_TimeArea.m_Scale.y * -1f;
            float zeroValuePixel = m_TimeArea.shownArea.yMin * pixelPerValue * -1f;

            float value = (height - pixelValue - zeroValuePixel) / pixelPerValue;

            return value;
        }

        public float TimeToPixel(float time)
        {
            float width = contentRect.width;
            float visibleTimeSpan = m_TimeArea.shownArea.xMax - m_TimeArea.shownArea.xMin;
            float minVisibleTime = m_TimeArea.shownArea.xMin;

            float pixelTime = (time - minVisibleTime) * width / visibleTimeSpan;

            return pixelTime;
        }

        public float ValueToPixel(float value)
        {
            float height = contentRect.height;

            float pixelPerValue = m_TimeArea.m_Scale.y * -1f;
            float zeroValuePixel = m_TimeArea.shownArea.yMin * pixelPerValue * -1f;

            float pixelValue = height - (value * pixelPerValue + zeroValuePixel);

            return pixelValue;
        }

        public void HandleClutchKeys()
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.KeyDown:
                    if (evt.keyCode == KeyCode.R)
                    {
                        m_RippleTimeClutch = true;
                    }
                    break;

                case EventType.KeyUp:
                    if (evt.keyCode == KeyCode.R)
                    {
                        m_RippleTimeClutch = false;
                    }
                    break;
            }
        }
    }
}
