// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageAddButton : PackageToolBarRegularButton
    {
        public static readonly string k_InstallButtonText = L10n.Tr("Install");
        public static readonly string k_InstallingButtonText = L10n.Tr("Installing");

        public static readonly string k_EnableButtonText = L10n.Tr("Enable");
        public static readonly string k_EnablingButtonText = L10n.Tr("Enabling");

        public static readonly string k_UpdateToButtonTextFormat = L10n.Tr("Update to {0}");
        public static readonly string k_UpdatingToButtonTextFormat = L10n.Tr("Updating to {0}");

        private IPackageVersion targetVersion
        {
            get
            {
                if (m_Version?.isInstalled == true && m_Version != m_Package.versions.recommended)
                    return m_Package.versions.latest ?? m_Version;
                return m_Version;
            }
        }

        private ApplicationProxy m_Application;
        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        public PackageAddButton(ApplicationProxy applicationProxy,
                                PackageDatabase packageDatabase,
                                PageManager pageManager)
        {
            m_Application = applicationProxy;
            m_PackageDatabase = packageDatabase;
            m_PageManager = pageManager;
        }

        protected override bool TriggerAction()
        {
            var installedVersion = m_Package.versions.installed;
            var targetVersion = this.targetVersion;
            if (installedVersion != null && !installedVersion.isDirectDependency && installedVersion != targetVersion)
            {
                var featureSetDependents = m_PackageDatabase.GetFeatureDependents(m_Package.versions.installed);
                // if the installed version is being used by a Feature Set show the more specific
                //  Feature Set dialog instead of the generic one
                if (featureSetDependents.Any())
                {
                    var message = string.Format(L10n.Tr("Changing a {0} that is part of a feature can lead to errors. Are you sure you want to proceed?"), m_Package.GetDescriptor());
                    if (!m_Application.DisplayDialog(L10n.Tr("Warning"), message, L10n.Tr("Yes"), L10n.Tr("No")))
                        return false;
                }
                else
                {
                    var message = L10n.Tr("This version of the package is being used by other packages. Upgrading a different version might break your project. Are you sure you want to continue?");
                    if (!m_Application.DisplayDialog(L10n.Tr("Unity Package Manager"), message, L10n.Tr("Yes"), L10n.Tr("No")))
                        return false;
                }
            }

            IPackage[] packageToUninstall = null;
            if (targetVersion.HasTag(PackageTag.Feature))
            {
                var customizedDependencies = m_PackageDatabase.GetCustomizedDependencies(targetVersion, true);
                if (customizedDependencies.Any())
                {
                    var packageNameAndVersions = string.Join("\n\u2022 ",
                        customizedDependencies.Select(package => $"{package.displayName} - {package.versions.lifecycleVersion.version}").ToArray());

                    var message = customizedDependencies.Length == 1 ?
                        string.Format(
                        L10n.Tr("This {0} includes a package version that is different from what's already installed. Would you like to reset the following package to the required version?\n\u2022 {1}"),
                        m_Package.GetDescriptor(), packageNameAndVersions) :
                        string.Format(
                        L10n.Tr("This {0} includes package versions that are different from what are already installed. Would you like to reset the following packages to the required versions?\n\u2022 {1}"),
                        m_Package.GetDescriptor(), packageNameAndVersions);

                    var result = m_Application.DisplayDialogComplex(L10n.Tr("Unity Package Manager"), message, L10n.Tr("Install and Reset"), L10n.Tr("Cancel"), L10n.Tr("Install Only"));
                    if (result == 1) // Cancel
                        return false;
                    if (result == 0) // Install and reset
                        packageToUninstall = customizedDependencies;
                }
            }

            if (packageToUninstall?.Any() == true)
            {
                m_PackageDatabase.InstallAndResetDependencies(targetVersion, packageToUninstall);
                PackageManagerWindowAnalytics.SendEvent("installAndReset", targetVersion?.uniqueId);
            }
            else
            {
                m_PackageDatabase.Install(targetVersion);

                var installType = installedVersion == null ? "New" : "Update";
                var installRecommended = m_Package.versions.recommended == targetVersion ? "Recommended" : "NonRecommended";
                var eventName = $"install{installType}{installRecommended}";
                PackageManagerWindowAnalytics.SendEvent(eventName, targetVersion?.uniqueId);
            }
            return true;
        }

        protected override bool isVisible
        {
            get
            {
                var installed = m_Package?.versions.installed;
                var targetVersion = this.targetVersion;
                return installed?.HasTag(PackageTag.VersionLocked) != true
                    && m_Version != null
                    && targetVersion?.HasTag(PackageTag.Installable) == true
                    && installed != targetVersion
                    && !UpmPackageVersion.IsRequestedButOverriddenVersion(m_Package, m_Version)
                    && m_PageManager.GetVisualState(m_Package)?.isLocked != true
                    && m_Version?.HasTag(PackageTag.Local) == false;
            }
        }

        protected override string GetTooltip(bool isInProgress)
        {
            if (isInProgress)
                return k_InProgressGenericTooltip;

            if (m_Version?.HasTag(PackageTag.BuiltIn) == true)
                return string.Format(L10n.Tr("Enable the use of this {0} in your project."), m_Package.GetDescriptor());

            if (m_Package.versions.installed != null)
                return string.Format(L10n.Tr("Click to update this {0} to the specified version."), m_Package.GetDescriptor());

            return string.Format(L10n.Tr("Click to install this {0} into your project."), m_Package.GetDescriptor());
        }

        protected override string GetText(bool isInProgress)
        {
            if (m_Version?.HasTag(PackageTag.BuiltIn) == true)
                return isInProgress ? k_EnablingButtonText : k_EnableButtonText;

            if (m_Package.versions.installed != null)
                return string.Format(isInProgress ? k_UpdatingToButtonTextFormat : k_UpdateToButtonTextFormat, targetVersion.version);

            return isInProgress ? k_InstallingButtonText : k_InstallButtonText;
        }

        protected override bool isInProgress => m_PackageDatabase.IsInstallInProgress(targetVersion);
    }
}
