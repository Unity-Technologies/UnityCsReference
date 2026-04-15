// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace UnityEngine.U2D.Runtime
{

    internal class Geometry
    {


        internal struct GenerateParams
        {
            public IntPtr   points;
            public int      pointCount;
            public IntPtr   edges;
            public int      edgeCount;
            public IntPtr   vertices;
            public int      maxVertexCount;
            public IntPtr   indices;
            public int      maxIndexCount;
            public float    areaFactor;
            public int      refineIterations;
            public int      smoothenIterations;
        }

        [RequiredByNativeCode]
        internal static int Generate(ref GenerateParams _params)
        {
            
            int outIndexCount = 0, outVertexCount = 0, outEdgeCount = 0;
            unsafe
            {

                // For Main-Thread only and also this only allocates minimal memory.
                NativeArray<int2> _edges = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int2>(_params.edges.ToPointer(), _params.edgeCount, Allocator.None);
                NativeArray<float2> _points = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<float2>(_params.points.ToPointer(), _params.pointCount, Allocator.None);
                NativeArray<int2> _edgesout = new NativeArray<int2>(_params.maxIndexCount, Allocator.Temp);
                NativeArray<int> _indices = new NativeArray<int>(_params.maxIndexCount, Allocator.Temp);
                NativeArray<float2> _vertices = new NativeArray<float2>(_params.maxVertexCount, Allocator.Temp);

                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref _edges, AtomicSafetyHandle.GetTempMemoryHandle());
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref _points, AtomicSafetyHandle.GetTempMemoryHandle());
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref _indices, AtomicSafetyHandle.GetTempMemoryHandle());
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref _edgesout, AtomicSafetyHandle.GetTempMemoryHandle());
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref _vertices, AtomicSafetyHandle.GetTempMemoryHandle());
                int maxTessArrayCount = 64; // Keep it to considerable Max for processing.
                int tessPointCount = _points.Length;
                NativeArray<float2> ov = new NativeArray<float2>(tessPointCount * maxTessArrayCount, Allocator.Temp);
                NativeArray<int> oi = new NativeArray<int>(tessPointCount * maxTessArrayCount, Allocator.Temp);
                NativeArray<int2> oe = new NativeArray<int2>(tessPointCount * maxTessArrayCount, Allocator.Temp);
                UnityEngine.U2D.UTess.ModuleHandle.Tessellate(Allocator.Temp, in _points, in _edges, ref ov, out var ovc, ref oi, out var oic, ref oe, out var oec, true);

                if (0 != _params.areaFactor)
                {
                    NativeArray<float2> iv = new NativeArray<float2>(ovc, Allocator.Temp);
                    NativeArray<int2> ie = new NativeArray<int2>(oec, Allocator.Temp);
                    NativeArray<float2>.Copy(ov, iv, ovc);
                    NativeArray<int2>.Copy(oe, ie, oec);

                    while (0 == outIndexCount && _params.areaFactor < 1.0f)
                    {
                        UnityEngine.U2D.UTess.ModuleHandle.Subdivide(Allocator.Temp, iv, ie, ref _vertices, ref outVertexCount, ref _indices, ref outIndexCount, ref _edgesout, ref outEdgeCount, _params.areaFactor, 0, _params.refineIterations, _params.smoothenIterations);
                        _params.areaFactor = _params.areaFactor + 0.1f;
                    }
                    if (0 != outIndexCount)
                    {
                        UnsafeUtility.MemCpy(_params.indices.ToPointer(), _indices.GetUnsafePtr(), outIndexCount * sizeof(int));
                        UnsafeUtility.MemCpy(_params.vertices.ToPointer(), _vertices.GetUnsafePtr(), outVertexCount * sizeof(float2));
                    }
                    ie.Dispose();
                    iv.Dispose();
                }
                else
                {
                    if (0 != oic)
                    {
                        UnsafeUtility.MemCpy(_params.indices.ToPointer(), oi.GetUnsafePtr(), oic * sizeof(int));
                        UnsafeUtility.MemCpy(_params.vertices.ToPointer(), ov.GetUnsafePtr(), ovc * sizeof(float2));
                    }
                    outIndexCount = oic;
                }

                oe.Dispose();
                oi.Dispose();
                ov.Dispose();
                _edges.Dispose();
                _points.Dispose();
                _edgesout.Dispose();
                _indices.Dispose();
                _vertices.Dispose();

            }
            return outIndexCount;
        }

    }

}
