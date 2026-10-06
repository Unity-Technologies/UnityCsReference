// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.JobsProfiling;

[System.Serializable]
internal class ZoomableArea
{
    // Global state
    [NoAutoStaticsCleanup] private static Vector2 m_MouseDownPosition = new Vector2(-1000000, -1000000); // in transformed space
    [NoAutoStaticsCleanup] private static int zoomableAreaHash2 = "ZoomableArea".GetHashCode();

    // Range lock settings
    [SerializeField] private bool m_HRangeLocked;
    [SerializeField] private bool m_VRangeLocked;
    internal bool hRangeLocked { get { return m_HRangeLocked; } set { m_HRangeLocked = value; } }
    internal bool vRangeLocked { get { return m_VRangeLocked; } set { m_VRangeLocked = value; } }
    // Zoom lock settings
    internal bool hZoomLockedByDefault = false;
    internal bool vZoomLockedByDefault = false;

    [SerializeField] private float m_HBaseRangeMin = 0;
    [SerializeField] private float m_HBaseRangeMax = 1;
    [SerializeField] private float m_VBaseRangeMin = 0;
    [SerializeField] private float m_VBaseRangeMax = 1;
    internal float hBaseRangeMin { get { return m_HBaseRangeMin; } set { m_HBaseRangeMin = value; } }
    internal float hBaseRangeMax { get { return m_HBaseRangeMax; } set { m_HBaseRangeMax = value; } }
    internal float vBaseRangeMin { get { return m_VBaseRangeMin; } set { m_VBaseRangeMin = value; } }
    internal float vBaseRangeMax { get { return m_VBaseRangeMax; } set { m_VBaseRangeMax = value; } }
    [SerializeField] private bool m_HAllowExceedBaseRangeMin = true;
    [SerializeField] private bool m_HAllowExceedBaseRangeMax = true;
    [SerializeField] private bool m_VAllowExceedBaseRangeMin = true;
    [SerializeField] private bool m_VAllowExceedBaseRangeMax = true;
    internal bool hAllowExceedBaseRangeMin { get { return m_HAllowExceedBaseRangeMin; } set { m_HAllowExceedBaseRangeMin = value; } }
    internal bool hAllowExceedBaseRangeMax { get { return m_HAllowExceedBaseRangeMax; } set { m_HAllowExceedBaseRangeMax = value; } }
    internal bool vAllowExceedBaseRangeMin { get { return m_VAllowExceedBaseRangeMin; } set { m_VAllowExceedBaseRangeMin = value; } }
    internal bool vAllowExceedBaseRangeMax { get { return m_VAllowExceedBaseRangeMax; } set { m_VAllowExceedBaseRangeMax = value; } }
    internal float hRangeMin
    {
        get { return (hAllowExceedBaseRangeMin ? Mathf.NegativeInfinity : hBaseRangeMin); }
        set { SetAllowExceed(ref m_HBaseRangeMin, ref m_HAllowExceedBaseRangeMin, value); }
    }
    internal float hRangeMax
    {
        get { return (hAllowExceedBaseRangeMax ? Mathf.Infinity : hBaseRangeMax); }
        set { SetAllowExceed(ref m_HBaseRangeMax, ref m_HAllowExceedBaseRangeMax, value); }
    }
    internal float vRangeMin
    {
        get { return (vAllowExceedBaseRangeMin ? Mathf.NegativeInfinity : vBaseRangeMin); }
        set { SetAllowExceed(ref m_VBaseRangeMin, ref m_VAllowExceedBaseRangeMin, value); }
    }
    internal float vRangeMax
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

    private float m_MinWidth = 0.01f;
    private const float kMinHeight = 0.05f;

    private const float k_ScrollStepSize = 10f; // mirrors GUI scrollstepsize as there is no global const for this.

    internal float minWidth
    {
        get { return m_MinWidth; }
        set
        {
            if (value > 0)
                m_MinWidth = value;
            else
            {
                Debug.LogWarning("Zoomable area width cannot have a value of " +
                    "or below 0. Reverting back to a default value of 0.05f");
                m_MinWidth = 0.05f;
            }
        }
    }

    internal float hScaleMin
    {
        get { return m_HScaleMin; }
        set
        {
            m_HScaleMin = Mathf.Clamp(value, kMinScale, kMaxScale);
            //styles.enableSliderZoomHorizontal = allowSliderZoomHorizontal;
        }
    }
    internal float hScaleMax
    {
        get { return m_HScaleMax; }
        set
        {
            m_HScaleMax = Mathf.Clamp(value, kMinScale, kMaxScale);
            //styles.enableSliderZoomHorizontal = allowSliderZoomHorizontal;
        }
    }
    internal float vScaleMin
    {
        get { return m_VScaleMin; }
        set
        {
            m_VScaleMin = Mathf.Clamp(value, kMinScale, kMaxScale);
            //styles.enableSliderZoomVertical = allowSliderZoomVertical;
        }
    }
    internal float vScaleMax
    {
        get { return m_VScaleMax; }
        set
        {
            m_VScaleMax = Mathf.Clamp(value, kMinScale, kMaxScale);
            //styles.enableSliderZoomVertical = allowSliderZoomVertical;
        }
    }


    // Window resize settings
    [SerializeField] private bool m_ScaleWithWindow = false;
    internal bool scaleWithWindow { get { return m_ScaleWithWindow; } set { m_ScaleWithWindow = value; } }

    // Slider settings
    //[SerializeField] private bool m_HSlider = false;
    //[SerializeField] private bool m_VSlider = false;
    //internal bool hSlider { get { return m_HSlider; } set { Rect r = rect; m_HSlider = value; rect = r; } }
    //internal bool vSlider { get { return m_VSlider; } set { Rect r = rect; m_VSlider = value; rect = r; } }

    [SerializeField] private bool m_IgnoreScrollWheelUntilClicked = false;
    internal bool ignoreScrollWheelUntilClicked { get { return m_IgnoreScrollWheelUntilClicked; } set { m_IgnoreScrollWheelUntilClicked = value; } }

    [SerializeField] private bool m_EnableMouseInput = true;
    internal bool enableMouseInput { get { return m_EnableMouseInput; } set { m_EnableMouseInput = value; } }

    [SerializeField] private bool m_EnableSliderZoomHorizontal = true;
    [SerializeField] private bool m_EnableSliderZoomVertical = true;

    // if the min and max scaling does not allow for actual zooming, there is no point in allowing it
    protected bool allowSliderZoomHorizontal { get { return m_EnableSliderZoomHorizontal && m_HScaleMin < m_HScaleMax; } }
    protected bool allowSliderZoomVertical { get { return m_EnableSliderZoomVertical && m_VScaleMin < m_VScaleMax; } }

    internal bool m_UniformScale;
    internal bool uniformScale { get { return m_UniformScale; } set { m_UniformScale = value; } }

    // This is optional now, but used to be default behaviour because ZoomableAreas are mostly graphs with +Y being up
    internal enum YDirection
    {
        Positive,
        Negative
    }
    [SerializeField] private YDirection m_UpDirection = YDirection.Positive;
    internal YDirection upDirection
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
    [SerializeField] internal float m_TempYTransition = 0.0f;
    [SerializeField] private float m_MarginLeft, m_MarginRight, m_MarginTop, m_MarginBottom;
    [SerializeField] private Rect m_LastShownAreaInsideMargins = new Rect(0, 0, 10, 10);

    internal Vector2 scale { get { return m_Scale; } }
    internal Vector2 translation { get { return m_Translation; } }
    internal float temp_y { get { return m_TempYTransition; } set { m_TempYTransition = value; } }
    internal float margin { set { m_MarginLeft = m_MarginRight = m_MarginTop = m_MarginBottom = value; } }
    internal float leftmargin { get { return m_MarginLeft; } set { m_MarginLeft = value; } }
    internal float rightmargin { get { return m_MarginRight; } set { m_MarginRight = value; } }
    internal float topmargin { get { return m_MarginTop; } set { m_MarginTop = value; } }
    internal float bottommargin { get { return m_MarginBottom; } set { m_MarginBottom = value; } }
    //internal float vSliderWidth { get { return vSlider ? styles.sliderWidth : 0f; } }
    //internal float hSliderHeight { get { return hSlider ? styles.sliderWidth : 0f; } }

    // IDs for controls
    internal int areaControlID;
    //int verticalScrollbarID, horizontalScrollbarID;

    [SerializeField] bool m_MinimalGUI;

    internal Rect rect
    {
        get { return new Rect(drawRect.x, drawRect.y, drawRect.width + (/*m_VSlider ? styles.visualSliderWidth :*/ 0), drawRect.height + (/*m_HSlider ? styles.visualSliderWidth :*/ 0)); }
        set
        {
            Rect newDrawArea = new Rect(value.x, value.y, value.width - (/*m_VSlider ? styles.visualSliderWidth :*/ 0), value.height - (/*m_HSlider ? styles.visualSliderWidth :*/ 0));
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
    internal Rect drawRect { get { return m_DrawArea; } }

    internal void SetShownHRangeInsideMargins(float min, float max)
    {
        float widthInsideMargins = drawRect.width - leftmargin - rightmargin;
        if (widthInsideMargins < m_MinWidth) widthInsideMargins = m_MinWidth;

        float denum = max - min;
        if (denum < m_MinWidth) denum = m_MinWidth;

        m_Scale.x = widthInsideMargins / denum;

        m_Translation.x = -min * m_Scale.x + leftmargin;
        EnforceScaleAndRange();
    }

    internal void SetShownHRange(float min, float max)
    {
        float denum = max - min;
        if (denum < m_MinWidth) denum = m_MinWidth;

        m_Scale.x = drawRect.width / denum;

        m_Translation.x = -min * m_Scale.x;
        EnforceScaleAndRange();
    }

    internal void SetShownVRangeInsideMargins(float min, float max)
    {
        float heightInsideMargins = drawRect.height - topmargin - bottommargin;
        if (heightInsideMargins < kMinHeight) heightInsideMargins = kMinHeight;

        float denum = max - min;
        if (denum < kMinHeight) denum = kMinHeight;

        if (m_UpDirection == YDirection.Positive)
        {
            m_Scale.y = -heightInsideMargins / denum;
            m_Translation.y = drawRect.height - min * m_Scale.y - topmargin;
        }
        else
        {
            m_Scale.y = heightInsideMargins / denum;
            m_Translation.y = -min * m_Scale.y - bottommargin;
        }
        EnforceScaleAndRange();
    }

    internal void SetShownVRange(float min, float max)
    {
        float denum = max - min;
        if (denum < kMinHeight) denum = kMinHeight;

        if (m_UpDirection == YDirection.Positive)
        {
            m_Scale.y = -drawRect.height / denum;
            m_Translation.y = drawRect.height - min * m_Scale.y;
        }
        else
        {
            m_Scale.y = drawRect.height / denum;
            m_Translation.y = -min * m_Scale.y;
        }
        EnforceScaleAndRange();
    }

    // ShownArea is in curve space
    internal Rect shownArea
    {
        set
        {
            float width = (value.width < m_MinWidth) ? m_MinWidth : value.width;
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

    internal Rect shownAreaInsideMargins
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
            float width = (value.width < m_MinWidth) ? m_MinWidth : value.width;
            float height = (value.height < kMinHeight) ? kMinHeight : value.height;

            float widthInsideMargins = drawRect.width - leftmargin - rightmargin;
            if (widthInsideMargins < m_MinWidth) widthInsideMargins = m_MinWidth;

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

    float GetWidthInsideMargins(float widthWithMargins, bool substractSliderWidth = false)
    {
        float width = (widthWithMargins < m_MinWidth) ? m_MinWidth : widthWithMargins;
        float widthInsideMargins = width - leftmargin - rightmargin - (substractSliderWidth ? (/*m_VSlider ? styles.visualSliderWidth :*/ 0) : 0);
        return Mathf.Max(widthInsideMargins, m_MinWidth);
    }

    float GetHeightInsideMargins(float heightWithMargins, bool substractSliderHeight = false)
    {
        float height = (heightWithMargins < kMinHeight) ? kMinHeight : heightWithMargins;
        float heightInsideMargins = height - topmargin - bottommargin - (substractSliderHeight ? (/*m_HSlider ? styles.visualSliderWidth :*/ 0) : 0);
        return Mathf.Max(heightInsideMargins, kMinHeight);
    }

    internal virtual Bounds drawingBounds
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

    internal Matrix4x4 drawingToViewMatrix
    {
        get
        {
            return Matrix4x4.TRS(m_Translation, Quaternion.identity, new Vector3(m_Scale.x, m_Scale.y, 1));
        }
    }

    internal Vector2 DrawingToViewTransformPoint(Vector2 lhs)
    { return new Vector2(lhs.x * m_Scale.x + m_Translation.x, lhs.y * m_Scale.y + m_Translation.y); }
    internal Vector3 DrawingToViewTransformPoint(Vector3 lhs)
    { return new Vector3(lhs.x * m_Scale.x + m_Translation.x, lhs.y * m_Scale.y + m_Translation.y, 0); }

    internal Vector2 ViewToDrawingTransformPoint(Vector2 lhs)
    { return new Vector2((lhs.x - m_Translation.x) / m_Scale.x, (lhs.y - m_Translation.y) / m_Scale.y); }
    internal Vector3 ViewToDrawingTransformPoint(Vector3 lhs)
    { return new Vector3((lhs.x - m_Translation.x) / m_Scale.x, (lhs.y - m_Translation.y) / m_Scale.y, 0); }

    internal Vector2 DrawingToViewTransformVector(Vector2 lhs)
    { return new Vector2(lhs.x * m_Scale.x, lhs.y * m_Scale.y); }
    internal Vector3 DrawingToViewTransformVector(Vector3 lhs)
    { return new Vector3(lhs.x * m_Scale.x, lhs.y * m_Scale.y, 0); }

    internal Vector2 ViewToDrawingTransformVector(Vector2 lhs)
    { return new Vector2(lhs.x / m_Scale.x, lhs.y / m_Scale.y); }
    internal Vector3 ViewToDrawingTransformVector(Vector3 lhs)
    { return new Vector3(lhs.x / m_Scale.x, lhs.y / m_Scale.y, 0); }

    internal Vector2 mousePositionInDrawing(Vector2 mousePos)
    {
        return ViewToDrawingTransformPoint(mousePos);
    }

    internal Vector2 NormalizeInViewSpace(Vector2 vec)
    {
        vec = Vector2.Scale(vec, m_Scale);
        vec /= vec.magnitude;
        return Vector2.Scale(vec, new Vector2(1 / m_Scale.x, 1 / m_Scale.y));
    }

    // Utility mouse event functions

    private bool IsZoomEventDown(PointerDownEvent evnt)
    {
        return (evnt.button == 1 && evnt.altKey); // right+alt drag
    }

    internal bool IsZoomEventMove(PointerMoveEvent evnt)
    {
        return ((evnt.pressedButtons & 2) == 2) && evnt.altKey; // right+alt drag
    }

    private bool IsPanEventDown(PointerDownEvent evnt)
    {
        return ((evnt.button == 0 && evnt.altKey) || evnt.button == 2); // left+alt drag or middle drag
    }

    private bool IsPanEventMove(PointerMoveEvent evnt)
    {
        int pressedButtons = evnt.pressedButtons;
        // left+alt drag or middle drag
        return ((pressedButtons & 1) == 1 && evnt.altKey) ||
                (pressedButtons & 4) == 4;
    }

    internal ZoomableArea()
    {
        m_MinimalGUI = true;
    }

    internal ZoomableArea(bool minimalGUI)
    {
        m_MinimalGUI = minimalGUI;
    }

    internal ZoomableArea(bool minimalGUI, bool enableSliderZoom) : this(minimalGUI, enableSliderZoom, enableSliderZoom) { }

    internal ZoomableArea(bool minimalGUI, bool enableSliderZoomHorizontal, bool enableSliderZoomVertical)
    {
        m_MinimalGUI = minimalGUI;
        m_EnableSliderZoomHorizontal = enableSliderZoomHorizontal;
        m_EnableSliderZoomVertical = enableSliderZoomVertical;
    }

    internal void UpdatePointerDown(PointerDownEvent evnt)
    {
        if (IsZoomEventDown(evnt) || IsPanEventDown(evnt))
        {
            m_MouseDownPosition = mousePositionInDrawing(evnt.localPosition);
        }
    }
    internal void UpdatePointerUpEvent(PointerUpEvent evnt)
    {
    }
    internal void UpdatePointerMove(PointerMoveEvent evnt)
    {
        if (IsZoomEventMove(evnt))
        {
            Vector2 mousePos = m_MouseDownPosition;
            // Zoom in around mouse down position
            HandleZoomEvent(mousePos, false);
        }
        else if (IsPanEventMove(evnt))
        {
            // Pan view
            Pan(evnt);
        }
    }


    internal void UpdateHorizontalScrolling(Vector2 range)
    {
        UpdateRangeMargins(range.x, range.y);
    }

    internal void UpdateScrollers(MinMaxSlider horizontal, Scroller vertical)
    {
        Bounds editorBounds = drawingBounds;
        Rect area = shownAreaInsideMargins;

        float min, max;
        float shownXRange = area.width;
        float shownXMin = area.xMin;

        min = shownXMin;
        max = shownXMin + shownXRange;

        float visualStart = Mathf.Min(min, editorBounds.min.x);
        float visualEnd = Mathf.Max(max, editorBounds.max.x);

        horizontal.minValue = visualStart;
        horizontal.maxValue = visualEnd;
        horizontal.lowLimit = visualStart;
        horizontal.highLimit = visualEnd;

        horizontal.value = new Vector2(min, max);

        UpdateRangeMargins(min, max);

        vertical.value = -temp_y;
    }

    void UpdateRangeMargins(float min, float max)
    {
        Rect area = shownAreaInsideMargins;
        float rectWidthWithinMargins = GetWidthInsideMargins(rect.width, true);

        if (min > area.xMin)
            min = Mathf.Min(min, max - rectWidthWithinMargins / m_HScaleMax);
        if (max < area.xMax)
            max = Mathf.Max(max, min + rectWidthWithinMargins / m_HScaleMax);

        SetShownHRangeInsideMargins(min, max);
    }

    private void Pan(PointerMoveEvent evnt)
    {
        m_Translation.x += evnt.deltaPosition.x;
        m_Translation.y += evnt.deltaPosition.y;
        m_TempYTransition += evnt.deltaPosition.y;

        if (m_TempYTransition > 0.0f)
            m_TempYTransition = 0.0f;

        EnforceScaleAndRange();
    }

    internal void ScrollWheelZoom(Vector2 mousePos)
    {
        m_MouseDownPosition = mousePositionInDrawing(mousePos);
        HandleZoomEvent(m_MouseDownPosition, true);
    }

    private void HandleZoomEvent(Vector2 zoomAround, bool scrollwhell)
    {
        // Get delta (from scroll wheel or mouse pad)
        // Add x and y delta to cover all cases
        // (scroll wheel has only y or only x when shift is pressed,
        // while mouse pad has both x and y at all times)
        float delta = Event.current.delta.x + Event.current.delta.y;

        if (scrollwhell)
            delta = -delta;

        // Scale multiplier. Don't allow scale of zero or below!
        float scale = Mathf.Max(0.01F, 1 + delta * 0.01F);

        // Cap scale when at min width to not "glide" away when zooming closer
        float width = shownAreaInsideMargins.width;
        if (width / scale <= m_MinWidth)
            return;

        //SetScaleFocused(zoomAround, scale * m_Scale, Event.current.shift, EditorGUI.actionKey);
        SetScaleFocused(zoomAround, scale * m_Scale, Event.current.shift, false);
    }

    // Sets a new scale, keeping focalPoint in the same relative position
    internal void SetScaleFocused(Vector2 focalPoint, Vector2 newScale)
    {
        SetScaleFocused(focalPoint, newScale, false, false);
    }

    internal void SetScaleFocused(Vector2 focalPoint, Vector2 newScale, bool lockHorizontal, bool lockVertical)
    {
        if (uniformScale)
            lockHorizontal = lockVertical = false;
        else
        {
            // if an axis is locked by default, it is as if that modifier key is permanently held down
            // actually pressing the key then lifts the lock. In other words, LockedByDefault acts like an inversion.
            if (hZoomLockedByDefault)
                lockHorizontal = !lockHorizontal;

            if (hZoomLockedByDefault)
                lockVertical = !lockVertical;
        }

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

    internal void SetTransform(Vector2 newTranslation, Vector2 newScale)
    {
        m_Scale = newScale;
        m_Translation = newTranslation;
        EnforceScaleAndRange();
    }

    internal void EnforceScaleAndRange()
    {
        Rect oldArea = m_LastShownAreaInsideMargins;
        Rect newArea = shownAreaInsideMargins;
        if (newArea == oldArea)
            return;

        float minChange = 0.01f;

        if (!Mathf.Approximately(newArea.width, oldArea.width))
        {
            float constrainedWidth = newArea.width;
            if (newArea.width < oldArea.width)
            {
                // The shown area decreasing in size means the scale is increasing. This happens e.g. while zooming in.
                // Only the max scale restricts the shown area size here, range has no influence.
                constrainedWidth = GetWidthInsideMargins(drawRect.width / m_HScaleMax, false);
            }
            else
            {
                constrainedWidth = GetWidthInsideMargins(drawRect.width / m_HScaleMin, false);

                if (hRangeMax != Mathf.Infinity && hRangeMin != Mathf.NegativeInfinity)
                {
                    // range only has an influence if it is enforced, i.e. not infinity
                    float denum = hRangeMax - hRangeMin;
                    if (denum < m_MinWidth) denum = m_MinWidth;

                    constrainedWidth = Mathf.Min(constrainedWidth, denum);
                }
            }

            float xLerp = Mathf.InverseLerp(oldArea.width, newArea.width, constrainedWidth);
            float newWidth = Mathf.Lerp(oldArea.width, newArea.width, xLerp);
            float widthChange = Mathf.Abs(newWidth - newArea.width);
            newArea = new Rect(
                // only affect the position if there was any significant change in width
                // this fixes an issue where if width was only different due to rounding issues, position changes are ignored as xLerp comes back 0 (or very nearly 0)
                widthChange > minChange ? Mathf.Lerp(oldArea.x, newArea.x, xLerp) : newArea.x,
                newArea.y,
                newWidth,
                newArea.height
            );
        }
        if (!Mathf.Approximately(newArea.height, oldArea.height))
        {
            float constrainedHeight = newArea.height;
            if (newArea.height < oldArea.height)
            {
                // The shown area decreasing in size means the scale is increasing. This happens e.g. while zooming in.
                // Only the max scale restricts the shown area size here, range has no influence.
                constrainedHeight = GetHeightInsideMargins(drawRect.height / m_VScaleMax, false);
            }
            else
            {
                constrainedHeight = GetHeightInsideMargins(drawRect.height / m_VScaleMin, false);

                if (vRangeMax != Mathf.Infinity && vRangeMin != Mathf.NegativeInfinity)
                {
                    // range only has an influence if it is enforced, i.e. not infinity
                    float denum = vRangeMax - vRangeMin;
                    if (denum < kMinHeight) denum = kMinHeight;
                    constrainedHeight = Mathf.Min(constrainedHeight, denum);
                }
            }

            float yLerp = Mathf.InverseLerp(oldArea.height, newArea.height, constrainedHeight);
            float newHeight = Mathf.Lerp(oldArea.height, newArea.height, yLerp);
            float heightChange = Mathf.Abs(newHeight - newArea.height);
            newArea = new Rect(
                newArea.x,
                // only affect the position if there was any significant change in height
                // this fixes an issue where if height was only different due to rounding issues, position changes are ignored as yLerp comes back 0 (or very nearly 0)
                heightChange > minChange ? Mathf.Lerp(oldArea.y, newArea.y, yLerp) : newArea.y,
                newArea.width,
                newHeight
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
        m_LastShownAreaInsideMargins = shownAreaInsideMargins;
    }

    internal float PixelToTime(float pixelX, Rect rect)
    {
        Rect area = shownArea;
        return ((pixelX - rect.x) * area.width / rect.width + area.x);
    }

    internal float TimeToPixel(float time, Rect rect)
    {
        Rect area = shownArea;
        return (time - area.x) / area.width * rect.width + rect.x;
    }

    internal float PixelDeltaToTime(Rect rect)
    {
        return shownArea.width / rect.width;
    }

    internal void UpdateZoomScale(float fMaxScaleValue, float fMinScaleValue)
    {
        // Update/reset the values of the scale to new zoom range, if the current values do not fall in the range of the new resolution

        if (m_Scale.y > fMaxScaleValue || m_Scale.y < fMinScaleValue)
        {
            m_Scale.y = m_Scale.y > fMaxScaleValue ? fMaxScaleValue : fMinScaleValue;
        }

        if (m_Scale.x > fMaxScaleValue || m_Scale.x < fMinScaleValue)
        {
            m_Scale.x = m_Scale.x > fMaxScaleValue ? fMaxScaleValue : fMinScaleValue;
        }
    }
}
