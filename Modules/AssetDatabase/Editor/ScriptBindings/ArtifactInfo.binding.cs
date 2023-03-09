// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [NativeHeader("Modules/AssetDatabase/Editor/V2/ArtifactInfo.h")]
    internal enum ImportActivityWindowStartupData
    {
        AllCurrentRevisions,
        LongestImportDuration,
        MostDependencies,
        ClearCache
    };

    [NativeHeader("Modules/AssetDatabase/Editor/V2/ArtifactInfo.h")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct ArtifactInfoImportStats
    {
        private string m_AssetPath;
        private string m_EditorRevision;
        private ulong m_ImportTimeMicroseconds;
        private long m_ImportedTimestamp;
        private bool m_ImportedFromCacheServer;
        private string m_ImporterClassName;

        public string assetPath { get { return m_AssetPath; } }
        public string editorRevision { get { return m_EditorRevision; } }
        public ulong importTimeMicroseconds { get { return m_ImportTimeMicroseconds; } }
        public long importedTimestamp { get { return m_ImportedTimestamp; } }
        public bool importedFromCacheServer { get { return m_ImportedFromCacheServer; } }
        public string importerClassName { get { return m_ImporterClassName; } }
    }

    [NativeHeader("Modules/AssetDatabase/Editor/V2/ArtifactInfo.h")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct ArtifactInfoProducedFiles
    {
        public static string kStorageInline = "Inline";
        public static string kStorageLibrary = "Library";

        private string m_Extension;
        private string m_LibraryPath;
        private string m_Storage;
        private int m_InlineStorage;

        public string extension { get { return m_Extension; } }
        public string libraryPath { get { return m_LibraryPath; } }
        public string storage {  get { return m_Storage; } }
        public int inlineStorage {  get { return m_InlineStorage; } }
    }

    [NativeHeader("Modules/AssetDatabase/Editor/V2/ArtifactInfo.h")]
    internal enum ArtifactInfoDependencyType
    {
        Static,
        Dynamic
    };

    [NativeHeader("Modules/AssetDatabase/Editor/V2/ArtifactInfo.h")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct ArtifactInfoDependency
    {
        internal ArtifactInfoDependency(object value, ArtifactInfoDependencyType type)
        {
            m_Value = value;
            m_Type = type;
        }

        private object m_Value;
        private ArtifactInfoDependencyType m_Type;

        public object value { get { return m_Value; } }
        public ArtifactInfoDependencyType type { get { return m_Type; } }
    }

    [NativeHeader("Modules/AssetDatabase/Editor/V2/ArtifactInfo.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal class ArtifactInfo : IDisposable
    {
        private static readonly string kInvalidArtifactID = "00000000000000000000000000000000";
        private IntPtr m_Ptr;

        private string m_ArtifactID;

        internal ArtifactInfo()
        {
            m_Ptr = Internal_Create();
            m_ArtifactID = kInvalidArtifactID;
        }

        ~ArtifactInfo()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }

        private bool IsValid()
        {
            return string.CompareOrdinal(m_ArtifactID, kInvalidArtifactID) != 0;
        }

        private static extern IntPtr Internal_Create();

        [FreeFunction("ArtifactInfoBindings::Internal_Destroy", IsThreadSafe = true)]
        private static extern void Internal_Destroy(IntPtr ptr);

        [FreeFunction("ArtifactInfoBindings::GetArtifactID_Internal")]
        private static extern string GetArtifactID_Internal(ArtifactInfo self);

        [FreeFunction("ArtifactInfoBindings::GetArtifactKey_Internal")]
        private static extern ArtifactKey GetArtifactKey_Internal(ArtifactInfo self);

        [FreeFunction("ArtifactInfoBindings::GetAssetPath_Internal")]
        private static extern string GetAssetPath_Internal(ArtifactInfo self);

        [FreeFunction("ArtifactInfoBindings::GetImportDuration_Internal")]
        private static extern ulong GetImportDuration_Internal(ArtifactInfo self);

        [FreeFunction("ArtifactInfoBindings::GetImportTimeStamp_Internal")]
        private static extern long GetImportTimeStamp_Internal(ArtifactInfo self);

        [FreeFunction("ArtifactInfoBindings::GetIsCurrentArtifact_Internal")]
        private static extern bool GetIsCurrentArtifact_Internal(ArtifactInfo self);

        [FreeFunction("ArtifactInfoBindings::GetImportStats_Internal")]
        private static extern ArtifactInfoImportStats GetImportStats_Internal(ArtifactInfo self);

        [FreeFunction("ArtifactInfoBindings::GetProducedFiles_Internal")]
        private static extern ArtifactInfoProducedFiles[] GetProducedFiles_Internal(ArtifactInfo self);

        [FreeFunction("ArtifactInfoBindings::GetDependencies_Internal")]
        private static extern void GetDependencies_Internal(ArtifactInfo self, [Out] string[] keys, [Out] ArtifactInfoDependency[] values);

        [FreeFunction("ArtifactInfoBindings::CreateDependencies_Internal")]
        private static extern int CreateDependencies_Internal(ArtifactInfo self);

        [FreeFunction("ArtifactInfoBindings::GetDependencyCount_Internal")]
        private static extern int GetDependencyCount_Internal(ArtifactInfo self);

        internal string artifactID
        {
            get
            {
                m_ArtifactID = m_ArtifactID ?? GetArtifactID_Internal(this);
                return m_ArtifactID;
            }
        }

        internal string assetPath { get { return GetAssetPath_Internal(this); } }
        internal ArtifactKey artifactKey { get { return GetArtifactKey_Internal(this); } }
        internal bool isCurrentArtifact { get { return GetIsCurrentArtifact_Internal(this); } }

        private ulong m_ImportDuration = 0;
        private long m_TimeStamp = 0;

        internal ulong importDuration
        {
            get
            {
                if (m_ImportDuration == 0)
                {
                    m_ImportDuration = GetImportDuration_Internal(this);
                }

                return m_ImportDuration;
            }
        }

        internal long timeStamp
        {
            get
            {
                if (m_TimeStamp == 0)
                {
                    m_TimeStamp = GetImportTimeStamp_Internal(this);
                }

                return m_TimeStamp;
            }
        }

        private ArtifactInfoImportStats m_ImportStats;

        internal ArtifactInfoImportStats importStats
        {
            get
            {
                if (!IsValid())
                {
                    Debug.LogError("ArtifactInfo has not been initialized. Please ensure its creation is done via AssetDatabase.GetArtifactInfos");
                    return default(ArtifactInfoImportStats);
                }

                if (m_ImportStats.assetPath == null)
                    m_ImportStats = GetImportStats_Internal(this);
                return m_ImportStats;
            }
        }

        private ArtifactInfoProducedFiles[] m_ProducedFiles;

        internal ArtifactInfoProducedFiles[] producedFiles
        {
            get
            {
                if (!IsValid())
                {
                    Debug.LogError("ArtifactInfo has not been initialized. Please ensure its creation is done via AssetDatabase.GetArtifactInfos");
                    return null;
                }

                if (m_ProducedFiles == null)
                    m_ProducedFiles = GetProducedFiles_Internal(this);
                return m_ProducedFiles;
            }
        }

        private Dictionary<string, ArtifactInfoDependency> m_Dependencies;

        internal IDictionary<string, ArtifactInfoDependency> dependencies
        {
            get
            {
                if (!IsValid())
                {
                    Debug.LogError("ArtifactInfo has not been initialized. Please ensure its creation is done via AssetDatabase.GetArtifactInfos");
                    return null;
                }

                if (m_Dependencies == null)
                {
                    int dependencyCount = CreateDependencies_Internal(this);

                    string[] keys = new string[dependencyCount];
                    ArtifactInfoDependency[] values = new ArtifactInfoDependency[dependencyCount];

                    GetDependencies_Internal(this, keys, values);

                    m_Dependencies = new Dictionary<string, ArtifactInfoDependency>();
                    for (int i = 0; i < keys.Length; ++i)
                    {
                        m_Dependencies.Add(keys[i], values[i]);
                    }
                }

                return m_Dependencies;
            }
        }

        private int m_DependencyCount = 0;

        internal int dependencyCount
        {
            get
            {
                if (!IsValid())
                {
                    Debug.LogError("ArtifactInfo has not been initialized. Please ensure its creation is done via AssetDatabase.GetArtifactInfos");
                    return -1;
                }

                if (m_DependencyCount == 0)
                    m_DependencyCount = GetDependencyCount_Internal(this);
                return m_DependencyCount;
            }
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(ArtifactInfo artifactInfo) => artifactInfo.m_Ptr;
        }
    }
}
