// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The dynamic border of <see cref="SubgraphNodeView"/>s, that has a tab.
    /// </summary>
    class DynamicSubgraphNodeBorder : DynamicBorder
    {
        /// <summary>
        /// The <see cref="SubgraphNodeView"/> this border is for.
        /// </summary>
        SubgraphNodeView SubgraphNodeView { get; }

        /// <summary>
        /// Initialize a new instance of the <see cref="DynamicSubgraphNodeBorder"/> class.
        /// </summary>
        /// <param name="subgraphNode">The subgraph node.</param>
        public DynamicSubgraphNodeBorder(SubgraphNodeView subgraphNode)
            : base(subgraphNode)
        {
            SubgraphNodeView = subgraphNode;
        }

        /// <inheritdoc />
        protected override void DrawBorder(MeshGenerationContext mgc, Rect r, float wantedWidth, Color[] colors, Vector2[] corners)
        {
            DrawBorder(mgc.painter2D, r, wantedWidth, colors[0], corners);
        }

        void DrawBorder(Painter2D p2d, Rect r, float wantedWidth, Color color, IReadOnlyList<Vector2> corners)
        {
            var tabElement = SubgraphNodeView.SafeQ(SubgraphNodeView.tabUssClassName);
            if (tabElement is null)
                return;

            const int distBeforeTab = 8;
            var tabRect = new Rect(
                distBeforeTab,
                tabElement.layout.y,
                tabElement.layout.width * 0.25f,
                tabElement.layout.height * 0.5f);

            DrawSubgraphNodeBorder(p2d, tabRect, r, cornersRadius: corners);
            p2d.strokeColor = color;
            p2d.lineWidth = wantedWidth;
            p2d.Stroke();
        }

        static void DrawSubgraphNodeBorder(Painter2D p2d, Rect tabRect, Rect nodeRect, IReadOnlyList<Vector2> cornersRadius)
        {
            SubgraphNodeView.DrawSubgraphTab(p2d, tabRect, nodeRect, cornersRadius[2], cornersRadius[3]);
        }
    }
}
