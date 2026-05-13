// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    [UxmlElement]
    internal partial class PackagePlatformList : VisualElement
    {
        private readonly IUpmCache m_UpmCache;

        public PackagePlatformList() : this(ServicesContainer.instance.Resolve<IUpmCache>())
        {
        }

        public PackagePlatformList(IUpmCache upmCache)
        {
            m_UpmCache = upmCache;

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
            var platformNames = upmReserved?.GetNewArray<string>("supportedPlatforms") ?? Array.Empty<string>();

            if (platformNames.Length == 0)
                return;

            UIUtils.SetElementDisplay(this, true);

            var listLabel = platformNames.Length > 1 ? L10n.Tr("Supported Platforms:") : L10n.Tr("Supported Platform:");
            m_TagLabelList.Refresh(listLabel, platformNames);
        }

        private readonly TagLabelList m_TagLabelList;
    }
}
