// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Collections.Generic;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageRemoveButton : PackageToolBarRegularButton
    {
        private ApplicationProxy m_Application;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private PackageDatabase m_PackageDatabase;
        private PackageOperationDispatcher m_OperationDispatcher;
        private PageManager m_PageManager;
        public PackageRemoveButton(ApplicationProxy applicationProxy,
                                   PackageManagerPrefs packageManagerPrefs,
                                   PackageDatabase packageDatabase,
                                   PackageOperationDispatcher operationDispatcher,
                                   PageManager pageManager)
        {
            m_Application = applicationProxy;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_PackageDatabase = packageDatabase;
            m_OperationDispatcher = operationDispatcher;
            m_PageManager = pageManager;
        }

        protected override bool TriggerAction(IList<IPackageVersion> versions)
        {
            var isModules = versions.FirstOrDefault()?.HasTag(PackageTag.BuiltIn) == true;
            var title = string.Format(L10n.Tr(isModules ? "Disabling {0} items" : "Removing {0} items"), versions.Count);

            var result = 0;
            if (!m_PackageManagerPrefs.skipMultiSelectRemoveConfirmation)
            {
                var message = L10n.Tr(isModules ? "Are you sure you want to disable these items?" : "Are you sure you want to remove these items?");
                result = m_Application.DisplayDialogComplex("removeMultiplePackages", title, message, L10n.Tr(isModules ? "Disable" : "Remove"), L10n.Tr("Cancel"), L10n.Tr("Never ask"));
            }

            // Cancel
            if (result == 1)
                return false;

            // Never ask
            if (result == 2)
                m_PackageManagerPrefs.skipMultiSelectRemoveConfirmation = true;

            m_OperationDispatcher.Uninstall(versions.Select(v => v.package));
            PackageManagerWindowAnalytics.SendEvent("uninstall", packageIds: versions.Select(v => v.uniqueId));
            // After a bulk removal, we want to deselect them to avoid installing them back by accident.
            DeselectVersions(versions);
            return true;
        }

        protected override bool TriggerAction(IPackageVersion version)
        {
            var result = 0;
            if (version.HasTag(PackageTag.BuiltIn))
            {
                if (!m_PackageManagerPrefs.skipDisableConfirmation)
                {
                    result = m_Application.DisplayDialogComplex("disableBuiltInPackage",
                        L10n.Tr("Disable Built-In Package"),
                        L10n.Tr("Are you sure you want to disable this built-in package?"),
                        L10n.Tr("Disable"), L10n.Tr("Cancel"), L10n.Tr("Never ask"));
                }
            }
            else
            {
                var isPartOfFeature = m_PackageDatabase.GetFeaturesThatUseThisPackage(version).Any(featureSet => featureSet.isInstalled);
                if (isPartOfFeature || !m_PackageManagerPrefs.skipRemoveConfirmation)
                {
                    var descriptor = version.GetDescriptor();
                    var title = string.Format(L10n.Tr("Removing {0}"), descriptor);
                    if (isPartOfFeature)
                    {
                        var message = string.Format(L10n.Tr("Are you sure you want to remove this {0} that is used by at least one installed feature?"), descriptor);
                        var removeIt = m_Application.DisplayDialog("removePackagePartOfFeature", title, message, L10n.Tr("Remove"), L10n.Tr("Cancel"));
                        result = removeIt ? 0 : 1;
                    }
                    else
                    {
                        var message = string.Format(L10n.Tr("Are you sure you want to remove this {0}?"), descriptor);
                        result = m_Application.DisplayDialogComplex("removePackage", title, message, L10n.Tr("Remove"), L10n.Tr("Cancel"), L10n.Tr("Never ask"));
                    }
                }
            }

            // Cancel
            if (result == 1)
                return false;

            // Do not ask again
            if (result == 2)
            {
                if (version.HasTag(PackageTag.BuiltIn))
                    m_PackageManagerPrefs.skipDisableConfirmation = true;
                else
                    m_PackageManagerPrefs.skipRemoveConfirmation = true;
            }

            // If the user is removing a package that is part of a feature set, lock it after removing from manifest
            // Having this check condition should be more optimal once we implement caching of Feature Set Dependents for each package
            if (m_PackageDatabase.GetFeaturesThatUseThisPackage(version.package.versions.installed)?.Any() == true)
                m_PageManager.activePage.SetPackagesUserUnlockedState(new List<string> { version.package.uniqueId }, false);

            // Remove
            m_OperationDispatcher.Uninstall(version.package);
            PackageManagerWindowAnalytics.SendEvent("uninstall", version?.uniqueId);
            return true;
        }

        protected override bool IsVisible(IPackageVersion version)
        {
            var installed = version?.package.versions.installed;
            return installed != null
                && version.HasTag(PackageTag.UpmFormat)
                && !version.HasTag(PackageTag.Placeholder | PackageTag.Custom)
                && (installed == version || version.IsRequestedButOverriddenVersion);
        }

        protected override string GetTooltip(IPackageVersion version, bool isInProgress)
        {
            if (isInProgress)
                return k_InProgressGenericTooltip;
            if (version?.HasTag(PackageTag.BuiltIn) == true)
                return string.Format(L10n.Tr("Disable the use of this {0} in your project."), version.GetDescriptor());
            return string.Format(L10n.Tr("Click to remove this {0} from your project."), version.GetDescriptor());
        }

        protected override string GetText(IPackageVersion version, bool isInProgress)
        {
            if (version?.HasTag(PackageTag.BuiltIn) == true)
                return isInProgress ? L10n.Tr("Disabling") : L10n.Tr("Disable");
            return isInProgress ? L10n.Tr("Removing") : L10n.Tr("Remove");
        }

        protected override IEnumerable<ButtonDisableCondition> GetDisableConditions(IPackageVersion version)
        {
            var isInstalledAsDependency = version.package.versions.installed == version
                && (!version.isDirectDependency || version.IsDifferentVersionThanRequested) && !version.isInvalidSemVerInManifest;
            yield return new ButtonDisableCondition(isInstalledAsDependency,
                string.Format(L10n.Tr("You cannot remove this {0} because another installed package or feature depends on it. See dependencies for more details."), version.GetDescriptor()));
        }

        protected override bool IsInProgress(IPackageVersion version) => m_OperationDispatcher.IsUninstallInProgress(version.package);

        private void DeselectVersions(IList<IPackageVersion> versions)
        {
            m_PageManager.activePage.RemoveSelection(versions.Select(v => new PackageAndVersionIdPair(v.package.uniqueId, v.uniqueId)));
        }
    }
}
