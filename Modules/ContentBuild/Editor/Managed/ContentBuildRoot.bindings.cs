// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Content
{
    [NativeHeader("Modules/ContentBuild/Editor/Ucbp/ContentBuildRoot.h")]
    [StructLayout(LayoutKind.Sequential)]
    [UnityEngine.Scripting.UsedByNativeCode]
    internal struct ContentBuildId : IEquatable<ContentBuildId>
    {
        public Hash128 hash;

        public ContentBuildId(Hash128 hash)
        {
            this.hash = hash;
        }

        public bool Equals(ContentBuildId other) => hash.Equals(other.hash);
        public override bool Equals(object obj) => obj is ContentBuildId other && Equals(other);
        public override int GetHashCode() => hash.GetHashCode();
        public override string ToString() => hash.ToString();

        public static bool operator ==(ContentBuildId a, ContentBuildId b) => a.Equals(b);
        public static bool operator !=(ContentBuildId a, ContentBuildId b) => !a.Equals(b);
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/ContentBuild/Editor/Ucbp/ContentBuildRoot.h")]
    internal struct MetadataFileEntry
    {
        internal string filename;
        internal Hash128 hash;

        public MetadataFileEntry(string filename, Hash128 hash)
        {
            this.filename = filename;
            this.hash = hash;
        }

        public string Filename => filename;
        public Hash128 Hash => hash;
    }

    [NativeHeader("Modules/ContentBuild/Editor/Ucbp/ContentBuildRoot.h")]
    [NativeHeader("Modules/ContentBuild/Editor/Ucbp/ContentBuildRootUtilities.h")]
    [StructLayout(LayoutKind.Sequential)]
    internal class ContentBuildRoot : IDisposable
    {
        private IntPtr m_Ptr;

        internal ContentBuildRoot(IntPtr ptr)
        {
            m_Ptr = ptr;
        }

        ~ContentBuildRoot()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(ContentBuildRoot obj) => obj.m_Ptr;
            public static ContentBuildRoot ConvertToManaged(IntPtr ptr) => new ContentBuildRoot(ptr);
        }

        // Public API
        [FreeFunction("BuildPipeline::ContentBuildRoot_LoadFromId")]
        public extern static ContentBuildRoot Load(ContentBuildId buildId);

        public extern BuildArtifactMetadataId ManifestMetadataHash { get; }
        public extern MetadataFileEntry[] MetadataFiles { get; }


        [FreeFunction("BuildPipeline::ContentBuildRoot_Destroy")]
        private static extern void Internal_Destroy(IntPtr ptr);
    }
}
