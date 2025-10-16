// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.InternalBridge
{
    static class MeshGenerationContextBridge
    {
        public static void SolidRectangle(this MeshGenerationContext meshGenerationContext, Rect rectParams, Color color)
        {
            var playModeTintColor = meshGenerationContext.visualElement?.playModeTintColor ?? Color.white;
            meshGenerationContext.meshGenerator.DrawRectangle(UnityEngine.UIElements.UIR.MeshGenerator.RectangleParams.MakeSolid(rectParams, color, playModeTintColor));
        }

        public static void Border(this MeshGenerationContext mgc, Rect rectParams, Color color, float borderWidth, Vector2 radius, ContextType context)
        {
            var borderParams = new UnityEngine.UIElements.UIR.MeshGenerator.BorderParams
            {
                rect = rectParams,
                playmodeTintColor = context == ContextType.Editor
                    ? UIElementsUtility.editorPlayModeTintColor
                    : Color.white,
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

        public static void Border(this MeshGenerationContext mgc, Rect rectParams, Color[] colors, float borderWidth, Vector2[] radii, ContextType context)
        {
            var borderParams = new UnityEngine.UIElements.UIR.MeshGenerator.BorderParams
            {
                rect = rectParams,
                playmodeTintColor = context == ContextType.Editor
                    ? UIElementsUtility.editorPlayModeTintColor
                    : Color.white,
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
    }
}
