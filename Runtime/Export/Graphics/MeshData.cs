// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Internal;
using UnityEngine.Rendering;

namespace UnityEngine
{
    public sealed partial class Mesh
    {
        // Represents mesh data snapshots for an array of Meshes; primarily
        // so that a bunch of meshes can be snapshotted with a single native call,
        // and more importantly, guarded by a single dispose sentinel (one GC
        // allocation, and enables parallel-for jobs to use the snapshots array).
        [NativeContainer]
        [NativeContainerSupportsMinMaxWriteRestriction]
        public unsafe partial struct MeshDataArray : IDisposable
        {
            [NativeDisableUnsafePtrRestriction] IntPtr* m_Ptrs;
            internal int m_Length;

            internal int m_MinIndex;
            internal int m_MaxIndex;
            AtomicSafetyHandle m_Safety;
            [NativeSetClassTypeToNullOnSchedule] DisposeSentinel m_DisposeSentinel;


            public int Length => m_Length;
            public MeshData this[int index]
            {
                get
                {
                    CheckElementReadAccess(index);
                    MeshData res;
                    res.m_Ptr = m_Ptrs[index];
                    res.m_Safety = m_Safety;
                    return res;
                }
            }

            public void Dispose()
            {
                DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
                if (m_Length != 0)
                {
                    ReleaseMeshDatas(m_Ptrs, m_Length);
                    UnsafeUtility.Free(m_Ptrs, Allocator.Persistent);
                }
                m_Ptrs = null;
                m_Length = 0;
            }

            internal void ApplyToMeshAndDispose(Mesh mesh, MeshUpdateFlags flags)
            {
                if (!mesh.canAccess)
                    throw new InvalidOperationException($"Not allowed to access vertex data on mesh '{mesh.name}' (isReadable is false; Read/Write must be enabled in import settings)");
                ApplyToMeshImpl(mesh, m_Ptrs[0], flags);
                Dispose();
            }

            internal void ApplyToMeshesAndDispose(Mesh[] meshes, MeshUpdateFlags flags)
            {
                for (int i = 0; i < m_Length; ++i)
                {
                    Mesh m = meshes[i];
                    if (m == null)
                        throw new ArgumentNullException(nameof(meshes), $"Mesh at index {i} is null");
                    if (!m.canAccess)
                        throw new InvalidOperationException($"Not allowed to access vertex data on mesh '{m.name}' at array index {i} (isReadable is false; Read/Write must be enabled in import settings)");
                }
                ApplyToMeshesImpl(meshes, m_Ptrs, m_Length, flags);
                Dispose();
            }

            internal MeshDataArray(Mesh mesh)
            {
                // error checking
                if (mesh == null)
                    throw new ArgumentNullException(nameof(mesh), "Mesh is null");
                if (!mesh.canAccess)
                    throw new InvalidOperationException($"Not allowed to access vertex data on mesh '{mesh.name}' (isReadable is false; Read/Write must be enabled in import settings)");

                m_Length = 1;
                var totalSize = UnsafeUtility.SizeOf<IntPtr>();
                m_Ptrs = (IntPtr*)UnsafeUtility.Malloc(totalSize, UnsafeUtility.AlignOf<IntPtr>(), Allocator.Persistent);
                AcquireReadOnlyMeshData(mesh, m_Ptrs);

                m_MinIndex = 0;
                m_MaxIndex = m_Length - 1;
                DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 1, Allocator.TempJob);
                // secondary version with write disabled makes the NativeArrays returned
                // by MeshData actually be read-only
                AtomicSafetyHandle.SetAllowSecondaryVersionWriting(m_Safety, false);
                AtomicSafetyHandle.UseSecondaryVersion(ref m_Safety);
            }

            internal MeshDataArray(Mesh[] meshes, int meshesCount)
            {
                // error checking
                if (meshes.Length < meshesCount)
                    throw new InvalidOperationException($"Meshes array size ({meshes.Length}) is smaller than meshes count ({meshesCount})");
                for (int i = 0; i < meshesCount; ++i)
                {
                    Mesh m = meshes[i];
                    if (m == null)
                        throw new ArgumentNullException(nameof(meshes), $"Mesh at index {i} is null");
                    if (!m.canAccess)
                        throw new InvalidOperationException($"Not allowed to access vertex data on mesh '{m.name}' at array index {i} (isReadable is false; Read/Write must be enabled in import settings)");
                }

                m_Length = meshesCount;
                var totalSize = UnsafeUtility.SizeOf<IntPtr>() * meshesCount;
                m_Ptrs = (IntPtr*)UnsafeUtility.Malloc(totalSize, UnsafeUtility.AlignOf<IntPtr>(), Allocator.Persistent);
                AcquireReadOnlyMeshDatas(meshes, m_Ptrs, meshesCount);

                m_MinIndex = 0;
                m_MaxIndex = m_Length - 1;
                DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 1, Allocator.TempJob);
                // secondary version with write disabled makes the NativeArrays returned
                // by MeshData actually be read-only
                AtomicSafetyHandle.SetAllowSecondaryVersionWriting(m_Safety, false);
                AtomicSafetyHandle.UseSecondaryVersion(ref m_Safety);
            }

            internal MeshDataArray(int meshesCount)
            {
                if (meshesCount < 0)
                    throw new InvalidOperationException($"Mesh count can not be negative (was {meshesCount})");
                m_Length = meshesCount;
                var totalSize = UnsafeUtility.SizeOf<IntPtr>() * meshesCount;
                m_Ptrs = (IntPtr*)UnsafeUtility.Malloc(totalSize, UnsafeUtility.AlignOf<IntPtr>(), Allocator.Persistent);
                CreateNewMeshDatas(m_Ptrs, meshesCount);

                m_MinIndex = 0;
                m_MaxIndex = m_Length - 1;
                DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 1, Allocator.TempJob);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void CheckElementReadAccess(int index)
            {
                if (index < m_MinIndex || index > m_MaxIndex)
                    FailOutOfRangeError(index);

                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            }

            [BurstDiscard]
            void FailOutOfRangeError(int index)
            {
                if (index < Length && (m_MinIndex != 0 || m_MaxIndex != Length - 1))
                    throw new IndexOutOfRangeException(
                        $"Index {index} is out of restricted IJobParallelFor range [{m_MinIndex}...{m_MaxIndex}] in {nameof(MeshDataArray)}.\n" +
                        "You can only access the element at the job index, unless [NativeDisableParallelForRestriction] is used on the variable.");
                throw new IndexOutOfRangeException($"Index {index} is out of range of '{Length}' Length.");
            }

        }

        public partial struct MeshData
        {
            [NativeDisableUnsafePtrRestriction] internal IntPtr m_Ptr;
            internal AtomicSafetyHandle m_Safety;

            public int vertexCount
            {
                get
                {
                    CheckReadAccess();
                    return GetVertexCount(m_Ptr);
                }
            }

            public int vertexBufferCount
            {
                get
                {
                    CheckReadAccess();
                    return GetVertexBufferCount(m_Ptr);
                }
            }

            public bool HasVertexAttribute(VertexAttribute attr)
            {
                CheckReadAccess();
                return HasVertexAttribute(m_Ptr, attr);
            }

            public int GetVertexAttributeDimension(VertexAttribute attr)
            {
                CheckReadAccess();
                return GetVertexAttributeDimension(m_Ptr, attr);
            }

            public VertexAttributeFormat GetVertexAttributeFormat(VertexAttribute attr)
            {
                CheckReadAccess();
                return GetVertexAttributeFormat(m_Ptr, attr);
            }

            public void GetVertices(NativeArray<Vector3> outVertices)
            {
                CopyAttributeInto(outVertices, VertexAttribute.Position, VertexAttributeFormat.Float32, 3);
            }

            public void GetNormals(NativeArray<Vector3> outNormals)
            {
                CopyAttributeInto(outNormals, VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);
            }

            public void GetTangents(NativeArray<Vector4> outTangents)
            {
                CopyAttributeInto(outTangents, VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4);
            }

            public void GetColors(NativeArray<Color> outColors)
            {
                CopyAttributeInto(outColors, VertexAttribute.Color, VertexAttributeFormat.Float32, 4);
            }

            public void GetColors(NativeArray<Color32> outColors)
            {
                CopyAttributeInto(outColors, VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4);
            }

            public void GetUVs(int channel, NativeArray<Vector2> outUVs)
            {
                if (channel < 0 || channel > 7)
                    throw new ArgumentOutOfRangeException(nameof(channel), channel, "The uv index is invalid. Must be in the range 0 to 7.");
                CopyAttributeInto(outUVs, GetUVChannel(channel), VertexAttributeFormat.Float32, 2);
            }

            public void GetUVs(int channel, NativeArray<Vector3> outUVs)
            {
                if (channel < 0 || channel > 7)
                    throw new ArgumentOutOfRangeException(nameof(channel), channel, "The uv index is invalid. Must be in the range 0 to 7.");
                CopyAttributeInto(outUVs, GetUVChannel(channel), VertexAttributeFormat.Float32, 3);
            }

            public void GetUVs(int channel, NativeArray<Vector4> outUVs)
            {
                if (channel < 0 || channel > 7)
                    throw new ArgumentOutOfRangeException(nameof(channel), channel, "The uv index is invalid. Must be in the range 0 to 7.");
                CopyAttributeInto(outUVs, GetUVChannel(channel), VertexAttributeFormat.Float32, 4);
            }

            public NativeArray<T> GetVertexData<T>([DefaultValue("0")] int stream = 0) where T : struct
            {
                CheckReadAccess();
                if (stream < 0 || stream >= vertexBufferCount)
                    throw new ArgumentOutOfRangeException($"{nameof(stream)} out of bounds, should be below {vertexBufferCount} but was {stream}");

                ulong vbSize = GetVertexDataSize(m_Ptr, stream);
                ulong stride = (ulong)UnsafeUtility.SizeOf<T>();
                if (vbSize % stride != 0)
                    throw new ArgumentException($"Type passed to {nameof(GetVertexData)} can't capture the vertex buffer. Mesh vertex buffer size is {vbSize} which is not a multiple of type size {stride}");

                var arrSize = vbSize / stride;
                unsafe
                {
                    var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)GetVertexDataPtr(m_Ptr, stream), (int)arrSize, Allocator.None);
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
                    return array;
                }
            }

            unsafe void CopyAttributeInto<T>(NativeArray<T> buffer, VertexAttribute channel, VertexAttributeFormat format, int dim) where T : struct
            {
                CheckReadAccess();
                if (!HasVertexAttribute(channel))
                    throw new InvalidOperationException($"Mesh data does not have {channel} vertex component");
                if (buffer.Length < vertexCount)
                    throw new InvalidOperationException($"Not enough space in output buffer (need {vertexCount}, has {buffer.Length})");
                CopyAttributeIntoPtr(m_Ptr, channel, format, dim, (IntPtr)buffer.GetUnsafePtr());
            }

            public void SetVertexBufferParams(int vertexCount, params VertexAttributeDescriptor[] attributes)
            {
                CheckWriteAccess();
                SetVertexBufferParamsImpl(m_Ptr, vertexCount, attributes);
            }

            public void SetIndexBufferParams(int indexCount, IndexFormat format)
            {
                CheckWriteAccess();
                SetIndexBufferParamsImpl(m_Ptr, indexCount, format);
            }

            public IndexFormat indexFormat
            {
                get
                {
                    CheckReadAccess();
                    return GetIndexFormat(m_Ptr);
                }
            }

            public void GetIndices(NativeArray<ushort> outIndices, int submesh, [DefaultValue("true")] bool applyBaseVertex = true)
            {
                CheckReadAccess();
                if (submesh < 0 || submesh >= subMeshCount)
                    throw new IndexOutOfRangeException($"Specified submesh ({submesh}) is out of range. Must be greater or equal to 0 and less than subMeshCount ({subMeshCount}).");
                int indexCount = GetIndexCount(m_Ptr, submesh);
                if (outIndices.Length < indexCount)
                    throw new InvalidOperationException($"Not enough space in output buffer (need {indexCount}, has {outIndices.Length})");
                unsafe
                {
                    CopyIndicesIntoPtr(m_Ptr, submesh, applyBaseVertex, 2, (IntPtr)outIndices.GetUnsafePtr());
                }
            }

            public void GetIndices(NativeArray<int> outIndices, int submesh, [DefaultValue("true")] bool applyBaseVertex = true)
            {
                CheckReadAccess();
                if (submesh < 0 || submesh >= subMeshCount)
                    throw new IndexOutOfRangeException($"Specified submesh ({submesh}) is out of range. Must be greater or equal to 0 and less than subMeshCount ({subMeshCount}).");
                int indexCount = GetIndexCount(m_Ptr, submesh);
                if (outIndices.Length < indexCount)
                    throw new InvalidOperationException($"Not enough space in output buffer (need {indexCount}, has {outIndices.Length})");
                unsafe
                {
                    CopyIndicesIntoPtr(m_Ptr, submesh, applyBaseVertex, 4, (IntPtr)outIndices.GetUnsafePtr());
                }
            }

            public NativeArray<T> GetIndexData<T>() where T : struct
            {
                CheckReadAccess();
                ulong ibSize = GetIndexDataSize(m_Ptr);
                ulong stride = (ulong)UnsafeUtility.SizeOf<T>();
                if (ibSize % stride != 0)
                    throw new ArgumentException($"Type passed to {nameof(GetIndexData)} can't capture the index buffer. Mesh index buffer size is {ibSize} which is not a multiple of type size {stride}");

                var arrSize = ibSize / stride;
                unsafe
                {
                    var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)GetIndexDataPtr(m_Ptr), (int)arrSize, Allocator.None);
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
                    return array;
                }
            }

            public int subMeshCount
            {
                get
                {
                    CheckReadAccess();
                    return GetSubMeshCount(m_Ptr);
                }
                set
                {
                    CheckWriteAccess();
                    SetSubMeshCount(m_Ptr, value);
                }
            }

            public SubMeshDescriptor GetSubMesh(int index)
            {
                CheckReadAccess();
                return GetSubMesh(m_Ptr, index);
            }

            public void SetSubMesh(int index, SubMeshDescriptor desc, MeshUpdateFlags flags = MeshUpdateFlags.Default)
            {
                CheckWriteAccess();
                SetSubMeshImpl(m_Ptr, index, desc, flags);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void CheckReadAccess()
            {
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void CheckWriteAccess()
            {
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            }
        }
    }
}
