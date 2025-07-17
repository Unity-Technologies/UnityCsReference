// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IUpmPackageData
    {
        PackageInfo mainSearchInfo { get; }
        PackageInfo installedInfo { get; }
        RegistryType availableRegistryType { get; }
        bool loadAllVersions { get; }
        string name { get; }
        string displayName { get; }
        VersionsInfo availableVersions { get; }
        bool isDiscoverable { get; }
        bool isDeprecated { get; }
        string deprecationMessage { get; }
        PackageCompliance compliance { get; }
        PackageInfo GetSearchInfo(string version);
        bool IsVersionDeprecated(string version);
    }


    internal class UpmPackageData : IUpmPackageData
    {
        public PackageInfo mainSearchInfo { get; }
        public PackageInfo installedInfo { get; }
        public RegistryType availableRegistryType { get; }
        public bool loadAllVersions { get; }
        public string name => m_NewestInfo.name;
        public string displayName => m_NewestInfo.displayName;
        public VersionsInfo availableVersions => m_NewestInfo.versions;
        public bool isDiscoverable => mainSearchInfo != null;
        public bool isDeprecated => m_NewestInfo.unityLifecycle?.isDeprecated ?? false;
        public string deprecationMessage => isDeprecated ? m_NewestInfo.unityLifecycle.deprecationMessage : null;
        public PackageCompliance compliance => m_NewestInfo.compliance;

        private readonly Dictionary<string, PackageInfo> m_ExtraSearchInfos;
        private readonly PackageInfo m_NewestInfo;

        public UpmPackageData(PackageInfo installedInfo, long installedInfoTimestamp, PackageInfo mainSearchInfo, long searchInfoTimestamp, bool loadAllVersions, Dictionary<string, PackageInfo> extraInfos)
        {
            this.installedInfo = installedInfo;
            this.mainSearchInfo = mainSearchInfo;
            this.loadAllVersions = loadAllVersions;

            m_ExtraSearchInfos = extraInfos;
            m_NewestInfo = installedInfoTimestamp > searchInfoTimestamp ? installedInfo ?? mainSearchInfo : mainSearchInfo ?? installedInfo;
            availableRegistryType = m_NewestInfo.GetAvailableRegistryType();
        }

        public PackageInfo GetSearchInfo(string version)
        {
            if (string.IsNullOrEmpty(version))
                return null;
            return mainSearchInfo?.version == version ? mainSearchInfo : m_ExtraSearchInfos?.GetValueOrDefault(version);
        }

        public bool IsVersionDeprecated(string version) => Array.IndexOf(availableVersions.deprecated, version) >= 0;
    }
}
