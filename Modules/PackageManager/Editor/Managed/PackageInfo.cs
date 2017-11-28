// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager
{
    [Serializable]
    public class PackageInfo
    {
        [SerializeField]
        private UpmPackageInfo m_UpmPackageInfo;

        private PackageInfo() {}

        internal PackageInfo(UpmPackageInfo upmPackageInfo)
        {
            m_UpmPackageInfo = upmPackageInfo;
        }

        public string packageId { get { return m_UpmPackageInfo.packageId;  } }
        public string version { get { return m_UpmPackageInfo.version;  } }
        public string resolvedPath { get { return m_UpmPackageInfo.resolvedPath;  } }
        public string name { get { return m_UpmPackageInfo.name;  } }
        public string displayName { get { return m_UpmPackageInfo.displayName;  } }
        public string category { get { return m_UpmPackageInfo.category;  } }
        public string description { get { return m_UpmPackageInfo.description;  } }
    }
}

