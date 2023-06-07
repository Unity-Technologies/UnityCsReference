// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal;

internal class ResetAction : PackageAction
{
    private readonly PackageOperationDispatcher m_OperationDispatcher;
    private readonly ApplicationProxy m_Application;
    private readonly PackageDatabase m_PackageDatabase;
    private readonly PageManager m_PageManager;
    public ResetAction(PackageOperationDispatcher operationDispatcher,
        ApplicationProxy applicationProxy,
        PackageDatabase packageDatabase,
        PageManager pageManager)
    {
        m_OperationDispatcher = operationDispatcher;
        m_Application = applicationProxy;
        m_PackageDatabase = packageDatabase;
        m_PageManager = pageManager;
    }

    public override Icon icon => Icon.Customized;

    protected override bool TriggerActionImplementation(IPackageVersion version)
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

        m_PageManager.activePage.SetPackagesUserUnlockedState(packagesToUninstall.Select(p => p.uniqueId), false);
        m_OperationDispatcher.ResetDependencies(version, packagesToUninstall);

        PackageManagerWindowAnalytics.SendEvent("reset", version?.uniqueId);
        return true;
    }

    public override bool IsVisible(IPackageVersion version)
    {
        var installed = version?.package.versions.installed;
        return installed != null
               && installed == version
               && version.HasTag(PackageTag.Feature)
               && !version.HasTag(PackageTag.Custom)
               && m_PackageDatabase.GetCustomizedDependencies(version).Any();
    }

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        return string.Format(L10n.Tr("Click to reset this {0} dependencies to their default versions."), version.GetDescriptor());
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Reset");
    }

    public override bool IsInProgress(IPackageVersion version) => false;

    internal class DisableIfCannotReset : DisableCondition
    {
        public DisableIfCannotReset(PackageDatabase packageDatabase, IPackageVersion version)
        {
            var customizedDependencies = packageDatabase.GetCustomizedDependencies(version);
            var oneDependencyIsInDevelopment = customizedDependencies.Any(p => p.versions.installed?.HasTag(PackageTag.Custom) ?? false);
            if (oneDependencyIsInDevelopment)
            {
                active = true;
                tooltip =string.Format(L10n.Tr("You cannot reset this {0} because one of its included packages is customized. You must remove them manually. See the list of packages in the {0} for more information."), version.GetDescriptor());
            }
            else
            {
                var oneDependencyIsDirectAndMatchManifestVersion = customizedDependencies.Any(p => (p.versions.installed?.isDirectDependency ?? false) &&
                                                                                                   p.versions.installed?.versionString == p.versions.installed?.versionInManifest);
                active = !oneDependencyIsDirectAndMatchManifestVersion;
                tooltip = string.Format(L10n.Tr("You cannot reset this {0} because one of its included packages has changed version. See the list of packages in the {0} for more information."), version.GetDescriptor());
            }
        }
    }

    protected override IEnumerable<DisableCondition> GetAllTemporaryDisableConditions()
    {
        yield return new DisableIfInstallOrUninstallInProgress(m_OperationDispatcher);
        yield return new DisableIfCompiling(m_Application);
    }

    protected override IEnumerable<DisableCondition> GetAllDisableConditions(IPackageVersion version)
    {
        yield return new DisableIfCannotReset(m_PackageDatabase, version);
    }
}
