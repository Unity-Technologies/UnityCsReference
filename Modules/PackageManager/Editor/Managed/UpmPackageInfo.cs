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
    public class UpmPackageInfo
    {
        [SerializeField]
        [NativeName("packageId")]
        private string m_PackageId;
        [SerializeField]
        [NativeName("tag")]
        private string m_Tag;
        [SerializeField]
        [NativeName("version")]
        private string m_Version;
        [SerializeField]
        [NativeName("originType")]
        private OriginType m_OriginType;
        [SerializeField]
        [NativeName("originLocation")]
        private string m_OriginLocation;
        [SerializeField]
        [NativeName("relationType")]
        private RelationType m_RelationType;
        [SerializeField]
        [NativeName("resolvedPath")]
        private string m_ResolvedPath;
        [SerializeField]
        [NativeName("name")]
        private string m_Name;
        [SerializeField]
        [NativeName("displayName")]
        private string m_DisplayName;
        [SerializeField]
        [NativeName("category")]
        private string m_Category;
        [SerializeField]
        [NativeName("description")]
        private string m_Description;
        [SerializeField]
        [NativeName("status")]
        private PackageStatus m_Status;
        [SerializeField]
        [NativeName("errors")]
        private Error[] m_Errors;

        private UpmPackageInfo() {}

        internal UpmPackageInfo(
            string packageId,
            string displayName = "",
            string category = "",
            string description = "",
            string resolvedPath = "",
            string tag = "",
            PackageStatus status = PackageStatus.Unavailable,
            IEnumerable<Error> errors = null)
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
            m_Status = status;
            m_Errors = (errors ?? new Error[] {}).ToArray();

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
        public PackageStatus status { get { return m_Status;  } }
        public Error[] errors { get { return m_Errors;  } }
    }
}

