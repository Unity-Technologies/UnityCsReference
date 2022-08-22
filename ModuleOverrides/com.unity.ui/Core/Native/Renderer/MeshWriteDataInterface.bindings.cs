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
    [StructLayout(LayoutKind.Sequential)]
    internal struct MeshWriteDataInterface
    {
        public IntPtr vertices;
        public IntPtr indices;
        public int vertexCount;
        public int indexCount;
    }
}
