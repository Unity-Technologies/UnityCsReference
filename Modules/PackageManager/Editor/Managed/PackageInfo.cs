// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.PackageManager
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    [NativeType(IntermediateScriptingStructName = "PackageManager_PackageInfo")]
    public class PackageInfo
    {
        [SerializeField]
        [NativeName("packageId")]
        private string m_PackageId = "";

        [SerializeField]
        [NativeName("isRootDependency")]
        private bool m_IsRootDependency = false;

        [SerializeField]
        [NativeName("version")]
        private string m_Version = "";

        [SerializeField]
        [NativeName("source")]
        private PackageSource m_Source = PackageSource.Unknown;

        [SerializeField]
        [NativeName("resolvedPath")]
        private string m_ResolvedPath = "";

        [SerializeField]
        [NativeName("assetPath")]
        private string m_AssetPath = "";

        [SerializeField]
        [NativeName("name")]
        private string m_Name = "";

        [SerializeField]
        [NativeName("displayName")]
        private string m_DisplayName = "";

        [SerializeField]
        [NativeName("category")]
        private string m_Category = "";

        [SerializeField]
        [NativeName("type")]
        private string m_Type = "";

        [SerializeField]
        [NativeName("description")]
        private string m_Description = "";

        [SerializeField]
        [NativeName("status")]
        private PackageStatus m_Status = PackageStatus.Unknown;

        [SerializeField]
        [NativeName("errors")]
        private Error[] m_Errors = new Error[0];

        [SerializeField]
        [NativeName("versions")]
        private VersionsInfo m_Versions = new VersionsInfo(null, null, null);

        [SerializeField]
        [NativeName("dependencies")]
        private DependencyInfo[] m_Dependencies = new DependencyInfo[0];

        [SerializeField]
        [NativeName("resolvedDependencies")]
        private DependencyInfo[] m_ResolvedDependencies = new DependencyInfo[0];

        [SerializeField]
        [NativeName("keywords")]
        private string[] m_Keywords = new string[0];

        [SerializeField]
        [NativeName("author")]
        private AuthorInfo m_Author = new AuthorInfo();

        private PackageInfo() {}

        public string packageId { get { return m_PackageId;  } }
        internal bool isRootDependency { get { return m_IsRootDependency;  } }
        public string version { get { return m_Version;  } }
        public PackageSource source { get { return m_Source;  } }
        public string resolvedPath { get { return m_ResolvedPath;  } }
        public string assetPath { get { return m_AssetPath;  } }
        public string name { get { return m_Name;  } }
        public string displayName { get { return m_DisplayName;  } }
        public string category { get { return m_Category;  } }
        internal string type { get { return m_Type;  } }
        public string description { get { return m_Description;  } }
        public PackageStatus status { get { return m_Status;  } }
        public Error[] errors { get { return m_Errors;  } }
        public VersionsInfo versions { get { return m_Versions; } }
        public DependencyInfo[] dependencies { get { return m_Dependencies; } }
        public DependencyInfo[] resolvedDependencies { get { return m_ResolvedDependencies; } }
        public string[] keywords { get { return m_Keywords;  } }
        public AuthorInfo author { get { return m_Author;  } }
    }
}
