// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDeselectButton : PackageToolBarRegularButton
    {
        private string m_AnalyticsEventName;

        private PageManager m_PageManager;
        public PackageDeselectButton(PageManager pageManager, string analyticsEventName = null)
        {
            m_PageManager = pageManager;
            m_AnalyticsEventName = analyticsEventName;
        }

        protected override bool TriggerAction(IList<IPackageVersion> versions)
        {
            m_PageManager.RemoveSelection(versions.Select(v => new PackageAndVersionIdPair(v.packageUniqueId, v.uniqueId)));
            if (!string.IsNullOrEmpty(m_AnalyticsEventName))
                PackageManagerWindowAnalytics.SendEvent(m_AnalyticsEventName, packageIds: versions.Select(v => v.packageUniqueId));
            return true;
        }

        protected override bool TriggerAction(IPackageVersion version)
        {
            m_PageManager.RemoveSelection(new[] { new PackageAndVersionIdPair(version.packageUniqueId, version.uniqueId) });
            return true;
        }

        protected override bool IsVisible(IPackageVersion version) => true;

        protected override string GetTooltip(IPackageVersion version, bool isInProgress)
        {
            return L10n.Tr("Click to deselect these items from the list.");
        }

        protected override string GetText(IPackageVersion version, bool isInProgress)
        {
            return L10n.Tr("Deselect");
        }

        protected override bool IsInProgress(IPackageVersion version) => false;
    }
}
