// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal;

internal class ResetAction : PackageAction
{
    private readonly IPackageOperationDispatcher m_OperationDispatcher;
    private readonly IApplicationProxy m_Application;
    private readonly IPackageDatabase m_PackageDatabase;
    private readonly IPageManager m_PageManager;
    public ResetAction(IPackageOperationDispatcher operationDispatcher,
        IApplicationProxy applicationProxy,
        IPackageDatabase packageDatabase,
        IPageManager pageManager)
    {
        m_OperationDispatcher = operationDispatcher;
        m_Application = applicationProxy;
        m_PackageDatabase = packageDatabase;
        m_PageManager = pageManager;
    }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        var packagesToUninstall = m_PackageDatabase.GetCustomizedDependencies(version, CustomizedDependencyType.Resettable);
        if (packagesToUninstall.Count == 0)
            return false;

        var packageNameAndVersions = string.Join("\n\u2022 ",
            packagesToUninstall.SelectAsEnumerable(package => $"{package.displayName} - {package.versions.recommended.version}"));

        var title = string.Format(L10n.Tr("Resetting {0}"), version.GetDescriptor());
        var message = packagesToUninstall.Count == 1 ?
            string.Format(
                L10n.Tr("Are you sure you want to reset this {0}?\nThe following included package will reset to the required version:\n\u2022 {1}"),
                version.GetDescriptor(), packageNameAndVersions) :
            string.Format(
                L10n.Tr("Are you sure you want to reset this {0}?\nThe following included packages will reset to their required versions:\n\u2022 {1}"),
                version.GetDescriptor(), packageNameAndVersions);

        if (!m_Application.DisplayDialog("resetPackage", title, message, L10n.Tr("Continue"), L10n.Tr("Cancel")))
            return false;

        m_PageManager.activePage.SetUserUnlockedState(packagesToUninstall.SelectAsEnumerable(p => p.uniqueId), false);
        m_OperationDispatcher.ResetDependencies(version, packagesToUninstall);

        PackageManagerWindowAnalytics.SendEvent("reset", version.uniqueId);
        return true;
    }

    public override bool IsVisible(IPackageVersion version)
    {
        return version.isInstalled
               && version.HasTag(PackageTag.Feature)
               // We use CustomizedDependencyType.All here because we want to show the Reset action even if there are only non-resettable customized dependencies.
               // We have the `DisableIfCannotReset` condition to disable the action and show the correct tooltip in this case.
               && m_PackageDatabase.HasCustomizedDependencies(version, CustomizedDependencyType.All);
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
        public DisableIfCannotReset(IPackageDatabase packageDatabase, IPackageVersion version)
        {
            var nonResettableCustomizedDependencies = packageDatabase.GetCustomizedDependencies(version, CustomizedDependencyType.NonResettable);
            active = nonResettableCustomizedDependencies.Count > 0;
            if (!active)
                return;

            var anyCustomDependencies = nonResettableCustomizedDependencies.AnyMatches(p => p.versions.installed.HasTag(PackageTag.Custom));
            tooltip = anyCustomDependencies ?
                string.Format(L10n.Tr("You cannot reset this {0} because one of its included packages is customized. " +
                                      "You must remove them manually. See the list of packages in the {0} for more information."), version.GetDescriptor()) :
                string.Format(L10n.Tr("You cannot reset this {0} because one of its included packages has changed version. " +
                                      "See the list of packages in the {0} for more information."), version.GetDescriptor());
        }
    }

    protected override IEnumerable<DisableCondition> GetAllTemporaryDisableConditions()
    {
        yield return new DisableIfInstallOrEmbedOrUninstallInProgress(m_OperationDispatcher);
        yield return new DisableIfCompiling(m_Application);
    }

    protected override IEnumerable<DisableCondition> GetAllDisableConditions(IPackageVersion version)
    {
        yield return new DisableIfCannotReset(m_PackageDatabase, version);
    }
}
