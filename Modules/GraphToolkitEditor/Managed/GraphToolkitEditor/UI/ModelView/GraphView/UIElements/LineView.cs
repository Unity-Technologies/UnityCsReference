// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A VisualElement to draw lines over the graph view.
    /// </summary>
    [UnityRestricted]
    internal class LineView : VisualElement
    {
        static readonly CustomStyleProperty<Color> k_LineColorProperty = new("--snapping-line-color");

        /// <summary>
        /// The USS class name of the arrow input of a <see cref="LineView"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-line-view";

        static Color DefaultLineColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return new Color(200 / 255f, 200 / 255f, 200 / 255f, 0.05f);
                }

                return new Color(65 / 255f, 65 / 255f, 65 / 255f, 0.07f);
            }
        }

        Color m_LineColor;

        /// <summary>
        /// The lines to draw.
        /// </summary>
        public List<Line> Lines { get; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="LineView"/> class.
        /// </summary>
        public LineView()
        {
            this.AddPackageStylesheet("LineView.uss");
            this.StretchToParentSize();
            AddToClassList(ussClassName);
            generateVisualContent += OnGenerateVisualContent;
            pickingMode = PickingMode.Ignore;
            m_LineColor = DefaultLineColor;

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            if (e.customStyle.TryGetValue(k_LineColorProperty, out var lineColor))
                m_LineColor = lineColor;
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (parent is not GraphView graphView)
            {
                return;
            }
            var container = graphView.ContentViewContainer;
            foreach (var line in Lines)
            {
                var start = container.ChangeCoordinatesTo(graphView, line.Start);
                var end = container.ChangeCoordinatesTo(graphView, line.End);
                var x = Math.Min(start.x, end.x);
                var y = Math.Min(start.y, end.y);
                var width = Math.Max(1, Math.Abs(start.x - end.x));
                var height = Math.Max(1, Math.Abs(start.y - end.y));
                var r = new Rect(x, y, width, height);

                MeshDrawingHelpers.SolidRectangle(mgc, r, m_LineColor);
            }
        }
    }
}
