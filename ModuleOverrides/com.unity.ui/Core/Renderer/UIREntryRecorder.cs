// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace UnityEngine.UIElements.UIR
{
    enum EntryType
    {
        DrawSolidMesh,
        DrawTexturedMesh,
        DrawTexturedMeshSkipAtlas,
        DrawSdfTextMesh,
        DrawGradients,
        DrawImmediate,
        DrawImmediateCull,
        DrawChildren,
        BeginStencilMask,
        EndStencilMask,
        PopStencilMask,
        PushClippingRect,
        PopClippingRect,
        PushScissors,
        PopScissors,
        PushGroupMatrix,
        PopGroupMatrix,
        PushRenderTexture,
        BlitAndPopRenderTexture,
        PushDefaultMaterial,
        PopDefaultMaterial,
        DedicatedPlaceholder
    }

    class Entry
    {
        public EntryType type;

        // In an entry, the winding order is ALWAYS clockwise (front-facing)
        public NativeSlice<Vertex> vertices;
        public NativeSlice<ushort> indices;

        public Texture texture;
        public float textScale;
        public VectorImage gradientsOwner;
        public Material material;
        public Action immediateCallback;

        public Entry nextSibling;
        public Entry firstChild;
    }

    // This class converts the most basic operations into entries. It performs no transformation of any kind,
    // no tessellation. Any higher-level operation that uses these operations must NOT be added to this class. This
    // must be the ONLY place where we create entries.
    class EntryRecorder
    {
        Entry m_Parent;
        Entry m_Previous;

        // True when the previous entry is a dedicated placeholder or used like a placeholder
        bool m_PreviousAsPlaceholder;
        EntryPool m_EntryPool;

        public EntryRecorder(EntryPool entryPool)
        {
            Debug.Assert(entryPool != null);
            m_EntryPool = entryPool;
        }

        public void Begin(Entry parent)
        {
            Debug.Assert(m_Parent == null);
            Debug.Assert(m_Previous == null);
            Debug.Assert(parent.firstChild == null);
            m_Parent = parent;
        }

        public void End()
        {
            Debug.Assert(m_Parent != null);
            m_Parent = null;
            m_Previous = null;
            m_PreviousAsPlaceholder = false;
        }

        public void DrawMesh(NativeSlice<Vertex> vertices, NativeSlice<ushort> indices)
        {
            DrawMesh(vertices, indices, null, false);
        }

        public void DrawMesh(NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, Texture texture, bool skipAtlas)
        {
            var entry = m_EntryPool.Get();
            entry.vertices = vertices;
            entry.indices = indices;
            entry.texture = texture;

            if (texture == null)
                entry.type = EntryType.DrawSolidMesh;
            else
                entry.type = skipAtlas ? EntryType.DrawTexturedMeshSkipAtlas : EntryType.DrawTexturedMesh;

            AppendMeshEntry(entry);
        }

        public void DrawSdfText(NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, Texture texture, float scale)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.DrawSdfTextMesh;
            entry.vertices = vertices;
            entry.indices = indices;
            entry.texture = texture;
            entry.textScale = scale;
            AppendMeshEntry(entry);
        }

        // Note: A vector image that doesn't use gradients must NOT be submitted with this call.
        public void DrawGradients(NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, VectorImage gradientsOwner)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.DrawGradients;
            entry.vertices = vertices;
            entry.indices = indices;
            entry.gradientsOwner = gradientsOwner;
            AppendMeshEntry(entry);
        }

        public void DrawImmediate(Action callback, bool cullingEnabled)
        {
            var entry = m_EntryPool.Get();
            entry.type = cullingEnabled ? EntryType.DrawImmediateCull : EntryType.DrawImmediate;
            entry.immediateCallback = callback;
            Append(entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawChildren()
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.DrawChildren;
            Append(entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginStencilMask()
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.BeginStencilMask;
            Append(entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndStencilMask()
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.EndStencilMask;
            Append(entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PopStencilMask()
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PopStencilMask;
            Append(entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushClippingRect()
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PushClippingRect;
            Append(entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PopClippingRect()
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PopClippingRect;
            Append(entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushScissors()
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PushScissors;
            Append(entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PopScissors()
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PopScissors;
            Append(entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushGroupMatrix()
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PushGroupMatrix;
            Append(entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PopGroupMatrix()
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PopGroupMatrix;
            Append(entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushRenderTexture()
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PushRenderTexture;
            Append(entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BlitAndPopRenderTexture()
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.BlitAndPopRenderTexture;
            Append(entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushDefaultMaterial(Material material)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PushDefaultMaterial;
            entry.material = material;
            Append(entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PopDefaultMaterial()
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PopDefaultMaterial;
            Append(entry);
        }

        // Returns an entry to which children can be added
        // The entry itself might be a dedicated placeholder or the previous entry if it can be used as a parent
        public Entry InsertPlaceholder()
        {
            Entry entry;

            if (m_Previous == null || m_PreviousAsPlaceholder)
            {
                entry = m_EntryPool.Get();
                entry.type = EntryType.DedicatedPlaceholder;
                Append(entry);
            }
            else
                entry = m_Previous;

            m_PreviousAsPlaceholder = true;
            return entry;
        }

        void AppendMeshEntry(Entry entry)
        {
            int vertexCount = entry.vertices.Length;
            int indexCount = entry.indices.Length;

            if (vertexCount == 0)
            {
                Debug.LogError("Attempting to add an entry without vertices.");
                return;
            }

            if (vertexCount > UIRenderDevice.maxVerticesPerPage)
            {
                Debug.LogError($"Attempting to add an entry with {vertexCount} vertices. The maximum number of vertices per entry is {UIRenderDevice.maxVerticesPerPage}.");
                return;
            }

            if (indexCount == 0)
            {
                Debug.LogError("Attempting to add an entry without indices.");
                return;
            }

            Append(entry);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        void Append(Entry entry)
        {
            if (m_Previous != null)
                m_Previous.nextSibling = entry;
            else
                m_Parent.firstChild = entry;
            m_Previous = entry;
            m_PreviousAsPlaceholder = entry.type == EntryType.DedicatedPlaceholder;
        }
    }
}
