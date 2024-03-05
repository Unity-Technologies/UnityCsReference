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

        unsafe static public MeshWriteDataInterface FromMeshWriteData(MeshWriteData data)
        {
            var mwdi = new MeshWriteDataInterface();
            mwdi.vertices = new IntPtr(data.m_Vertices.GetUnsafePtr());
            mwdi.indices = new IntPtr(data.m_Indices.GetUnsafePtr());
            mwdi.vertexCount = data.m_Vertices.Length;
            mwdi.indexCount = data.m_Indices.Length;
            return mwdi;
        }
    }
}
