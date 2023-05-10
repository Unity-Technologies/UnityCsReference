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
    class DynamicBlockBorder_Internal : DynamicBorder
    {
        static Vector2[] s_EtchCorners = new Vector2[4];

        /// <summary>
        /// The <see cref="BlockNode"/> this border is for.
        /// </summary>
        BlockNode Node { get; }

        /// <summary>
        /// Initialize a new instance of the <see cref="DynamicBlockBorder_Internal"/> class.
        /// </summary>
        /// <param name="view"></param>
        public DynamicBlockBorder_Internal(BlockNode view)
            : base(view)
        {
            Node = view;

            s_EtchCorners[0] = Vector2.zero;
            s_EtchCorners[1] = Vector2.zero;
        }

        bool m_DisplayEtch = true;

        BlockDrawParams_Internal m_DrawParams;

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


        bool m_IsLast;
        bool m_IsFirst;

        /// <summary>
        /// Whether it is the border of the last block.
        /// </summary>
        public bool IsLast
        {
            get => m_IsLast;
            set
            {
                if (m_IsLast != value)
                {
                    m_IsLast = value;
                    MarkDirtyRepaint();
                }
            }
        }

        /// <summary>
        /// Whether it is the border of the first block.
        /// </summary>
        public bool IsFirst
        {
            get => m_IsFirst;
            set
            {
                if (m_IsFirst != value)
                {
                    m_IsFirst = value;
                    MarkDirtyRepaint();
                }
            }
        }

        public void SetDrawParams( ref BlockDrawParams_Internal drawParams)
        {
            m_DrawParams = drawParams;
            MarkDirtyRepaint();
        }


        public void DrawBorder(Painter2D p2d, Rect r, float wantedWidth, Color color)
        {
            r.height -= Node.resolvedStyle.paddingBottom;

            var offset = -wantedWidth * 0.5f;

            BlockNode.Inset_Internal(ref r, - Vector2.one * offset );

            var actualParams = m_DrawParams;

            if (Selected)
            {
                actualParams.topEtchMargin += offset;
                actualParams.bottomEtchMargin -= 1 - offset ;
                actualParams.bottomEtchWidth += 2;
            }
            else
            {
                actualParams.topEtchMargin += offset * 2;
                actualParams.topEtchWidth -= offset * 2;

                actualParams.bottomEtchMargin -= 1 ;
                actualParams.bottomEtchWidth += offset * 2 + 2;
            }
            actualParams.etchOuterRadius -= offset;
            actualParams.extremeBlockRadius += offset;
            actualParams.etchInnerRadius += offset;

            if (actualParams.etchOuterRadius < wantedWidth)
                actualParams.etchOuterRadius = wantedWidth;

            if (actualParams.etchInnerRadius < wantedWidth)
                actualParams.etchInnerRadius = wantedWidth;

            if (actualParams.extremeBlockRadius < wantedWidth)
                actualParams.extremeBlockRadius = wantedWidth;

            BlockNode.DrawBlock_Internal(ref r, p2d, m_DisplayEtch, IsLast, IsFirst, ref actualParams);
            p2d.strokeColor = color;
            p2d.lineWidth = wantedWidth;

            p2d.Stroke();
        }

        /// <inheritdoc />
        protected override void DrawBorder(MeshGenerationContext mgc, Rect r, float wantedWidth, Color[] colors, Vector2[] corners)
        {
            if (GetFirstAncestorOfType<ContextNode>() != null)
                return;

            var p2d = mgc.painter2D;

            DrawBorder(p2d, r, wantedWidth, colors[0]);
        }
    }
}
