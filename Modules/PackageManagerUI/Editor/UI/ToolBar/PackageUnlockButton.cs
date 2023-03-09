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
            m_PageManager.activePage.SetPackagesUserUnlockedState(versions.Select(v => v.package.uniqueId), true);
            PackageManagerWindowAnalytics.SendEvent("unlock", packageIds: versions.Select(v => v.package.uniqueId));
            return true;
        }

        protected override bool TriggerAction(IPackageVersion version)
        {
            m_PageManager.activePage.SetPackagesUserUnlockedState(new string[1] { version.package.uniqueId }, true);
            PackageManagerWindowAnalytics.SendEvent("unlock", version.package.uniqueId);
            return true;
        }

        protected override bool IsVisible(IPackageVersion version) => m_PageManager.activePage.visualStates.Get(version?.package?.uniqueId)?.isLocked == true;

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
