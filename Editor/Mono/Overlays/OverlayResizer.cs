// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace UnityEditor.Overlays
{
    class OverlayResizerGroup : VisualElement
    {
        const float k_CornerSize = 8;
        const float k_SideSize = 6;
        const float k_MinDistanceFromEdge = 10;

        [Flags]
        enum Direction
        {
            Left = 1 << 0,
            Bottom = 1 << 1,
            Top = 1 << 2,
            Right = 1 << 3,

            TopLeft = Top | Left,
            TopRight = Top | Right,
            BottomLeft = Bottom | Left,
            BottomRight = Bottom | Right,
        }

        class OverlayResizer : VisualElement
        {
            readonly Direction m_Direction;
            readonly Overlay m_Overlay;
            readonly bool m_ModifyPosition;

            Vector2 m_OriginalMousePosition;
            Rect m_OriginalRect;
            Rect m_ContainerRect;
            Vector2 m_MinSize;
            Vector2 m_MaxSize;
            bool m_Active;

            public OverlayResizer(Overlay overlay, Direction direction)
            {
                m_Overlay = overlay;
                m_Direction = direction;

                style.position = Position.Absolute;

                bool hasLeft = HasDirection(Direction.Left);
                bool hasRight = HasDirection(Direction.Right);
                bool hasTop = HasDirection(Direction.Top);
                bool hasBottom = HasDirection(Direction.Bottom);
                bool isCorner = HasMultipleDirections();

                var size = isCorner ? k_CornerSize : k_SideSize;

                if (hasLeft) style.left = -size * .5f;
                if (hasRight) style.right = -size * .5f;
                if (hasTop) style.top = -size * .5f;
                if (hasBottom) style.bottom = -size * .5f;

                style.width = hasLeft || hasRight ? size : new Length(100, LengthUnit.Percent);
                style.height = hasTop || hasBottom ? size : new Length(100, LengthUnit.Percent);

                m_ModifyPosition = HasDirection(Direction.Left) || HasDirection(Direction.Top);

                style.cursor = new Cursor { defaultCursorId = (int)GetCursor(direction) };

                RegisterCallback<MouseDownEvent>(OnMouseDown);
                RegisterCallback<MouseMoveEvent>(OnMouseMove);
                RegisterCallback<MouseUpEvent>(OnMouseUp);
            }

            void OnMouseDown(MouseDownEvent evt)
            {
                var container = m_Overlay.rootVisualElement.GetFirstAncestorOfType<OverlayContainer>();


                var overlayPosition = m_Overlay.rootVisualElement.layout.position;
                if (container != null)
                    overlayPosition = m_Overlay.rootVisualElement.parent.ChangeCoordinatesTo(container, overlayPosition);

                m_OriginalRect = new Rect(
                    m_Overlay.floating ?  m_Overlay.floatingPosition : overlayPosition,
                    m_Overlay.size);

                m_ContainerRect = container?.rect ?? new Rect(float.NegativeInfinity,float.NegativeInfinity,float.PositiveInfinity,float.PositiveInfinity);
                m_OriginalMousePosition = evt.mousePosition;
                m_MaxSize = m_Overlay.maxSize;
                m_MinSize = m_Overlay.minSize;

                this.CaptureMouse();
                evt.StopPropagation();
                m_Active = true;
            }

            void OnMouseMove(MouseMoveEvent evt)
            {
                if (m_Active)
                {
                    var translation = evt.mousePosition - m_OriginalMousePosition;
                    var rect = ResizeRect(m_OriginalRect, translation);

                    m_Overlay.size = rect.size;

                    if (m_ModifyPosition && m_Overlay.floating)
                        m_Overlay.floatingPosition = rect.position;

                    evt.StopPropagation();
                }
            }

            void OnMouseUp(MouseUpEvent evt)
            {
                if (m_Active)
                {
                    evt.StopPropagation();
                    this.ReleaseMouse();
                    m_Active = false;
                }
            }

            Rect ResizeRect(Rect rect, Vector2 delta)
            {
                delta = ClampDeltaToMinMax(delta, rect);
                if (HasDirection(Direction.Left))
                    rect.xMin = Mathf.Max(m_ContainerRect.xMin, rect.xMin + delta.x);
                else if (HasDirection(Direction.Right))
                    rect.xMax = Mathf.Min(m_ContainerRect.xMax, rect.xMax + delta.x);

                if (HasDirection(Direction.Top))
                    rect.yMin = Mathf.Max(m_ContainerRect.yMin, rect.yMin + delta.y);
                else if (HasDirection(Direction.Bottom))
                    rect.yMax = Mathf.Min(m_ContainerRect.yMax, rect.yMax + delta.y);

                return rect;
            }

            Vector2 ClampDeltaToMinMax(Vector2 delta, Rect rect)
            {
                if (HasDirection(Direction.Left))
                    delta.x = rect.width - Mathf.Clamp(rect.width - delta.x, m_MinSize.x, m_MaxSize.x);
                else if (HasDirection(Direction.Right))
                    delta.x = Mathf.Clamp(rect.width + delta.x, m_MinSize.x, m_MaxSize.x) - rect.width;

                if (HasDirection(Direction.Top))
                    delta.y = rect.height - Mathf.Clamp(rect.height - delta.y, m_MinSize.y, m_MaxSize.y);
                else if (HasDirection(Direction.Bottom))
                    delta.y = Mathf.Clamp(rect.height + delta.y, m_MinSize.y, m_MaxSize.y) - rect.height;

                return delta;
            }

            static MouseCursor GetCursor(Direction direction)
            {
                MouseCursor cursorStyle = MouseCursor.Arrow;

                switch (direction)
                {
                    case Direction.TopLeft:
                    case Direction.BottomRight:
                        cursorStyle = MouseCursor.ResizeUpLeft;
                        break;
                    case Direction.TopRight:
                    case Direction.BottomLeft:
                        cursorStyle = MouseCursor.ResizeUpRight;
                        break;
                    case Direction.Left:
                    case Direction.Right:
                        cursorStyle = MouseCursor.ResizeHorizontal;
                        break;
                    case Direction.Top:
                    case Direction.Bottom:
                        cursorStyle = MouseCursor.ResizeVertical;
                        break;

                }
                return cursorStyle;
            }

            public bool HasDirection(Direction target)
            {
                return (m_Direction & target) == target;
            }

            public bool HasMultipleDirections()
            {
                return (m_Direction & (m_Direction - 1)) != 0;
            }
        }

        readonly Overlay m_Overlay;
        readonly OverlayResizer[] m_Resizers;

        public OverlayResizerGroup(Overlay overlay)
        {
            this.StretchToParentSize();
            pickingMode = PickingMode.Ignore;

            m_Resizers = new []
            {
                new OverlayResizer(overlay, Direction.Top) { name = "ResizerTop" },
                new OverlayResizer(overlay, Direction.Bottom) { name = "ResizerBottom" },
                new OverlayResizer(overlay, Direction.Left) { name = "ResizerLeft" },
                new OverlayResizer(overlay, Direction.Right) { name = "ResizerRight" },
                new OverlayResizer(overlay, Direction.TopLeft) { name = "ResizerTopLeft" },
                new OverlayResizer(overlay, Direction.TopRight) { name = "ResizerTopRight" },
                new OverlayResizer(overlay, Direction.BottomLeft) { name = "ResizerBottomLeft" },
                new OverlayResizer(overlay, Direction.BottomRight) { name = "ResizerBottomRight" },
            };

            m_Overlay = overlay;
            foreach (var resizer in m_Resizers)
                Add(resizer);

            overlay.containerChanged += OnOverlayContainerChanged;
            overlay.layoutChanged += OnOverlayLayoutChanged;
            overlay.floatingPositionChanged += OnOverlayPositionChanged;
            overlay.collapsedChanged += OnOverlayCollaspedChanged;
            m_Overlay.rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnOverlayGeometryChanged);
            UpdateResizerVisibility();
        }

        void OnOverlayLayoutChanged(Layout layout)
        {
            UpdateResizerVisibility();
        }

        void OnOverlayContainerChanged(OverlayContainer container)
        {
            UpdateResizerVisibility();
        }

        void OnOverlayGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateResizerVisibility();
        }

        void OnOverlayPositionChanged(Vector3 position)
        {
            UpdateResizerVisibility();
        }

        void OnOverlayCollaspedChanged(bool collapsed)
        {
            UpdateResizerVisibility();
        }

        bool ContainerCanShowResizer(OverlayResizer resizer)
        {
            var container = m_Overlay.container;
            if (container == null)
                return false;

            if (container is FloatingOverlayContainer)
                return true;

            if (container is ToolbarOverlayContainer)
                return false;

            var alignment = container.resolvedStyle.alignItems;
            bool hide = false;

            // Check the opposite direction. If the content is align to one side, hide resizer on that side
            switch (alignment)
            {
                case Align.FlexStart:
                    hide |= resizer.HasDirection(container.isHorizontal ? Direction.Top : Direction.Left);
                    break;

                case Align.FlexEnd:
                    hide |= resizer.HasDirection(container.isHorizontal ? Direction.Bottom : Direction.Right);
                    break;
            }

            return !hide;
        }

        void UpdateResizerVisibility()
        {
            bool globalHide = m_Overlay.layout != Layout.Panel && !m_Overlay.collapsed;
            foreach (var resizer in m_Resizers)
            {
                bool hide = globalHide || !ContainerCanShowResizer(resizer);

                if (resizer.HasMultipleDirections())
                {
                    hide |= Mathf.Approximately(m_Overlay.minSize.x, m_Overlay.maxSize.x);
                    hide |= Mathf.Approximately(m_Overlay.minSize.y, m_Overlay.maxSize.y);
                }
                else
                {
                    if (resizer.HasDirection(Direction.Left) || resizer.HasDirection(Direction.Right))
                        hide |= Mathf.Approximately(m_Overlay.minSize.x, m_Overlay.maxSize.x);

                    if (resizer.HasDirection(Direction.Top) || resizer.HasDirection(Direction.Bottom))
                        hide |= Mathf.Approximately(m_Overlay.minSize.y, m_Overlay.maxSize.y);
                }

                if (m_Overlay.canvas != null)
                {
                    var canvas = m_Overlay.canvas.rootVisualElement;
                    var canvasRect = canvas.rect;
                    var overlayRect = canvas.WorldToLocal(m_Overlay.rootVisualElement.worldBound);

                    if (resizer.HasDirection(Direction.Left))
                        hide |= overlayRect.xMin <= k_MinDistanceFromEdge;

                    if (resizer.HasDirection(Direction.Right))
                        hide |= overlayRect.xMax >= canvasRect.xMax - k_MinDistanceFromEdge;

                    if (resizer.HasDirection(Direction.Top))
                        hide |= overlayRect.yMin <= k_MinDistanceFromEdge;

                    if (resizer.HasDirection(Direction.Bottom))
                        hide |= overlayRect.yMax >= canvasRect.yMax - k_MinDistanceFromEdge;
                }


                resizer.style.display = hide ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }
    }
}
