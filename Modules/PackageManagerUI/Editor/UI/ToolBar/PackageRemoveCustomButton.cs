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
        private PackageOperationDispatcher m_OperationDispatcher;
        private PageManager m_PageManager;
        public PackageRemoveCustomButton(ApplicationProxy applicationProxy,
                                   PackageOperationDispatcher operationDispatcher,
                                   PageManager pageManager)
        {
            m_Application = applicationProxy;
            m_OperationDispatcher = operationDispatcher;
            m_PageManager = pageManager;
        }

        protected override bool TriggerAction(IPackageVersion version)
        {
            if (version.HasTag(PackageTag.Custom))
            {
                if (!m_Application.DisplayDialog("removeEmbeddedPackage", L10n.Tr("Removing package in development"), L10n.Tr("You will lose all your changes (if any) if you delete a package in development. Are you sure?"), L10n.Tr("Yes"), L10n.Tr("No")))
                    return false;

                m_OperationDispatcher.RemoveEmbedded(version.package);
                PackageManagerWindowAnalytics.SendEvent("removeEmbedded", version.uniqueId);
                return true;
            }

            return false;
        }

        protected override bool IsVisible(IPackageVersion version)
        {
            var installed = version?.package.versions.installed;
            return installed != null
                && version.HasTag(PackageTag.UpmFormat)
                && version.HasTag(PackageTag.Custom)
                && (installed == version || version.IsRequestedButOverriddenVersion);
        }

        protected override string GetTooltip(IPackageVersion version, bool isInProgress)
        {
            if (isInProgress)
                return k_InProgressGenericTooltip;
            return string.Format(L10n.Tr("Click to remove this {0} from your project."), version.GetDescriptor());
        }

        protected override string GetText(IPackageVersion version, bool isInProgress)
        {
            return isInProgress ? L10n.Tr("Removing") : L10n.Tr("Remove");
        }

        protected override bool IsInProgress(IPackageVersion version) => m_OperationDispatcher.IsUninstallInProgress(version.package);

        private void DeselectVersions(IList<IPackageVersion> versions)
        {
            m_PageManager.activePage.RemoveSelection(versions.Select(v => new PackageAndVersionIdPair(v.package.uniqueId, v.uniqueId)));
        }
    }
}
