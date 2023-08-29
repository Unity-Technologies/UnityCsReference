// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.UIR;

namespace Unity.GraphToolsFoundation.Editor
{
    static class MeshDrawingHelpers_Internal
    {
        public static void SolidRectangle(MeshGenerationContext mgc, Rect rectParams, Color color)
        {
            var playModeTintColor = mgc.visualElement?.playModeTintColor ?? Color.white;
            mgc.meshGenerator.DrawRectangle(MeshGenerator.RectangleParams.MakeSolid(rectParams, color, playModeTintColor));
        }

        public static void Border(MeshGenerationContext mgc, Rect rectParams, Color color, float width)
        {
            Border(mgc, rectParams, color, width, Vector2.zero);
        }

        public static void Border(MeshGenerationContext mgc, Rect rectParams, Color[] colors, float borderWidth, Vector2[] radii)
        {
            var playModeTintColor = mgc.visualElement?.playModeTintColor ?? Color.white;
            var borderParams = new MeshGenerator.BorderParams
            {
                rect = rectParams,
                playmodeTintColor = playModeTintColor,
                bottomColor = colors[0],
                topColor = colors[1],
                leftColor = colors[2],
                rightColor = colors[3],
                leftWidth = borderWidth,
                rightWidth = borderWidth,
                topWidth = borderWidth,
                bottomWidth = borderWidth,
                topLeftRadius = radii[0],
                topRightRadius = radii[1],
                bottomRightRadius = radii[2],
                bottomLeftRadius = radii[3]
            };
            mgc.meshGenerator.DrawBorder(borderParams);
        }

        static void Border(MeshGenerationContext mgc, Rect rectParams, Color color, float borderWidth, Vector2 radius)
        {
            var playModeTintColor = mgc.visualElement?.playModeTintColor ?? Color.white;
            var borderParams = new MeshGenerator.BorderParams
            {
                rect = rectParams,
                playmodeTintColor = playModeTintColor,
                bottomColor = color,
                topColor = color,
                leftColor = color,
                rightColor = color,
                leftWidth = borderWidth,
                rightWidth = borderWidth,
                topWidth = borderWidth,
                bottomWidth = borderWidth,
                topLeftRadius = radius,
                topRightRadius = radius,
                bottomRightRadius = radius,
                bottomLeftRadius = radius
            };
            mgc.meshGenerator.DrawBorder(borderParams);
        }
    }
}
