// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageRemoveCustomButton : PackageToolBarRegularButton
    {
        private ApplicationProxy m_Application;
        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        public PackageRemoveCustomButton(ApplicationProxy applicationProxy,
                                   PackageDatabase packageDatabase,
                                   PageManager pageManager)
        {
            m_Application = applicationProxy;
            m_PackageDatabase = packageDatabase;
            m_PageManager = pageManager;
        }

        protected override bool TriggerAction(IPackageVersion version)
        {
            if (version.HasTag(PackageTag.Custom))
            {
                if (!m_Application.DisplayDialog("removeEmbeddedPackage", L10n.Tr("Removing package in development"), L10n.Tr("You will lose all your changes (if any) if you delete a package in development. Are you sure?"), L10n.Tr("Yes"), L10n.Tr("No")))
                    return false;

                m_PackageDatabase.RemoveEmbedded(version.package);
                PackageManagerWindowAnalytics.SendEvent("removeEmbedded", version.uniqueId);
                return true;
            }

            return false;
        }

        protected override bool IsVisible(IPackageVersion version)
        {
            var installed = version?.package.versions.installed;
            return installed != null
                && version.HasTag(PackageTag.Removable)
                && version.HasTag(PackageTag.Custom)
                && (installed == version || version.IsRequestedButOverriddenVersion);
        }

        protected override string GetTooltip(IPackageVersion version, bool isInProgress)
        {
            if (isInProgress)
                return k_InProgressGenericTooltip;
            return string.Format(L10n.Tr("Click to remove this {0} from your project."), version.package.GetDescriptor());
        }

        protected override string GetText(IPackageVersion version, bool isInProgress)
        {
            return isInProgress ? L10n.Tr("Removing") : L10n.Tr("Remove");
        }

        protected override bool IsInProgress(IPackageVersion version) => m_PackageDatabase.IsUninstallInProgress(version.package);

        private void DeselectVersions(IList<IPackageVersion> versions)
        {
            m_PageManager.RemoveSelection(versions.Select(v => new PackageAndVersionIdPair(v.packageUniqueId, v.uniqueId)));
        }
    }
}
