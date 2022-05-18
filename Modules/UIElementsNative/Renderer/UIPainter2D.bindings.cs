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
    [NativeHeader("Modules/UIElementsNative/Renderer/UIPainter2D.bindings.h")]
    internal static class UIPainter2D
    {
        public static extern IntPtr Create(float maxArcRadius);
        public static extern void Destroy(IntPtr handle);

        public static extern void Reset(IntPtr handle);

        public static extern float GetLineWidth(IntPtr handle);
        public static extern void SetLineWidth(IntPtr handle, float value);

        public static extern Color GetStrokeColor(IntPtr handle);
        public static extern void SetStrokeColor(IntPtr handle, Color value);

        public static extern Color GetFillColor(IntPtr handle);
        public static extern void SetFillColor(IntPtr handle, Color value);

        public static extern int GetLineJoin(IntPtr handle);
        public static extern void SetLineJoin(IntPtr handle, int value);

        public static extern int GetLineCap(IntPtr handle);
        public static extern void SetLineCap(IntPtr handle, int value);

        public static extern float GetMiterLimit(IntPtr handle);
        public static extern void SetMiterLimit(IntPtr handle, float value);

        public static extern void BeginPath(IntPtr handle);
        public static extern void MoveTo(IntPtr handle, Vector2 pos);
        public static extern void LineTo(IntPtr handle, Vector2 pos);
        public static extern void ArcTo(IntPtr handle, Vector2 p1, Vector2 p2, float radius);
        public static extern void Arc(IntPtr handle, Vector2 center, float radius, float startAngleRads, float endAngleRads, int direction);
        public static extern void BezierCurveTo(IntPtr handle, Vector2 p1, Vector2 p2, Vector2 p3);
        public static extern void QuadraticCurveTo(IntPtr handle, Vector2 p1, Vector2 p2);
        public static extern void ClosePath(IntPtr handle);

        public static extern MeshWriteDataInterface Stroke(IntPtr handle);
        public static extern MeshWriteDataInterface Fill(IntPtr handle, int fillRule);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MeshWriteDataInterface
    {
        public IntPtr vertices;
        public IntPtr indices;
        public int vertexCount;
        public int indexCount;
    }
}
