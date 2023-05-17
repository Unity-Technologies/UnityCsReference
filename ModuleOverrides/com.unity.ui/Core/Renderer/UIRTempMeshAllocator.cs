// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

namespace UnityEngine.UIElements
{
    using UIR;

    /// <summary>
    /// Used in jobs to allocate UI Toolkit temporary meshes.
    /// </summary>
    [NativeContainer]
    [NativeContainerIsReadOnly]
    public struct TempMeshAllocator
    {
        GCHandle m_Handle;
        AtomicSafetyHandle m_Safety;

        // This should only be called from TempMeshAllocatorImpl
        internal static void Create(GCHandle handle, AtomicSafetyHandle safety, out TempMeshAllocator allocator)
        {
            allocator = new TempMeshAllocator { m_Handle = handle, m_Safety = safety };
        }

        /// <summary>
        /// Allocates the specified number of vertices and indices from a temporary allocator.
        /// </summary>
        /// <remarks>
        /// You can only call this method during the mesh generation phase of the panel and shouldn't use it beyond.
        /// </remarks>
        /// <param name="vertexCount">The number of vertices to allocate, with a maximum limit of 65535 (or UInt16.MaxValue).</param>
        /// <param name="indexCount">The number of triangle list indices to allocate, where every three indices represent one triangle. Therefore, this value should always be a multiple of three.</param>
        /// <param name="vertices">The returned vertices.</param>
        /// <param name="indices">The returned indices.</param>
        public void AllocateTempMesh(int vertexCount, int indexCount, out NativeSlice<Vertex> vertices, out NativeSlice<ushort> indices)
        {
            // TempMeshAllocatorImpl is thread-safe: reading from multiple jobs is OK. However, the safety must not
            // have been released, which would typically mean that the allocator has been reset or disposed.
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

            var impl = m_Handle.Target as TempMeshAllocatorImpl;
            Debug.Assert(impl != null);
            impl.AllocateTempMesh(vertexCount, indexCount, out vertices, out indices);
        }
    }
}

namespace UnityEngine.UIElements.UIR
{
    // This class has partial thread safety: it supports the main thread and the job system.
    unsafe class TempMeshAllocatorImpl : IDisposable
    {
        struct ThreadData
        {
            public List<IntPtr> allocations;
            public List<AtomicSafetyHandle> safetyHandles;
        }

        GCHandle m_GCHandle;
        ThreadData[] m_ThreadData;

        static int s_StaticSafetyId;
        AtomicSafetyHandle m_SafetyHandle;

        TempAllocator<Vertex> m_VertexPool = new(8192, 2048, 64 * 1024);
        TempAllocator<UInt16> m_IndexPool = new(8192 << 1, 2048 << 1, (64 * 1024) << 1);

        public TempMeshAllocatorImpl()
        {
            m_GCHandle = GCHandle.Alloc(this);
            m_ThreadData = new ThreadData[JobsUtility.ThreadIndexCount];
            for (int i = 0; i < JobsUtility.ThreadIndexCount; ++i)
            {
                m_ThreadData[i].allocations = new List<IntPtr>();
                m_ThreadData[i].safetyHandles = new List<AtomicSafetyHandle>();
            }

            if (s_StaticSafetyId == 0)
                s_StaticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<TempMeshAllocator>();
            CreateSafetyHandle();
        }

        public void CreateNativeHandle(out TempMeshAllocator allocator)
        {
            TempMeshAllocator.Create(m_GCHandle, m_SafetyHandle, out allocator);
        }

        NativeSlice<T> Allocate<T>(int count, int alignment) where T : struct
        {
            ref ThreadData threadData = ref m_ThreadData[UIRUtility.GetThreadIndex()];

            Debug.Assert(count > 0);

            long size = UnsafeUtility.SizeOf<T>() * count;
            void* address = UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<T>(), Allocator.TempJob);
            threadData.allocations.Add((IntPtr)address);
            NativeArray<T> array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(address, count, Allocator.Invalid);

            var safetyHandle = AtomicSafetyHandle.Create();
            threadData.safetyHandles.Add(safetyHandle);
            AtomicSafetyHandle.SetStaticSafetyId(ref safetyHandle, s_StaticSafetyId);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, safetyHandle);

            return array;
        }

        void CreateSafetyHandle()
        {
            m_SafetyHandle = AtomicSafetyHandle.Create();
            AtomicSafetyHandle.SetStaticSafetyId(ref m_SafetyHandle, s_StaticSafetyId);
        }

        public void AllocateTempMesh(int vertexCount, int indexCount, out NativeSlice<Vertex> vertices, out NativeSlice<ushort> indices)
        {
            if (vertexCount > UIRenderDevice.maxVerticesPerPage)
                throw new ArgumentOutOfRangeException(nameof(vertexCount), $"Attempting to allocate {vertexCount} vertices which exceeds the limit of {UIRenderDevice.maxVerticesPerPage}.");

            if (!JobsUtility.IsExecutingJob)
            {
                if (disposed)
                {
                    DisposeHelper.NotifyDisposedUsed(this);
                    vertices = new NativeSlice<Vertex>();
                    indices = new NativeSlice<ushort>();
                    return;
                }

                // On the main thread, our own allocator is faster.
                vertices = vertexCount > 0 ? m_VertexPool.Alloc(vertexCount) : new NativeSlice<Vertex>();
                indices = indexCount > 0 ? m_IndexPool.Alloc(indexCount) : new NativeSlice<ushort>();
                return;
            }

            // We cannot perform job safety check here because the safety handle flags have not been set on the struct
            // stored in this instance. The check is done in TempMeshAllocator.AllocateTempMesh instead. Also, we don't
            // need to perform the test on the main thread because Clear cannot be called from jobs, which implies that
            // we are not clearing and allocating at the same time (i.e. if main thread is allocating, it's not clearing
            // at the same time).

            vertices = vertexCount > 0 ? Allocate<Vertex>(vertexCount, 4) : new NativeSlice<Vertex>();
            indices = indexCount > 0 ? Allocate<ushort>(indexCount, 2) : new NativeSlice<ushort>();
        }

        public void Clear()
        {
            AtomicSafetyHandle.CheckDeallocateAndThrow(m_SafetyHandle);
            AtomicSafetyHandle.Release(m_SafetyHandle);
            CreateSafetyHandle();

            for (int i = 0; i < m_ThreadData.Length; ++i)
            {
                List<AtomicSafetyHandle> safetyHandles = m_ThreadData[i].safetyHandles;
                for (int j = 0; j < safetyHandles.Count; ++j)
                {
                    var safetyHandle = safetyHandles[j];
                    AtomicSafetyHandle.CheckDeallocateAndThrow(safetyHandle);
                    AtomicSafetyHandle.Release(safetyHandle);
                }

                safetyHandles.Clear();

                foreach (IntPtr ptr in m_ThreadData[i].allocations)
                    UnsafeUtility.Free(ptr.ToPointer(), Allocator.TempJob);

                m_ThreadData[i].allocations.Clear();
            }

            m_VertexPool.Reset();
            m_IndexPool.Reset();
        }

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                Clear();

                // Because Clear() re-creates the safety handle
                AtomicSafetyHandle.CheckDeallocateAndThrow(m_SafetyHandle);
                AtomicSafetyHandle.Release(m_SafetyHandle);
                m_GCHandle.Free();

                m_VertexPool.Dispose();
                m_IndexPool.Dispose();
            }
            else
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern
    }
}
