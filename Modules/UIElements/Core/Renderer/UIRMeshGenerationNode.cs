// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

namespace UnityEngine.UIElements
{
    using UIR;

    /// <summary>
    /// Contains a part of the draw sequence of a VisualElement. You can use it in a job to add nested draw calls.
    /// </summary>
    [NativeContainer]
    public struct MeshGenerationNode
    {
        UnsafeMeshGenerationNode m_UnsafeNode;

        internal UnsafeMeshGenerationNode unsafeNode { get { return m_UnsafeNode; } }

        AtomicSafetyHandle m_Safety;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Create(GCHandle handle, AtomicSafetyHandle safety, out MeshGenerationNode node)
        {
            node = new MeshGenerationNode { m_Safety = safety };
            UnsafeMeshGenerationNode.Create(handle, out node.m_UnsafeNode );
        }

        /// <summary>
        /// Records a draw command with the provided triangle-list indexed mesh.
        /// </summary>
        /// <param name="vertices">The vertices to be drawn. All referenced vertices must be initialized.</param>
        /// <param name="indices">The triangle list indices. Must be a multiple of 3. All indices must be initialized.</param>
        /// <param name="texture">An optional texture to be applied on the triangles. Pass null to rely on vertex colors only.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawMesh(NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, Texture texture = null)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            m_UnsafeNode.DrawMesh(vertices, indices, texture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Entry GetParentEntry() => m_UnsafeNode.GetParentEntry();
    }

    struct UnsafeMeshGenerationNode
    {
        GCHandle m_Handle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        MeshGenerationNodeImpl GetManaged() => (MeshGenerationNodeImpl)m_Handle.Target;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Create(GCHandle handle, out UnsafeMeshGenerationNode node)
        {
            node = new UnsafeMeshGenerationNode { m_Handle = handle };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawMesh(NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, Texture texture = null)
        {
            GetManaged().DrawMesh(vertices, indices, texture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DrawMeshInternal(NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, Texture texture = null, TextureOptions textureOptions = TextureOptions.None)
        {
            GetManaged().DrawMesh(vertices, indices, texture, textureOptions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DrawGradientsInternal(NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, VectorImage gradientsOwner)
        {
            GetManaged().DrawGradients(vertices, indices, gradientsOwner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Entry GetParentEntry() => GetManaged().GetParentEntry();
    }

    class MeshGenerationNodeImpl : IDisposable
    {
        GCHandle m_SelfHandle;

        Entry m_ParentEntry;
        EntryRecorder m_EntryRecorder;

        static int s_StaticSafetyId;
        AtomicSafetyHandle m_Safety;
        bool m_Safe;

        static MeshGenerationNodeImpl()
        {
            if (s_StaticSafetyId == 0)
                s_StaticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<MeshGenerationNode>();
        }

        public MeshGenerationNodeImpl()
        {
            m_SelfHandle = GCHandle.Alloc(this);
        }

        // Must not be called from a job
        public void Init(Entry parentEntry, EntryRecorder entryRecorder, bool safe)
        {
            Debug.Assert(m_ParentEntry == null);
            Debug.Assert(parentEntry != null);
            Debug.Assert(entryRecorder != null);
            m_ParentEntry = parentEntry;
            m_EntryRecorder = entryRecorder;
            m_Safe = safe;
            if (m_Safe)
            {
                m_Safety = AtomicSafetyHandle.Create();
                AtomicSafetyHandle.SetStaticSafetyId(ref m_Safety, s_StaticSafetyId);
            }
        }

        // Must not be called from a job
        public void Reset()
        {
            Debug.Assert(m_ParentEntry != null);
            Debug.Assert(m_EntryRecorder != null);
            if (m_Safe)
            {
                AtomicSafetyHandle.CheckDeallocateAndThrow(m_Safety);
                AtomicSafetyHandle.Release(m_Safety);
            }
            m_ParentEntry = null;
            m_EntryRecorder = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetNode(out MeshGenerationNode node)
        {
            MeshGenerationNode.Create(m_SelfHandle, m_Safety, out node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetUnsafeNode(out UnsafeMeshGenerationNode node)
        {
            UnsafeMeshGenerationNode.Create(m_SelfHandle, out node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entry GetParentEntry() => m_ParentEntry;

        public void DrawMesh(NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, Texture texture = null, TextureOptions textureOptions = TextureOptions.None)
        {
            if (vertices.Length == 0 || indices.Length == 0)
                return;

            m_EntryRecorder.DrawMesh(m_ParentEntry, vertices, indices, texture, textureOptions);
        }

        public void DrawGradients(NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, VectorImage gradientsOwner)
        {
            if (vertices.Length == 0 || indices.Length == 0 || gradientsOwner == null)
                return;

            m_EntryRecorder.DrawGradients(m_ParentEntry, vertices, indices, gradientsOwner);
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
                if (m_ParentEntry != null)
                    Reset();
                m_SelfHandle.Free();
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern
    }

    class MeshGenerationNodeManager : IDisposable
    {
        List<MeshGenerationNodeImpl> m_Nodes = new(8);
        int m_UsedCounter;
        EntryRecorder m_EntryRecorder;

        public MeshGenerationNodeManager(EntryRecorder entryRecorder)
        {
            m_EntryRecorder = entryRecorder;
        }

        public void CreateNode(Entry parentEntry, out MeshGenerationNode node)
        {
            MeshGenerationNodeImpl nodeImpl = CreateImpl(parentEntry, true);
            nodeImpl.GetNode(out node);
        }

        public void CreateUnsafeNode(Entry parentEntry, out UnsafeMeshGenerationNode node)
        {
            MeshGenerationNodeImpl nodeImpl = CreateImpl(parentEntry, false);
            nodeImpl.GetUnsafeNode(out node);
        }

        MeshGenerationNodeImpl CreateImpl(Entry parentEntry, bool safe)
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return null;
            }

            if (m_Nodes.Count == m_UsedCounter)
            {
                for(int i = 0 ; i < 200 ; ++i)
                    m_Nodes.Add(new MeshGenerationNodeImpl());
            }

            var nodeImpl = m_Nodes[m_UsedCounter++];
            nodeImpl.Init(parentEntry, m_EntryRecorder, safe);

            return nodeImpl;
        }

        public void ResetAll()
        {
            for (int i = 0; i < m_UsedCounter; ++i)
                m_Nodes[i].Reset();
            m_UsedCounter = 0;
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
                for (int i = 0, count = m_Nodes.Count; i < count; ++i)
                    m_Nodes[i].Dispose();
                m_Nodes.Clear();
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern

    }
}
