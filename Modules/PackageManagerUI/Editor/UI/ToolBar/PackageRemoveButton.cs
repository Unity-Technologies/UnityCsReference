// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageRemoveButton : PackageToolBarRegularButton
    {
        private ApplicationProxy m_Application;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        public PackageRemoveButton(ApplicationProxy applicationProxy,
                                   PackageManagerPrefs packageManagerPrefs,
                                   PackageDatabase packageDatabase,
                                   PageManager pageManager)
        {
            m_Application = applicationProxy;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_PackageDatabase = packageDatabase;
            m_PageManager = pageManager;
        }

        protected override bool TriggerAction()
        {
            if (m_Version.HasTag(PackageTag.Custom))
            {
                if (!m_Application.DisplayDialog(L10n.Tr("Unity Package Manager"), L10n.Tr("You will lose all your changes (if any) if you delete a package in development. Are you sure?"), L10n.Tr("Yes"), L10n.Tr("No")))
                    return false;

                m_PackageDatabase.RemoveEmbedded(m_Package);
                PackageManagerWindowAnalytics.SendEvent("removeEmbedded", m_Version.uniqueId);
                return true;
            }

            var result = 0;
            if (m_Version.HasTag(PackageTag.BuiltIn))
            {
                if (!m_PackageManagerPrefs.skipDisableConfirmation)
                {
                    result = m_Application.DisplayDialogComplex(L10n.Tr("Disable Built-In Package"),
                        L10n.Tr("Are you sure you want to disable this built-in package?"),
                        L10n.Tr("Disable"), L10n.Tr("Cancel"), L10n.Tr("Never ask"));
                }
            }
            else
            {
                var isPartOfFeature = m_PackageDatabase.GetFeatureDependents(m_Version).Any(featureSet => featureSet.isInstalled);
                if (isPartOfFeature || !m_PackageManagerPrefs.skipRemoveConfirmation)
                {
                    var descriptor = m_Package.GetDescriptor();
                    var title = string.Format(L10n.Tr("Removing {0}"), CultureInfo.InvariantCulture.TextInfo.ToTitleCase(descriptor));
                    if (isPartOfFeature)
                    {
                        var message = string.Format(L10n.Tr("Are you sure you want to remove this {0} that is used by at least one installed feature?"), descriptor);
                        var removeIt = m_Application.DisplayDialog(title, message, L10n.Tr("Remove"), L10n.Tr("Cancel"));
                        result = removeIt ? 0 : 1;
                    }
                    else
                    {
                        var message = string.Format(L10n.Tr("Are you sure you want to remove this {0}?"), descriptor);
                        result = m_Application.DisplayDialogComplex(title, message, L10n.Tr("Remove"), L10n.Tr("Cancel"), L10n.Tr("Never ask"));
                    }
                }
            }

            // Cancel
            if (result == 1)
                return false;

            // Do not ask again
            if (result == 2)
            {
                if (m_Version.HasTag(PackageTag.BuiltIn))
                    m_PackageManagerPrefs.skipDisableConfirmation = true;
                else
                    m_PackageManagerPrefs.skipRemoveConfirmation = true;
            }

            // If the user is removing a package that is part of a feature set, lock it after removing from manifest
            // Having this check condition should be more optimal once we implement caching of Feature Set Dependents for each package
            if (m_PackageDatabase.GetFeatureDependents(m_Package.versions.installed)?.Any() == true)
                m_PageManager.SetPackagesUserUnlockedState(new List<string> { m_Package.uniqueId }, false);

            // Remove
            m_PackageDatabase.Uninstall(m_Package);
            PackageManagerWindowAnalytics.SendEvent("uninstall", m_Version?.uniqueId);
            return true;
        }

        protected override bool isVisible
        {
            get
            {
                var installed = m_Package?.versions.installed;
                return m_Version?.HasTag(PackageTag.Removable) == true
                    && installed != null
                    && (installed == m_Version || UpmPackageVersion.IsRequestedButOverriddenVersion(m_Package, m_Version));
            }
        }

        protected override string GetTooltip(bool isInProgress)
        {
            if (isInProgress)
                return k_InProgressGenericTooltip;
            if (m_Version?.HasTag(PackageTag.BuiltIn) == true)
                return string.Format(L10n.Tr("Disable the use of this {0} in your project."), m_Package.GetDescriptor());
            return string.Format(L10n.Tr("Click to remove this {0} from your project."), m_Package.GetDescriptor());
        }

        protected override string GetText(bool isInProgress)
        {
            if (m_Version?.HasTag(PackageTag.BuiltIn) == true)
                return isInProgress ? L10n.Tr("Disabling") : L10n.Tr("Disable");
            return isInProgress ? L10n.Tr("Removing") : L10n.Tr("Remove");
        }

        protected override IEnumerable<ButtonDisableCondition> GetDisableConditions()
        {
            var isInstalledAsDependency = m_Package.versions.installed == m_Version
                && (!m_Version.isDirectDependency || UpmPackageVersion.IsDifferentVersionThanRequested(m_Version));
            yield return new ButtonDisableCondition(isInstalledAsDependency,
                string.Format(L10n.Tr("You cannot remove this {0} because another installed package or feature depends on it. See dependencies for more details."), m_Package.GetDescriptor()));
        }

        protected override bool isInProgress => m_PackageDatabase.IsUninstallInProgress(m_Package);
    }
}
