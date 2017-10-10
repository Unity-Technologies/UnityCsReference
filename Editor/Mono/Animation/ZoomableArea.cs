// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace UnityEditor
{
    // NOTE: do _not_ use GUILayout to get the rectangle for zoomable area,
    // will not work and will start failing miserably (mouse events not hitting it, etc.).
    // That's because ZoomableArea is using GUILayout itself, and needs an actual
    // physical rectangle.

    [System.Serializable]
    internal class ZoomableArea
    {
        // Global state
        private static Vector2 m_MouseDownPosition = new Vector2(-1000000, -1000000); // in transformed space
        private static int zoomableAreaHash = "ZoomableArea".GetHashCode();

        // Range lock settings
        [SerializeField] private bool m_HRangeLocked;
        [SerializeField] private bool m_VRangeLocked;
        public bool hRangeLocked { get { return m_HRangeLocked; } set { m_HRangeLocked = value; } }
        public bool vRangeLocked { get { return m_VRangeLocked; } set { m_VRangeLocked = value; } }


        [SerializeField] private float m_HBaseRangeMin = 0;
        [SerializeField] private float m_HBaseRangeMax = 1;
        [SerializeField] private float m_VBaseRangeMin = 0;
        [SerializeField] private float m_VBaseRangeMax = 1;
        public float hBaseRangeMin { get { return m_HBaseRangeMin; } set { m_HBaseRangeMin = value; } }
        public float hBaseRangeMax { get { return m_HBaseRangeMax; } set { m_HBaseRangeMax = value; } }
        public float vBaseRangeMin { get { return m_VBaseRangeMin; } set { m_VBaseRangeMin = value; } }
        public float vBaseRangeMax { get { return m_VBaseRangeMax; } set { m_VBaseRangeMax = value; } }
        [SerializeField] private bool m_HAllowExceedBaseRangeMin = true;
        [SerializeField] private bool m_HAllowExceedBaseRangeMax = true;
        [SerializeField] private bool m_VAllowExceedBaseRangeMin = true;
        [SerializeField] private bool m_VAllowExceedBaseRangeMax = true;
        public bool hAllowExceedBaseRangeMin { get { return m_HAllowExceedBaseRangeMin; } set { m_HAllowExceedBaseRangeMin = value; } }
        public bool hAllowExceedBaseRangeMax { get { return m_HAllowExceedBaseRangeMax; } set { m_HAllowExceedBaseRangeMax = value; } }
        public bool vAllowExceedBaseRangeMin { get { return m_VAllowExceedBaseRangeMin; } set { m_VAllowExceedBaseRangeMin = value; } }
        public bool vAllowExceedBaseRangeMax { get { return m_VAllowExceedBaseRangeMax; } set { m_VAllowExceedBaseRangeMax = value; } }
        public float hRangeMin
        {
            get { return (hAllowExceedBaseRangeMin ? Mathf.NegativeInfinity : hBaseRangeMin); }
            set { SetAllowExceed(ref m_HBaseRangeMin, ref m_HAllowExceedBaseRangeMin, value); }
        }
        public float hRangeMax
        {
            get { return (hAllowExceedBaseRangeMax ? Mathf.Infinity : hBaseRangeMax); }
            set { SetAllowExceed(ref m_HBaseRangeMax, ref m_HAllowExceedBaseRangeMax, value); }
        }
        public float vRangeMin
        {
            get { return (vAllowExceedBaseRangeMin ? Mathf.NegativeInfinity : vBaseRangeMin); }
            set { SetAllowExceed(ref m_VBaseRangeMin, ref m_VAllowExceedBaseRangeMin, value); }
        }
        public float vRangeMax
        {
            get { return (vAllowExceedBaseRangeMax ? Mathf.Infinity : vBaseRangeMax); }
            set { SetAllowExceed(ref m_VBaseRangeMax, ref m_VAllowExceedBaseRangeMax, value); }
        }
        private void SetAllowExceed(ref float rangeEnd, ref bool allowExceed, float value)
        {
            if (value == Mathf.NegativeInfinity || value == Mathf.Infinity)
            {
                rangeEnd = (value == Mathf.NegativeInfinity ? 0 : 1);
                allowExceed = true;
            }
            else
            {
                rangeEnd = value;
                allowExceed = false;
            }
        }

        private const float kMinScale = 0.00001f;
        private const float kMaxScale = 100000.0f;
        private float m_HScaleMin = kMinScale;
        private float m_HScaleMax = kMaxScale;
        private float m_VScaleMin = kMinScale;
        private float m_VScaleMax = kMaxScale;

        private const float kMinWidth = 0.05f;
        private const float kMinHeight = 0.05f;

        public float hScaleMin
        {
            get { return m_HScaleMin; }
            set { m_HScaleMin = Mathf.Clamp(value, kMinScale, kMaxScale); }
        }
        public float hScaleMax
        {
            get { return m_HScaleMax; }
            set { m_HScaleMax = Mathf.Clamp(value, kMinScale, kMaxScale); }
        }
        public float vScaleMin
        {
            get { return m_VScaleMin; }
            set { m_VScaleMin = Mathf.Clamp(value, kMinScale, kMaxScale); }
        }
        public float vScaleMax
        {
            get { return m_VScaleMax; }
            set { m_VScaleMax = Mathf.Clamp(value, kMinScale, kMaxScale); }
        }

        // Window resize settings
        [SerializeField] private bool m_ScaleWithWindow = false;
        public bool scaleWithWindow { get { return m_ScaleWithWindow; } set { m_ScaleWithWindow = value; } }

        // Slider settings
        [SerializeField] private bool m_HSlider = true;
        [SerializeField] private bool m_VSlider = true;
        public bool hSlider { get { return m_HSlider; } set { Rect r = rect; m_HSlider = value; rect = r; } }
        public bool vSlider { get { return m_VSlider; } set { Rect r = rect; m_VSlider = value; rect = r; } }

        [SerializeField] private bool m_IgnoreScrollWheelUntilClicked = false;
        public bool ignoreScrollWheelUntilClicked { get { return m_IgnoreScrollWheelUntilClicked; } set { m_IgnoreScrollWheelUntilClicked = value; } }

        [SerializeField] private bool m_EnableMouseInput = true;
        public bool enableMouseInput { get { return m_EnableMouseInput; } set { m_EnableMouseInput = value; } }

        [SerializeField] private bool m_EnableSliderZoom = true;

        public bool m_UniformScale;
        public bool uniformScale { get { return m_UniformScale; } set { m_UniformScale = value; } }

        // This is optional now, but used to be default behaviour because ZoomableAreas are mostly graphs with +Y being up
        public enum YDirection
        {
            Positive,
            Negative
        }
        [SerializeField] private YDirection m_UpDirection = YDirection.Positive;
        public YDirection upDirection
        {
            get
            {
                return m_UpDirection;
            }
            set
            {
                if (m_UpDirection != value)
                {
                    m_UpDirection = value;
                    m_Scale.y = -m_Scale.y;
                }
            }
        }

        // View and drawing settings
        [SerializeField] private Rect m_DrawArea = new Rect(0, 0, 100, 100);
        internal void SetDrawRectHack(Rect r) { m_DrawArea = r; }
        [SerializeField] internal Vector2 m_Scale = new Vector2(1, -1);
        [SerializeField] internal Vector2 m_Translation = new Vector2(0, 0);
        [SerializeField] private float m_MarginLeft, m_MarginRight, m_MarginTop, m_MarginBottom;
        [SerializeField] private Rect m_LastShownAreaInsideMargins = new Rect(0, 0, 100, 100);

        public Vector2 scale { get { return m_Scale; } }
        public Vector2 translation { get { return m_Translation; } }
        public float margin { set { m_MarginLeft = m_MarginRight = m_MarginTop = m_MarginBottom = value; } }
        public float leftmargin { get { return m_MarginLeft; } set { m_MarginLeft = value; } }
        public float rightmargin { get { return m_MarginRight; } set { m_MarginRight = value; } }
        public float topmargin { get { return m_MarginTop; } set { m_MarginTop = value; } }
        public float bottommargin { get { return m_MarginBottom; } set { m_MarginBottom = value; } }

        // IDs for scrollbars
        int verticalScrollbarID, horizontalScrollbarID;

        [SerializeField] bool m_MinimalGUI;

        public class Styles
        {
            public GUIStyle horizontalScrollbar;
            public GUIStyle horizontalMinMaxScrollbarThumb;
            public GUIStyle horizontalScrollbarLeftButton;
            public GUIStyle horizontalScrollbarRightButton;
            public GUIStyle verticalScrollbar;
            public GUIStyle verticalMinMaxScrollbarThumb;
            public GUIStyle verticalScrollbarUpButton;
            public GUIStyle verticalScrollbarDownButton;

            public float sliderWidth;
            public float visualSliderWidth;
            public Styles(bool minimalGUI)
            {
                if (minimalGUI)
                {
                    visualSliderWidth = 0;
                    sliderWidth = 15;
                }
                else
                {
                    visualSliderWidth = 15;
                    sliderWidth = 15;
                }
            }

            public void InitGUIStyles(bool minimalGUI, bool enableSliderZoom)
            {
                if (minimalGUI)
                {
                    horizontalMinMaxScrollbarThumb = enableSliderZoom ? "MiniMinMaxSliderHorizontal" : "MiniSliderhorizontal";
                    horizontalScrollbarLeftButton = GUIStyle.none;
                    horizontalScrollbarRightButton = GUIStyle.none;
                    horizontalScrollbar = GUIStyle.none;
                    verticalMinMaxScrollbarThumb = enableSliderZoom ? "MiniMinMaxSlidervertical" : "MiniSliderVertical";
                    verticalScrollbarUpButton = GUIStyle.none;
                    verticalScrollbarDownButton = GUIStyle.none;
                    verticalScrollbar = GUIStyle.none;
                }
                else
                {
                    horizontalMinMaxScrollbarThumb = enableSliderZoom ? "horizontalMinMaxScrollbarThumb" : "horizontalscrollbarthumb";
                    horizontalScrollbarLeftButton = "horizontalScrollbarLeftbutton";
                    horizontalScrollbarRightButton = "horizontalScrollbarRightbutton";
                    horizontalScrollbar = GUI.skin.horizontalScrollbar;
                    verticalMinMaxScrollbarThumb = enableSliderZoom ? "verticalMinMaxScrollbarThumb" : "verticalscrollbarthumb";
                    verticalScrollbarUpButton = "verticalScrollbarUpbutton";
                    verticalScrollbarDownButton = "verticalScrollbarDownbutton";
                    verticalScrollbar = GUI.skin.verticalScrollbar;
                }
            }
        }

        private Styles m_Styles;
        private Styles styles
        {
            get
            {
                if (m_Styles == null)
                    m_Styles = new Styles(m_MinimalGUI);
                return m_Styles;
            }
        }

        public Rect rect
        {
            get { return new Rect(drawRect.x, drawRect.y, drawRect.width + (m_VSlider ? styles.visualSliderWidth : 0), drawRect.height + (m_HSlider ? styles.visualSliderWidth : 0)); }
            set
            {
                Rect newDrawArea = new Rect(value.x, value.y, value.width - (m_VSlider ? styles.visualSliderWidth : 0), value.height - (m_HSlider ? styles.visualSliderWidth : 0));
                if (newDrawArea != m_DrawArea)
                {
                    if (m_ScaleWithWindow)
                    {
                        m_DrawArea = newDrawArea;
                        shownAreaInsideMargins = m_LastShownAreaInsideMargins;
                    }
                    else
                    {
                        m_Translation += new Vector2((newDrawArea.width - m_DrawArea.width) / 2, (newDrawArea.height - m_DrawArea.height) / 2);
                        m_DrawArea = newDrawArea;
                    }
                }
                EnforceScaleAndRange();
            }
        }
        public Rect drawRect { get { return m_DrawArea; } }

        public void SetShownHRangeInsideMargins(float min, float max)
        {
            float widthInsideMargins = drawRect.width - leftmargin - rightmargin;
            if (widthInsideMargins < kMinWidth) widthInsideMargins = kMinWidth;

            float denum = max - min;
            if (denum < kMinWidth) denum = kMinWidth;

            m_Scale.x = widthInsideMargins / denum;

            m_Translation.x = -min * m_Scale.x + leftmargin;
            EnforceScaleAndRange();
        }

        public void SetShownHRange(float min, float max)
        {
            float denum = max - min;
            if (denum < kMinWidth) denum = kMinWidth;

            m_Scale.x = drawRect.width / denum;

            m_Translation.x = -min * m_Scale.x;
            EnforceScaleAndRange();
        }

        public void SetShownVRangeInsideMargins(float min, float max)
        {
            if (m_UpDirection == YDirection.Positive)
            {
                m_Scale.y = -(drawRect.height - topmargin - bottommargin) / (max - min);
                m_Translation.y = drawRect.height - min * m_Scale.y - topmargin;
            }
            else
            {
                m_Scale.y = (drawRect.height - topmargin - bottommargin) / (max - min);
                m_Translation.y = -min * m_Scale.y - bottommargin;
            }
            EnforceScaleAndRange();
        }

        public void SetShownVRange(float min, float max)
        {
            if (m_UpDirection == YDirection.Positive)
            {
                m_Scale.y = -drawRect.height / (max - min);
                m_Translation.y = drawRect.height - min * m_Scale.y;
            }
            else
            {
                m_Scale.y = drawRect.height / (max - min);
                m_Translation.y = -min * m_Scale.y;
            }
            EnforceScaleAndRange();
        }

        // ShownArea is in curve space
        public Rect shownArea
        {
            set
            {
                float width = (value.width < kMinWidth) ? kMinWidth : value.width;
                float height = (value.height < kMinHeight) ? kMinHeight : value.height;

                if (m_UpDirection == YDirection.Positive)
                {
                    m_Scale.x = drawRect.width / width;
                    m_Scale.y = -drawRect.height / height;
                    m_Translation.x = -value.x * m_Scale.x;
                    m_Translation.y = drawRect.height - value.y * m_Scale.y;
                }
                else
                {
                    m_Scale.x = drawRect.width / width;
                    m_Scale.y = drawRect.height / height;
                    m_Translation.x = -value.x * m_Scale.x;
                    m_Translation.y = -value.y * m_Scale.y;
                }
                EnforceScaleAndRange();
            }
            get
            {
                if (m_UpDirection == YDirection.Positive)
                {
                    return new Rect(
                        -m_Translation.x / m_Scale.x,
                        -(m_Translation.y - drawRect.height) / m_Scale.y,
                        drawRect.width / m_Scale.x,
                        drawRect.height / -m_Scale.y
                        );
                }
                else
                {
                    return new Rect(
                        -m_Translation.x / m_Scale.x,
                        -m_Translation.y / m_Scale.y,
                        drawRect.width / m_Scale.x,
                        drawRect.height / m_Scale.y
                        );
                }
            }
        }

        public Rect shownAreaInsideMargins
        {
            set
            {
                shownAreaInsideMarginsInternal = value;
                EnforceScaleAndRange();
            }
            get
            {
                return shownAreaInsideMarginsInternal;
            }
        }

        private Rect shownAreaInsideMarginsInternal
        {
            set
            {
                float width = (value.width < kMinWidth) ? kMinWidth : value.width;
                float height = (value.height < kMinHeight) ? kMinHeight : value.height;

                float widthInsideMargins = drawRect.width - leftmargin - rightmargin;
                if (widthInsideMargins < kMinWidth) widthInsideMargins = kMinWidth;

                float heightInsideMargins = drawRect.height - topmargin - bottommargin;
                if (heightInsideMargins < kMinHeight) heightInsideMargins = kMinHeight;

                if (m_UpDirection == YDirection.Positive)
                {
                    m_Scale.x = widthInsideMargins / width;
                    m_Scale.y = -heightInsideMargins / height;
                    m_Translation.x = -value.x * m_Scale.x + leftmargin;
                    m_Translation.y = drawRect.height - value.y * m_Scale.y - topmargin;
                }
                else
                {
                    m_Scale.x = widthInsideMargins / width;
                    m_Scale.y = heightInsideMargins / height;
                    m_Translation.x = -value.x * m_Scale.x + leftmargin;
                    m_Translation.y = -value.y * m_Scale.y + topmargin;
                }
            }
            get
            {
                float leftmarginRel = leftmargin / m_Scale.x;
                float rightmarginRel = rightmargin / m_Scale.x;
                float topmarginRel = topmargin / m_Scale.y;
                float bottommarginRel = bottommargin / m_Scale.y;

                Rect area = shownArea;
                area.x += leftmarginRel;
                area.y -= topmarginRel;
                area.width -= leftmarginRel + rightmarginRel;
                area.height += topmarginRel + bottommarginRel;
                return area;
            }
        }

        public virtual Bounds drawingBounds
        {
            get
            {
                return new Bounds(
                    new Vector3((hBaseRangeMin + hBaseRangeMax) * 0.5f, (vBaseRangeMin + vBaseRangeMax) * 0.5f, 0),
                    new Vector3(hBaseRangeMax - hBaseRangeMin, vBaseRangeMax - vBaseRangeMin, 1)
                    );
            }
        }


        // Utility transform functions

        public Matrix4x4 drawingToViewMatrix
        {
            get
            {
                return Matrix4x4.TRS(m_Translation, Quaternion.identity, new Vector3(m_Scale.x, m_Scale.y, 1));
            }
        }

        public Vector2 DrawingToViewTransformPoint(Vector2 lhs)
        { return new Vector2(lhs.x * m_Scale.x + m_Translation.x, lhs.y * m_Scale.y + m_Translation.y); }
        public Vector3 DrawingToViewTransformPoint(Vector3 lhs)
        { return new Vector3(lhs.x * m_Scale.x + m_Translation.x, lhs.y * m_Scale.y + m_Translation.y, 0); }

        public Vector2 ViewToDrawingTransformPoint(Vector2 lhs)
        { return new Vector2((lhs.x - m_Translation.x) / m_Scale.x , (lhs.y - m_Translation.y) / m_Scale.y); }
        public Vector3 ViewToDrawingTransformPoint(Vector3 lhs)
        { return new Vector3((lhs.x - m_Translation.x) / m_Scale.x , (lhs.y - m_Translation.y) / m_Scale.y, 0); }

        public Vector2 DrawingToViewTransformVector(Vector2 lhs)
        { return new Vector2(lhs.x * m_Scale.x, lhs.y * m_Scale.y); }
        public Vector3 DrawingToViewTransformVector(Vector3 lhs)
        { return new Vector3(lhs.x * m_Scale.x, lhs.y * m_Scale.y, 0); }

        public Vector2 ViewToDrawingTransformVector(Vector2 lhs)
        { return new Vector2(lhs.x / m_Scale.x, lhs.y / m_Scale.y); }
        public Vector3 ViewToDrawingTransformVector(Vector3 lhs)
        { return new Vector3(lhs.x / m_Scale.x, lhs.y / m_Scale.y, 0); }

        public Vector2 mousePositionInDrawing
        {
            get { return ViewToDrawingTransformPoint(Event.current.mousePosition); }
        }

        public Vector2 NormalizeInViewSpace(Vector2 vec)
        {
            vec = Vector2.Scale(vec, m_Scale);
            vec /= vec.magnitude;
            return Vector2.Scale(vec, new Vector2(1 / m_Scale.x, 1 / m_Scale.y));
        }

        // Utility mouse event functions

        private bool IsZoomEvent()
        {
            return (
                (Event.current.button == 1 && Event.current.alt) // right+alt drag
                //|| (Event.current.button == 0 && Event.current.command) // left+commend drag
                //|| (Event.current.button == 2 && Event.current.command) // middle+command drag

                );
        }

        private bool IsPanEvent()
        {
            return (
                (Event.current.button == 0 && Event.current.alt) // left+alt drag
                || (Event.current.button == 2 && !Event.current.command) // middle drag
                );
        }

        public ZoomableArea()
        {
            m_MinimalGUI = false;
        }

        public ZoomableArea(bool minimalGUI)
        {
            m_MinimalGUI = minimalGUI;
        }

        public ZoomableArea(bool minimalGUI, bool enableSliderZoom)
        {
            m_MinimalGUI = minimalGUI;
            m_EnableSliderZoom = enableSliderZoom;
        }

        public void BeginViewGUI()
        {
            if (styles.horizontalScrollbar == null)
                styles.InitGUIStyles(m_MinimalGUI, m_EnableSliderZoom);

            if (enableMouseInput)
                HandleZoomAndPanEvents(m_DrawArea);

            horizontalScrollbarID = GUIUtility.GetControlID(EditorGUIExt.s_MinMaxSliderHash, FocusType.Passive);
            verticalScrollbarID = GUIUtility.GetControlID(EditorGUIExt.s_MinMaxSliderHash, FocusType.Passive);

            if (!m_MinimalGUI || Event.current.type != EventType.Repaint)
                SliderGUI();
        }

        public void HandleZoomAndPanEvents(Rect area)
        {
            GUILayout.BeginArea(area);

            area.x = 0;
            area.y = 0;
            int id = GUIUtility.GetControlID(zoomableAreaHash, FocusType.Passive, area);

            switch (Event.current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (area.Contains(Event.current.mousePosition))
                    {
                        // Catch keyboard control when clicked inside zoomable area
                        // (used to restrict scrollwheel)
                        GUIUtility.keyboardControl = id;

                        if (IsZoomEvent() || IsPanEvent())
                        {
                            GUIUtility.hotControl = id;
                            m_MouseDownPosition = mousePositionInDrawing;

                            Event.current.Use();
                        }
                    }
                    break;
                case EventType.MouseUp:
                    //Debug.Log("mouse-up!");
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;

                        // If we got the mousedown, the mouseup is ours as well
                        // (no matter if the click was in the area or not)
                        m_MouseDownPosition = new Vector2(-1000000, -1000000);
                        //Event.current.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != id) break;

                    if (IsZoomEvent())
                    {
                        // Zoom in around mouse down position
                        HandleZoomEvent(m_MouseDownPosition, false);
                        Event.current.Use();
                    }
                    else if (IsPanEvent())
                    {
                        // Pan view
                        Pan();
                        Event.current.Use();
                    }
                    break;
                case EventType.ScrollWheel:
                    if (!area.Contains(Event.current.mousePosition))
                        break;
                    if (m_IgnoreScrollWheelUntilClicked && GUIUtility.keyboardControl != id)
                        break;

                    // Zoom in around cursor position
                    HandleZoomEvent(mousePositionInDrawing, true);
                    Event.current.Use();
                    break;
            }

            GUILayout.EndArea();
        }

        public void EndViewGUI()
        {
            if (m_MinimalGUI && Event.current.type == EventType.Repaint)
                SliderGUI();
        }

        void SliderGUI()
        {
            if (!m_HSlider && !m_VSlider)
                return;

            using (new EditorGUI.DisabledScope(!enableMouseInput))
            {
                Bounds editorBounds = drawingBounds;
                Rect area = shownAreaInsideMargins;
                float min, max;
                float inset = styles.sliderWidth - styles.visualSliderWidth;
                float otherInset = (vSlider && hSlider) ? inset : 0;

                Vector2 scaleDelta = m_Scale;
                // Horizontal range slider
                if (m_HSlider)
                {
                    Rect hRangeSliderRect = new Rect(drawRect.x + 1, drawRect.yMax - inset, drawRect.width - otherInset, styles.sliderWidth);
                    float shownXRange = area.width;
                    float shownXMin = area.xMin;
                    if (m_EnableSliderZoom)
                    {
                        EditorGUIExt.MinMaxScroller(hRangeSliderRect, horizontalScrollbarID,
                            ref shownXMin, ref shownXRange,
                            editorBounds.min.x, editorBounds.max.x,
                            Mathf.NegativeInfinity, Mathf.Infinity,
                            styles.horizontalScrollbar, styles.horizontalMinMaxScrollbarThumb,
                            styles.horizontalScrollbarLeftButton, styles.horizontalScrollbarRightButton, true);
                    }
                    else
                    {
                        shownXMin = GUI.Scroller(hRangeSliderRect,
                                shownXMin, shownXRange, editorBounds.min.x, editorBounds.max.x,
                                styles.horizontalScrollbar, styles.horizontalMinMaxScrollbarThumb,
                                styles.horizontalScrollbarLeftButton, styles.horizontalScrollbarRightButton, true);
                    }
                    min = shownXMin;
                    max = shownXMin + shownXRange;
                    if (min > area.xMin)
                        min = Mathf.Min(min, max - rect.width / m_HScaleMax);
                    if (max < area.xMax)
                        max = Mathf.Max(max, min + rect.width / m_HScaleMax);
                    SetShownHRangeInsideMargins(min, max);
                }

                // Vertical range slider
                // Reverse y values since y increses upwards for the draw area but downwards for the slider
                if (m_VSlider)
                {
                    if (m_UpDirection == YDirection.Positive)
                    {
                        Rect vRangeSliderRect = new Rect(drawRect.xMax - inset, drawRect.y, styles.sliderWidth, drawRect.height - otherInset);
                        float shownYRange = area.height;
                        float shownYMin = -area.yMax;
                        if (m_EnableSliderZoom)
                        {
                            EditorGUIExt.MinMaxScroller(vRangeSliderRect, verticalScrollbarID,
                                ref shownYMin, ref shownYRange,
                                -editorBounds.max.y, -editorBounds.min.y,
                                Mathf.NegativeInfinity, Mathf.Infinity,
                                styles.verticalScrollbar, styles.verticalMinMaxScrollbarThumb,
                                styles.verticalScrollbarUpButton, styles.verticalScrollbarDownButton, false);
                        }
                        else
                        {
                            shownYMin = GUI.Scroller(vRangeSliderRect,
                                    shownYMin, shownYRange, -editorBounds.max.y, -editorBounds.min.y,
                                    styles.verticalScrollbar, styles.verticalMinMaxScrollbarThumb,
                                    styles.verticalScrollbarUpButton, styles.verticalScrollbarDownButton, false);
                        }
                        min = -(shownYMin + shownYRange);
                        max = -shownYMin;
                        if (min > area.yMin)
                            min = Mathf.Min(min, max - rect.height / m_VScaleMax);
                        if (max < area.yMax)
                            max = Mathf.Max(max, min + rect.height / m_VScaleMax);
                        SetShownVRangeInsideMargins(min, max);
                    }
                    else
                    {
                        Rect vRangeSliderRect = new Rect(drawRect.xMax - inset, drawRect.y, styles.sliderWidth, drawRect.height - otherInset);
                        float shownYRange = area.height;
                        float shownYMin = area.yMin;
                        if (m_EnableSliderZoom)
                        {
                            EditorGUIExt.MinMaxScroller(vRangeSliderRect, verticalScrollbarID,
                                ref shownYMin, ref shownYRange,
                                editorBounds.min.y, editorBounds.max.y,
                                Mathf.NegativeInfinity, Mathf.Infinity,
                                styles.verticalScrollbar, styles.verticalMinMaxScrollbarThumb,
                                styles.verticalScrollbarUpButton, styles.verticalScrollbarDownButton, false);
                        }
                        else
                        {
                            shownYMin = GUI.Scroller(vRangeSliderRect,
                                    shownYMin, shownYRange, editorBounds.min.y, editorBounds.max.y,
                                    styles.verticalScrollbar, styles.verticalMinMaxScrollbarThumb,
                                    styles.verticalScrollbarUpButton, styles.verticalScrollbarDownButton, false);
                        }
                        min = shownYMin;
                        max = shownYMin + shownYRange;
                        if (min > area.yMin)
                            min = Mathf.Min(min, max - rect.height / m_VScaleMax);
                        if (max < area.yMax)
                            max = Mathf.Max(max, min + rect.height / m_VScaleMax);
                        SetShownVRangeInsideMargins(min, max);
                    }
                }

                if (uniformScale)
                {
                    float aspect = drawRect.width / drawRect.height;
                    scaleDelta -= m_Scale;
                    var delta = new Vector2(-scaleDelta.y * aspect, -scaleDelta.x / aspect);

                    m_Scale -= delta;
                    m_Translation.x -= scaleDelta.y / 2;
                    m_Translation.y -= scaleDelta.x / 2;
                    EnforceScaleAndRange();
                }
            }
        }

        private void Pan()
        {
            if (!m_HRangeLocked)
                m_Translation.x += Event.current.delta.x;
            if (!m_VRangeLocked)
                m_Translation.y += Event.current.delta.y;

            EnforceScaleAndRange();
        }

        private void HandleZoomEvent(Vector2 zoomAround, bool scrollwhell)
        {
            // Get delta (from scroll wheel or mouse pad)
            // Add x and y delta to cover all cases
            // (scrool view has only y or only x when shift is pressed,
            // while mouse pad has both x and y at all times)
            float delta = Event.current.delta.x + Event.current.delta.y;

            if (scrollwhell)
                delta = -delta;

            // Scale multiplier. Don't allow scale of zero or below!
            float scale = Mathf.Max(0.01F, 1 + delta * 0.01F);

            // Cap scale when at min width to not "glide" away when zooming closer
            float width = shownAreaInsideMargins.width;
            if (width / scale <= kMinWidth)
                return;

            SetScaleFocused(zoomAround, scale * m_Scale, Event.current.shift, EditorGUI.actionKey);
        }

        // Sets a new scale, keeping focalPoint in the same relative position
        public void SetScaleFocused(Vector2 focalPoint, Vector2 newScale)
        {
            SetScaleFocused(focalPoint, newScale, false, false);
        }

        public void SetScaleFocused(Vector2 focalPoint, Vector2 newScale, bool lockHorizontal, bool lockVertical)
        {
            if (uniformScale)
                lockHorizontal = lockVertical = false;

            if (!m_HRangeLocked && !lockHorizontal)
            {
                // Offset to make zoom centered around cursor position
                m_Translation.x -= focalPoint.x * (newScale.x - m_Scale.x);

                // Apply zooming
                m_Scale.x = newScale.x;
            }
            if (!m_VRangeLocked && !lockVertical)
            {
                // Offset to make zoom centered around cursor position
                m_Translation.y -= focalPoint.y * (newScale.y - m_Scale.y);

                // Apply zooming
                m_Scale.y = newScale.y;
            }

            EnforceScaleAndRange();
        }

        public void SetTransform(Vector2 newTranslation, Vector2 newScale)
        {
            m_Scale = newScale;
            m_Translation = newTranslation;
            EnforceScaleAndRange();
        }

        public void EnforceScaleAndRange()
        {
            // Minimum scale might also be constrained by maximum range
            float constrainedHScaleMin = rect.width / m_HScaleMin;
            float constrainedVScaleMin = rect.height / m_VScaleMin;
            if (hRangeMax != Mathf.Infinity && hRangeMin != Mathf.NegativeInfinity)
                constrainedHScaleMin = Mathf.Min(constrainedHScaleMin, hRangeMax - hRangeMin);
            if (vRangeMax != Mathf.Infinity && vRangeMin != Mathf.NegativeInfinity)
                constrainedVScaleMin = Mathf.Min(constrainedVScaleMin, vRangeMax - vRangeMin);

            Rect oldArea = m_LastShownAreaInsideMargins;
            Rect newArea = shownAreaInsideMargins;
            if (newArea == oldArea)
                return;

            float epsilon = 0.00001f;

            if (newArea.width < oldArea.width - epsilon)
            {
                float xLerp = Mathf.InverseLerp(oldArea.width, newArea.width, rect.width / m_HScaleMax);
                newArea = new Rect(
                        Mathf.Lerp(oldArea.x, newArea.x, xLerp),
                        newArea.y,
                        Mathf.Lerp(oldArea.width, newArea.width, xLerp),
                        newArea.height
                        );
            }
            if (newArea.height < oldArea.height - epsilon)
            {
                float yLerp = Mathf.InverseLerp(oldArea.height, newArea.height, rect.height / m_VScaleMax);
                newArea = new Rect(
                        newArea.x,
                        Mathf.Lerp(oldArea.y, newArea.y, yLerp),
                        newArea.width,
                        Mathf.Lerp(oldArea.height, newArea.height, yLerp)
                        );
            }
            if (newArea.width > oldArea.width + epsilon)
            {
                float xLerp = Mathf.InverseLerp(oldArea.width, newArea.width, constrainedHScaleMin);
                newArea = new Rect(
                        Mathf.Lerp(oldArea.x, newArea.x, xLerp),
                        newArea.y,
                        Mathf.Lerp(oldArea.width, newArea.width, xLerp),
                        newArea.height
                        );
            }
            if (newArea.height > oldArea.height + epsilon)
            {
                float yLerp = Mathf.InverseLerp(oldArea.height, newArea.height, constrainedVScaleMin);
                newArea = new Rect(
                        newArea.x,
                        Mathf.Lerp(oldArea.y, newArea.y, yLerp),
                        newArea.width,
                        Mathf.Lerp(oldArea.height, newArea.height, yLerp)
                        );
            }

            // Enforce ranges
            if (newArea.xMin < hRangeMin)
                newArea.x = hRangeMin;
            if (newArea.xMax > hRangeMax)
                newArea.x = hRangeMax - newArea.width;
            if (newArea.yMin < vRangeMin)
                newArea.y = vRangeMin;
            if (newArea.yMax > vRangeMax)
                newArea.y = vRangeMax - newArea.height;

            shownAreaInsideMarginsInternal = newArea;
            m_LastShownAreaInsideMargins = newArea;
        }

        public float PixelToTime(float pixelX, Rect rect)
        {
            return ((pixelX - rect.x) * shownArea.width / rect.width + shownArea.x);
        }

        public float TimeToPixel(float time, Rect rect)
        {
            return (time - shownArea.x) / shownArea.width * rect.width + rect.x;
        }

        public float PixelDeltaToTime(Rect rect)
        {
            return shownArea.width / rect.width;
        }
    }

    [System.Serializable]
    class TimeArea : ZoomableArea
    {
        [SerializeField] private TickHandler m_HTicks;
        public TickHandler hTicks { get { return m_HTicks; } set { m_HTicks = value; } }
        [SerializeField] private TickHandler m_VTicks;
        public TickHandler vTicks { get { return m_VTicks; } set { m_VTicks = value; } }

        internal const int kTickRulerDistMin   =  3;// min distance between ruler tick marks before they disappear completely
        internal const int kTickRulerDistFull  = 80;// distance between ruler tick marks where they gain full strength
        internal const int kTickRulerDistLabel = 40; // min distance between ruler tick mark labels
        internal const float kTickRulerHeightMax    = 0.7f;// height of the ruler tick marks when they are highest
        internal const float kTickRulerFatThreshold = 0.5f; // size of ruler tick marks at which they begin getting fatter

        public enum TimeFormat
        {
            None,       // Unformatted time
            TimeFrame,  // Time:Frame
            Frame       // Integer frame
        };

        class Styles2
        {
            public GUIStyle timelineTick = "AnimationTimelineTick";
            public GUIStyle labelTickMarks = "CurveEditorLabelTickMarks";
            public GUIStyle playhead = "AnimationPlayHead";
        }
        static Styles2 styles;

        static void InitStyles()
        {
            if (styles == null)
                styles = new Styles2();
        }

        public TimeArea(bool minimalGUI) : base(minimalGUI)
        {
            float[] modulos = new float[] {
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

            Color tickColor = styles.timelineTick.normal.textColor;
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

        public void TimeRuler(Rect position, float frameRate, bool labels, bool useEntireHeight, float alpha, TimeFormat timeFormat)
        {
            Color backupCol = GUI.color;
            GUI.BeginGroup(position);
            InitStyles();

            HandleUtility.ApplyWireMaterial();

            Color tempBackgroundColor = GUI.backgroundColor;

            SetTickMarkerRanges();
            hTicks.SetTickStrengths(kTickRulerDistMin, kTickRulerDistFull, true);

            Color baseColor = styles.timelineTick.normal.textColor;
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

                        float height = useEntireHeight ? position.height : position.height * Mathf.Min(1, strength) * kTickRulerHeightMax;
                        float x = FrameToPixel(frame, frameRate, position, cachedShowArea);

                        // Draw line
                        DrawVerticalLineFast(x, position.height - height + 0.5f, position.height - 0.5f, new Color(1, 1, 1, strength / kTickRulerFatThreshold) * baseColor);
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
                    GUI.Label(new Rect(labelpos + 3, -3, 40, 20), label, styles.timelineTick);
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
            Color lineColor = styles.playhead.normal.textColor.AlphaMultiplied(alpha);
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
            None, Start, End, Dragging, Cancel
        }
        static float s_OriginalTime;
        static float s_PickOffset;
        public TimeRulerDragMode BrowseRuler(Rect position, ref float time, float frameRate, bool pickAnywhere, GUIStyle thumbStyle)
        {
            int id = GUIUtility.GetControlID(3126789, FocusType.Passive);
            return BrowseRuler(position, id, ref time, frameRate, pickAnywhere, thumbStyle);
        }

        public TimeRulerDragMode BrowseRuler(Rect position, int id, ref float time, float frameRate, bool pickAnywhere, GUIStyle thumbStyle)
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
                        thumbStyle.Draw(pickRect, id == GUIUtility.hotControl, hover || id == GUIUtility.hotControl, false, false);
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
                        float newT = SnapTimeToWholeFPS(PixelToTime(evt.mousePosition.x - s_PickOffset, position), frameRate);
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
                str = EditorGUI.DoTextField(EditorGUI.s_RecycledEditor, id, rect, str, EditorStyles.numberField, allowedCharacters, out changed, false, false, false);

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
                    hDecimals =  MathUtils.GetNumberOfDecimalsForMinimumDifference(shownArea.width / drawRect.width);

                return time.ToString("N" + hDecimals);
            }

            int frame = Mathf.RoundToInt(time * frameRate);

            if (timeFormat == TimeFormat.TimeFrame)
            {
                int frameDigits = ((int)frameRate).ToString().Length;
                string sign = string.Empty;
                if (frame < 0)
                {
                    sign = "-";
                    frame = -frame;
                }
                return sign + (frame / (int)frameRate).ToString() + ":" + (frame % frameRate).ToString().PadLeft(frameDigits, '0');
            }
            else
            {
                return frame.ToString();
            }
        }

        public string FormatValue(float value)
        {
            int vDecimals =  MathUtils.GetNumberOfDecimalsForMinimumDifference(shownArea.height / drawRect.height);
            return value.ToString("N" + vDecimals);
        }

        public float SnapTimeToWholeFPS(float time, float frameRate)
        {
            if (frameRate == 0)
                return time;
            return Mathf.Round(time * frameRate) / frameRate;
        }
    }
} // namespace
