// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Elements that displays a miniature view of the graph.
    /// </summary>
    class MiniMap : ModelView
    {
        public static readonly string ussClassName = "ge-minimap";

        static readonly CustomStyleProperty<Color> k_ViewportColorProperty = new CustomStyleProperty<Color>("--viewport-color");
        static readonly CustomStyleProperty<Color> k_SelectedElementColorProperty = new CustomStyleProperty<Color>("--selected-element-color");
        static readonly CustomStyleProperty<Color> k_HighlightedElementColorProperty = new CustomStyleProperty<Color>("--highlighted-element-color");
        static readonly CustomStyleProperty<Color> k_PlacematBorderColorProperty = new CustomStyleProperty<Color>("--placemat-border-color");

        static Color DefaultViewportColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return new Color(230/255f, 230/255f, 230/255f, 0.5f);
                }

                return new Color(138/255f, 138/255f, 138/255f, 1f);
            }
        }

        static Color DefaultSelectedElementColor => DynamicBorder.DefaultSelectionColor;
        static Color DefaultHighlightedElementColor => DynamicBorder.DefaultHighlightColor;

        static Color DefaultPlacematBorderColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return new Color(26/255f, 26/255f, 26/255f);
                }

                return new Color(138/255f, 138/255f, 138/255f, 1f);
            }
        }

        Color m_ViewportColor = DefaultViewportColor;
        Color m_SelectedElementColor = DefaultSelectedElementColor;
        Color m_HighlightedElementColor = DefaultHighlightedElementColor;
        Color m_PlacematBorderColor = DefaultPlacematBorderColor;

        Rect m_ViewportRect;        // Rect that represents the current viewport

        Rect m_ContentRect;         // Rect that represents the rect needed to encompass all Graph Elements

        Rect m_ContentRectLocal;    // Rect that represents the rect needed to encompass all Graph Elements in local coords

        int TitleBarOffset => (int)resolvedStyle.paddingTop;

        public GraphModel GraphModel => Model as GraphModel;

        public Action<string> ZoomFactorTextChanged { get; set; }

        GraphView GraphView => (RootView as MiniMapView)?.MiniMapViewModel.ParentGraphView;

        /// <summary>
        /// Initializes a new instance of the <see cref="MiniMap"/> class.
        /// </summary>
        public MiniMap()
        {
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            AddToClassList(ussClassName);

            generateVisualContent += OnGenerateVisualContent;
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            // Ask for a repaint to trigger OnGenerateVisualContent().
            MarkDirtyRepaint();
        }

        protected override void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            base.OnCustomStyleResolved(evt);

            if (evt.customStyle.TryGetValue(k_ViewportColorProperty, out var viewportColor))
                m_ViewportColor = viewportColor;
            if (evt.customStyle.TryGetValue(k_SelectedElementColorProperty, out var selectedElementColor))
                m_SelectedElementColor = selectedElementColor;
            if (evt.customStyle.TryGetValue(k_HighlightedElementColorProperty, out var highlightedElementColor))
                m_HighlightedElementColor = highlightedElementColor;
            if (evt.customStyle.TryGetValue(k_PlacematBorderColorProperty, out var placematBorderColor))
                m_PlacematBorderColor = placematBorderColor;
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
            ZoomFactorTextChanged?.Invoke(zoomFactorText);
        }

        void CalculateRects()
        {
            if (GraphView == null)
            {
                // Nothing to do in this case.
                return;
            }

            var container = GraphView.ContentViewContainer;
            m_ContentRect = GraphView.CalculateRectToFitAll();
            m_ContentRectLocal = m_ContentRect;

            // Retrieve viewport rectangle as if zoom and pan were inactive
            var containerInvTransform = container.worldTransformInverse;
            var containerInvTranslation = containerInvTransform.GetColumn(3);
            var containerInvScale = new Vector2(containerInvTransform.m00, containerInvTransform.m11);

            var graphViewLayout = GraphView.layout;
            m_ViewportRect =  new Rect(0.0f, 0.0f, graphViewLayout.width, graphViewLayout.height);

            // Bring back viewport coordinates to (0,0), scale 1:1
            m_ViewportRect.x += containerInvTranslation.x;
            m_ViewportRect.y += containerInvTranslation.y;

            var graphViewWorldBound = GraphView.worldBound;

            m_ViewportRect.x += graphViewWorldBound.x * containerInvScale.x;
            m_ViewportRect.y += graphViewWorldBound.y * containerInvScale.y;
            m_ViewportRect.width *= containerInvScale.x;
            m_ViewportRect.height *= containerInvScale.y;

            // Update label with new value
            var containerZoomFactor = container.worldTransform.m00;
            SetZoomFactorText(String.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:F2}", containerZoomFactor) + "x");

            // Adjust rects for MiniMap
            var effectiveWidth = layout.width - 1;
            var effectiveHeight = layout.height - 1;

            // Encompass viewport rectangle (as if zoom and pan were inactive)
            var totalRect = RectUtils_Internal.Encompass(m_ContentRect, m_ViewportRect);
            var minimapFactor = effectiveWidth / totalRect.width;

            // Transform each rect to MiniMap coordinates
            ChangeToMiniMapCoords(ref totalRect, minimapFactor, Vector3.zero);

            var minimapTranslation = new Vector3(-totalRect.x, TitleBarOffset - totalRect.y);
            ChangeToMiniMapCoords(ref m_ViewportRect, minimapFactor, minimapTranslation);
            ChangeToMiniMapCoords(ref m_ContentRect, minimapFactor, minimapTranslation);

            // Diminish and center everything to fit vertically
            if (totalRect.height > (effectiveHeight - TitleBarOffset))
            {
                var totalRectFactor = (effectiveHeight - TitleBarOffset) / totalRect.height;
                var totalRectOffsetX = (effectiveWidth - (totalRect.width * totalRectFactor)) / 2.0f;
                var totalRectOffsetY = TitleBarOffset - ((totalRect.y + minimapTranslation.y) * totalRectFactor);

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

        Rect CalculateElementRect(ModelView elem)
        {
            var r = elem.parent.ChangeCoordinatesTo(GraphView.ContentViewContainer, elem.layout);
            r.x = m_ContentRect.x + ((r.x - m_ContentRectLocal.x) * m_ContentRect.width / m_ContentRectLocal.width);
            r.y = m_ContentRect.y + ((r.y - m_ContentRectLocal.y) * m_ContentRect.height / m_ContentRectLocal.height);
            r.width *= m_ContentRect.width / m_ContentRectLocal.width;
            r.height *= m_ContentRect.height / m_ContentRectLocal.height;

            // Clip using a minimal 2 pixel wide frame around wires
            // (except yMin since we already have the titleBar offset which is enough for clipping)
            var xMin = 2;
            var yMin = 2;
            var xMax = layout.width - 2;
            var yMax = layout.height - 2;

            if (r.x < xMin)
            {
                if (r.x < xMin - r.width)
                    return new Rect(0, 0, 0, 0);
                r.width -= xMin - r.x;
                r.x = xMin;
            }

            if (r.x + r.width >= xMax)
            {
                if (r.x >= xMax)
                    return new Rect(0, 0, 0, 0);
                r.width -= r.x + r.width - xMax;
            }

            if (r.y < yMin + TitleBarOffset)
            {
                if (r.y < yMin + TitleBarOffset - r.height)
                    return new Rect(0, 0, 0, 0);
                r.height -= yMin + TitleBarOffset - r.y;
                r.y = yMin + TitleBarOffset;
            }

            if (r.y + r.height >= yMax)
            {
                if (r.y >= yMax)
                    return new Rect(0, 0, 0, 0);
                r.height -= r.y + r.height - yMax;
            }

            return r;
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (GraphView == null)
            {
                return;
            }

            // // Retrieve all container relative information
            // var containerTransform = GraphView.ViewTransform.matrix;
            // var containerScale = new Vector2(containerTransform.m00, containerTransform.m11);

            // Refresh MiniMap rects
            CalculateRects();

            var painter = mgc.painter2D;

            DrawElements(painter);

            // Draw viewport outline
            PathRectangle(painter, m_ViewportRect);
            painter.strokeColor = m_ViewportColor;
            painter.Stroke();
        }

        static readonly List<ModelView> k_DrawElementsAllUIs = new List<ModelView>();
        void DrawElements(Painter2D painter)
        {
            if (GraphModel == null)
                return;

            // Draw placemats first ...
            foreach (var placemat in GraphModel.PlacematModels)
            {
                var placematUI = placemat.GetView<GraphElement>(GraphView);

                if (placematUI == null)
                    continue;

                var elemRect = CalculateElementRect(placematUI);
                PathRectangle(painter, elemRect);

                var fillColor = placematUI.resolvedStyle.backgroundColor;
                fillColor.a = 0.50f;
                painter.fillColor = fillColor;
                painter.Fill();
                painter.strokeColor = placematUI.IsSelected() ? m_SelectedElementColor : m_PlacematBorderColor;
                painter.Stroke();
            }

            // ... then the other elements
            GraphModel.GraphElementModels.GetAllViewsInList_Internal(GraphView,
                elem => (!(elem is GraphElement ge) || ge.ShowInMiniMap && ge.visible) && !(elem is Placemat), k_DrawElementsAllUIs);
            foreach (var elem in k_DrawElementsAllUIs.OfType<GraphElement>())
            {
                var elemRect = CalculateElementRect(elem);
                PathRectangle(painter, elemRect);
                painter.fillColor = elem.MinimapColor;
                painter.Fill();
                painter.strokeColor = elem.IsSelected() ? m_SelectedElementColor : elem.ShouldBeHighlighted() ? m_HighlightedElementColor: elem.MinimapColor;
                painter.Stroke();
            }
            k_DrawElementsAllUIs.Clear();
        }

        void PathRectangle(Painter2D painter, Rect r)
        {
            painter.BeginPath();
            painter.MoveTo(r.min);
            painter.LineTo(new Vector2(r.xMin, r.yMax));
            painter.LineTo(new Vector2(r.xMax, r.yMax));
            painter.LineTo(new Vector2(r.xMax, r.yMin));
            painter.ClosePath();
        }

        static readonly List<ModelView> k_OnMouseDownAllUIs = new List<ModelView>();
        void OnMouseDown(MouseDownEvent e)
        {
            if (GraphView == null)
            {
                // Nothing to do if we're not attached to a GraphView!
                return;
            }

            // Refresh MiniMap rects
            CalculateRects();

            var mousePosition = e.localMousePosition;

            GraphModel.GraphElementModels.GetAllViewsInList_Internal(GraphView,
                elem => elem != null, k_OnMouseDownAllUIs);
            foreach (var child in k_OnMouseDownAllUIs.OfType<GraphElement>())
            {
                var isSelectable = child.GraphElementModel?.IsSelectable() ?? false;
                if (!isSelectable)
                {
                    continue;
                }

                if (CalculateElementRect(child).Contains(mousePosition))
                {
                    GraphView.DispatchFrameAndSelectElementsCommand(true, child);
                    e.StopPropagation();
                    break;
                }
            }

            k_OnMouseDownAllUIs.Clear();
        }
    }
}
