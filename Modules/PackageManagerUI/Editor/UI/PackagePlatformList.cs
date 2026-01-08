// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackagePlatformList : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new PackagePlatformList();
        }

        private IUpmCache m_UpmCache;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_UpmCache = container.Resolve<IUpmCache>();
        }

        public PackagePlatformList()
        {
            ResolveDependencies();
            m_TagLabelList = new TagLabelList();
            Add(m_TagLabelList);
        }

        public void Refresh(IPackageVersion version)
        {
            UIUtils.SetElementDisplay(this, false);

            var packageInfo = version != null ? m_UpmCache.GetBestMatchPackageInfo(version.name, version.package.product?.id ?? 0, version.isInstalled, version.versionString) : null;
            if (packageInfo == null)
                return;

            var upmReserved = m_UpmCache.ParseUpmReserved(packageInfo);
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var platformNames = upmReserved?.GetList<string>("supportedPlatforms") ?? Enumerable.Empty<string>();
#pragma warning restore RS0030

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (!platformNames.Any())
#pragma warning restore RS0030
                return;

            UIUtils.SetElementDisplay(this, true);

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var listLabel = platformNames.Count() > 1 ? L10n.Tr("Supported Platforms:") : L10n.Tr("Supported Platform:");
#pragma warning restore RS0030
            m_TagLabelList.Refresh(listLabel, platformNames);
        }

        private TagLabelList m_TagLabelList;
    }
}
