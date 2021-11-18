// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageUnlockButton : PackageToolBarRegularButton
    {
        private PageManager m_PageManager;
        public PackageUnlockButton(PageManager pageManager)
        {
            m_PageManager = pageManager;
        }

        protected override bool TriggerAction(IList<IPackageVersion> versions)
        {
            m_PageManager.SetPackagesUserUnlockedState(versions.Select(v => v.packageUniqueId), true);
            return true;
        }

        protected override bool TriggerAction(IPackageVersion version)
        {
            m_PageManager.SetPackagesUserUnlockedState(new string[1] { version.packageUniqueId }, true);
            PackageManagerWindowAnalytics.SendEvent("unlock", version.packageUniqueId);
            return true;
        }

        protected override bool IsVisible(IPackageVersion version) => m_PageManager.GetVisualState(version?.package)?.isLocked == true;

        protected override string GetTooltip(IPackageVersion version, bool isInProgress)
        {
            return L10n.Tr("Unlock to make changes");
        }

        protected override string GetText(IPackageVersion version, bool isInProgress)
        {
            return L10n.Tr("Unlock");
        }

        protected override bool IsInProgress(IPackageVersion version) => false;
    }
}
