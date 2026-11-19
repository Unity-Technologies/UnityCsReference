// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderVariantAnalyser
{
    [StructLayout(LayoutKind.Sequential)]
    struct GlobalStats
    {
        public int shadersCount;
        public int variantsCount;
        public int materialsCount;
    };

    [NativeHeader("Modules/ShaderVariantAnalyserEditor/Private/DataReader.h")]
    class DataReader : IDisposable
    {
        IntPtr m_Ptr;

        public GlobalStats Stats { get; private set; }
        public IndexReader Materials { get; private set; }
        public FirstNIndex OverviewMaterials { get; private set; }
        public IndexReader Shaders { get; private set; }
        public PrefilteredIndex OverviewShaders { get; private set; }

        DataReader(IntPtr ptr)
        {
            m_Ptr = ptr;

            Stats = GetStats();
            Materials = new IndexReader(new (GetMaterialEntries()), new (new(GetMaterialNames())));
            OverviewMaterials = new FirstNIndex(Materials, 10);
            Shaders = new IndexReader(new (GetShaderEntries()), new(new(GetShaderNames())));
            OverviewShaders = new PrefilteredIndex(Shaders, new (GetTopShaders()));
        }

        public static DataReader LoadFolder(string folder)
        {
            var ptr = Create_Internal(Path.Combine(folder, "variant_analyzer"));
            if (ptr == IntPtr.Zero)
                return null;

            return new DataReader(ptr);
        }

        [NativeMethod] extern ReadOnlySpan<IndexEntry> GetShaderEntries();
        [NativeMethod] extern ReadOnlySpan<byte> GetShaderNames();
        [NativeMethod] extern ReadOnlySpan<int> GetTopShaders();
        [NativeMethod] extern ReadOnlySpan<IndexEntry> GetMaterialEntries();
        [NativeMethod] extern ReadOnlySpan<byte> GetMaterialNames();
        [NativeMethod] public extern ShaderReader CreateShaderReader(int idx);
        [NativeMethod] public extern MaterialReader CreateMaterialReader(int idx);
        [NativeMethod] extern GlobalStats GetStats();

        [NativeMethod] extern static IntPtr Create_Internal(string folder);
        [NativeMethod] extern static void Destroy_Internal(IntPtr ptr);

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
                Destroy_Internal(m_Ptr);

            m_Ptr = IntPtr.Zero;
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(DataReader d) => d.m_Ptr;
            public static IntPtr ConvertToUnmanaged(DataReader d) => d != null ? d.m_Ptr : IntPtr.Zero;
        }
    }
}
