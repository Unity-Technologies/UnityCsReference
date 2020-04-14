// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.GraphView
{
    class LineView : VisualElement
    {
        // color for lines
        internal static PrefColor s_SnappingLineColor = new PrefColor("General/Graph Snapping Line Color", 68 / 255f, 192 / 255f, 255 / 255f, 0.2f);

        public List<Line2> lines { get; private set; } = new List<Line2>();
        public LineView()
        {
            this.StretchToParentSize();
            generateVisualContent += OnGenerateVisualContent;
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            GraphView gView = GetFirstAncestorOfType<GraphView>();
            if (gView == null)
                return;

            VisualElement container = gView.contentViewContainer;
            foreach (Line2 line in lines)
            {
                Vector2 start = container.ChangeCoordinatesTo(gView, line.start);
                Vector2 end = container.ChangeCoordinatesTo(gView, line.end);
                float x = Math.Min(start.x, end.x);
                float y = Math.Min(start.y, end.y);
                float width = Math.Max(1, Math.Abs(start.x - end.x));
                float height = Math.Max(1, Math.Abs(start.y - end.y));

                var rect = new Rect(x, y, width, height);

                mgc.Rectangle(MeshGenerationContextUtils.RectangleParams.MakeSolid(rect, s_SnappingLineColor, ContextType.Editor));
            }
        }
    }
}
