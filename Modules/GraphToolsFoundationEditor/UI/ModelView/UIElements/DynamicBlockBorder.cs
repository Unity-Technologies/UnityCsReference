// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The dynamic border of <see cref="BlockNode"/>s, that have an etch.
    /// </summary>
    class DynamicBlockBorder : DynamicBorder
    {
        static Vector2[] s_EtchCorners = new Vector2[4];

        /// <summary>
        /// The <see cref="BlockNode"/> this border is for.
        /// </summary>
        BlockNode Node { get; }

        /// <summary>
        /// Initialize a new instance of the <see cref="DynamicBlockBorder"/> class.
        /// </summary>
        /// <param name="view"></param>
        public DynamicBlockBorder(BlockNode view)
            : base(view)
        {
            Node = view;

            s_EtchCorners[0] = Vector2.zero;
            s_EtchCorners[1] = Vector2.zero;
        }

        bool m_DisplayEtch = true;

        /// <summary>
        /// Whether the etch is drawn with the border
        /// </summary>
        public bool DisplayEtch
        {
            get => m_DisplayEtch;
            set
            {
                if (m_DisplayEtch != value)
                {
                    m_DisplayEtch = value;
                    MarkDirtyRepaint();
                }
            }
        }

        /// <inheritdoc />
        protected override void DrawBorder(MeshGenerationContext mgc, Rect r, float wantedWidth, Color[] colors, Vector2[] corners)
        {

            if (!DisplayEtch)
            {
                MeshDrawingHelpers_Internal.Border(mgc, r, colors, wantedWidth, corners, ContextType.Editor);
                return;
            }


            var painter = mgc.painter2D;


            painter.lineWidth = wantedWidth;

            var bounds = Node.Etch_Internal.worldBound;
            var rectEtch = this.WorldToLocal(bounds);
            rectEtch.y -= wantedWidth * 0.5f;
            rectEtch.height += wantedWidth * 0.5f;


            painter.BeginPath();
            painter.strokeColor = colors[0];
            painter.MoveTo(new Vector2(r.xMin, r.yMin + corners[0].y));
            painter.ArcTo(new Vector2(r.xMin, r.yMin), new Vector2(r.xMin + corners[0].x, r.yMin), corners[0].x);
            painter.LineTo(new Vector2(r.xMax - corners[1].x, r.yMin));
            painter.ArcTo(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMin + corners[1].y), corners[1].x);
            painter.LineTo(new Vector2(r.xMax, r.yMax - corners[2].y));
            painter.ArcTo(new Vector2(r.xMax, r.yMax), new Vector2(r.xMax - corners[2].x, r.yMax), corners[2].x);
            painter.LineTo(new Vector2(rectEtch.xMax, r.yMax));
            painter.LineTo(new Vector2(rectEtch.xMax, rectEtch.yMax - 3.0f));
            painter.ArcTo(new Vector2(rectEtch.xMax, rectEtch.yMax), new Vector2(rectEtch.xMax - 3, rectEtch.yMax), 3);
            painter.LineTo(new Vector2(rectEtch.xMin + 3.0f, rectEtch.yMax));
            painter.ArcTo(new Vector2(rectEtch.xMin, rectEtch.yMax), new Vector2(rectEtch.xMin, rectEtch.yMax - 3), 3);
            painter.LineTo(new Vector2(rectEtch.xMin, r.yMax));
            painter.LineTo(new Vector2(r.xMin + corners[3].y,  r.yMax));
            painter.ArcTo(new Vector2(r.xMin , r.yMax), new Vector2(r.xMin, r.yMax - corners[3].y),corners[3].y);
            painter.LineTo(new Vector2(r.xMin, r.yMin + corners[0].y));

            painter.Stroke();
        }
    }
}
