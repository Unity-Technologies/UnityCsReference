// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.GraphView
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
        protected readonly Color m_SelectedChildrenColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);
        readonly Color m_PlacematBorderColor = new Color(0.23f, 0.23f, 0.23f);

        // Various rects used by the MiniMap
        Rect m_ViewportRect;        // Rect that represents the current viewport
        Rect m_ContentRect;         // Rect that represents the rect needed to encompass all Graph Elements
        Rect m_ContentRectLocal;    // Rect that represents the rect needed to encompass all Graph Elements in local coords

        int titleBarOffset { get { return (int)resolvedStyle.paddingTop; } }

        public Action<string> zoomFactorTextChanged;

        private bool m_Anchored;
        public bool anchored
        {
            get { return m_Anchored; }
            set
            {
                if (windowed || m_Anchored == value)
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

        bool m_Windowed;
        public bool windowed
        {
            get { return m_Windowed; }
            set
            {
                if (m_Windowed == value) return;

                if (value)
                {
                    anchored = false; // Can't be anchored and windowed
                    capabilities &= ~Capabilities.Movable;
                    AddToClassList("windowed");
                    this.RemoveManipulator(m_Dragger);
                }
                else
                {
                    capabilities |= Capabilities.Movable;
                    RemoveFromClassList("windowed");
                    this.AddManipulator(m_Dragger);
                }
                m_Windowed = value;
            }
        }

        public MiniMap()
        {
            capabilities = Capabilities.Movable;

            m_Dragger = new Dragger { clampToParentEdges = true };
            this.AddManipulator(m_Dragger);

            anchored = false;

            maxWidth = 200;
            maxHeight = 200;

            m_Label = new Label("Floating Minimap");

            Add(m_Label);

            RegisterCallback<MouseDownEvent>(OnMouseDown);
            m_Label.RegisterCallback<MouseDownEvent>(EatMouseDown);

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
            AddStyleSheetPath("StyleSheets/GraphView/Minimap.uss");

            this.generateVisualContent += OnGenerateVisualContent;
        }

        private GraphView m_GraphView;
        public GraphView graphView
        {
            get
            {
                if (!windowed && m_GraphView == null)
                    m_GraphView = GetFirstAncestorOfType<GraphView>();
                return m_GraphView;
            }

            set
            {
                if (!windowed)
                    return;
                m_GraphView = value;
            }
        }

        void ToggleAnchorState(DropdownMenuAction a)
        {
            anchored = !anchored;
        }

        public virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (!windowed)
                evt.menu.AppendAction(anchored ? "Make floating" : "Anchor", ToggleAnchorState, DropdownMenuAction.AlwaysEnabled);
        }

        public void OnResized()
        {
            Resize();
        }

        void Resize()
        {
            if (windowed || parent == null)
                return;

            style.width = maxWidth;
            style.height = maxHeight;

            // Relocate if partially visible on bottom or right side (left/top not checked, only bottom/right affected by a size change)
            if (resolvedStyle.left + resolvedStyle.width > parent.layout.x + parent.layout.width)
            {
                var newPosition = layout;
                newPosition.x -= resolvedStyle.left + resolvedStyle.width - (parent.layout.x + parent.layout.width);
                layout = newPosition;
            }

            if (resolvedStyle.top + resolvedStyle.height > parent.layout.y + parent.layout.height)
            {
                var newPosition = layout;
                newPosition.y -= resolvedStyle.top + resolvedStyle.height - (parent.layout.y + parent.layout.height);
                layout = newPosition;
            }

            var newMiniMapPos = layout;
            newMiniMapPos.width = resolvedStyle.width;
            newMiniMapPos.height = resolvedStyle.height;
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

        void SetZoomFactorText(string zoomFactorText)
        {
            m_Label.text = "MiniMap  " + zoomFactorText;
            zoomFactorTextChanged?.Invoke(zoomFactorText);
        }

        void CalculateRects(VisualElement container)
        {
            if (graphView == null)
            {
                // Nothing to do in this case.
                return;
            }

            m_ContentRect = graphView.CalculateRectToFitAll(container);
            m_ContentRectLocal = m_ContentRect;

            // Retrieve viewport rectangle as if zoom and pan were inactive
            Matrix4x4 containerInvTransform = container.worldTransformInverse;
            Vector4 containerInvTranslation = containerInvTransform.GetColumn(3);
            var containerInvScale = new Vector2(containerInvTransform.m00, containerInvTransform.m11);

            m_ViewportRect = graphView.rect;

            // Bring back viewport coordinates to (0,0), scale 1:1
            m_ViewportRect.x += containerInvTranslation.x;
            m_ViewportRect.y += containerInvTranslation.y;

            var graphViewWB = graphView.worldBound;

            m_ViewportRect.x += graphViewWB.x * containerInvScale.x;
            m_ViewportRect.y += graphViewWB.y * containerInvScale.y;
            m_ViewportRect.width *= containerInvScale.x;
            m_ViewportRect.height *= containerInvScale.y;

            // Update label with new value
            var containerZoomFactor = container.worldTransform.m00;
            SetZoomFactorText(UnityString.Format("{0:F2}", containerZoomFactor) + "x");

            // Adjust rects for MiniMap
            float effectiveWidth = layout.width - 1;
            float effectiveHeight = layout.height - 1;

            // Encompass viewport rectangle (as if zoom and pan were inactive)
            var totalRect = RectUtils.Encompass(m_ContentRect, m_ViewportRect);
            var minimapFactor = effectiveWidth / totalRect.width;

            // Transform each rect to MiniMap coordinates
            ChangeToMiniMapCoords(ref totalRect, minimapFactor, Vector3.zero);

            var minimapTranslation = new Vector3(-totalRect.x, titleBarOffset - totalRect.y);
            ChangeToMiniMapCoords(ref m_ViewportRect, minimapFactor, minimapTranslation);
            ChangeToMiniMapCoords(ref m_ContentRect, minimapFactor, minimapTranslation);

            // Diminish and center everything to fit vertically
            if (totalRect.height > (effectiveHeight - titleBarOffset))
            {
                float totalRectFactor = (effectiveHeight - titleBarOffset) / totalRect.height;
                float totalRectOffsetX = (effectiveWidth - (totalRect.width * totalRectFactor)) / 2.0f;
                float totalRectOffsetY = titleBarOffset - ((totalRect.y + minimapTranslation.y) * totalRectFactor);

                m_ContentRect.width *= totalRectFactor;
                m_ContentRect.height *= totalRectFactor;
                m_ContentRect.x *= totalRectFactor;
                m_ContentRect.y *= totalRectFactor;
                m_ContentRect.x += totalRectOffsetX;
                m_ContentRect.y += totalRectOffsetY;

                m_ViewportRect.width *= totalRectFactor;
                m_ViewportRect.height *= totalRectFactor;
                m_ViewportRect.x *= totalRectFactor;
                m_ViewportRect.y *= totalRectFactor;
                m_ViewportRect.x += totalRectOffsetX;
                m_ViewportRect.y += totalRectOffsetY;
            }
        }

        Rect CalculateElementRect(GraphElement elem)
        {
            Rect rect = elem.ChangeCoordinatesTo(graphView.contentViewContainer, elem.rect);
            rect.x = m_ContentRect.x + ((rect.x - m_ContentRectLocal.x) * m_ContentRect.width / m_ContentRectLocal.width);
            rect.y = m_ContentRect.y + ((rect.y - m_ContentRectLocal.y) * m_ContentRect.height / m_ContentRectLocal.height);
            rect.width *= m_ContentRect.width / m_ContentRectLocal.width;
            rect.height *= m_ContentRect.height / m_ContentRectLocal.height;

            // Clip using a minimal 2 pixel wide frame around edges
            // (except yMin since we already have the titleBar offset which is enough for clipping)
            var xMin = 2;
            var yMin = windowed ? 2 : 0;
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

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            // This control begs to be fully rewritten and it shouldn't use immediate
            // mode rendering at all. It should maintain its vertex/index lists and only
            // update affected vertices when their respective elements are changed. This
            // way the cost of GenerateVisualContent becomes effectively only two memcpys.
            mgc.painter.DrawImmediate(DrawMinimapContent, true);
        }

        void DrawSolidRectangleWithOutline(ref Vector3[] cachedRect, Color faceColor, Color typeColor)
        {
            Handles.DrawSolidRectangleWithOutline(cachedRect, faceColor, typeColor);
        }

        void DrawMinimapContent()
        {
            Color currentColor = Handles.color;

            if (graphView == null)
            {
                // Just need to draw the minimum rect.
                Resize();
                return;
            }

            VisualElement container = graphView.contentViewContainer;

            // Retrieve all container relative information
            Matrix4x4 containerTransform = graphView.viewTransform.matrix;
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

            DrawElements();

            // Draw viewport outline
            DrawRectangleOutline(m_ViewportRect, m_ViewportColor);

            Handles.color = currentColor;
        }

        void DrawElements()
        {
            // Draw placemats first ...
            var placemats = graphView.placematContainer.Placemats;
            foreach (var placemat in placemats.Where(p => p.showInMiniMap && p.visible))
            {
                var elemRect = CalculateElementRect(placemat);

                s_CachedRect[0].Set(elemRect.xMin, elemRect.yMin, 0.0f);
                s_CachedRect[1].Set(elemRect.xMax, elemRect.yMin, 0.0f);
                s_CachedRect[2].Set(elemRect.xMax, elemRect.yMax, 0.0f);
                s_CachedRect[3].Set(elemRect.xMin, elemRect.yMax, 0.0f);

                Color fillColor = placemat.resolvedStyle.backgroundColor;
                fillColor.a = 0.15f;

                DrawSolidRectangleWithOutline(ref s_CachedRect, fillColor, m_PlacematBorderColor);
            }

            // ... then the other elements
            Color darken = UIElementsUtility.editorPlayModeTintColor;
            graphView.graphElements.ForEach(elem =>
            {
                if (!elem.showInMiniMap || !elem.visible || elem is Placemat)
                    return;

                var elemRect = CalculateElementRect(elem);
                s_CachedRect[0].Set(elemRect.xMin, elemRect.yMin, 0.0f);
                s_CachedRect[1].Set(elemRect.xMax, elemRect.yMin, 0.0f);
                s_CachedRect[2].Set(elemRect.xMax, elemRect.yMax, 0.0f);
                s_CachedRect[3].Set(elemRect.xMin, elemRect.yMax, 0.0f);

                Handles.color = elem.elementTypeColor * darken;

                DrawSolidRectangleWithOutline(ref s_CachedRect, elem.elementTypeColor, elem.elementTypeColor);

                if (elem.selected)
                    DrawRectangleOutline(elemRect, m_SelectedChildrenColor);
            });
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
            if (graphView == null)
            {
                // Nothing to do if we're not attached to a GraphView!
                return;
            }

            // Refresh MiniMap rects
            CalculateRects(graphView.contentViewContainer);

            var mousePosition = e.localMousePosition;

            graphView.graphElements.ForEach(child =>
            {
                if (child == null)
                    return;
                var selectable = child.GetFirstOfType<ISelectable>();
                if (selectable == null || !selectable.IsSelectable())
                    return;

                if (CalculateElementRect(child).Contains(mousePosition))
                {
                    graphView.ClearSelection();
                    graphView.AddToSelection(selectable);
                    graphView.FrameSelection();
                    e.StopPropagation();
                }
            });

            EatMouseDown(e);
        }
    }
}
