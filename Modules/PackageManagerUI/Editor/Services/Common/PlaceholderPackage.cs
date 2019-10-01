// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class PlaceholderPackage : BasePackage
    {
        [SerializeField]
        private string m_UniqueId;
        public override string uniqueId => m_UniqueId;

        [SerializeField]
        private PlaceholderVersionList m_VersionList;

        public override IVersionList versions => m_VersionList;

        public PlaceholderPackage(string uniqueId, PackageType type = PackageType.None, PackageTag tag = PackageTag.None, PackageProgress progress = PackageProgress.None)
        {
            m_Type = type;
            m_UniqueId = uniqueId;
            m_Progress = progress;
            m_VersionList = new PlaceholderVersionList(new PlaceholderPackageVersion(uniqueId, uniqueId, tag));
        }

        public override IPackage Clone()
        {
            return (IPackage)MemberwiseClone();
        }
    }
}
