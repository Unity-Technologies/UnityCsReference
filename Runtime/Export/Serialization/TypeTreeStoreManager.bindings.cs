// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Runtime/Serialize/TypeTreeStoreManager.h")]
    [StaticAccessor("GetTypeTreeStoreManager()", StaticAccessorType.Dot)]
    public static class TypeTreeStoreManager
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SourceHandle
        {
            UInt64 m_Handle;
        }
        public extern static SourceHandle AddTypeTreeSourceFromFile(string path);
        public extern static bool RemoveTypeTreeSource(SourceHandle handle);

        [StructLayout(LayoutKind.Sequential)]
        internal struct DiagnosticCacheInfo
        {
            public int typeTreeMemoryUsage;
            public Hash128[] typeTreeHashes; 
        }
        internal extern static DiagnosticCacheInfo GetTypeTreeCacheDiagnosticInfo();
    }
}
