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
        public string name { get; }
        public string displayName { get; }
        public VersionsInfo availableVersions { get; }
        public bool isDiscoverable => mainSearchInfo != null;
        public bool isDeprecated { get; }
        public string deprecationMessage { get; }
        public PackageCompliance compliance { get; }

        private readonly Dictionary<string, PackageInfo> m_ExtraSearchInfos;

        public UpmPackageData(PackageInfo installedInfo, long installedInfoTimestamp, PackageInfo mainSearchInfo, long searchInfoTimestamp, bool loadAllVersions, Dictionary<string, PackageInfo> extraInfos)
        {
            if (installedInfo == null && mainSearchInfo == null)
                throw new ArgumentException("InstalledInfo and mainSearchInfo cannot both be null.");

            this.installedInfo = installedInfo;
            this.mainSearchInfo = mainSearchInfo;
            this.loadAllVersions = loadAllVersions;

            m_ExtraSearchInfos = extraInfos;

            var searchInfoFirst = mainSearchInfo ?? installedInfo;
            var installedInfoFirst = installedInfo ?? mainSearchInfo;

            var newerInfo = installedInfoTimestamp > searchInfoTimestamp ? installedInfoFirst : searchInfoFirst;

            name = newerInfo.name;
            availableVersions = newerInfo.versions;
            compliance = newerInfo.compliance;

            availableRegistryType = searchInfoFirst.GetAvailableRegistryType();
            isDeprecated = searchInfoFirst.isDeprecated;
            deprecationMessage = searchInfoFirst.deprecationMessage;

            displayName = installedInfoFirst.displayName;
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
