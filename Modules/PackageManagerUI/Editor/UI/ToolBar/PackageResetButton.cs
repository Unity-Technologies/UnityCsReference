// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageResetButton : PackageToolBarDropdownButton
    {
        private ApplicationProxy m_Application;
        private PackageDatabase m_PackageDatabase;
        private PackageOperationDispatcher m_OperationDispatcher;
        private PageManager m_PageManager;
        public PackageResetButton(ApplicationProxy applicationProxy,
                                  PackageDatabase packageDatabase,
                                  PackageOperationDispatcher operationDispatcher,
                                  PageManager pageManager)
        {
            m_Application = applicationProxy;
            m_PackageDatabase = packageDatabase;
            m_OperationDispatcher = operationDispatcher;
            m_PageManager = pageManager;
        }

        protected override bool TriggerAction(IPackageVersion version)
        {
            var packagesToUninstall = m_PackageDatabase.GetCustomizedDependencies(version, true);
            if (!packagesToUninstall.Any())
                return false;

            var packageNameAndVersions = string.Join("\n\u2022 ",
                packagesToUninstall.Select(package => $"{package.displayName} - {package.versions.lifecycleVersion.version}").ToArray());

            var title = string.Format(L10n.Tr("Resetting {0}"), version.GetDescriptor());
            var message = packagesToUninstall.Length == 1 ?
                string.Format(
                L10n.Tr("Are you sure you want to reset this {0}?\nThe following included package will reset to the required version:\n\u2022 {1}"),
                version.GetDescriptor(), packageNameAndVersions) :
                string.Format(
                L10n.Tr("Are you sure you want to reset this {0}?\nThe following included packages will reset to their required versions:\n\u2022 {1}"),
                version.GetDescriptor(), packageNameAndVersions);

            if (!m_Application.DisplayDialog("resetPackage", title, message, L10n.Tr("Continue"), L10n.Tr("Cancel")))
                return false;

            m_PageManager.GetPage().SetPackagesUserUnlockedState(packagesToUninstall.Select(p => p.uniqueId), false);
            m_OperationDispatcher.ResetDependencies(version, packagesToUninstall);

            PackageManagerWindowAnalytics.SendEvent("reset", version?.uniqueId);
            return true;
        }

        protected override bool IsVisible(IPackageVersion version)
        {
            var installed = version?.package.versions.installed;
            return installed != null
                && installed == version
                && version.HasTag(PackageTag.Feature)
                && !version.HasTag(PackageTag.Custom)
                && version.HasTag(PackageTag.UpmFormat)
                && m_PackageDatabase.GetCustomizedDependencies(version).Any();
        }

        protected override string GetTooltip(IPackageVersion version, bool isInProgress)
        {
            return string.Format(L10n.Tr("Click to reset this {0} dependencies to their default versions."), version.GetDescriptor());
        }

        protected override string GetText(IPackageVersion version, bool isInProgress)
        {
            return L10n.Tr("Reset");
        }

        protected override bool IsInProgress(IPackageVersion version) => false;

        protected override IEnumerable<ButtonDisableCondition> GetDisableConditions(IPackageVersion version)
        {
            var customizedDependencies = m_PackageDatabase.GetCustomizedDependencies(version);
            var oneDependencyIsInDevelopment = customizedDependencies.Any(p => p.versions.installed?.HasTag(PackageTag.Custom) ?? false);
            yield return new ButtonDisableCondition(oneDependencyIsInDevelopment,
                string.Format(L10n.Tr("You cannot reset this {0} because one of its included packages is customized. You must remove them manually. See the list of packages in the {0} for more information."), version.GetDescriptor()));

            var oneDependencyIsDirectAndMatchManifestVersion = customizedDependencies.Any(p => (p.versions.installed?.isDirectDependency ?? false) &&
                p.versions.installed?.versionString == p.versions.installed?.versionInManifest);
            yield return new ButtonDisableCondition(!oneDependencyIsDirectAndMatchManifestVersion,
                string.Format(L10n.Tr("You cannot reset this {0} because one of its included packages has changed version. See the list of packages in the {0} for more information."), version.GetDescriptor()));
        }
    }
}
