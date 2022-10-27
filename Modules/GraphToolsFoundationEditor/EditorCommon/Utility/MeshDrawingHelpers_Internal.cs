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
        public static void SolidRectangle(MeshGenerationContext mgc, Rect rectParams, Color color, ContextType context)
        {
            mgc.Rectangle(MeshGenerationContextUtils.RectangleParams.MakeSolid(rectParams, color, context));
        }

        public static void Border(MeshGenerationContext mgc, Rect rectParams, Color color, float width, ContextType context)
        {
            Border(mgc, rectParams, color, width, Vector2.zero, context);
        }

        public static void Border(MeshGenerationContext mgc, Rect rectParams, Color[] colors, float borderWidth, Vector2[] radii, ContextType context)
        {
            var borderParams = new MeshGenerationContextUtils.BorderParams
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
            mgc.Border(borderParams);
        }

        static void Border(MeshGenerationContext mgc, Rect rectParams, Color color,float borderWidth,Vector2 radius, ContextType context)
        {
            var borderParams = new MeshGenerationContextUtils.BorderParams
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
            mgc.Border(borderParams);
        }
    }
}
