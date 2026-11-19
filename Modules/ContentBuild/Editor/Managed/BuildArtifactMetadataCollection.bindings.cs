// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Content
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct UCBPKeyValuePair
    {
        public string key;
        public string value;
    }

    // Managed owner of a native BuildArtifactMetadataCollection. The collection owns the deserialized
    // metadata nodes; per-node data is read back through the accessors below, keyed by id. A
    // BuildArtifactMetadata value struct is only valid while the collection that produced it is alive.
    [NativeHeader("Modules/ContentBuild/Editor/Ucbp/BuildArtifactMetadataCollectionBindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    internal class BuildArtifactMetadataCollection : IDisposable
    {
        private IntPtr m_Ptr;

        public BuildArtifactMetadataCollection()
        {
            m_Ptr = Internal_Create();
        }

#pragma warning disable UAC0045 // Intentional leak warning: native destroy is not thread-safe, so the finalizer only logs when Dispose was not called.
        ~BuildArtifactMetadataCollection()
        {
            Dispose(false);
        }
#pragma warning restore UAC0045

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_Ptr == IntPtr.Zero)
                return;

            // Internal_Destroy releases native UDS handles through UDSInterface, which is not thread-safe and
            // must run on the main thread. The GC finalizer runs on its own thread (and may run after the
            // storage reader has been torn down during shutdown/domain reload), so freeing there risks
            // deadlocks or use-after-free. Only free on an explicit Dispose; from the finalizer, warn about
            // the leak instead so the missing Dispose gets fixed.
            if (disposing)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
            else
            {
                Debug.LogWarning($"{nameof(BuildArtifactMetadataCollection)} was not disposed; dispose it explicitly on the main thread (e.g. with a using block). The native collection has leaked.");
            }
        }

        private void ThrowIfDisposed()
        {
            if (m_Ptr == IntPtr.Zero)
                throw new ObjectDisposedException(nameof(BuildArtifactMetadataCollection));
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(BuildArtifactMetadataCollection obj) => obj.m_Ptr;
        }

        public bool Add(BuildArtifactMetadataId id, bool recursive)
        {
            ThrowIfDisposed();
            return Internal_Add(this, id, recursive);
        }

        public BuildArtifactMetadata Get(BuildArtifactMetadataId id) => new BuildArtifactMetadata(this, id);

        internal string GetCategory(BuildArtifactMetadataId id)                  { ThrowIfDisposed(); return Internal_GetCategory(this, id); }
        internal Hash128 GetArtifactCAH(BuildArtifactMetadataId id)             { ThrowIfDisposed(); return Internal_GetArtifactCAH(this, id); }
        internal ulong GetArtifactSize(BuildArtifactMetadataId id)             { ThrowIfDisposed(); return Internal_GetArtifactSize(this, id); }
        internal bool HasTypeSpecificMetadata(BuildArtifactMetadataId id)      { ThrowIfDisposed(); return Internal_HasTypeSpecificMetadata(this, id); }
        internal Hash128 GetTypeSpecificMetadata(BuildArtifactMetadataId id)   { ThrowIfDisposed(); return Internal_GetTypeSpecificMetadata(this, id); }
        internal BuildArtifactMetadataId[] GetReferences(BuildArtifactMetadataId id) { ThrowIfDisposed(); return Internal_GetReferences(this, id); }
        internal UCBPKeyValuePair[] GetKeyValuePairs(BuildArtifactMetadataId id)     { ThrowIfDisposed(); return Internal_GetKeyValuePairs(this, id); }

        [FreeFunction("BuildPipeline::BuildArtifactMetadataCollection_Create")]
        private static extern IntPtr Internal_Create();

        [FreeFunction("BuildPipeline::BuildArtifactMetadataCollection_Destroy")]
        private static extern void Internal_Destroy(IntPtr ptr);

        [FreeFunction("BuildPipeline::BuildArtifactMetadataCollection_Add")]
        private static extern bool Internal_Add(BuildArtifactMetadataCollection coll, BuildArtifactMetadataId id, bool recursive);

        [FreeFunction("BuildPipeline::BuildArtifactMetadataCollection_GetCategory", ThrowsException = true)]
        private static extern string Internal_GetCategory(BuildArtifactMetadataCollection coll, BuildArtifactMetadataId id);

        [FreeFunction("BuildPipeline::BuildArtifactMetadataCollection_GetArtifactCAH", ThrowsException = true)]
        private static extern Hash128 Internal_GetArtifactCAH(BuildArtifactMetadataCollection coll, BuildArtifactMetadataId id);

        [FreeFunction("BuildPipeline::BuildArtifactMetadataCollection_GetArtifactSize", ThrowsException = true)]
        private static extern ulong Internal_GetArtifactSize(BuildArtifactMetadataCollection coll, BuildArtifactMetadataId id);

        [FreeFunction("BuildPipeline::BuildArtifactMetadataCollection_HasTypeSpecificMetadata", ThrowsException = true)]
        private static extern bool Internal_HasTypeSpecificMetadata(BuildArtifactMetadataCollection coll, BuildArtifactMetadataId id);

        [FreeFunction("BuildPipeline::BuildArtifactMetadataCollection_GetTypeSpecificMetadata", ThrowsException = true)]
        private static extern Hash128 Internal_GetTypeSpecificMetadata(BuildArtifactMetadataCollection coll, BuildArtifactMetadataId id);

        [FreeFunction("BuildPipeline::BuildArtifactMetadataCollection_GetReferences", ThrowsException = true)]
        private static extern BuildArtifactMetadataId[] Internal_GetReferences(BuildArtifactMetadataCollection coll, BuildArtifactMetadataId id);

        [FreeFunction("BuildPipeline::BuildArtifactMetadataCollection_GetKeyValuePairs", ThrowsException = true)]
        private static extern UCBPKeyValuePair[] Internal_GetKeyValuePairs(BuildArtifactMetadataCollection coll, BuildArtifactMetadataId id);
    }
}
