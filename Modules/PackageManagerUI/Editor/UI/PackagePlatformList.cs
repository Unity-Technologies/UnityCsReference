// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackagePlatformList : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackagePlatformList> {}

        private UpmCache m_UpmCache;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_UpmCache = container.Resolve<UpmCache>();
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

            var packageInfo = version != null ? m_UpmCache.GetBestMatchPackageInfo(version.name, version.isInstalled, version.versionString) : null;
            if (packageInfo == null)
                return;

            var upmReserved = m_UpmCache.ParseUpmReserved(packageInfo);
            var platformNames = upmReserved?.GetList<string>("supportedPlatforms") ?? Enumerable.Empty<string>();

            if (!platformNames.Any())
                return;

            UIUtils.SetElementDisplay(this, true);

            var listLabel = platformNames.Count() > 1 ? L10n.Tr("Supported Platforms:") : L10n.Tr("Supported Platform:");
            m_TagLabelList.Refresh(listLabel, platformNames);
        }

        private TagLabelList m_TagLabelList;
    }
}
