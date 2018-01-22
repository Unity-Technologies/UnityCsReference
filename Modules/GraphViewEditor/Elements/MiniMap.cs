// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public class MiniMap : GraphElement
    {
        public float maxHeight { get; set; }
        public float maxWidth { get; set; }

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

        private bool m_Anchored;
        public bool anchored
        {
            get { return m_Anchored; }
            set
            {
                if (m_Anchored == value)
                    return;

                m_Anchored = value;

                if (m_Anchored)
                {
                    capabilities &= ~Capabilities.Movable;
                    ResetPositionProperties();
                    AddToClassList("anchored");
                }
                else
                {
                    capabilities |= Capabilities.Movable;
                    RemoveFromClassList("anchored");
                }

                Resize();
            }
        }

        public MiniMap()
        {
            clippingOptions = ClippingOptions.NoClipping;

            capabilities = Capabilities.Movable;

            m_Dragger = new Dragger { clampToParentEdges = true };
            this.AddManipulator(m_Dragger);

            anchored = false;

            maxWidth = 200;
            maxHeight = 180;

            m_Label = new Label("Floating Minimap");

            Add(m_Label);

            RegisterCallback<MouseDownEvent>(OnMouseDown);
            m_Label.RegisterCallback<MouseDownEvent>(EatMouseDown);

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        private GraphView m_GraphView;
        private GraphView graphView
        {
            get
            {
                if (m_GraphView == null) m_GraphView = GetFirstAncestorOfType<GraphView>();
                return m_GraphView;
            }
        }

        void ToggleAnchorState(EventBase e)
        {
            // TODO: Remove when removing presenters.
            if (dependsOnPresenter)
            {
                var bPresenter = GetPresenter<MiniMapPresenter>();
                bPresenter.anchored = !bPresenter.anchored;
            }
            else
            {
                anchored = !anchored;
            }
        }

        public virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            bool isAnchored;
            // TODO: Remove when removing presenters.
            if (dependsOnPresenter)
            {
                var boxPresenter = GetPresenter<MiniMapPresenter>();
                isAnchored = boxPresenter.anchored;
            }
            else
            {
                isAnchored = anchored;
            }
            evt.menu.AppendAction(isAnchored ? "Make floating" : "Anchor", ToggleAnchorState, ContextualMenu.MenuAction.AlwaysEnabled);
        }

        // TODO: Remove when removing presenters.
        public override void OnDataChanged()
        {
            base.OnDataChanged();
            AdjustAnchoring();

            var miniMapPresenter = GetPresenter<MiniMapPresenter>();
            style.width = miniMapPresenter.maxWidth;
            style.height = miniMapPresenter.maxHeight;

            Resize();

            UpdatePresenterPosition();
        }

        // TODO: Remove when removing presenters.
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
                presenter.capabilities |= Capabilities.Movable;
                RemoveFromClassList("anchored");
            }
        }

        void Resize()
        {
            if (parent == null)
                return;

            style.width = maxWidth;
            style.height = maxHeight;

            // Relocate if partially visible on bottom or right side (left/top not checked, only bottom/right affected by a size change)
            if (style.positionLeft + style.width > parent.layout.x + parent.layout.width)
            {
                var newPosition = layout;
                newPosition.x -= style.positionLeft + style.width - (parent.layout.x + parent.layout.width);
                layout = newPosition;
            }

            if (style.positionTop + style.height > parent.layout.y + parent.layout.height)
            {
                var newPosition = layout;
                newPosition.y -= style.positionTop + style.height - (parent.layout.y + parent.layout.height);
                layout = newPosition;
            }

            var newMiniMapPos = layout;
            newMiniMapPos.width = style.width;
            newMiniMapPos.height = style.height;
            newMiniMapPos.x = Mathf.Max(parent.layout.x, newMiniMapPos.x);
            newMiniMapPos.y = Mathf.Max(parent.layout.y, newMiniMapPos.y);
            layout = newMiniMapPos;
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

        void CalculateRects(VisualElement container)
        {
            m_ContentRect = graphView.CalculateRectToFitAll(container);
            m_ContentRectLocal = m_ContentRect;

            // Retrieve viewport rectangle as if zoom and pan were inactive
            Matrix4x4 containerInvTransform = container.worldTransform.inverse;
            Vector4 containerInvTranslation = containerInvTransform.GetColumn(3);
            var containerInvScale = new Vector2(containerInvTransform.m00, containerInvTransform.m11);

            m_ViewportRect = parent.rect;

            // Bring back viewport coordinates to (0,0), scale 1:1
            m_ViewportRect.x += containerInvTranslation.x;
            m_ViewportRect.y += containerInvTranslation.y;
            m_ViewportRect.x += (parent.worldBound.x * containerInvScale.x);
            m_ViewportRect.y += (parent.worldBound.y * containerInvScale.y);
            m_ViewportRect.width *= containerInvScale.x;
            m_ViewportRect.height *= containerInvScale.y;

            // Update label with new value
            var containerZoomFactor = container.worldTransform.m00;
            m_Label.text = "MiniMap  " + string.Format("{0:F2}", containerZoomFactor) + "x";

            // Adjust rects for MiniMap

            // Encompass viewport rectangle (as if zoom and pan were inactive)
            var totalRect = RectUtils.Encompass(m_ContentRect, m_ViewportRect);
            var minimapFactor = layout.width / totalRect.width;

            // Transform each rect to MiniMap coordinates
            ChangeToMiniMapCoords(ref totalRect, minimapFactor, Vector3.zero);

            var minimapTranslation = new Vector3(-totalRect.x, titleBarOffset - totalRect.y);
            ChangeToMiniMapCoords(ref m_ViewportRect, minimapFactor, minimapTranslation);
            ChangeToMiniMapCoords(ref m_ContentRect, minimapFactor, minimapTranslation);

            // Diminish and center everything to fit vertically
            if (totalRect.height > (layout.height - titleBarOffset))
            {
                float totalRectFactor = (layout.height - titleBarOffset) / totalRect.height;
                float totalRectOffsetX = (layout.width - (totalRect.width * totalRectFactor)) / 2.0f;
                float totalRectOffsetY = titleBarOffset - ((totalRect.y + minimapTranslation.y) * totalRectFactor);

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
            if (elem is Edge)
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
            var xMin = 2;
            var yMin = 0;
            var xMax = layout.width - 2;
            var yMax = layout.height - 2;

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

            if (rect.y < yMin + titleBarOffset)
            {
                if (rect.y < yMin + titleBarOffset - rect.height)
                    return new Rect(0, 0, 0, 0);
                rect.height -= yMin + titleBarOffset - rect.y;
                rect.y = yMin + titleBarOffset;
            }

            if (rect.y + rect.height >= yMax)
            {
                if (rect.y >= yMax)
                    return new Rect(0, 0, 0, 0);
                rect.height -= rect.y + rect.height - yMax;
            }

            return rect;
        }

        private static Vector3[] s_CachedRect = new Vector3[4];
        public override void DoRepaint()
        {
            var gView = graphView;
            VisualElement container = gView.contentViewContainer;

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
            CalculateRects(container);

            // Let the base call draw the background and so on
            base.DoRepaint();

            // Display elements in the MiniMap
            Color currentColor = Handles.color;
            gView.graphElements.ForEach(elem =>
                {
                    if (elem is Edge)
                        return;
                    var rect = CalculateElementRect(elem);
                    Handles.color = elem.elementTypeColor;

                    s_CachedRect[0].Set(rect.xMin, rect.yMin, 0.0f);
                    s_CachedRect[1].Set(rect.xMax, rect.yMin, 0.0f);
                    s_CachedRect[2].Set(rect.xMax, rect.yMax, 0.0f);
                    s_CachedRect[3].Set(rect.xMin, rect.yMax, 0.0f);
                    Handles.DrawSolidRectangleWithOutline(s_CachedRect, elem.elementTypeColor, elem.elementTypeColor);

                    // TODO: Remove when removing presenters.
                    if (elem.dependsOnPresenter)
                    {
                        var elementPresenter = elem.GetPresenter<GraphElementPresenter>();
                        if (elementPresenter != null && elementPresenter.selected)
                            DrawRectangleOutline(rect, m_SelectedChildrenColor);
                    }
                    else if (elem.selected)
                    {
                        DrawRectangleOutline(rect, m_SelectedChildrenColor);
                    }
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

        private void EatMouseDown(MouseDownEvent e)
        {
            // The minimap should not let any left mouse down go through when it's not movable.
            if (e.button == (int)MouseButton.LeftMouse &&
                (capabilities & Capabilities.Movable) == 0)
            {
                e.StopPropagation();
            }
        }

        private void OnMouseDown(MouseDownEvent e)
        {
            var gView = graphView;

            // Refresh MiniMap rects
            CalculateRects(gView.contentViewContainer);

            var mousePosition = e.localMousePosition;

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

            EatMouseDown(e);
        }
    }
}
