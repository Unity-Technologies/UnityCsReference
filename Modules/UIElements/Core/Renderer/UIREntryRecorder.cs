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
        CutRenderChain,
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
        public float fontSharpness;
        public VectorImage gradientsOwner;
        public Material material;
        public Action immediateCallback;

        public Entry nextSibling;

        public Entry firstChild;
        public Entry lastChild;
    }

    // This class converts the most basic operations into entries. It performs no transformation of any kind,
    // no tessellation. Any higher-level operation that uses these operations must NOT be added to this class. This
    // must be the ONLY place where we create entries.
    class EntryRecorder
    {
        EntryPool m_EntryPool;

        public EntryRecorder(EntryPool entryPool)
        {
            Debug.Assert(entryPool != null);
            m_EntryPool = entryPool;
        }

        public void DrawMesh(Entry parentEntry, NativeSlice<Vertex> vertices, NativeSlice<ushort> indices)
        {
            DrawMesh(parentEntry, vertices, indices, null, false);
        }

        public void DrawMesh(Entry parentEntry, NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, Texture texture, bool skipAtlas)
        {
            var entry = m_EntryPool.Get();
            entry.vertices = vertices;
            entry.indices = indices;
            entry.texture = texture;

            if (object.ReferenceEquals(null, texture))
                entry.type = EntryType.DrawSolidMesh;
            else
                entry.type = skipAtlas ? EntryType.DrawTexturedMeshSkipAtlas : EntryType.DrawTexturedMesh;

            AppendMeshEntry(parentEntry, entry);
        }

        public void DrawSdfText(Entry parentEntry, NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, Texture texture, float scale, float sharpness)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.DrawSdfTextMesh;
            entry.vertices = vertices;
            entry.indices = indices;
            entry.texture = texture;
            entry.textScale = scale;
            entry.fontSharpness = sharpness;
            AppendMeshEntry(parentEntry, entry);
        }

        // Note: A vector image that doesn't use gradients must NOT be submitted with this call.
        public void DrawGradients(Entry parentEntry, NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, VectorImage gradientsOwner)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.DrawGradients;
            entry.vertices = vertices;
            entry.indices = indices;
            entry.gradientsOwner = gradientsOwner;
            AppendMeshEntry(parentEntry, entry);
        }

        public void DrawImmediate(Entry parentEntry, Action callback, bool cullingEnabled)
        {
            var entry = m_EntryPool.Get();
            entry.type = cullingEnabled ? EntryType.DrawImmediateCull : EntryType.DrawImmediate;
            entry.immediateCallback = callback;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawChildren(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.DrawChildren;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginStencilMask(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.BeginStencilMask;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndStencilMask(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.EndStencilMask;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PopStencilMask(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PopStencilMask;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushClippingRect(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PushClippingRect;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PopClippingRect(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PopClippingRect;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushScissors(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PushScissors;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PopScissors(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PopScissors;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushGroupMatrix(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PushGroupMatrix;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PopGroupMatrix(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PopGroupMatrix;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushRenderTexture(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PushRenderTexture;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BlitAndPopRenderTexture(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.BlitAndPopRenderTexture;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushDefaultMaterial(Entry parentEntry, Material material)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PushDefaultMaterial;
            entry.material = material;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PopDefaultMaterial(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PopDefaultMaterial;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CutRenderChain(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.CutRenderChain;
            Append(parentEntry, entry);
        }

        // Returns an entry to which children can be added
        public Entry InsertPlaceholder(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.DedicatedPlaceholder;
            Append(parentEntry, entry);
            return entry;
        }

        static void AppendMeshEntry(Entry parentEntry, Entry entry)
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

            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        static void Append(Entry parentEntry, Entry entry)
        {
            if (parentEntry.lastChild == null)
            {
                Debug.Assert(parentEntry.firstChild == null);
                parentEntry.firstChild = entry;
                parentEntry.lastChild = entry;
            }
            else
            {
                parentEntry.lastChild.nextSibling = entry;
                parentEntry.lastChild = entry;
            }
        }
    }
}
