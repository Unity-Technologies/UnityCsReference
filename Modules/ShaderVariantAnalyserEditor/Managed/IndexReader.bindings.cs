// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderVariantAnalyser
{
    [NativeHeader("Modules/ShaderVariantAnalyserEditor/Private/Indices.h")]
    struct StringsBlob
    {
        NativeReadOnlySpan<byte> m_Data;

        public StringsBlob(NativeReadOnlySpan<byte> data)
        {
            m_Data = data;
        }

        public readonly NativeReadOnlySpan<byte> RawData => m_Data;

        public readonly string GetAtOffset(ulong offset) => GetStringFromBlobAtOffset(m_Data, offset);

        [FreeFunction("ShaderVariantAnalyser::StringsBlob::GetStringFromBlobAtOffset")]
        extern static string GetStringFromBlobAtOffset(ReadOnlySpan<byte> blob, ulong offset);
    }

    enum EntryType : byte
    {
        Shader,
        ComputeShader,
        Material,
        VariantCollection,
    }

    [StructLayout(LayoutKind.Sequential)]
    struct IndexEntry
    {
        public AssetId id;
        public ulong nameOffset;
        public ulong dataOffset;
        public int variantCount;
        public int referenceCount;
        public EntryType type;
    }

    struct IndexEntryRef
    {
        IndexEntry m_Data;
        StringsBlob m_Names;

        public IndexEntryRef(IndexEntry data, StringsBlob names)
        {
            m_Data = data;
            m_Names = names;
        }

        public readonly string Name => m_Names.GetAtOffset(m_Data.nameOffset);
        public readonly EntryType Type => m_Data.type;
        public readonly int VariantCount => m_Data.variantCount;
        public readonly int MaterialCount => m_Data.referenceCount;
    }

    interface IByNameFilter
    {
        int FilterByName(string filter, Span<int> filteredIndices);
    }

    interface IFilterableIndex : IFilterable<IndexEntryRef>
    {
        int ResetFilter(Span<int> indices);
    }

    class FirstNIndex : IFilteredList<IndexEntryRef>
    {
        IFilterableIndex m_Source;
        int m_Count;

        public FirstNIndex(IFilterableIndex src, int count)
        {
            m_Source = src;
            m_Count = Math.Min(m_Source.MaxCount, count);
        }

        public int ToRawIndex(int idx) => idx;
        public int FindRawIndex(int idx) => idx;

        public int Count => m_Count;

        public IndexEntryRef this[int index]
        {
            get
            {
                if (index < 0 || index >= m_Count)
                    throw new IndexOutOfRangeException();

                return m_Source.AtRawIndex(index);
            }
        }
        public IEnumerator<IndexEntryRef> GetEnumerator()
        {
            for (int i = 0; i < m_Count; ++i)
                yield return m_Source.AtRawIndex(i);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    class PrefilteredIndex : IFilterableIndex, IFilteredList<IndexEntryRef>
    {
        IFilterableIndex m_Source;
        NativeReadOnlySpan<int> m_Prefiltered;

        public PrefilteredIndex(IFilterableIndex src, NativeReadOnlySpan<int> indices)
        {
            m_Source = src;
            m_Prefiltered = indices;
        }

        public int MaxCount => m_Prefiltered.Length;
        public int MaxSourceCount => m_Source.MaxSourceCount;

        public int Count => m_Prefiltered.Length;

        public IndexEntryRef this[int index] => AtRawIndex(ToRawIndex(index));

        public int ToRawIndex(int idx) => m_Prefiltered[idx];
        public int FindRawIndex(int idx)
        {
            for (int i = 0; i < m_Prefiltered.Length; ++i)
            {
                if (m_Prefiltered[i] == idx)
                    return i;
            }
            return -1;
        }

        public IndexEntryRef AtRawIndex(int idx) => m_Source.AtRawIndex(idx);

        public int ResetFilter(Span<int> indices)
        {
            int count = Math.Min(indices.Length, Count);
            for (int i = 0; i < count; ++i)
                indices[i] = m_Prefiltered[i];
            return count;
        }

        public IEnumerator<IndexEntryRef> GetEnumerator()
        {
            for (int i = 0; i < Count; ++i)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    class ByNameIndex : IFilteredList<IndexEntryRef>
    {
        IByNameFilter m_Filter;
        IFilterableIndex m_Source;
        int[] m_Filtered;
        int m_Count = 0;
        string m_LastFilter = string.Empty;

        public ByNameIndex(IFilterableIndex src, IByNameFilter filter)
        {
            m_Filter = filter;
            m_Source = src;
            m_Filtered = new int[src.MaxCount];
            m_Count = src.ResetFilter(m_Filtered);
        }

        public int Count => m_Count;

        public IndexEntryRef this[int index] => AtRawIndex(ToRawIndex(index));

        public int ToRawIndex(int idx)
        {
            if (idx >= m_Count)
                throw new IndexOutOfRangeException();

            return m_Filtered[idx];
        }

        public int FindRawIndex(int idx) => Array.IndexOf(m_Filtered, idx);

        public IndexEntryRef AtRawIndex(int idx) => m_Source.AtRawIndex(idx);

        public void UpdateFilter(string filter)
        {
            if (m_LastFilter == filter)
                return;

            // If the new filter isn't a subset of the existing one, we need to first reset the filter
            // Case sensitive because C++ doesn't handle UTF-8 case properly and it would give us inconsistent results
            if (!filter.Contains(m_LastFilter))
                m_Count = m_Source.ResetFilter(m_Filtered);

            m_Count = m_Filter.FilterByName(filter, m_Filtered.AsSpan().Slice(0, m_Count));

            m_LastFilter = filter;
        }

        public void Refresh()
        {
            m_Count = m_Source.ResetFilter(m_Filtered);
            m_Count = m_Filter.FilterByName(m_LastFilter, m_Filtered.AsSpan().Slice(0, m_Count));
        }

        public IEnumerator<IndexEntryRef> GetEnumerator()
        {
            for (int i = 0; i < Count; ++i)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [NativeHeader("Modules/ShaderVariantAnalyserEditor/Private/Indices.h")]
    class IndexReader : IFilterableIndex, IByNameFilter
    {
        NativeReadOnlySpan<IndexEntry> m_Entries;
        StringsBlob m_Names;

        internal IndexReader(NativeReadOnlySpan<IndexEntry> entries, StringsBlob names)
        {
            m_Names = names;
            m_Entries = entries;
        }
        public int MaxCount => m_Entries.Length;
        public int MaxSourceCount => m_Entries.Length;

        public IndexEntryRef AtRawIndex(int idx) => new(m_Entries[idx], m_Names);
        public int ToRawIndex(int idx) => idx;

        public int ResetFilter(Span<int> indices)
        {
            int size = Math.Min(MaxCount, indices.Length);
            for (int i = 0; i < size; ++i)
                indices[i] = i;

            return size;
        }

        public int FilterByName(string filter, Span<int> filteredIndices)
        {
            return FilterIndexByName_Internal(m_Entries, m_Names.RawData, filter, filteredIndices);
        }

        public int FindAssetIndex(GUID guid)
        {
            for (int i = 0; i < m_Entries.Length; ++i)
            {
                if (m_Entries[i].id.guid == guid)
                    return i;
            }
            return -1;
        }

        [FreeFunction("ShaderVariantAnalyser::FilterIndexByName")]
        extern static int FilterIndexByName_Internal(ReadOnlySpan<IndexEntry> entries, ReadOnlySpan<byte> names, string filter, Span<int> filtered);
    }
}
