// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class PackageInfoDictionary : ISerializationCallbackReceiver
    {
        private Dictionary<string, PackageInfo> m_ByName = new();
        private readonly Dictionary<long, PackageInfo> m_ByProductId = new();
        private Dictionary<string, long> m_TimestampByName = new();

        [SerializeField]
        private PackageInfo[] m_Serialized = Array.Empty<PackageInfo>();

        [SerializeField]
        private long[] m_SerializedTimestamps = Array.Empty<long>();

        [SerializeField]
        private long m_Timestamp = -1;

        public long timestamp => m_Timestamp;
        public IReadOnlyCollection<PackageInfo> values => m_ByName.Values;

        public PackageInfo GetByName(string packageName) => m_ByName.GetValueOrDefault(packageName);
        public PackageInfo GetByProductId(long productId) => m_ByProductId.GetValueOrDefault(productId);

        public (PackageInfo info, long timestamp)? GetByNameWithTimestamp(string packageName)
        {
            var info = m_ByName.GetValueOrDefault(packageName);
            return info == null ? null : (info, m_TimestampByName.GetValueOrDefault(packageName, m_Timestamp));
        }

        public List<(PackageInfo oldInfo, PackageInfo newInfo)> ReplaceAll(IEnumerable<PackageInfo> packageInfos, long timestamp)
        {
            m_Timestamp = timestamp;
            m_TimestampByName.Clear();
            var old = m_ByName;
            m_ByName = packageInfos.ToNewDictionary(p => p.name);
            RebuildByProductId();
            return FindUpdatedPackageInfos(old, m_ByName);
        }

        public (PackageInfo oldInfo, PackageInfo newInfo)? AddOrUpdate(string packageName, PackageInfo packageInfo, long timestamp)
        {
            var oldInfo = m_ByName.GetValueOrDefault(packageName);
            var oldProductId = oldInfo?.ParseProductId() ?? 0;
            if (oldProductId > 0)
                m_ByProductId.Remove(oldProductId);
            m_ByName[packageName] = packageInfo;
            m_TimestampByName[packageName] = timestamp;
            var newProductId = packageInfo.ParseProductId();
            if (newProductId > 0)
                m_ByProductId[newProductId] = packageInfo;
            if (oldInfo == null || IsDifferent(oldInfo, packageInfo))
                return (oldInfo, packageInfo);
            return null;
        }

        private static List<(PackageInfo oldInfo, PackageInfo newInfo)> FindUpdatedPackageInfos(Dictionary<string, PackageInfo> oldInfos, Dictionary<string, PackageInfo> newInfos)
        {
            var result = new List<(PackageInfo oldInfo, PackageInfo newInfo)>();
            foreach (var oldInfo in oldInfos.Values)
            {
                if (newInfos.TryGetValue(oldInfo.name, out var newInfo) && !IsDifferent(oldInfo, newInfo))
                    continue;
                result.Add((oldInfo, newInfo));
            }
            foreach (var newInfo in newInfos.Values.Filter(p => !oldInfos.ContainsKey(p.name)))
                result.Add((null, newInfo));
            return result;
        }

        // For BuiltIn and Registry packages, we want to only compare a subset of PackageInfo attributes,
        // as most attributes never change if their PackageId is the same. For other types of packages, always consider them different
        private static bool IsDifferent(PackageInfo p1, PackageInfo p2)
        {
            if (p1.packageId != p2.packageId ||
                p1.isDirectDependency != p2.isDirectDependency ||
                p1.version != p2.version ||
                p1.source != p2.source ||
                p1.resolvedPath != p2.resolvedPath ||
                p1.entitlements.isAllowed != p2.entitlements.isAllowed ||
                p1.entitlements.licensingModel != p2.entitlements.licensingModel ||
                p1.versions.recommended != p2.versions.recommended ||
                !p1.versions.compatible.IsSequenceEqual(p2.versions.compatible) ||
                !p1.versions.all.IsSequenceEqual(p2.versions.all) ||
                !p1.errors.IsSequenceEqual(p2.errors) ||
                !p1.dependencies.IsSequenceEqual(p2.dependencies) ||
                !p1.resolvedDependencies.IsSequenceEqual(p2.resolvedDependencies) ||
                p1.projectDependenciesEntry != p2.projectDependenciesEntry ||
                p1.signature.status != p2.signature.status ||
                p1.trustLevel != p2.trustLevel ||
                p1.documentationUrl != p2.documentationUrl ||
                p1.changelogUrl != p2.changelogUrl ||
                p1.licensesUrl != p2.licensesUrl ||
                p1.assetStore?.productId != p2.assetStore?.productId ||
                !p1.registry.IsEquivalentTo(p2.registry) ||
                !p1.compliance.IsEquivalentTo(p2.compliance))
                return true;

            if (p1.source is PackageSource.BuiltIn or PackageSource.Registry)
                return false;

            if (p1.source == PackageSource.Git)
                return p1.git.hash != p2.git?.hash || p1.git.revision != p2.git?.revision;

            return true;
        }

        public void Clear()
        {
            m_ByName.Clear();
            m_ByProductId.Clear();
            m_TimestampByName.Clear();
            m_Serialized = Array.Empty<PackageInfo>();
            m_SerializedTimestamps = Array.Empty<long>();
            m_Timestamp = -1;
        }

        private void RebuildByProductId()
        {
            m_ByProductId.Clear();
            foreach (var info in m_ByName.Values)
            {
                var productId = info.ParseProductId();
                if (productId > 0)
                    m_ByProductId[productId] = info;
            }
        }

        public void OnBeforeSerialize()
        {
            m_ByName.Values.ToArray(ref m_Serialized);
            m_SerializedTimestamps = m_TimestampByName.Count > 0
                ? m_Serialized.SelectToNewArray(p => m_TimestampByName.GetValueOrDefault(p.name))
                : Array.Empty<long>();
        }

        public void OnAfterDeserialize()
        {
            m_Serialized.ToDictionary(p => p.name, ref m_ByName);
            m_TimestampByName = new Dictionary<string, long>();
            for (var i = 0; i < m_SerializedTimestamps.Length && i < m_Serialized.Length; i++)
                m_TimestampByName[m_Serialized[i].name] = m_SerializedTimestamps[i];
            RebuildByProductId();
        }
    }
}
