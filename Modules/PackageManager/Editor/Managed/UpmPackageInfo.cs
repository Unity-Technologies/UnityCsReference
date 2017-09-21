// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.PackageManager
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public class UpmPackageInfo
    {
        [SerializeField]
        private string m_PackageId;
        [SerializeField]
        private string m_Tag;
        [SerializeField]
        private string m_Version;
        [SerializeField]
        private OriginType m_OriginType;
        [SerializeField]
        private string m_OriginLocation;
        [SerializeField]
        private RelationType m_RelationType;
        [SerializeField]
        private string m_ResolvedPath;
        [SerializeField]
        private string m_Name;
        [SerializeField]
        private string m_DisplayName;
        [SerializeField]
        private string m_Category;
        [SerializeField]
        private string m_Description;

        private UpmPackageInfo() {}

        internal UpmPackageInfo(
            string packageId,
            string displayName = "",
            string category = "",
            string description = "",
            string resolvedPath = "",
            string tag = "")
        {
            // Set the default values
            m_OriginType = OriginType.Unknown;
            m_RelationType = RelationType.Unknown;
            m_Tag = tag;
            m_OriginLocation = "not implemented";
            m_PackageId = packageId;
            m_DisplayName = displayName;
            m_Category = category;
            m_Description = description;
            m_ResolvedPath = resolvedPath;

            // Populate name and version
            var nameAndVersion = packageId.Split('@');
            m_Name = nameAndVersion[0];
            m_Version = nameAndVersion[1];
        }

        public string packageId { get { return m_PackageId;  } }
        public string tag { get { return m_Tag;  } }
        public string version { get { return m_Version;  } }
        public OriginType originType { get { return m_OriginType;  } }
        public string originLocation { get { return m_OriginLocation;  } }
        public RelationType relationType { get { return m_RelationType;  } }
        public string resolvedPath { get { return m_ResolvedPath;  } }
        public string name { get { return m_Name;  } }
        public string displayName { get { return m_DisplayName;  } }
        public string category { get { return m_Category;  } }
        public string description { get { return m_Description;  } }
    }
}

