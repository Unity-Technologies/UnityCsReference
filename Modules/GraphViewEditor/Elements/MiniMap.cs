// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    class MiniMap : GraphElement
    {
        float m_PreviousContainerWidth = -1;
        float m_PreviousContainerHeight = -1;

        readonly Label m_Label;
        Dragger m_Dragger;

        readonly Color m_ViewportColor = new Color(1.0f, 1.0f, 0.0f, 0.35f);
        readonly Color m_SelectedChildrenColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);

        // Various rects used by the MiniMap
        Rect m_ViewportRect;        // Rect that represents the current viewport
        Rect m_ContentRect;         // Rect that represents the rect needed to encompass all Graph Elements
        Rect m_ContentRectLocal;    // Rect that represents the rect needed to encompass all Graph Elements in local coords

        int titleBarOffset { get { return (int)style.paddingTop; } }

        public MiniMap()
        {
            clippingOptions = ClippingOptions.NoClipping;

            m_Label = new Label("Floating Minimap");

            Add(m_Label);

            RegisterCallback<MouseUpEvent>(ShowContextualMenu);
            RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        protected void ShowContextualMenu(MouseUpEvent e)
        {
            if (e.button == (int)MouseButton.RightMouse)
            {
                var boxPresenter = GetPresenter<MiniMapPresenter>();
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent(boxPresenter.anchored ? "Make floating" :  "Anchor"), false,
                    contentView =>
                    {
                        var bPresenter = GetPresenter<MiniMapPresenter>();
                        bPresenter.anchored = !bPresenter.anchored;
                    },
                    this);
                menu.DropDown(new Rect(e.mousePosition.x, e.mousePosition.y, 0, 0));
                e.StopPropagation();
            }
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            AdjustAnchoring();
            Resize();
        }

        void AdjustAnchoring()
        {
            var miniMapPresenter = GetPresenter<MiniMapPresenter>();
            if (miniMapPresenter == null)
                return;

            if (miniMapPresenter.anchored)
            {
                miniMapPresenter.capabilities &= ~Capabilities.Movable;
                ResetPositionProperties();
                AddToClassList("anchored");
            }
            else
            {
                if (m_Dragger == null)
                {
                    m_Dragger = new Dragger {clampToParentEdges = true};
                    this.AddManipulator(m_Dragger);
                }
                presenter.capabilities |= Capabilities.Movable;
                RemoveFromClassList("anchored");
            }
        }

        void Resize()
        {
            if (parent == null)
                return;

            var miniMapPresenter = GetPresenter<MiniMapPresenter>();
            style.width = miniMapPresenter.maxWidth;
            style.height = miniMapPresenter.maxHeight;

            // Relocate if partially visible on bottom or right side (left/top not checked, only bottom/right affected by a size change)
            if (style.positionLeft + style.width > parent.layout.x + parent.layout.width)
            {
                var newPosition = miniMapPresenter.position;
                newPosition.x -= style.positionLeft + style.width - (parent.layout.x + parent.layout.width);
                miniMapPresenter.position = newPosition;
            }

            if (style.positionTop + style.height > parent.layout.y + parent.layout.height)
            {
                var newPosition = miniMapPresenter.position;
                newPosition.y -= style.positionTop + style.height - (parent.layout.y + parent.layout.height);
                miniMapPresenter.position = newPosition;
            }

            var newMiniMapPos = miniMapPresenter.position;
            newMiniMapPos.width = style.width;
            newMiniMapPos.height = style.height;
            newMiniMapPos.x = Mathf.Max(parent.layout.x, newMiniMapPos.x);
            newMiniMapPos.y = Mathf.Max(parent.layout.y, newMiniMapPos.y);
            miniMapPresenter.position = newMiniMapPos;

            if (!miniMapPresenter.anchored)
            {
                // Update to prevent onscreen mishaps especially at tiny window sizes
                layout = miniMapPresenter.position;
            }
        }

        static void ChangeToMiniMapCoords(ref Rect rect, float factor, Vector3 translation)
        {
            // Apply factor
            rect.width *= factor;
            rect.height *= factor;
            rect.x *= factor;
            rect.y *= factor;

            // Apply translation
            rect.x += translation.x;
            rect.y += translation.y;
        }

        void CalculateRects(GraphView gView)
        {
            m_ContentRect = gView.CalculateRectToFitAll();
            m_ContentRectLocal = m_ContentRect;

            // Retrieve viewport rectangle as if zoom and pan were inactive
            Matrix4x4 containerInvTransform = gView.contentViewContainer.worldTransform.inverse;
            Vector4 containerInvTranslation = containerInvTransform.GetColumn(3);
            var containerInvScale = new Vector2(containerInvTransform.m00, containerInvTransform.m11);

            m_ViewportRect = parent.layout;

            // Bring back viewport coordinates to (0,0), scale 1:1
            m_ViewportRect.x += containerInvTranslation.x;
            m_ViewportRect.y += containerInvTranslation.y;
            m_ViewportRect.width *= containerInvScale.x;
            m_ViewportRect.height *= containerInvScale.y;

            // Update label with new value
            m_Label.text = "MiniMap v: " +
                string.Format("{0:0}", m_ViewportRect.width) + "x" +
                string.Format("{0:0}", m_ViewportRect.height);

            // Adjust rects for MiniMap

            // Encompass viewport rectangle (as if zoom and pan were inactive)
            var totalRect = RectUtils.Encompass(m_ContentRect, m_ViewportRect);
            var minimapFactor = layout.width / totalRect.width;

            // Transform each rect to MiniMap coordinates
            ChangeToMiniMapCoords(ref totalRect, minimapFactor, Vector3.zero);

            var minimapTranslation = new Vector3(layout.x - totalRect.x, layout.y + titleBarOffset - totalRect.y);
            ChangeToMiniMapCoords(ref m_ViewportRect, minimapFactor, minimapTranslation);
            ChangeToMiniMapCoords(ref m_ContentRect, minimapFactor, minimapTranslation);

            // Diminish and center everything to fit vertically
            if (totalRect.height > (layout.height - titleBarOffset))
            {
                float totalRectFactor = (layout.height - titleBarOffset) / totalRect.height;
                float totalRectOffsetX = (layout.width - (totalRect.width * totalRectFactor)) / 2.0f;
                float totalRectOffsetY = layout.y + titleBarOffset - ((totalRect.y + minimapTranslation.y) * totalRectFactor);

                m_ContentRect.width *= totalRectFactor;
                m_ContentRect.height *= totalRectFactor;
                m_ContentRect.y *= totalRectFactor;
                m_ContentRect.x += totalRectOffsetX;
                m_ContentRect.y += totalRectOffsetY;

                m_ViewportRect.width *= totalRectFactor;
                m_ViewportRect.height *= totalRectFactor;
                m_ViewportRect.y *= totalRectFactor;
                m_ViewportRect.x += totalRectOffsetX;
                m_ViewportRect.y += totalRectOffsetY;
            }
        }

        Rect CalculateElementRect(GraphElement elem)
        {
            // TODO: Should Edges be displayed at all?
            // TODO: Maybe edges need their own capabilities flag.
            var elementPresenter = elem.GetPresenter<GraphElementPresenter>();
            if ((elementPresenter.capabilities & Capabilities.Floating) != 0 ||
                (elementPresenter is EdgePresenter))
            {
                return new Rect(0, 0, 0, 0);
            }

            Rect rect = elem.localBound;
            rect.x = m_ContentRect.x + ((rect.x - m_ContentRectLocal.x) * m_ContentRect.width / m_ContentRectLocal.width);
            rect.y = m_ContentRect.y + ((rect.y - m_ContentRectLocal.y) * m_ContentRect.height / m_ContentRectLocal.height);
            rect.width *= m_ContentRect.width / m_ContentRectLocal.width;
            rect.height *= m_ContentRect.height / m_ContentRectLocal.height;

            // Clip using a minimal 2 pixel wide frame around edges
            // (except yMin since we already have the titleBar offset which is enough for clipping)
            var xMin = layout.xMin + 2;
            var xMax = layout.xMax - 2;
            var yMax = layout.yMax - 2;

            if (rect.x < xMin)
            {
                if (rect.x < xMin - rect.width)
                    return new Rect(0, 0, 0, 0);
                rect.width -= xMin - rect.x;
                rect.x = xMin;
            }

            if (rect.x + rect.width >= xMax)
            {
                if (rect.x >= xMax)
                    return new Rect(0, 0, 0, 0);
                rect.width -= rect.x + rect.width - xMax;
            }

            if (rect.y < layout.yMin + titleBarOffset)
            {
                if (rect.y < layout.yMin + titleBarOffset - rect.height)
                    return new Rect(0, 0, 0, 0);
                rect.height -= layout.yMin + titleBarOffset - rect.y;
                rect.y = layout.yMin + titleBarOffset;
            }

            if (rect.y + rect.height >= yMax)
            {
                if (rect.y >= yMax)
                    return new Rect(0, 0, 0, 0);
                rect.height -= rect.y + rect.height - yMax;
            }

            return rect;
        }

        public override void DoRepaint()
        {
            var gView = this.GetFirstAncestorOfType<GraphView>();

            // Retrieve all container relative information
            Matrix4x4 containerTransform = gView.viewTransform.matrix;
            var containerScale = new Vector2(containerTransform.m00, containerTransform.m11);
            float containerWidth = parent.layout.width / containerScale.x;
            float containerHeight = parent.layout.height / containerScale.y;

            if (Mathf.Abs(containerWidth - m_PreviousContainerWidth) > Mathf.Epsilon ||
                Mathf.Abs(containerHeight - m_PreviousContainerHeight) > Mathf.Epsilon)
            {
                m_PreviousContainerWidth = containerWidth;
                m_PreviousContainerHeight = containerHeight;
                Resize();
            }

            // Refresh MiniMap rects
            CalculateRects(gView);

            // Let the base call draw the background and so on
            base.DoRepaint();

            // Display elements in the MiniMap
            Color currentColor = Handles.color;
            gView.graphElements.ForEach(elem =>
                {
                    var rect = CalculateElementRect(elem);
                    Handles.color = elem.elementTypeColor;
                    Handles.DrawSolidRectangleWithOutline(rect, elem.elementTypeColor, elem.elementTypeColor);
                    var elementPresenter = elem.GetPresenter<GraphElementPresenter>();
                    if (elementPresenter != null && elementPresenter.selected)
                        DrawRectangleOutline(rect, m_SelectedChildrenColor);
                });

            // Draw viewport outline
            DrawRectangleOutline(m_ViewportRect, m_ViewportColor);

            Handles.color = currentColor;
        }

        void DrawRectangleOutline(Rect rect, Color color)
        {
            Color currentColor = Handles.color;
            Handles.color = color;

            // Draw viewport outline
            Vector3[] points = new Vector3[5];
            points[0] = new Vector3(rect.x, rect.y, 0.0f);
            points[1] = new Vector3(rect.x + rect.width, rect.y, 0.0f);
            points[2] = new Vector3(rect.x + rect.width, rect.y + rect.height, 0.0f);
            points[3] = new Vector3(rect.x, rect.y + rect.height, 0.0f);
            points[4] = new Vector3(rect.x, rect.y, 0.0f);
            Handles.DrawPolyLine(points);

            Handles.color = currentColor;
        }

        protected void OnMouseDown(MouseDownEvent e)
        {
            var gView = this.GetFirstAncestorOfType<GraphView>();

            // Refresh MiniMap rects
            CalculateRects(gView);

            var mousePosition = e.localMousePosition;
            mousePosition.x += layout.x;
            mousePosition.y += layout.y;

            gView.graphElements.ForEach(child =>
                {
                    if (child == null)
                        return;
                    var selectable = child.GetFirstOfType<ISelectable>();
                    if (selectable == null || !selectable.IsSelectable())
                        return;

                    if (CalculateElementRect(child).Contains(mousePosition))
                    {
                        gView.ClearSelection();
                        gView.AddToSelection(selectable);
                        gView.FrameSelection();
                        e.StopPropagation();
                    }
                });
        }
    }
}
