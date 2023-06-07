// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Vector2 = UnityEngine.Vector2;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A visual element which will be in charge of drawing the border of a ModelView.
    /// </summary>
    class DynamicBorder : VisualElement
    {
        public static readonly Color32 DefaultSelectionColor = new Color32(68, 192, 255, 255);
        public static readonly Color32 DefaultHighlightColor = new Color32(68, 0, 255, 128);
        public static readonly Color32 DefaultHoverOnlyColor = new Color32(68, 192, 255, 128);

        static readonly CustomStyleProperty<float> k_SelectionWidthProperty = new CustomStyleProperty<float>("--selection-width");
        static readonly CustomStyleProperty<float> k_SmallSelectionWidthProperty = new CustomStyleProperty<float>("--small-selection-width");
        static readonly CustomStyleProperty<float> k_HoverWidthProperty = new CustomStyleProperty<float>("--hover-width");
        static readonly CustomStyleProperty<float> k_SmallWidthThresholdProperty = new CustomStyleProperty<float>("--small-width-threshold");
        static readonly CustomStyleProperty<float> k_CornersThresholdProperty = new CustomStyleProperty<float>("--corners-threshold");

        static readonly CustomStyleProperty<Color> k_SelectionColorProperty = new CustomStyleProperty<Color>("--selection-color");
        static readonly CustomStyleProperty<Color> k_HoverOnlyColorProperty = new CustomStyleProperty<Color>("--hover-only-color");
        static readonly CustomStyleProperty<Color> k_HighlightColorProperty = new CustomStyleProperty<Color>("--highlight-color");

        static readonly Vector2[] k_EmptyCorners = Enumerable.Repeat(Vector2.zero, 4).ToArray();
        static Vector2[] s_Corners = new Vector2[4];
        static Color[] s_Colors = new Color[4];

        protected bool m_Hover;
        protected bool m_Selected;
        protected bool m_Highlighted;

        float m_Zoom = 1;
        protected const float k_MinZoom = 0.1f;
        GraphView m_GraphView;

        /// <summary>
        /// The width of the selection outline.
        /// </summary>
        public float SelectionWidth { get; private set; } = 2;

        /// <summary>
        /// The width of the selection outline. When below <see cref="SmallWidthThreshold"/>.
        /// </summary>
        public float SmallSelectionWidth { get; private set; } = 1;

        /// <summary>
        /// The width of the hover outline.
        /// </summary>
        public float HoverWidth { get; private set; } = 1;

        /// <summary>
        /// The zoom threshold at which the selection width change.
        /// </summary>
        public float SmallWidthThreshold { get; private set; } = 0.5f;

        /// <summary>
        /// The zoom threshold at which the corners are no longer rounded.
        /// </summary>
        public float CornersThreshold { get; private set; } = 0.33f;

        /// <summary>
        /// The color of the outline when the <see cref="GraphElement"/> is selected.
        /// </summary>
        public Color SelectionColor { get; private set; } = DefaultSelectionColor;

        /// <summary>
        /// The color of the outline when the <see cref="GraphElement"/> is only hovered.
        /// </summary>
        public Color HoverOnlyColor { get; private set; } = DefaultHoverOnlyColor;

        /// <summary>
        /// The color of the outline when the <see cref="GraphElement"/> is highlighted.
        /// </summary>
        public Color HighlightColor { get; private set; } = DefaultHighlightColor;

        /// <summary>
        /// The zoom of the <see cref="ModelView"/>.
        /// </summary>
        public float Zoom
        {
            get => m_Zoom;
            set
            {
                if (m_Zoom != value)
                {
                    m_Zoom = value;
                    MarkDirtyRepaint();
                }
            }
        }

        /// <summary>
        /// Whether the <see cref="ModelView"/> is hovered.
        /// </summary>
        public bool Hovered
        {
            get => m_Hover;
            set
            {
                if (m_Hover != value)
                {
                    m_Hover = value;
                    MarkDirtyRepaint();
                }
            }
        }

        /// <summary>
        /// Whether the <see cref="ModelView"/> is selected.
        /// </summary>
        public bool Selected
        {
            get => m_Selected;
            set
            {
                if (m_Selected != value)
                {
                    m_Selected = value;
                    MarkDirtyRepaint();
                }
            }
        }

        /// <summary>
        /// Whether the <see cref="ModelView"/> is highlighted.
        /// </summary>
        public bool Highlighted
        {
            get => m_Highlighted;
            set
            {
                if (m_Highlighted != value)
                {
                    m_Highlighted = value;
                    if (!Selected)
                        MarkDirtyRepaint();
                }
            }
        }

        /// <summary>
        /// Gets the offset added to the border radius.
        /// </summary>
        /// <param name="width">The computed width of the border.</param>
        /// <returns>The offset added to the border radius.</returns>
        protected virtual float GetCornerOffset(float width) => 0;

        /// <summary>
        /// The color of the border based on the current state.
        /// </summary>
        public Color ComputedColor => m_Selected ? SelectionColor : m_Highlighted ? HighlightColor : m_Hover ? HoverOnlyColor : Color.clear;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicBorder"/> class.
        /// </summary>
        /// <param name="view">The <see cref="ModelView"/> on which the border will appear.</param>
        public DynamicBorder(ModelView view)
        {
            generateVisualContent += OnGenerateVisualContent;
            style.position = Position.Absolute;
            pickingMode = PickingMode.Ignore;

            m_GraphView = view.RootView as GraphView;
            view.RegisterCallback<MouseEnterEvent>(OnEnter);
            view.RegisterCallback<MouseLeaveEvent>(OnLeave);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            style.left = 0;
            style.right = 0;
            style.bottom = 0;
            style.top = 0;
        }

        void OnEnter(MouseEnterEvent e)
        {
            // When a wire is being dragged, graph elements' hover borders should not be shown.
            if (m_GraphView is { IsWireDragging: true })
                return;

            m_Hover = true;
            MarkDirtyRepaint();
        }

        void OnLeave(MouseLeaveEvent e)
        {
            m_Hover = false;
            MarkDirtyRepaint();
        }

        /// <summary>
        /// Called when custom styles are resolved.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            if (e.customStyle.TryGetValue(k_SelectionWidthProperty, out var value))
                SelectionWidth = value;
            if (e.customStyle.TryGetValue(k_SmallSelectionWidthProperty, out value))
                SmallSelectionWidth = value;
            if (e.customStyle.TryGetValue(k_HoverWidthProperty, out value))
                HoverWidth = value;
            if (e.customStyle.TryGetValue(k_SmallWidthThresholdProperty, out value))
                SmallWidthThreshold = value;
            if (e.customStyle.TryGetValue(k_CornersThresholdProperty, out value))
                CornersThreshold = value;

            if (e.customStyle.TryGetValue(k_SelectionColorProperty, out var colorValue))
                SelectionColor = colorValue;
            if (e.customStyle.TryGetValue(k_HoverOnlyColorProperty, out colorValue))
                HoverOnlyColor = colorValue;
            if (e.customStyle.TryGetValue(k_HighlightColorProperty, out colorValue))
                HighlightColor = colorValue;
        }

        /// <summary>
        /// Called after bounds are computed so the inheriting class has a chance to change it.
        /// </summary>
        /// <param name="bounds">The bounds of the border that will be drawn.</param>
        /// <param name="width">the computed width of the border.</param>
        protected virtual void AlterBounds(ref Rect bounds, float width)
        {
        }

        /// <summary>
        /// Draws a border based on current state.
        /// </summary>
        /// <param name="mgc">The <see cref="MeshGenerationContext"/>.</param>
        /// <param name="localRect">The rectangle in local coordinates.</param>
        /// <param name="wantedWidth">The width of the border.</param>
        /// <param name="colors">The colors (top,left,right,bottom) of the border.</param>
        /// <param name="corners">The radii (top,left,right,bottom) of the border corners.</param>
        protected virtual void DrawBorder(MeshGenerationContext mgc, Rect localRect, float wantedWidth, Color[] colors,Vector2[] corners)
        {
            MeshDrawingHelpers_Internal.Border(mgc, localRect, colors, wantedWidth,corners, ContextType.Editor);
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (!m_Hover && !Selected && !Highlighted)
                return;

            var bounds = localBound;
            bounds.position -= layout.position;

            float zoomLevel = Zoom;
            Vector2[] corners;
            var width = ComputedWidth;

            AlterBounds(ref bounds, width);

            if (zoomLevel < CornersThreshold)
                corners = k_EmptyCorners;
            else
            {
                var tlr = parent.resolvedStyle.borderTopLeftRadius;
                var trr = parent.resolvedStyle.borderTopRightRadius;
                var brr = parent.resolvedStyle.borderBottomRightRadius;
                var brl = parent.resolvedStyle.borderBottomLeftRadius;

                var cornerOffset = GetCornerOffset(width);
                s_Corners[0].x = tlr + cornerOffset;
                s_Corners[0].y = tlr + cornerOffset;
                s_Corners[1].x = trr + cornerOffset;
                s_Corners[1].y = trr + cornerOffset;
                s_Corners[2].x = brr + cornerOffset;
                s_Corners[2].y = brr + cornerOffset;
                s_Corners[3].x = brl + cornerOffset;
                s_Corners[3].y = brl + cornerOffset;

                corners = s_Corners;
            }


            s_Colors[0] = ComputedColor;
            s_Colors[1] = s_Colors[0];
            s_Colors[2] = s_Colors[0];
            s_Colors[3] = s_Colors[0];

            DrawBorder(mgc, bounds, width, s_Colors, corners);
        }

        /// <summary>
        /// The width of the border based on the current state.
        /// </summary>
        public float ComputedWidth
        {
            get
            {
                var zoomLevel = Zoom;
                var selectionFactor = m_Selected || m_Highlighted ? 1.0f : 0;
                float wantedWidth;

                if (zoomLevel < SmallWidthThreshold)
                    wantedWidth = selectionFactor * SmallSelectionWidth + (m_Hover ? HoverWidth : 0);
                else
                    wantedWidth = selectionFactor * SelectionWidth + (m_Hover ? HoverWidth : 0);

                if (zoomLevel > 1.0f)
                    zoomLevel = 1.0f;
                return wantedWidth / zoomLevel;
            }
        }
    }
}
