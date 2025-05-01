// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    // A ScopedRegistryPage is basically a special case MyRegistriesPage, where only packages from a specified registry is shown
    // hence we are using MyRegistriesPage as the parent class and reuse some implementations there.
    [Serializable]
    internal class ScopedRegistryPage : MyRegistriesPage
    {
        public const string k_IdPrefix = "ScopedRegistry";

        public static string GetIdFromRegistry(RegistryInfo registryInfo) => $"{k_IdPrefix}/{registryInfo.id}";

        [SerializeField]
        private RegistryInfo m_RegistryInfo;
        public override RegistryInfo scopedRegistry  => m_RegistryInfo;

        public override string id => GetIdFromRegistry(m_RegistryInfo);
        public override string displayName => m_RegistryInfo.name;

        [NonSerialized]
        private IUpmCache m_UpmCache;
        public void ResolveDependencies(IPackageDatabase packageDatabase, IUpmCache upmCache)
        {
            ResolveDependencies(packageDatabase);
            m_UpmCache = upmCache;
        }

        public ScopedRegistryPage(IPackageDatabase packageDatabase, IUpmCache upmCache, RegistryInfo registryInfo)
            : base(packageDatabase)
        {
            ResolveDependencies(packageDatabase, upmCache);
            m_RegistryInfo = registryInfo;
        }

        public void UpdateRegistry(RegistryInfo registryInfo)
        {
            if (m_RegistryInfo.IsEquivalentTo(registryInfo))
                return;
            m_RegistryInfo = registryInfo;
            RebuildVisualStatesAndUpdateVisibilityWithSearchText();
        }

        public override bool ShouldInclude(IPackage package)
        {
            return base.ShouldInclude(package) && package.versions.Any(v =>
            {
                var packageInfo = m_UpmCache.GetBestMatchPackageInfo(v.name, v.isInstalled);
                return packageInfo?.registry != null && m_RegistryInfo.IsEquivalentTo(packageInfo.registry);
            });
        }
    }
}
