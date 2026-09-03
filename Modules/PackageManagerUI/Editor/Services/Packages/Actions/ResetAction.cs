// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

        var title = string.Format(L10n.Tr("Resetting {0}", null), version.GetDescriptor());
        var message = packagesToUninstall.Count == 1 ?
            string.Format(
                L10n.Tr("Are you sure you want to reset this {0}?\nThe following included package will reset to the required version:\n\u2022 {1}", null),
                version.GetDescriptor(), packageNameAndVersions) :
            string.Format(
                L10n.Tr("Are you sure you want to reset this {0}?\nThe following included packages will reset to their required versions:\n\u2022 {1}", null),
                version.GetDescriptor(), packageNameAndVersions);

        if (!m_Application.DisplayDialog("resetPackage", title, message, L10n.Tr("Continue", null), L10n.Tr("Cancel", null)))
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
        return string.Format(L10n.Tr("Click to reset this {0} dependencies to their default versions.", null), version.GetDescriptor());
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Reset", null);
    }

    public override bool IsInProgress(IPackageVersion version) => false;

    internal class DisableIfCannotReset : IDisableCondition<IPackageVersion>
    {
        private readonly IPackageDatabase m_PackageDatabase;
        public DisableIfCannotReset(IPackageDatabase packageDatabase)
        {
            m_PackageDatabase = packageDatabase;
        }

        public bool IsActive(IPackageVersion version, out string tooltip)
        {
            tooltip = null;
            var nonResettableCustomizedDependencies = m_PackageDatabase.GetCustomizedDependencies(version, CustomizedDependencyType.NonResettable);
            if (nonResettableCustomizedDependencies.Count == 0)
                return false;

            var anyCustomDependencies = nonResettableCustomizedDependencies.AnyMatches(p => p.versions.installed.HasTag(PackageTag.Custom));
            tooltip = anyCustomDependencies ?
                string.Format(L10n.Tr("You cannot reset this {0} because one of its included packages is customized. " +
                                      "You must remove them manually. See the list of packages in the {0} for more information.", null), version.GetDescriptor()) :
                string.Format(L10n.Tr("You cannot reset this {0} because one of its included packages has changed version. " +
                                      "See the list of packages in the {0} for more information.", null), version.GetDescriptor());
            return true;
        }
    }

    protected override DisableConditionList<IPackageVersion> CreateTemporaryDisableConditions() => new(
        new DisableIfInstallOrEmbedOrUninstallInProgress(m_OperationDispatcher),
        new DisableIfCompiling(m_Application)
    );

    protected override DisableConditionList<IPackageVersion> CreateDisableConditions() => new(
        new DisableIfCannotReset(m_PackageDatabase)
    );
}
