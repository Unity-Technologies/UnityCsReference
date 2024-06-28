// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [NativeHeader("Modules/UIElements/Core/Native/Renderer/UIRMeshBuilder.bindings.h")]
    internal static class MeshBuilderNative
    {
        public const float kEpsilon = 0.001f;

        public struct NativeColorPage
        {
            public int isValid;
            public Color32 pageAndID;
        }

        public struct NativeBorderParams
        {
            public Rect rect;

            public Color leftColor;
            public Color topColor;
            public Color rightColor;
            public Color bottomColor;

            public float leftWidth;
            public float topWidth;
            public float rightWidth;
            public float bottomWidth;

            public Vector2 topLeftRadius;
            public Vector2 topRightRadius;
            public Vector2 bottomRightRadius;
            public Vector2 bottomLeftRadius;

            internal NativeColorPage leftColorPage;
            internal NativeColorPage topColorPage;
            internal NativeColorPage rightColorPage;
            internal NativeColorPage bottomColorPage;
        }

        public struct NativeRectParams
        {
            public Rect rect;
            public Rect subRect;
            public Rect uv;
            public Color color;
            public ScaleMode scaleMode;

            public Vector2 topLeftRadius;
            public Vector2 topRightRadius;
            public Vector2 bottomRightRadius;
            public Vector2 bottomLeftRadius;

            public Rect backgroundRepeatRect;

            public IntPtr texture;
            public IntPtr sprite;
            public IntPtr vectorImage;

            // Extracted sprite properties for the job system
            public IntPtr spriteTexture;
            public IntPtr spriteVertices;
            public IntPtr spriteUVs;
            public IntPtr spriteTriangles;

            public Rect spriteGeomRect;
            public Vector2 contentSize;
            public Vector2 textureSize;
            public float texturePixelsPerPoint;

            public int leftSlice;
            public int topSlice;
            public int rightSlice;
            public int bottomSlice;
            public float sliceScale;

            public Vector4 rectInset;

            public NativeColorPage colorPage;

            public int meshFlags;
        }

        [ThreadSafe] public static extern MeshWriteDataInterface MakeBorder(NativeBorderParams borderParams, float posZ);
        [ThreadSafe] public static extern MeshWriteDataInterface MakeSolidRect(NativeRectParams rectParams, float posZ);
        [ThreadSafe] public static extern MeshWriteDataInterface MakeTexturedRect(NativeRectParams rectParams, float posZ);
        [ThreadSafe] public static extern MeshWriteDataInterface MakeVectorGraphicsStretchBackground(Vertex[] svgVertices, UInt16[] svgIndices, float svgWidth, float svgHeight, Rect targetRect, Rect sourceUV, ScaleMode scaleMode, Color tint, NativeColorPage colorPage);
        [ThreadSafe] public static extern MeshWriteDataInterface MakeVectorGraphics9SliceBackground(Vertex[] svgVertices, UInt16[] svgIndices, float svgWidth, float svgHeight, Rect targetRect, Vector4 sliceLTRB, Color tint, NativeColorPage colorPage);
    }
}
