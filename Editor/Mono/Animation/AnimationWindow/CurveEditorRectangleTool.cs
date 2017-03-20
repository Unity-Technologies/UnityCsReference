// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using RectangleToolFlags = UnityEditor.CurveEditorSettings.RectangleToolFlags;

namespace UnityEditor
{
    internal class CurveEditorRectangleTool : RectangleTool
    {
        const int kHBarMinWidth = 14;
        const int kHBarHeight = 13;

        const int kHBarLeftWidth = 15;
        const int kHBarLeftHeight = 13;
        const int kHBarRightWidth = 15;
        const int kHBarRightHeight = 13;

        const int kHLabelMarginHorizontal = 3;
        const int kHLabelMarginVertical = 1;

        const int kVBarMinHeight = 15;
        const int kVBarWidth = 13;

        const int kVBarBottomWidth = 13;
        const int kVBarBottomHeight = 15;
        const int kVBarTopWidth = 13;
        const int kVBarTopHeight = 15;

        const int kVLabelMarginHorizontal = 1;
        const int kVLabelMarginVertical = 2;

        const int kScaleLeftWidth = 17;
        const int kScaleLeftMarginHorizontal = 0;
        const float kScaleLeftRatio = 0.80f;

        const int kScaleRightWidth = 17;
        const int kScaleRightMarginHorizontal = 0;
        const float kScaleRightRatio = 0.80f;

        const int kScaleBottomHeight = 17;
        const int kScaleBottomMarginVertical = 0;
        const float kScaleBottomRatio = 0.80f;

        const int kScaleTopHeight = 17;
        const int kScaleTopMarginVertical = 0;
        const float kScaleTopRatio = 0.80f;

        static Rect g_EmptyRect = new Rect(0f, 0f, 0f, 0f);

        struct ToolLayout
        {
            public Rect selectionRect;

            public Rect hBarRect;
            public Rect hBarLeftRect;
            public Rect hBarRightRect;

            public bool displayHScale;

            public Rect vBarRect;
            public Rect vBarBottomRect;
            public Rect vBarTopRect;

            public bool displayVScale;

            public Rect selectionLeftRect;
            public Rect selectionTopRect;

            public Rect underlayTopRect;
            public Rect underlayLeftRect;

            public Rect scaleLeftRect;
            public Rect scaleRightRect;
            public Rect scaleTopRect;
            public Rect scaleBottomRect;

            public Vector2 leftLabelAnchor;
            public Vector2 rightLabelAnchor;
            public Vector2 bottomLabelAnchor;
            public Vector2 topLabelAnchor;
        }

        private CurveEditor m_CurveEditor;

        private ToolLayout m_Layout;

        private Vector2 m_Pivot;
        private Vector2 m_Previous;
        private Vector2 m_MouseOffset;

        enum DragMode
        {
            None = 0,
            MoveHorizontal = 1 << 0,
            MoveVertical = 1 << 1,
            MoveBothAxis = MoveHorizontal | MoveVertical,
            ScaleHorizontal = 1 << 2,
            ScaleVertical = 1 << 3,
            ScaleBothAxis = ScaleHorizontal | ScaleVertical,
            MoveScaleHorizontal = MoveHorizontal | ScaleHorizontal,
            MoveScaleVertical = MoveVertical | ScaleVertical
        }

        private DragMode m_DragMode;
        private bool m_RippleTime;
        private float m_RippleTimeStart;
        private float m_RippleTimeEnd;

        private AreaManipulator m_HBarLeft;
        private AreaManipulator m_HBarRight;
        private AreaManipulator m_HBar;

        private AreaManipulator m_VBarBottom;
        private AreaManipulator m_VBarTop;
        private AreaManipulator m_VBar;

        private AreaManipulator m_SelectionBox;

        private AreaManipulator m_SelectionScaleLeft;
        private AreaManipulator m_SelectionScaleRight;
        private AreaManipulator m_SelectionScaleBottom;
        private AreaManipulator m_SelectionScaleTop;

        private bool hasSelection { get { return m_CurveEditor.hasSelection && !m_CurveEditor.IsDraggingCurveOrRegion(); } }
        private Bounds selectionBounds { get { return m_CurveEditor.selectionBounds; } }

        private float frameRate { get { return m_CurveEditor.invSnap; } }

        private DragMode dragMode
        {
            get
            {
                if (m_DragMode != DragMode.None)
                    return m_DragMode;

                if (m_CurveEditor.IsDraggingKey())
                    return DragMode.MoveBothAxis;

                return DragMode.None;
            }
        }

        public override void Initialize(TimeArea timeArea)
        {
            base.Initialize(timeArea);
            m_CurveEditor = timeArea as CurveEditor;

            if (m_HBarLeft == null)
            {
                m_HBarLeft = new AreaManipulator(styles.rectangleToolHBarLeft, MouseCursor.ResizeHorizontal);

                m_HBarLeft.onStartDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        if (hasSelection && manipulator.rect.Contains(evt.mousePosition))
                        {
                            OnStartScale(ToolCoord.Right, ToolCoord.Left, new Vector2(PixelToTime(evt.mousePosition.x, frameRate), 0f), DragMode.ScaleHorizontal, rippleTimeClutch);
                            return true;
                        }

                        return false;
                    };
                m_HBarLeft.onDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnScaleTime(PixelToTime(evt.mousePosition.x, frameRate));
                        return true;
                    };
                m_HBarLeft.onEndDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnEndScale();
                        return true;
                    };
            }

            if (m_HBarRight == null)
            {
                m_HBarRight = new AreaManipulator(styles.rectangleToolHBarRight, MouseCursor.ResizeHorizontal);

                m_HBarRight.onStartDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        if (hasSelection && manipulator.rect.Contains(evt.mousePosition))
                        {
                            OnStartScale(ToolCoord.Left, ToolCoord.Right, new Vector2(PixelToTime(evt.mousePosition.x, frameRate), 0f), DragMode.ScaleHorizontal, rippleTimeClutch);
                            return true;
                        }

                        return false;
                    };
                m_HBarRight.onDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnScaleTime(PixelToTime(evt.mousePosition.x, frameRate));
                        return true;
                    };
                m_HBarRight.onEndDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnEndScale();
                        return true;
                    };
            }

            if (m_HBar == null)
            {
                m_HBar = new AreaManipulator(styles.rectangleToolHBar, MouseCursor.MoveArrow);

                m_HBar.onStartDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        if (hasSelection && manipulator.rect.Contains(evt.mousePosition))
                        {
                            OnStartMove(new Vector2(PixelToTime(evt.mousePosition.x, frameRate), 0f), DragMode.MoveHorizontal, rippleTimeClutch);
                            return true;
                        }

                        return false;
                    };
                m_HBar.onDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnMove(new Vector2(PixelToTime(evt.mousePosition.x, frameRate), 0f));
                        return true;
                    };
                m_HBar.onEndDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnEndMove();
                        return true;
                    };
            }

            if (m_VBarBottom == null)
            {
                m_VBarBottom = new AreaManipulator(styles.rectangleToolVBarBottom, MouseCursor.ResizeVertical);

                m_VBarBottom.onStartDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        if (hasSelection && manipulator.rect.Contains(evt.mousePosition))
                        {
                            OnStartScale(ToolCoord.Top, ToolCoord.Bottom, new Vector2(0f, PixelToValue(evt.mousePosition.y)), DragMode.ScaleVertical, false);
                            return true;
                        }

                        return false;
                    };
                m_VBarBottom.onDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnScaleValue(PixelToValue(evt.mousePosition.y));
                        return true;
                    };
                m_VBarBottom.onEndDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnEndScale();
                        return true;
                    };
            }

            if (m_VBarTop == null)
            {
                m_VBarTop = new AreaManipulator(styles.rectangleToolVBarTop, MouseCursor.ResizeVertical);

                m_VBarTop.onStartDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        if (hasSelection && manipulator.rect.Contains(evt.mousePosition))
                        {
                            OnStartScale(ToolCoord.Bottom, ToolCoord.Top, new Vector2(0f, PixelToValue(evt.mousePosition.y)), DragMode.ScaleVertical, false);
                            return true;
                        }

                        return false;
                    };
                m_VBarTop.onDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnScaleValue(PixelToValue(evt.mousePosition.y));
                        return true;
                    };
                m_VBarTop.onEndDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnEndScale();
                        return true;
                    };
            }

            if (m_VBar == null)
            {
                m_VBar = new AreaManipulator(styles.rectangleToolVBar, MouseCursor.MoveArrow);

                m_VBar.onStartDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        if (hasSelection && manipulator.rect.Contains(evt.mousePosition))
                        {
                            OnStartMove(new Vector2(0f, PixelToValue(evt.mousePosition.y)), DragMode.MoveVertical, false);
                            return true;
                        }

                        return false;
                    };
                m_VBar.onDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnMove(new Vector2(0f, PixelToValue(evt.mousePosition.y)));
                        return true;
                    };
                m_VBar.onEndDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnEndMove();
                        return true;
                    };
            }

            if (m_SelectionBox == null)
            {
                m_SelectionBox = new AreaManipulator(styles.rectangleToolSelection, MouseCursor.MoveArrow);

                m_SelectionBox.onStartDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        bool curveEditorOverride = evt.shift || EditorGUI.actionKey;
                        if (!curveEditorOverride && hasSelection && manipulator.rect.Contains(evt.mousePosition))
                        {
                            OnStartMove(new Vector2(PixelToTime(evt.mousePosition.x, frameRate), PixelToValue(evt.mousePosition.y)), rippleTimeClutch ? DragMode.MoveHorizontal : DragMode.MoveBothAxis, rippleTimeClutch);
                            return true;
                        }

                        return false;
                    };
                m_SelectionBox.onDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        // Only drag along x OR y when shift is held down
                        if (evt.shift && m_DragMode == DragMode.MoveBothAxis)
                        {
                            float deltaX = evt.mousePosition.x - TimeToPixel(m_Previous.x);
                            float deltaY = evt.mousePosition.y - ValueToPixel(m_Previous.y);
                            m_DragMode = Mathf.Abs(deltaX) > Mathf.Abs(deltaY) ? DragMode.MoveHorizontal : DragMode.MoveVertical;
                        }

                        float posX = ((m_DragMode & DragMode.MoveHorizontal) != 0) ? PixelToTime(evt.mousePosition.x, frameRate) : m_Previous.x;
                        float posY = ((m_DragMode & DragMode.MoveVertical) != 0) ? PixelToValue(evt.mousePosition.y) : m_Previous.y;

                        OnMove(new Vector2(posX, posY));
                        return true;
                    };
                m_SelectionBox.onEndDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnEndMove();
                        return true;
                    };
            }

            if (m_SelectionScaleLeft == null)
            {
                m_SelectionScaleLeft = new AreaManipulator(styles.rectangleToolScaleLeft, MouseCursor.ResizeHorizontal);

                m_SelectionScaleLeft.onStartDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        if (hasSelection && manipulator.rect.Contains(evt.mousePosition))
                        {
                            OnStartScale(ToolCoord.Right, ToolCoord.Left, new Vector2(PixelToTime(evt.mousePosition.x, frameRate), 0f), DragMode.ScaleHorizontal, rippleTimeClutch);
                            return true;
                        }

                        return false;
                    };
                m_SelectionScaleLeft.onDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnScaleTime(PixelToTime(evt.mousePosition.x, frameRate));
                        return true;
                    };
                m_SelectionScaleLeft.onEndDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnEndScale();
                        return true;
                    };
            }

            if (m_SelectionScaleRight == null)
            {
                m_SelectionScaleRight = new AreaManipulator(styles.rectangleToolScaleRight, MouseCursor.ResizeHorizontal);

                m_SelectionScaleRight.onStartDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        if (hasSelection && manipulator.rect.Contains(evt.mousePosition))
                        {
                            OnStartScale(ToolCoord.Left, ToolCoord.Right, new Vector2(PixelToTime(evt.mousePosition.x, frameRate), 0f), DragMode.ScaleHorizontal, rippleTimeClutch);
                            return true;
                        }

                        return false;
                    };
                m_SelectionScaleRight.onDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnScaleTime(PixelToTime(evt.mousePosition.x, frameRate));
                        return true;
                    };
                m_SelectionScaleRight.onEndDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnEndScale();
                        return true;
                    };
            }

            if (m_SelectionScaleBottom == null)
            {
                m_SelectionScaleBottom = new AreaManipulator(styles.rectangleToolScaleBottom, MouseCursor.ResizeVertical);

                m_SelectionScaleBottom.onStartDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        if (hasSelection && manipulator.rect.Contains(evt.mousePosition))
                        {
                            OnStartScale(ToolCoord.Top, ToolCoord.Bottom, new Vector2(0f, PixelToValue(evt.mousePosition.y)), DragMode.ScaleVertical, false);
                            return true;
                        }

                        return false;
                    };
                m_SelectionScaleBottom.onDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnScaleValue(PixelToValue(evt.mousePosition.y));
                        return true;
                    };
                m_SelectionScaleBottom.onEndDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnEndScale();
                        return true;
                    };
            }

            if (m_SelectionScaleTop == null)
            {
                m_SelectionScaleTop = new AreaManipulator(styles.rectangleToolScaleTop, MouseCursor.ResizeVertical);

                m_SelectionScaleTop.onStartDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        if (hasSelection && manipulator.rect.Contains(evt.mousePosition))
                        {
                            OnStartScale(ToolCoord.Bottom, ToolCoord.Top, new Vector2(0f, PixelToValue(evt.mousePosition.y)), DragMode.ScaleVertical, false);
                            return true;
                        }

                        return false;
                    };
                m_SelectionScaleTop.onDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnScaleValue(PixelToValue(evt.mousePosition.y));
                        return true;
                    };
                m_SelectionScaleTop.onEndDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnEndScale();
                        return true;
                    };
            }
        }

        public void OnGUI()
        {
            if (!hasSelection)
                return;

            if (Event.current.type != EventType.Repaint)
                return;

            RectangleToolFlags flags = m_CurveEditor.settings.rectangleToolFlags;
            if (flags == RectangleToolFlags.NoRectangleTool)
                return;

            Color oldColor = GUI.color;
            GUI.color = Color.white;

            m_Layout = CalculateLayout();

            if (flags == RectangleToolFlags.FullRectangleTool)
            {
                GUI.Label(m_Layout.selectionLeftRect, GUIContent.none, styles.rectangleToolHighlight);
                GUI.Label(m_Layout.selectionTopRect, GUIContent.none, styles.rectangleToolHighlight);
                GUI.Label(m_Layout.underlayLeftRect, GUIContent.none, styles.rectangleToolHighlight);
                GUI.Label(m_Layout.underlayTopRect, GUIContent.none, styles.rectangleToolHighlight);
            }

            m_SelectionBox.OnGUI(m_Layout.selectionRect);

            m_SelectionScaleTop.OnGUI(m_Layout.scaleTopRect);
            m_SelectionScaleBottom.OnGUI(m_Layout.scaleBottomRect);
            m_SelectionScaleLeft.OnGUI(m_Layout.scaleLeftRect);
            m_SelectionScaleRight.OnGUI(m_Layout.scaleRightRect);

            GUI.color = oldColor;
        }

        public void OverlayOnGUI()
        {
            if (!hasSelection)
                return;

            if (Event.current.type != EventType.Repaint)
                return;

            Color oldColor = GUI.color;

            RectangleToolFlags flags = m_CurveEditor.settings.rectangleToolFlags;
            if (flags == RectangleToolFlags.FullRectangleTool)
            {
                GUI.color = Color.white;

                m_HBar.OnGUI(m_Layout.hBarRect);
                m_HBarLeft.OnGUI(m_Layout.hBarLeftRect);
                m_HBarRight.OnGUI(m_Layout.hBarRightRect);

                m_VBar.OnGUI(m_Layout.vBarRect);
                m_VBarBottom.OnGUI(m_Layout.vBarBottomRect);
                m_VBarTop.OnGUI(m_Layout.vBarTopRect);
            }

            DrawLabels();

            GUI.color = oldColor;
        }

        public void HandleEvents()
        {
            RectangleToolFlags flags = m_CurveEditor.settings.rectangleToolFlags;
            if (flags == RectangleToolFlags.NoRectangleTool)
                return;

            m_SelectionScaleTop.HandleEvents();
            m_SelectionScaleBottom.HandleEvents();
            m_SelectionScaleLeft.HandleEvents();
            m_SelectionScaleRight.HandleEvents();

            m_SelectionBox.HandleEvents();
        }

        public void HandleOverlayEvents()
        {
            HandleClutchKeys();

            RectangleToolFlags flags = m_CurveEditor.settings.rectangleToolFlags;
            if (flags == RectangleToolFlags.NoRectangleTool)
                return;

            if (flags == RectangleToolFlags.FullRectangleTool)
            {
                m_VBarBottom.HandleEvents();
                m_VBarTop.HandleEvents();
                m_VBar.HandleEvents();

                m_HBarLeft.HandleEvents();
                m_HBarRight.HandleEvents();
                m_HBar.HandleEvents();
            }
        }

        private ToolLayout CalculateLayout()
        {
            ToolLayout layout = new ToolLayout();

            bool canScaleX = !Mathf.Approximately(selectionBounds.size.x, 0f);
            bool canScaleY = !Mathf.Approximately(selectionBounds.size.y, 0f);

            float xMin = TimeToPixel(selectionBounds.min.x);
            float xMax = TimeToPixel(selectionBounds.max.x);

            float yMin = ValueToPixel(selectionBounds.max.y);
            float yMax = ValueToPixel(selectionBounds.min.y);

            layout.selectionRect = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);

            // Horizontal layout
            layout.displayHScale = true;
            float dragHorizWidth = layout.selectionRect.width - kHBarLeftWidth - kHBarRightWidth;

            if (dragHorizWidth < kHBarMinWidth)
            {
                layout.displayHScale = false;

                dragHorizWidth = layout.selectionRect.width;
                if (dragHorizWidth < kHBarMinWidth)
                {
                    layout.selectionRect.x = layout.selectionRect.center.x - kHBarMinWidth * 0.5f;
                    layout.selectionRect.width = kHBarMinWidth;

                    dragHorizWidth = kHBarMinWidth;
                }
            }

            if (layout.displayHScale)
            {
                layout.hBarLeftRect = new Rect(layout.selectionRect.xMin, contentRect.yMin, kHBarLeftWidth, kHBarLeftHeight);
                layout.hBarRect = new Rect(layout.hBarLeftRect.xMax, contentRect.yMin, dragHorizWidth, kHBarHeight);
                layout.hBarRightRect = new Rect(layout.hBarRect.xMax, contentRect.yMin, kHBarLeftWidth, kHBarRightHeight);
            }
            else
            {
                layout.hBarRect = new Rect(layout.selectionRect.xMin, contentRect.yMin, dragHorizWidth, kHBarHeight);

                layout.hBarLeftRect = new Rect(0f, 0f, 0f, 0f);
                layout.hBarRightRect = new Rect(0f, 0f, 0f, 0f);
            }

            // Vertical layout
            layout.displayVScale = true;
            float dragVertHeight = layout.selectionRect.height - kVBarBottomHeight - kVBarTopHeight;

            if (dragVertHeight < kVBarMinHeight)
            {
                layout.displayVScale = false;

                dragVertHeight = layout.selectionRect.height;
                if (dragVertHeight < kVBarMinHeight)
                {
                    layout.selectionRect.y = layout.selectionRect.center.y - kVBarMinHeight * 0.5f;
                    layout.selectionRect.height = kVBarMinHeight;

                    dragVertHeight = kVBarMinHeight;
                }
            }

            if (layout.displayVScale)
            {
                layout.vBarTopRect = new Rect(contentRect.xMin, layout.selectionRect.yMin, kVBarTopWidth, kVBarTopHeight);
                layout.vBarRect = new Rect(contentRect.xMin, layout.vBarTopRect.yMax, kVBarWidth, dragVertHeight);
                layout.vBarBottomRect = new Rect(contentRect.xMin, layout.vBarRect.yMax, kVBarBottomWidth, kVBarBottomHeight);
            }
            else
            {
                layout.vBarRect = new Rect(contentRect.xMin, layout.selectionRect.yMin, kVBarWidth, dragVertHeight);

                layout.vBarTopRect = g_EmptyRect;
                layout.vBarBottomRect = g_EmptyRect;
            }

            // Scale handles.
            if (canScaleX)
            {
                float leftRatio = (1.0f - kScaleLeftRatio) * 0.5f;
                float rightRatio = (1.0f - kScaleRightRatio) * 0.5f;

                layout.scaleLeftRect = new Rect(layout.selectionRect.xMin - kScaleLeftMarginHorizontal - kScaleLeftWidth, layout.selectionRect.yMin + layout.selectionRect.height * leftRatio, kScaleLeftWidth, layout.selectionRect.height * kScaleLeftRatio);
                layout.scaleRightRect = new Rect(layout.selectionRect.xMax + kScaleRightMarginHorizontal, layout.selectionRect.yMin + layout.selectionRect.height * rightRatio, kScaleRightWidth, layout.selectionRect.height * kScaleRightRatio);
            }
            else
            {
                layout.scaleLeftRect = g_EmptyRect;
                layout.scaleRightRect = g_EmptyRect;
            }

            if (canScaleY)
            {
                float bottomRatio = (1.0f - kScaleBottomRatio) * 0.5f;
                float topRatio = (1.0f - kScaleTopRatio) * 0.5f;

                layout.scaleTopRect = new Rect(layout.selectionRect.xMin + layout.selectionRect.width * topRatio, layout.selectionRect.yMin - kScaleTopMarginVertical - kScaleTopHeight, layout.selectionRect.width * kScaleTopRatio, kScaleTopHeight);
                layout.scaleBottomRect = new Rect(layout.selectionRect.xMin + layout.selectionRect.width * bottomRatio, layout.selectionRect.yMax + kScaleBottomMarginVertical, layout.selectionRect.width * kScaleBottomRatio, kScaleBottomHeight);
            }
            else
            {
                layout.scaleTopRect = g_EmptyRect;
                layout.scaleBottomRect = g_EmptyRect;
            }

            // Labels.
            if (canScaleX)
            {
                layout.leftLabelAnchor = new Vector2(layout.selectionRect.xMin - kHLabelMarginHorizontal, contentRect.yMin + kHLabelMarginVertical);
                layout.rightLabelAnchor = new Vector2(layout.selectionRect.xMax + kHLabelMarginHorizontal, contentRect.yMin + kHLabelMarginVertical);
            }
            else
            {
                layout.leftLabelAnchor = layout.rightLabelAnchor = new Vector2(layout.selectionRect.xMax + kHLabelMarginHorizontal, contentRect.yMin + kHLabelMarginVertical);
            }

            if (canScaleY)
            {
                layout.bottomLabelAnchor = new Vector2(contentRect.xMin + kVLabelMarginHorizontal, layout.selectionRect.yMax + kVLabelMarginVertical);
                layout.topLabelAnchor = new Vector2(contentRect.xMin + kVLabelMarginHorizontal, layout.selectionRect.yMin - kVLabelMarginVertical);
            }
            else
            {
                layout.bottomLabelAnchor = layout.topLabelAnchor = new Vector2(contentRect.xMin + kVLabelMarginHorizontal, layout.selectionRect.yMin - kVLabelMarginVertical);
            }

            // Extra ui.
            layout.selectionLeftRect = new Rect(contentRect.xMin + kVBarWidth, layout.selectionRect.yMin, layout.selectionRect.xMin - kVBarWidth, layout.selectionRect.height);
            layout.selectionTopRect = new Rect(layout.selectionRect.xMin, contentRect.yMin + kHBarHeight, layout.selectionRect.width, layout.selectionRect.yMin - kHBarHeight);

            layout.underlayTopRect = new Rect(contentRect.xMin, contentRect.yMin, contentRect.width, kHBarHeight);
            layout.underlayLeftRect = new Rect(contentRect.xMin, contentRect.yMin + kHBarHeight, kVBarWidth, contentRect.height - kHBarHeight);

            return layout;
        }

        private void DrawLabels()
        {
            if (dragMode == DragMode.None)
                return;

            RectangleToolFlags flags = m_CurveEditor.settings.rectangleToolFlags;

            bool canScaleX = !Mathf.Approximately(selectionBounds.size.x, 0f);
            bool canScaleY = !Mathf.Approximately(selectionBounds.size.y, 0f);

            if (flags == RectangleToolFlags.FullRectangleTool)
            {
                // Horizontal labels
                if ((dragMode & DragMode.MoveScaleHorizontal) != 0)
                {
                    if (canScaleX)
                    {
                        GUIContent leftLabelContent = new GUIContent(string.Format("{0}", m_CurveEditor.FormatTime(selectionBounds.min.x, m_CurveEditor.invSnap, m_CurveEditor.timeFormat)));
                        GUIContent rightLabelContent = new GUIContent(string.Format("{0}", m_CurveEditor.FormatTime(selectionBounds.max.x, m_CurveEditor.invSnap, m_CurveEditor.timeFormat)));

                        Vector2 leftLabelSize = styles.dragLabel.CalcSize(leftLabelContent);
                        Vector2 rightLabelSize = styles.dragLabel.CalcSize(rightLabelContent);

                        EditorGUI.DoDropShadowLabel(new Rect(m_Layout.leftLabelAnchor.x - leftLabelSize.x, m_Layout.leftLabelAnchor.y, leftLabelSize.x, leftLabelSize.y), leftLabelContent, styles.dragLabel, 0.3f);
                        EditorGUI.DoDropShadowLabel(new Rect(m_Layout.rightLabelAnchor.x, m_Layout.rightLabelAnchor.y, rightLabelSize.x, rightLabelSize.y), rightLabelContent, styles.dragLabel, 0.3f);
                    }
                    else
                    {
                        GUIContent labelContent = new GUIContent(string.Format("{0}", m_CurveEditor.FormatTime(selectionBounds.center.x, m_CurveEditor.invSnap, m_CurveEditor.timeFormat)));
                        Vector2 labelSize = styles.dragLabel.CalcSize(labelContent);

                        EditorGUI.DoDropShadowLabel(new Rect(m_Layout.leftLabelAnchor.x, m_Layout.leftLabelAnchor.y, labelSize.x, labelSize.y), labelContent, styles.dragLabel, 0.3f);
                    }
                }

                // Vertical labels
                if ((dragMode & DragMode.MoveScaleVertical) != 0)
                {
                    if (canScaleY)
                    {
                        GUIContent bottomLabelContent = new GUIContent(string.Format("{0}", m_CurveEditor.FormatValue(selectionBounds.min.y)));
                        GUIContent topLabelContent = new GUIContent(string.Format("{0}", m_CurveEditor.FormatValue(selectionBounds.max.y)));

                        Vector2 bottomLabelSize = styles.dragLabel.CalcSize(bottomLabelContent);
                        Vector2 topLabelSize = styles.dragLabel.CalcSize(topLabelContent);

                        EditorGUI.DoDropShadowLabel(new Rect(m_Layout.bottomLabelAnchor.x, m_Layout.bottomLabelAnchor.y, bottomLabelSize.x, bottomLabelSize.y), bottomLabelContent, styles.dragLabel, 0.3f);
                        EditorGUI.DoDropShadowLabel(new Rect(m_Layout.topLabelAnchor.x, m_Layout.topLabelAnchor.y - topLabelSize.y, topLabelSize.x, topLabelSize.y), topLabelContent, styles.dragLabel, 0.3f);
                    }
                    else
                    {
                        GUIContent labelContent = new GUIContent(string.Format("{0}", m_CurveEditor.FormatValue(selectionBounds.center.y)));
                        Vector2 labelSize = styles.dragLabel.CalcSize(labelContent);

                        EditorGUI.DoDropShadowLabel(new Rect(m_Layout.topLabelAnchor.x, m_Layout.topLabelAnchor.y - labelSize.y, labelSize.x, labelSize.y), labelContent, styles.dragLabel, 0.3f);
                    }
                }
            }
            else if (flags == RectangleToolFlags.MiniRectangleTool)
            {
                if ((dragMode & DragMode.MoveBothAxis) != 0)
                {
                    Vector2 localPosition = (canScaleX || canScaleY) ? new Vector2(PixelToTime(Event.current.mousePosition.x, frameRate), PixelToValue(Event.current.mousePosition.y)) : (Vector2)selectionBounds.center;
                    Vector2 labelPosition = new Vector2(TimeToPixel(localPosition.x), ValueToPixel(localPosition.y));

                    GUIContent labelContent = new GUIContent(string.Format("{0}, {1}", m_CurveEditor.FormatTime(localPosition.x, m_CurveEditor.invSnap, m_CurveEditor.timeFormat), m_CurveEditor.FormatValue(localPosition.y)));
                    Vector2 labelSize = styles.dragLabel.CalcSize(labelContent);

                    EditorGUI.DoDropShadowLabel(new Rect(labelPosition.x, labelPosition.y - labelSize.y, labelSize.x, labelSize.y), labelContent, styles.dragLabel, 0.3f);
                }
            }
        }

        private void OnStartScale(ToolCoord pivotCoord, ToolCoord pickedCoord, Vector2 mousePos, DragMode dragMode, bool rippleTime)
        {
            Bounds bounds = selectionBounds;
            m_DragMode = dragMode;
            m_Pivot = ToolCoordToPosition(pivotCoord, bounds);
            m_Previous = ToolCoordToPosition(pickedCoord, bounds);
            m_MouseOffset = mousePos - m_Previous;
            m_RippleTime = rippleTime;
            m_RippleTimeStart = bounds.min.x;
            m_RippleTimeEnd = bounds.max.x;

            m_CurveEditor.StartLiveEdit();
        }

        private void OnScaleTime(float time)
        {
            Matrix4x4 transform;
            bool flipX;
            if (CalculateScaleTimeMatrix(m_Previous.x, time, m_MouseOffset.x, m_Pivot.x, frameRate, out transform, out flipX))
                TransformKeys(transform, flipX, false);
        }

        private void OnScaleValue(float val)
        {
            Matrix4x4 transform;
            bool flipY;
            if (CalculateScaleValueMatrix(m_Previous.y, val, m_MouseOffset.y, m_Pivot.y, out transform, out flipY))
                TransformKeys(transform, false, flipY);
        }

        private void OnEndScale()
        {
            m_CurveEditor.EndLiveEdit();
            m_DragMode = DragMode.None;
            GUI.changed = true;
        }

        internal void OnStartMove(Vector2 position, bool rippleTime)
        {
            OnStartMove(position, DragMode.None, rippleTime);
        }

        private void OnStartMove(Vector2 position, DragMode dragMode, bool rippleTime)
        {
            Bounds bounds = selectionBounds;

            m_DragMode = dragMode;
            m_Previous = position;
            m_RippleTime = rippleTime;
            m_RippleTimeStart = bounds.min.x;
            m_RippleTimeEnd = bounds.max.x;

            m_CurveEditor.StartLiveEdit();
        }

        internal void OnMove(Vector2 position)
        {
            Vector2 dv = position - m_Previous;

            Matrix4x4 transform = Matrix4x4.identity;
            transform.SetTRS(new Vector3(dv.x, dv.y, 0f), Quaternion.identity, Vector3.one);

            TransformKeys(transform, false, false);
        }

        internal void OnEndMove()
        {
            m_CurveEditor.EndLiveEdit();
            m_DragMode = DragMode.None;
            GUI.changed = true;
        }

        private void TransformKeys(Matrix4x4 matrix, bool flipX, bool flipY)
        {
            if (m_RippleTime)
            {
                m_CurveEditor.TransformRippleKeys(matrix, m_RippleTimeStart, m_RippleTimeEnd, flipX);
                GUI.changed = true;
            }
            else
            {
                m_CurveEditor.TransformSelectedKeys(matrix, flipX, flipY);
                GUI.changed = true;
            }
        }
    }
}
