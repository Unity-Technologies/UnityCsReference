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
    [StructLayout(LayoutKind.Sequential)]
    internal struct ArtifactInfoImportStats
    {
        private string m_AssetPath;
        private string m_EditorRevision;
        private ulong m_ImportTimeMicroseconds;
        private long m_ImportedTimestamp;
        private string m_UserName;
        private string m_ImportIpAddress;

        public string assetPath { get { return m_AssetPath; } }
        public string editorRevision { get { return m_EditorRevision; } }
        public ulong importTimeMicroseconds { get { return m_ImportTimeMicroseconds; } }
        public long importedTimestamp { get { return m_ImportedTimestamp; } }
        public string userName { get { return m_UserName; } }
        public string importIpAddress { get { return m_ImportIpAddress; } }
    }

    [NativeHeader("Modules/AssetDatabase/Editor/V2/ArtifactInfo.h")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct ArtifactInfoProducedFiles
    {
        private string m_Extension;
        private string m_LibraryPath;

        public string extension { get { return m_Extension; } }
        public string libraryPath { get { return m_LibraryPath; } }
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
        private object m_Value;
        private ArtifactInfoDependencyType m_Type;

        public object value { get { return m_Value; } }
        public ArtifactInfoDependencyType type { get { return m_Type; } }
    }

    [NativeHeader("Modules/AssetDatabase/Editor/V2/ArtifactInfo.h")]
    [StructLayout(LayoutKind.Sequential)]
    internal class ArtifactInfo : IDisposable
    {
        private IntPtr m_Ptr;

        private string m_ArtifactID;

        internal ArtifactInfo()
        {
            m_Ptr = Internal_Create();
        }

        ~ArtifactInfo()
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

        private static extern IntPtr Internal_Create();

        private static extern void Internal_Destroy(IntPtr ptr);

        [FreeFunction("ArtifactInfoBindings::GetArtifactID_Internal")]
        private static extern string GetArtifactID_Internal(ArtifactInfo self);

        [FreeFunction("ArtifactInfoBindings::GetArtifactKey_Internal")]
        private static extern ArtifactKey GetArtifactKey_Internal(ArtifactInfo self);

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

        internal string artifactID
        {
            get
            {
                m_ArtifactID = m_ArtifactID ?? GetArtifactID_Internal(this);
                return m_ArtifactID;
            }
        }

        internal ArtifactKey artifactKey { get { return GetArtifactKey_Internal(this); } }
        internal bool isCurrentArtifact { get { return GetIsCurrentArtifact_Internal(this); } }

        internal ArtifactInfoImportStats importStats { get { return GetImportStats_Internal(this); } }
        internal ArtifactInfoProducedFiles[] producedFiles { get { return GetProducedFiles_Internal(this); } }

        private Dictionary<string, ArtifactInfoDependency> m_Dependencies;

        internal IDictionary<string, ArtifactInfoDependency> dependencies
        {
            get
            {
                if (artifactID == string.Empty)
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
    }
}
