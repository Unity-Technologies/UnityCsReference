// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.InternalBridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    static class MeshDrawingHelpers
    {
        public static void SolidRectangle(MeshGenerationContext mgc, Rect rectParams, Color color, ContextType context)
        {
            mgc.SolidRectangle(rectParams, color);
        }

        public static void Border(MeshGenerationContext mgc, Rect rectParams, Color color, float width, ContextType context)
        {
            mgc.Border(rectParams, color, width, Vector2.zero, context);
        }

        public static void Border(MeshGenerationContext mgc, Rect rectParams, Color[] colors, float borderWidth, Vector2[] radii, ContextType context)
        {
            mgc.Border(rectParams, colors, borderWidth, radii, context);
        }
    }
}
