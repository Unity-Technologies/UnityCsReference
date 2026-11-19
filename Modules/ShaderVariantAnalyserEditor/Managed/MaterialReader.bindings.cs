// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderVariantAnalyser
{
    [NativeHeader("Modules/ShaderVariantAnalyserEditor/Private/MaterialRuntimeTypes.h")]
    partial class MaterialReader : IDetailsReader
    {
        IntPtr m_Ptr;
        public string Name { get; private set; }
        ShaderReader[] m_Shaders;

        public FilteredVariants Variants
        {
            get
            {
                var sources = new IFilterableVariants[m_Shaders.Length];
                var filters = new IVariantsFilter[m_Shaders.Length];
                for (int i = 0; i < m_Shaders.Length; ++i)
                {
                    sources[i] = m_Shaders[i].MaterialVariants;
                    filters[i] = m_Shaders[i];
                }

                var combined = new CombinedShaders(sources, filters);
                return new FilteredVariants(combined, combined);
            }
        }

        public IndexByVariantSelectionFilter GetReferencingMaterials(IndexReader allMaterials) => throw new InvalidOperationException();

        internal MaterialReader(IntPtr ptr)
        {
            m_Ptr = ptr;
            m_Shaders = GetShaders();
            Name = GetName();
        }

        [NativeMethod] extern ShaderReader[] GetShaders();
        [NativeMethod] extern string GetName();

        [AutoStaticsCleanup]
        static ProfilerMarker s_TreeMarker = new ProfilerMarker("ShaderVariantAnalyser.GenerateTree");
        public List<TreeViewItemData<HierarchyNodeManaged>> GenerateTree()
        {
            using (s_TreeMarker.Auto())
            {
                int nextId = 0;
                List<TreeViewItemData<HierarchyNodeManaged>> res = new();
                for (int i = 0; i < m_Shaders.Length; ++i)
                {
                    FilterElement f = new(FilterType.Shader, m_Shaders[i].Name);
                    res.Add(new(nextId++, new()
                    {
                        value = f,
                        count = (uint)m_Shaders[i].MaterialVariants.MaxCount,
                    }, m_Shaders[i].GenerateTree(ref nextId)));
                }
                return res;
            }
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(MaterialReader d) => d.m_Ptr;
            public static IntPtr ConvertToUnmanaged(MaterialReader d) => d != null ? d.m_Ptr : IntPtr.Zero;
            public static MaterialReader ConvertToManaged(IntPtr ptr) => ptr == IntPtr.Zero ? null : new MaterialReader(ptr);
        }
    }
}
