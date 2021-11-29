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
    [NativeHeader("Modules/UIElementsNative/UITessellation.bindings.h")]
    [VisibleToOtherModules("Unity.UIElements")]
    internal static class UITessellation
    {
        [FreeFunction(Name = "UITessellation::BeginTess")]
        public static unsafe extern IntPtr BeginTess();

        [FreeFunction(Name = "UITessellation::AddContour")]
        public static unsafe extern void AddContour(IntPtr handle, void* contour, int vertexCount, int stride);

        [FreeFunction(Name = "UITessellation::Tessellate")]
        public static unsafe extern bool Tessellate(IntPtr handle, bool oddEven);

        [FreeFunction(Name = "UITessellation::GetVertexCount")]
        public static unsafe extern int GetVertexCount(IntPtr handle);

        [FreeFunction(Name = "UITessellation::GetVertices")]
        public static unsafe extern float* GetVertices(IntPtr handle);

        [FreeFunction(Name = "UITessellation::GetElementCount")]
        public static unsafe extern int GetElementCount(IntPtr handle);

        [FreeFunction(Name = "UITessellation::GetElements")]
        public static unsafe extern int* GetElements(IntPtr handle);

        [FreeFunction(Name = "UITessellation::GetVertexIndices")]
        public static unsafe extern int* GetVertexIndices(IntPtr handle);

        [FreeFunction(Name = "UITessellation::EndTess")]
        public static unsafe extern void EndTess(IntPtr handle);
    }
}
