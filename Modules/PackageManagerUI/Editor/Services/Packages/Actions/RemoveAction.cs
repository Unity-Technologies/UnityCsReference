// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal;

internal class RemoveAction : PackageAction
{
    private readonly IPackageOperationDispatcher m_OperationDispatcher;
    private readonly IApplicationProxy m_Application;
    private readonly IPackageManagerPrefs m_PackageManagerPrefs;
    private readonly IPackageDatabase m_PackageDatabase;
    private readonly IPageManager m_PageManager;
    public RemoveAction(IPackageOperationDispatcher operationDispatcher,
        IApplicationProxy applicationProxy,
        IPackageManagerPrefs packageManagerPrefs,
        IPackageDatabase packageDatabase,
        IPageManager pageManager)
    {
        m_OperationDispatcher = operationDispatcher;
        m_Application = applicationProxy;
        m_PackageManagerPrefs = packageManagerPrefs;
        m_PackageDatabase = packageDatabase;
        m_PageManager = pageManager;
    }

    protected override bool TriggerActionImplementation(IReadOnlyCollection<IPackage> packages)
    {
        var isModules = packages.AnyMatches(p => p.versions.primary.HasTag(PackageTag.BuiltIn));
        var title = string.Format(L10n.Tr(isModules ? "Disabling {0} items" : "Removing {0} items", null), packages.Count);

        var result = 0;
        if (!m_PackageManagerPrefs.skipMultiSelectRemoveConfirmation)
        {
            var message = L10n.Tr(isModules ? "Are you sure you want to disable these items?" : "Are you sure you want to remove these items?", null);
            result = m_Application.DisplayDialogComplex("removeMultiplePackages", title, message, L10n.Tr(isModules ? "Disable" : "Remove", null), L10n.Tr("Cancel", null), L10n.Tr("Never ask", null));
        }

        // Cancel
        if (result == 1)
            return false;

        // Never ask
        if (result == 2)
            m_PackageManagerPrefs.skipMultiSelectRemoveConfirmation = true;

        m_OperationDispatcher.Uninstall(packages);
        PackageManagerWindowAnalytics.SendEvent("uninstall", packages);
        // After a bulk removal, we want to deselect them to avoid installing them back by accident.
        DeselectPackages(packages);
        return true;
    }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        var result = 0;
        if (version.HasTag(PackageTag.BuiltIn))
        {
            if (!m_PackageManagerPrefs.skipDisableConfirmation)
            {
                result = m_Application.DisplayDialogComplex("disableBuiltInPackage",
                    L10n.Tr("Disable Built-In Package", null),
                    L10n.Tr("Are you sure you want to disable this built-in package?", null),
                    L10n.Tr("Disable", null), L10n.Tr("Cancel", null), L10n.Tr("Never ask", null));
            }
        }
        else
        {
            var isPartOfFeature = m_PackageDatabase.IsUsedByFeature(version);
            if (isPartOfFeature || !m_PackageManagerPrefs.skipRemoveConfirmation)
            {
                var descriptor = version.GetDescriptor();
                var title = string.Format(L10n.Tr("Removing {0}", null), descriptor);
                if (isPartOfFeature)
                {
                    var message = string.Format(L10n.Tr("Are you sure you want to remove this {0} that is used by at least one installed feature?", null), descriptor);
                    var removeIt = m_Application.DisplayDialog("removePackagePartOfFeature", title, message, L10n.Tr("Remove", null), L10n.Tr("Cancel", null));
                    result = removeIt ? 0 : 1;
                }
                else
                {
                    var message = string.Format(L10n.Tr("Are you sure you want to remove this {0}?", null), descriptor);
                    result = m_Application.DisplayDialogComplex("removePackage", title, message, L10n.Tr("Remove", null), L10n.Tr("Cancel", null), L10n.Tr("Never ask", null));
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
        if (m_PackageDatabase.IsUsedByFeature(version.package.versions.installed))
            m_PageManager.activePage.SetUserUnlockedState(new [] { version.package.uniqueId }, false);

        // Remove
        m_OperationDispatcher.Uninstall(version.package);
        PackageManagerWindowAnalytics.SendEvent("uninstall", version);
        return true;
    }

    public override bool IsVisible(IPackageVersion version)
    {
        var installed = version?.package.versions.installed;
        return installed != null
               && version.HasTag(PackageTag.UpmFormat)
               && !version.HasTag(PackageTag.Placeholder | PackageTag.Custom)
               && (installed == version || version.IsRequestedButOverriddenVersion);
    }

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        if (isInProgress)
            return k_InProgressGenericTooltip;
        if (version?.HasTag(PackageTag.BuiltIn) == true)
            return string.Format(L10n.Tr("Disable the use of this {0} in your project.", null), version.GetDescriptor());
        return string.Format(L10n.Tr("Click to remove this {0} from your project.", null), version.GetDescriptor());
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        if (version?.HasTag(PackageTag.BuiltIn) == true)
            return isInProgress ? L10n.Tr("Disabling", null) : L10n.Tr("Disable", null);
        return isInProgress ? L10n.Tr("Removing", null) : L10n.Tr("Remove", null);
    }

    public override bool IsInProgress(IPackageVersion version) => m_OperationDispatcher.IsUninstallInProgress(version.package);

    protected override DisableConditionList<IPackageVersion> CreateTemporaryDisableConditions() => new(
        new DisableIfInstallOrEmbedOrUninstallInProgress(m_OperationDispatcher),
        new DisableIfCompiling(m_Application)
    );

    internal class DisableIfInstalledAsDependency : IDisableCondition<IPackageVersion>
    {
        private static readonly string k_TooltipTemplate = L10n.Tr("You cannot remove this {0} because another installed package or feature depends on it. See dependencies for more details.", null);

        public bool IsActive(IPackageVersion version, out string tooltip)
        {
            tooltip = null;
            if (version == null || version.package.versions.installed != version
                || version.isDirectDependency
                || version.isInvalidSemVerInManifest)
                return false;

            tooltip = string.Format(k_TooltipTemplate, version.GetDescriptor());
            return true;
        }
    }

    protected override DisableConditionList<IPackageVersion> CreateDisableConditions() => new(
        new DisableIfInstalledAsDependency(),
        new DisableIfExportingInProgress()
    );

    private void DeselectPackages(IReadOnlyCollection<IPackage> packages)
    {
        m_PageManager.activePage.RemoveSelection(packages.SelectAsEnumerable(p => p.uniqueId), false);
    }
}
