// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderVariantAnalyser
{
    interface IIndexByVariant
    {
        int FilterMaterialsFromHierarchy(Span<int> mats, int snippet, int keyword);
        int FilterMaterialsFromVariant(Span<int> mats, int idx);
    }

    class IndexByVariantSelectionFilter : IFilterableIndex
    {
        IFilterableIndex m_Source;
        IIndexByVariant m_Filter;
        int[] m_Indices;
        int m_Count;

        public int Count => m_Count;

        public IndexByVariantSelectionFilter(IFilterableIndex src, IIndexByVariant filter)
        {
            m_Source = src;
            m_Filter = filter;
            m_Indices = new int[src.MaxCount];
            FilterFromHierarchy(-1, -1);
        }

        public int MaxCount => m_Source.MaxCount;
        public int MaxSourceCount => m_Source.MaxSourceCount;

        public IndexEntryRef AtRawIndex(int idx) => m_Source.AtRawIndex(idx);

        public int ResetFilter(Span<int> indices)
        {
            var count = Math.Min(indices.Length, m_Count);
            for (int i = 0; i < count; ++i)
                indices[i] = m_Indices[i];

            return count;
        }

        public int ToRawIndex(int idx)
        {
            if (idx >= m_Count)
                throw new IndexOutOfRangeException();

            return m_Indices[idx];
        }

        public void FilterFromHierarchy(int snippet, int keyword)
        {
            m_Count = m_Filter.FilterMaterialsFromHierarchy(m_Indices, snippet, keyword);
        }
        public void FilterFromVariant(int idx)
        {
            m_Count = m_Filter.FilterMaterialsFromVariant(m_Indices, idx);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ShaderSnippetNode
    {
        public int value;
        public short parent;
        public short childCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct HierarchyNode
    {
        public int value;
        public uint count;
        public uint materials;
        public uint children;
    }

    struct HierarchyNodeManaged
    {
        public FilterElement value;
        public uint count;
        public uint materials;
    }

    struct VariantEntry : IEnumerable<FilterElement>
    {
        ShaderReader m_Shader;
        VariantRef m_Variant;

        public VariantEntry(ShaderReader shader, VariantRef variant)
        {
            m_Shader = shader;
            m_Variant = variant;
        }

        public IEnumerator<FilterElement> GetEnumerator() => m_Shader.ToManagedFilter(m_Variant).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    enum FilterType : int
    {
        Shader,
        Snippet,
        Api,
        Keyword,
        CommonKeywords,
    }

    [Serializable]
    struct FilterElement
    {
        public static FilterElement kCommon => new(FilterType.CommonKeywords, "Common Keywords");

        public FilterType type;
        public string value;
        public int hint;

        public FilterElement(FilterType t, string v, int h = -1)
        {
            type = t;
            value = v;
            hint = h;
        }

        public readonly string Tooltip
        {
            get
            {
                switch (type)
                {
                    case FilterType.Shader:
                        return "Shader";
                    case FilterType.Snippet:
                        return "Shader Snippet";
                    case FilterType.Api:
                        return "Graphics API";
                    case FilterType.Keyword:
                        return "Keyword";
                }
                return string.Empty;
            }
        }
    }

    struct VariantRef
    {
        public NativeReadOnlySpan<ulong> rawData;

        public VariantRef(NativeReadOnlySpan<ulong> data)
        {
            rawData = data;
        }

        public readonly Vector2Int ToBitIdx(int idx) => new(idx / 64, idx % 64);

        public readonly bool HasKeyword(int keyword)
        {
            var idx = ToBitIdx(keyword);
            return (rawData[idx.x] & ((ulong)1 << idx.y)) != 0;
        }
    }

    struct VariantRefWritable
    {
        public VariantRef readOnly;
        public NativeSpan<ulong> rawData;

        public VariantRefWritable(NativeSpan<ulong> data)
        {
            readOnly.rawData = data;
            rawData = data;
        }

        public void SetKeyword(int keyword)
        {
            var idx = readOnly.ToBitIdx(keyword);
            readOnly.rawData[idx.x] = readOnly.rawData[idx.x] | ((ulong)1 << idx.y);
        }

        public void FillWithZero()
        {
            for (int i = 0; i < readOnly.rawData.Length; ++i)
                readOnly.rawData[i] = 0;
        }
    }

    class OwnedVariant : IDisposable
    {
        VariantRefWritable m_Ref;
        NativeArray<ulong> m_Data;
        public VariantRefWritable Writable => m_Ref;
        public VariantRef ReadOnly => m_Ref.readOnly;

        public OwnedVariant(int wordCount, Allocator alloc)
        {
            m_Data = new NativeArray<ulong>(wordCount, alloc);
            m_Ref = new VariantRefWritable(new (m_Data.AsSpan()));
        }

        public void Dispose() => m_Data.Dispose();
    }

    class VariantSpan : IReadOnlyList<VariantRef>
    {
        NativeReadOnlySpan<ulong> m_RawData;
        int m_WordCount;

        public int WordCount => m_WordCount;

        public VariantSpan(NativeReadOnlySpan<ulong> rawData, int wordCount)
        {
            m_RawData = rawData;
            m_WordCount = wordCount;
        }

        public VariantRef this[int index] => new VariantRef { rawData = m_RawData.Slice(index * m_WordCount, m_WordCount) };
        public int Count => m_RawData.Length / m_WordCount;

        public IEnumerator<VariantRef> GetEnumerator()
        {
            for (int i = 0; i < m_RawData.Length; i += m_WordCount)
                yield return new VariantRef { rawData = m_RawData.Slice(i, m_WordCount) };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    interface IFilterableVariants : IFilterable<VariantEntry>
    {
        int ResetFilter(Span<int> filtered, int indexOffset);
    }

    interface IVariantsFilter
    {
        int UpdateVariantsFilter(Span<int> filtered, ReadOnlySpan<int> toFilter, int indexOffset, IEnumerable<FilterElement> filters, out ICollection<FilterElement> commonKeywords);
        IEnumerable<FilterElement> AvailableFilters { get; }
    }

    partial class FilteredVariants : IFilterableVariants, IFilteredList<VariantEntry>
    {
        IVariantsFilter m_Filter;
        IFilterableVariants m_Source;
        int[] m_Indices;
        int m_Count;
        ICollection<FilterElement> m_CommonKeywords = null;
        List<FilterElement> m_LastFilter = new();

        public IEnumerable<FilterElement> AvailableFilters => m_Filter.AvailableFilters;

        public FilteredVariants(IFilterableVariants src, IVariantsFilter filter)
        {
            m_Filter = filter;
            m_Source = src;
            m_Indices = new int[m_Source.MaxCount];
            m_Count = src.ResetFilter(m_Indices, 0);
            UpdateVariantsFilter(Array.Empty<FilterElement>());
        }

        public VariantEntry AtRawIndex(int idx) => m_Source.AtRawIndex(idx);
        public VariantEntry this[int index] => AtRawIndex(ToRawIndex(index));

        public int ToRawIndex(int idx)
        {
            if (idx >= m_Count)
                throw new IndexOutOfRangeException();

            return m_Indices[idx];
        }

        public int FindRawIndex(int idx) => Array.IndexOf(m_Indices, idx);

        public int MaxSourceCount => m_Source.MaxSourceCount;
        public int MaxCount => m_Source.MaxCount;
        public int Count => m_Count;

        public ICollection<FilterElement> CommonKeywords => m_CommonKeywords;

        public IEnumerator<VariantEntry> GetEnumerator()
        {
            for (int i = 0; i < m_Count; ++i)
                yield return this[i];
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        int IFilterableVariants.ResetFilter(Span<int> filtered, int indexOffset)
        {
            var size = Math.Min(filtered.Length, m_Count);
            for (int i = 0; i < size; ++i)
                filtered[i] = ToRawIndex(i) + indexOffset;

            return size;
        }

        [AutoStaticsCleanup]
        static ProfilerMarker s_FilterMarker = new ProfilerMarker("ShaderVariantAnalyser.FilterVariants");
        public void UpdateVariantsFilter(ICollection<FilterElement> filters)
        {
            using (s_FilterMarker.Auto())
            {
                foreach (var f in m_LastFilter)
                {
                    if (!filters.Contains(f))
                    {
                        m_Count = m_Source.ResetFilter(m_Indices, 0);
                        break;
                    }
                }
                m_LastFilter.Clear();
                m_LastFilter.AddRange(filters);

                m_Count = m_Filter.UpdateVariantsFilter(m_Indices, m_Indices.AsSpan().Slice(0, m_Count), 0, filters, out m_CommonKeywords);
            }
        }
    }

    class PrefilteredVariants : IFilterableVariants
    {
        IFilterableVariants m_Source;
        NativeReadOnlySpan<int> m_Indices;

        public PrefilteredVariants(IFilterableVariants src, NativeReadOnlySpan<int> data)
        {
            m_Source = src;
            m_Indices = data;
        }

        public int MaxSourceCount => m_Source.MaxSourceCount;
        public int MaxCount => m_Indices.Length;

        public VariantEntry AtRawIndex(int idx) => m_Source.AtRawIndex(idx);

        public int ResetFilter(Span<int> filtered, int indexOffset)
        {
            var size = Math.Min(filtered.Length, m_Indices.Length);
            for (int i = 0; i <  size; i++)
                filtered[i] = m_Indices[i] + indexOffset;

            return size;
        }

        public int ToRawIndex(int idx) => m_Indices[idx];
    }

    interface IShaderReader : IFilterableVariants, IVariantsFilter
    { }

    class CombinedShaders : IShaderReader
    {
        IFilterableVariants[] m_Sources;
        IVariantsFilter[] m_Filters;
        int[] m_FirstVariant;
        int m_LastSource = 0;
        int m_MaxCount;
        HashSet<FilterElement> m_Available = new();

        public CombinedShaders(ICollection<IFilterableVariants> srcs, ICollection<IVariantsFilter> filters)
        {
            if (srcs.Count != filters.Count)
                throw new ArgumentException();

            m_Sources = new IFilterableVariants[srcs.Count];
            srcs.CopyTo(m_Sources, 0);

            m_Filters = new IVariantsFilter[filters.Count];
            filters.CopyTo(m_Filters, 0);

            m_FirstVariant = new int[srcs.Count + 1];
            m_FirstVariant[0] = 0;
            for (int i = 0; i < m_Sources.Length; ++i)
            {
                m_FirstVariant[i + 1] = m_FirstVariant[i] + m_Sources[i].MaxSourceCount;
                m_MaxCount += m_Sources[i].MaxCount;
                m_Available.UnionWith(m_Filters[i].AvailableFilters);
            }
        }

        public int MaxSourceCount => m_FirstVariant[^1];
        public int MaxCount => m_MaxCount;

        public IEnumerable<FilterElement> AvailableFilters => m_Available;

        public VariantEntry AtRawIndex(int idx)
        {
            while (m_FirstVariant[m_LastSource] > idx)
                m_LastSource--;
            while (m_FirstVariant[m_LastSource + 1] <= idx)
                m_LastSource++;

            return m_Sources[m_LastSource].AtRawIndex(idx - m_FirstVariant[m_LastSource]);
        }

        public int ResetFilter(Span<int> filtered, int indexOffset)
        {
            var count = 0;
            for (int i = 0; i < m_Sources.Length; ++i)
                count += m_Sources[i].ResetFilter(filtered.Slice(count), m_FirstVariant[i] + indexOffset);

            return count;
        }

        public int ToRawIndex(int idx) => idx;

        public int UpdateVariantsFilter(Span<int> filtered, ReadOnlySpan<int> toFilter, int indexOffset, IEnumerable<FilterElement> filters, out ICollection<FilterElement> commonKeywords)
        {
            HashSet<FilterElement> common = new();
            bool first = true;
            int lastToFilter = 0;
            int count = 0;

            for (int i = 0; i < m_Filters.Length; ++i)
            {
                var firstToFilter = lastToFilter;
                // Find all the inidices that belong to the current source
                while (lastToFilter < toFilter.Length && (toFilter[lastToFilter] - indexOffset) < m_FirstVariant[i + 1])
                    lastToFilter++;

                int filteredCount = m_Filters[i].UpdateVariantsFilter(
                    filtered.Slice(count),
                    toFilter.Slice(firstToFilter, lastToFilter - firstToFilter),
                    m_FirstVariant[i] + indexOffset,
                    filters,
                    out var sourceCommon);

                count += filteredCount;
                if (filteredCount > 0)
                {
                    if (first)
                        common.UnionWith(sourceCommon);
                    else
                        common.IntersectWith(sourceCommon);
                    first = false;
                }
            }
            commonKeywords = common;
            return count;
        }
    }

    [NativeHeader("Modules/ShaderVariantAnalyserEditor/Private/ShaderRuntimeTypes.h")]
    partial class ShaderReader : IShaderReader, IIndexByVariant, IDetailsReader
    {
        IntPtr m_Ptr;
        public string Name { get; private set; }
        int m_KeywordCount;
        string[] m_ElementNames;
        NativeReadOnlySpan<ShaderSnippetNode> m_Nodes;
        VariantSpan m_VariantData;

        PrefilteredVariants m_MaterialVariants;

        int KeywordsStart => 0;
        int KeywordsEnd => m_KeywordCount;
        int NodesStart => KeywordsEnd;
        int NodesEnd => NodesStart + m_Nodes.Length;
        int ApisStart => NodesEnd;
        int ApisEnd => m_ElementNames.Length;

        public FilteredVariants Variants => new FilteredVariants(this, this);
        public PrefilteredVariants MaterialVariants => m_MaterialVariants;
        public IEnumerable<FilterElement> AvailableFilters
        {
            get
            {
                yield return new(FilterType.Shader, Name);
                yield return FilterElement.kCommon;

                for (int i = KeywordsStart; i < KeywordsEnd; ++i)
                    yield return new(FilterType.Keyword, m_ElementNames[i]);

                for (int i = ApisStart; i < ApisEnd; ++i)
                    yield return new(FilterType.Api, m_ElementNames[i]);
            }
        }

        public int MaxCount => m_VariantData.Count;
        public int MaxSourceCount => m_VariantData.Count;

        public VariantEntry AtRawIndex(int idx) => new VariantEntry(this, m_VariantData[idx]);

        public IEnumerable<FilterElement> ToManagedFilter(VariantRef variant)
        {
            yield return new(FilterType.Shader, Name);

            for (int i = NodesStart; i < NodesEnd; ++i)
            {
                if (variant.HasKeyword(i))
                    yield return new(FilterType.Snippet, m_ElementNames[i]);
            }

            for (int i = ApisStart; i < ApisEnd; ++i)
            {
                if (variant.HasKeyword(i))
                    yield return new(FilterType.Api, m_ElementNames[i]);
            }

            for (int i = KeywordsStart; i < KeywordsEnd; ++i)
            {
                if (variant.HasKeyword(i))
                    yield return new(FilterType.Keyword, m_ElementNames[i]);
            }
        }

        public IndexByVariantSelectionFilter GetReferencingMaterials(IndexReader allMaterials) => new(allMaterials, this);

        internal ShaderReader(IntPtr ptr)
        {
            m_Ptr = ptr;

            Name = GetName();
            m_KeywordCount = GetKeywordCount();
            m_Nodes = new (GetSnippetNodes());
            m_ElementNames = GetElementNames();
            m_VariantData = new VariantSpan(new (GetVariantData()), GetStateWordCount());

            m_MaterialVariants = new PrefilteredVariants(this, new(GetMaterialVariants()));
        }

        public int UpdateVariantsFilter(Span<int> filtered, ReadOnlySpan<int> toFilter, int indexOffset, IEnumerable<FilterElement> filters, out ICollection<FilterElement> commonKeywords)
        {
            using OwnedVariant filterUnmanaged = new(m_VariantData.WordCount, Allocator.Temp);
            using OwnedVariant commonUnmanaged = new(m_VariantData.WordCount, Allocator.Temp);

            if (!TryConvertToUnmanagedFilter(filters, filterUnmanaged.Writable))
            {
                commonKeywords = Array.Empty<FilterElement>();
                return 0;
            }

            var count = UpdateVariantsFilter_Internal(filtered, toFilter, indexOffset, filterUnmanaged.ReadOnly.rawData, commonUnmanaged.Writable.rawData);
            commonKeywords = new List<FilterElement>(ToManagedFilter(commonUnmanaged.ReadOnly));
            return count;
        }

        public FilterElement ToManagedFilter(int value)
        {
            if (value >= KeywordsStart && value < KeywordsEnd)
                return new(FilterType.Keyword, m_ElementNames[value]);

            if (value >= NodesStart && value < NodesEnd)
                return new(FilterType.Snippet, m_ElementNames[value]);

            if (value >= ApisStart && value < ApisEnd)
                return new(FilterType.Api, m_ElementNames[value]);

            return new();
        }

        public bool TryConvertToUnmanagedFilter(IEnumerable<FilterElement> managed, VariantRefWritable unmanaged)
        {
            unmanaged.FillWithZero();

            foreach (var f in managed)
            {
                if (f.type == FilterType.CommonKeywords)
                    continue;

                if (f.type == FilterType.Shader)
                {
                    if (f.value == Name)
                        continue;
                    else
                        return false;
                }

                int idx = f.hint;
                if (idx < 0)
                {
                    switch (f.type)
                    {
                        case FilterType.Api:
                            idx = Array.IndexOf(m_ElementNames, f.value, ApisStart, ApisEnd - ApisStart);
                            break;
                        case FilterType.Keyword:
                            idx = Array.IndexOf(m_ElementNames, f.value, KeywordsStart, KeywordsEnd - KeywordsStart);
                            break;
                        case FilterType.Snippet:
                            // Snippets can't be resolved without a hint as repeated names are expected (all the Vertex and Fragment for example)
                            // This is fine as they can only be selected from the hierarchy and the hierarchy sets the hint
                            return false;
                    }
                }

                if (idx < 0)
                    return false;

                unmanaged.SetKeyword(idx);
            }

            return true;
        }

        TreeViewItemData<HierarchyNodeManaged> GenerateTree(ReadOnlySpan<HierarchyNode> nodes, ref int idx, ref int nextId)
        {
            var root = idx++;
            List<TreeViewItemData<HierarchyNodeManaged>> children = new();

            for (int i = 0; i < nodes[root].children; ++i)
                children.Add(GenerateTree(nodes, ref idx, ref nextId));

            var value = ToManagedFilter(nodes[root].value);
            value.hint = nodes[root].value;

            return new(nextId++, new()
            {
                value = value,
                count = nodes[root].count,
                materials = nodes[root].materials,
            },
                children);
        }

        public List<TreeViewItemData<HierarchyNodeManaged>> GenerateTree()
        {
            int nextId = 0;
            return GenerateTree(ref nextId);
        }

        [AutoStaticsCleanup]
        static ProfilerMarker s_TreeMarker = new ProfilerMarker("ShaderVariantAnalyser.GenerateTree");
        internal List<TreeViewItemData<HierarchyNodeManaged>> GenerateTree(ref int nextId)
        {
            using (s_TreeMarker.Auto())
            {
                List<TreeViewItemData<HierarchyNodeManaged>> res = new();
                var nodes = GetTreeData();
                int i = 0;
                while (i < nodes.Length)
                    res.Add(GenerateTree(nodes, ref i, ref nextId));

                return res;
            }
        }

        [NativeMethod] extern string GetName();
        [NativeMethod] extern ReadOnlySpan<HierarchyNode> GetTreeData();
        [NativeMethod] extern string[] GetElementNames();
        [NativeMethod] extern ReadOnlySpan<ShaderSnippetNode> GetSnippetNodes();
        [NativeMethod] extern int GetStateWordCount();
        [NativeMethod] extern int GetKeywordCount();
        [NativeMethod] extern ReadOnlySpan<ulong> GetVariantData();

        [NativeMethod] extern ReadOnlySpan<int> GetMaterialVariants();
        [NativeMethod("UpdateVariantsFilter")]
        extern int UpdateVariantsFilter_Internal(Span<int> filtered, ReadOnlySpan<int> toFilter, int indexOffset, ReadOnlySpan<ulong> filters, Span<ulong> common);

        [NativeMethod] public extern int GetMaterialCount();
        [NativeMethod] public extern int FilterMaterialsFromHierarchy(Span<int> mats, int snippet, int keyword);
        [NativeMethod] public extern int FilterMaterialsFromVariant(Span<int> mats, int idx);

        public int ResetFilter(Span<int> filtered, int indexOffset)
        {
            var size = Math.Min(filtered.Length, MaxCount);
            for (int i = 0; i < size; ++i)
                filtered[i] = i + indexOffset;
            return size;
        }

        public int ToRawIndex(int idx) => idx;

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(ShaderReader d) => d.m_Ptr;
            public static IntPtr ConvertToUnmanaged(ShaderReader d) => d != null ? d.m_Ptr : IntPtr.Zero;
            public static ShaderReader ConvertToManaged(IntPtr ptr) => ptr == IntPtr.Zero ? null : new ShaderReader(ptr);
        }
    }

    interface IDetailsReader
    {
        string Name { get; }
        FilteredVariants Variants { get; }
        List<TreeViewItemData<HierarchyNodeManaged>> GenerateTree();
        IndexByVariantSelectionFilter GetReferencingMaterials(IndexReader allMaterials);
    }
}
