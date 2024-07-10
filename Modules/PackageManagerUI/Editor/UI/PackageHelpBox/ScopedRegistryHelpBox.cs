// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class ScopedRegistryHelpBox : PackageHelpBoxWithReadMore
    {
        private readonly IUpmCache m_UpmCache;
        public ScopedRegistryHelpBox(IApplicationProxy application, IUpmCache upmCache) : base(application)
        {
            m_UpmCache = upmCache;
            text = L10n.Tr("This package is hosted on a Scoped Registry.");
            messageType = HelpBoxMessageType.Info;
            m_ReadMoreUrl = $"https://docs.unity3d.com/{m_Application.shortUnityVersion}/Documentation/Manual/upm-scoped.html";
        }

        public override void Refresh(IPackageVersion version)
        {
            if (version == null || version.package.product != null)
            {
                UIUtils.SetElementDisplay(this, false);
                return;
            }

            var packageInfo = m_UpmCache.GetBestMatchPackageInfo(version.name, version.isInstalled, version.versionString);
            UIUtils.SetElementDisplay(this, packageInfo?.registry is { isDefault: false });
        }
    }
}
