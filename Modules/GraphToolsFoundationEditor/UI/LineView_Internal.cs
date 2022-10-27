// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    class LineView_Internal : VisualElement
    {
        static readonly CustomStyleProperty<Color> k_LineColorProperty = new CustomStyleProperty<Color>("--line-color");

        public static Color DefaultLineColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return new Color(200/255f, 200/255f, 200/255f, 0.05f);
                }

                return new Color(65/255f, 65/255f, 65/255f, 0.07f);
            }
        }

        Color m_LineColor = DefaultLineColor;

        GraphView m_GraphView;

        public LineView_Internal(GraphView graphView)
        {
            this.AddStylesheet_Internal("LineView.uss");
            this.StretchToParentSize();
            generateVisualContent += OnGenerateVisualContent;
            m_GraphView = graphView;

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            if (e.customStyle.TryGetValue(k_LineColorProperty, out var lineColor))
                m_LineColor = lineColor;
        }

        public List<Line_Internal> lines { get; } = new List<Line_Internal>();

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (m_GraphView == null)
            {
                return;
            }
            var container = m_GraphView.ContentViewContainer;
            foreach (var line in lines)
            {
                var start = container.ChangeCoordinatesTo(m_GraphView, line.Start);
                var end = container.ChangeCoordinatesTo(m_GraphView, line.End);
                var x = Math.Min(start.x, end.x);
                var y = Math.Min(start.y, end.y);
                var width = Math.Max(1, Math.Abs(start.x - end.x));
                var height = Math.Max(1, Math.Abs(start.y - end.y));
                var r = new Rect(x, y, width, height);

                MeshDrawingHelpers_Internal.SolidRectangle(mgc, r, m_LineColor, ContextType.Editor);
            }
        }
    }
}
