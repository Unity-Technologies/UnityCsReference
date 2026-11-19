// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.UIElements.UIR
{
    enum FilterParameterKind
    {
        Float,
        Vector,
        Matrix,
    }

    // What the update phase writes for one filter chain of an element, and its passes read when they draw. Held by value in
    // ExtraRenderData; the list and values persist (empty) across pooled reuse.
    struct FilterChainParameters
    {
        public FilterParameterValues values;

        // One pooled block per pass in flat chain order, only for a chain with a settings callback: the update phase fills the
        // block of such a pass, so user callbacks never run while a render target is bound
        public List<MaterialPropertyBlock> blocks;

        public void Prepare(RenderTreeManager renderTreeManager, int passCount, bool hasSettingsCallback)
        {
            values ??= new FilterParameterValues();
            values.Reset(renderTreeManager.filterParameterStorage, passCount);
            if (hasSettingsCallback)
                renderTreeManager.SizeFilterCallbackBlocks(blocks ??= new List<MaterialPropertyBlock>(passCount), passCount);
            else
                renderTreeManager.ReleaseFilterCallbackBlocks(blocks);
        }

        // Idempotent
        public void Release(RenderTreeManager renderTreeManager)
        {
            renderTreeManager.ReleaseFilterCallbackBlocks(blocks);
            values?.Clear();
        }
    }

    // The parameter values that the filter passes of one RenderTreeManager write in a walk. A value keeps its address until the
    // next walk resets the storage, so entries and writers point into it.
    unsafe class FilterParameterStorage : IDisposable
    {
        const string k_MemoryLabel = "UIR.FilterParameters";

        readonly NativePagedList<float> m_Floats = new(256, k_MemoryLabel);
        readonly NativePagedList<Vector4> m_Vectors = new(256, k_MemoryLabel);
        readonly NativePagedList<Matrix4x4> m_Matrices = new(64, k_MemoryLabel);

        // Changes on every reset: Reset frees the pages after the first, so the values of an older walk must never be read
        public int generation { get; private set; }

        public void Reset()
        {
            m_Floats.Reset();
            m_Vectors.Reset();
            m_Matrices.Reset();
            ++generation;
        }

        // Zeroed
        public void* Allocate(FilterParameterKind kind) => kind switch
        {
            FilterParameterKind.Float => (void*)m_Floats.AllocLast(),
            FilterParameterKind.Vector => (void*)m_Vectors.AllocLast(),
            _ => (void*)m_Matrices.AllocLast(),
        };

        public void Dispose()
        {
            m_Floats.Dispose();
            m_Vectors.Dispose();
            m_Matrices.Dispose();
            ++generation;
        }
    }

    // The parameters of the passes of one filter chain, by flat pass slot and shader property: the default bindings, then the
    // parameters callback. Written in the update phase, read when the passes draw. Values of an older walk read as never
    // written: passCount is then 0.
    unsafe class FilterParameterValues
    {
        public struct Entry
        {
            public int nameId;
            public FilterParameterKind kind;
            public void* data;

            public readonly float floatValue
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => *(float*)data;
            }

            public readonly Vector4 vector
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => kind == FilterParameterKind.Float ? new Vector4(*(float*)data, 0, 0, 0) : *(Vector4*)data;
            }

            public readonly ref readonly Matrix4x4 matrix
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref UnsafeUtility.AsRef<Matrix4x4>(data);
            }
        }

        struct PassRange
        {
            public int start;
            public int count;
        }

        Entry[] m_Entries = new Entry[8];
        int m_EntryCount;
        PassRange[] m_Passes = Array.Empty<PassRange>();
        int m_PassCount;
        int m_WritingPass = -1;
        FilterParameterStorage m_Storage;
        int m_Generation;

        // The pass count of the chain that the values were written for, in the current walk
        public int passCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Storage != null && m_Storage.generation == m_Generation ? m_PassCount : 0;
        }

        public void Reset(FilterParameterStorage storage, int chainPassCount)
        {
            m_Storage = storage;
            m_Generation = storage.generation;
            m_PassCount = chainPassCount;
            if (m_Passes.Length < chainPassCount)
                m_Passes = new PassRange[Mathf.NextPowerOfTwo(chainPassCount)];
            Array.Clear(m_Passes, 0, chainPassCount);
            m_EntryCount = 0;
            m_WritingPass = -1;
        }

        // Keeps the storage: a callback that removes its own element can still write, into slots nothing reads
        public void Clear()
        {
            m_PassCount = 0;
            m_EntryCount = 0;
            m_WritingPass = -1;
        }

        // Passes are written in slot order, so the pass being written holds the last entries. Beginning it again discards them.
        public void BeginPass(int passSlot)
        {
            if (passSlot >= m_PassCount)
            {
                // Cleared during the walk: a callback removed its own element
                m_WritingPass = -1;
                return;
            }

            Debug.Assert(passSlot >= m_WritingPass);
            if (passSlot == m_WritingPass)
                m_EntryCount = m_Passes[passSlot].start;
            m_Passes[passSlot] = new PassRange { start = m_EntryCount };
            m_WritingPass = passSlot;
        }

        // The slot of the property in the pass being written. A new slot is zeroed; another kind for the same property moves it
        // to a new slot.
        public void* Write(int nameId, FilterParameterKind kind)
        {
            if (m_WritingPass < 0)
                return m_Storage.Allocate(kind);

            ref var range = ref m_Passes[m_WritingPass];
            int end = range.start + range.count;
            for (int i = range.start; i < end; ++i)
            {
                ref var entry = ref m_Entries[i];
                if (entry.nameId != nameId)
                    continue;
                if (entry.kind != kind)
                {
                    entry.kind = kind;
                    entry.data = m_Storage.Allocate(kind);
                }
                return entry.data;
            }

            if (m_EntryCount == m_Entries.Length)
                Array.Resize(ref m_Entries, m_Entries.Length * 2);
            var data = m_Storage.Allocate(kind);
            m_Entries[m_EntryCount++] = new Entry { nameId = nameId, kind = kind, data = data };
            ++range.count;
            return data;
        }

        // The slot of the property in the pass being written, when it was written with this kind
        public bool TryGetWritten(int nameId, FilterParameterKind kind, out void* data)
        {
            data = null;
            if (m_WritingPass < 0)
                return false;

            var range = m_Passes[m_WritingPass];
            int end = range.start + range.count;
            for (int i = range.start; i < end; ++i)
            {
                ref readonly var entry = ref m_Entries[i];
                if (entry.nameId != nameId)
                    continue;
                if (entry.kind != kind)
                    return false;
                data = entry.data;
                return true;
            }
            return false;
        }

        public ReadOnlySpan<Entry> GetPass(int passSlot)
        {
            if ((uint)passSlot >= (uint)passCount)
                return default;
            var range = m_Passes[passSlot];
            return new ReadOnlySpan<Entry>(m_Entries, range.start, range.count);
        }

        // For a pass drawn one element at a time: the shader reads the same properties as uniforms
        public void ApplyTo(MaterialPropertyBlock block, int passSlot)
        {
            var entries = GetPass(passSlot);
            for (int i = 0; i < entries.Length; ++i)
            {
                ref readonly var entry = ref entries[i];
                switch (entry.kind)
                {
                    case FilterParameterKind.Float: block.SetFloat(entry.nameId, entry.floatValue); break;
                    case FilterParameterKind.Vector: block.SetVector(entry.nameId, entry.vector); break;
                    default: block.SetMatrix(entry.nameId, entry.matrix); break;
                }
            }
        }

        public bool TryGet(int passSlot, int nameId, out Entry entry)
        {
            var entries = GetPass(passSlot);
            for (int i = 0; i < entries.Length; ++i)
            {
                if (entries[i].nameId == nameId)
                {
                    entry = entries[i];
                    return true;
                }
            }
            entry = default;
            return false;
        }
    }
}
