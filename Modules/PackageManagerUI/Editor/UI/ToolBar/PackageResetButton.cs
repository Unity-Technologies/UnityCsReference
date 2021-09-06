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
        private PageManager m_PageManager;
        public PackageResetButton(ApplicationProxy applicationProxy,
                                  PackageDatabase packageDatabase,
                                  PageManager pageManager)
        {
            m_Application = applicationProxy;
            m_PackageDatabase = packageDatabase;
            m_PageManager = pageManager;
        }

        private IPackage[] m_CustomizedDependenciesCache;
        private IPackage[] customizedDependencies => m_CustomizedDependenciesCache ??= m_PackageDatabase.GetCustomizedDependencies(m_Version);

        protected override void SetPackageAndVersion(IPackage package, IPackageVersion version)
        {
            base.SetPackageAndVersion(package, version);

            // Reset the cache to make sure that the cache is recalculated in the refresh
            m_CustomizedDependenciesCache = null;
        }

        protected override bool TriggerAction()
        {
            var packagesToUninstall = m_PackageDatabase.GetCustomizedDependencies(m_Version, true);
            if (!packagesToUninstall.Any())
                return false;

            var packageNameAndVersions = string.Join("\n\u2022 ",
                packagesToUninstall.Select(package => $"{package.displayName} - {package.versions.lifecycleVersion.version}").ToArray());
            var message = packagesToUninstall.Length == 1 ?
                string.Format(
                L10n.Tr("Are you sure you want to reset this {0}?\nThe following included package will reset to the required version:\n\u2022 {1}"),
                m_Package.GetDescriptor(), packageNameAndVersions) :
                string.Format(
                L10n.Tr("Are you sure you want to reset this {0}?\nThe following included packages will reset to their required versions:\n\u2022 {1}"),
                m_Package.GetDescriptor(), packageNameAndVersions);

            if (!m_Application.DisplayDialog(L10n.Tr("Unity Package Manager"), message, L10n.Tr("Continue"), L10n.Tr("Cancel")))
                return false;

            m_PageManager.SetPackagesUserUnlockedState(packagesToUninstall.Select(p => p.uniqueId), false);
            m_PackageDatabase.ResetDependencies(m_Version, packagesToUninstall);

            PackageManagerWindowAnalytics.SendEvent("reset", m_Version?.uniqueId);
            return true;
        }

        protected override bool isVisible
        {
            get
            {
                var installed = m_Package?.versions.installed;
                return installed != null
                    && installed == m_Version
                    && m_Package.Is(PackageType.Feature)
                    && !m_Version.HasTag(PackageTag.Custom)
                    && m_Version.HasTag(PackageTag.Removable)
                    && customizedDependencies.Any();
            }
        }

        protected override string GetTooltip(bool isInProgress)
        {
            return string.Format(L10n.Tr("Click to reset this {0} dependencies to their default versions."), m_Package.GetDescriptor());
        }

        protected override string GetText(bool isInProgress)
        {
            return L10n.Tr("Reset");
        }

        protected override bool isInProgress => false;

        protected override IEnumerable<ButtonDisableCondition> GetDisableConditions()
        {
            var oneDependencyIsInDevelopment = customizedDependencies.Any(p => p.versions.installed?.HasTag(PackageTag.Custom) ?? false);
            yield return new ButtonDisableCondition(oneDependencyIsInDevelopment,
                string.Format(L10n.Tr("You cannot reset this {0} because one of its included packages is customized. You must remove them manually. See the list of packages in the {0} for more information."), m_Package.GetDescriptor()));

            var oneDependencyIsDirectAndMatchManifestVersion = customizedDependencies.Any(p => (p.versions.installed?.isDirectDependency ?? false) &&
                p.versions.installed?.versionString == p.versions.installed?.packageInfo.projectDependenciesEntry);
            yield return new ButtonDisableCondition(!oneDependencyIsDirectAndMatchManifestVersion,
                string.Format(L10n.Tr("You cannot reset this {0} because one of its included packages has changed version. See the list of packages in the {0} for more information."), m_Package.GetDescriptor()));
        }
    }
}
