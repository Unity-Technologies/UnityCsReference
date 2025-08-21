// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Text;

namespace UnityEditor.PackageManager.UI.Internal;

internal abstract class UpdateActionBase : PackageAction
{
    private static readonly string k_UpdateToButtonTextFormat = L10n.Tr("Update to {0}");
    private static readonly string k_UpdatingToButtonTextFormat = L10n.Tr("Updating to {0}");
    private static readonly string k_UpdateToWithoutVersionButtonText = L10n.Tr("Update");
    private static readonly string k_UpdatingToWithoutVersionButtonText = L10n.Tr("Updating");

    protected bool m_ShowVersion;

    protected readonly IPackageOperationDispatcher m_OperationDispatcher;
    protected readonly IApplicationProxy m_Application;
    protected readonly IPackageDatabase m_PackageDatabase;
    protected readonly IPageManager m_PageManager;

    protected UpdateActionBase(IPackageOperationDispatcher operationDispatcher,
        IApplicationProxy application,
        IPackageDatabase packageDatabase,
        IPageManager pageManager)
    {
        m_OperationDispatcher = operationDispatcher;
        m_Application = application;
        m_PackageDatabase = packageDatabase;
        m_PageManager = pageManager;
    }

    public abstract IPackageVersion GetUpdateTarget(IPackageVersion version);

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        var installedVersion = version?.package.versions?.installed;
        var targetVersion = GetUpdateTarget(version);
        if (installedVersion is { isDirectDependency: false } && installedVersion != targetVersion)
        {
            var featureSetDependents = m_PackageDatabase.GetFeaturesThatUseThisPackage(installedVersion);
            // if the installed version is being used by a Feature Set show the more specific
            //  Feature Set dialog instead of the generic one
            var title = string.Format(L10n.Tr("Updating {0}"), version.GetDescriptor());
            using var enumerator = featureSetDependents.GetEnumerator();
            if (featureSetDependents != null && enumerator.MoveNext())
            {
                var message = string.Format(L10n.Tr("Changing a {0} that is part of a feature can lead to errors. Are you sure you want to proceed?"), version.GetDescriptor());
                if (!m_Application.DisplayDialog("updatePackagePartOfFeature", title, message, L10n.Tr("Yes"), L10n.Tr("No")))
                    return false;
            }
            else
            {
                var message = L10n.Tr("This version of the package is being used by other packages. Upgrading a different version might break your project. Are you sure you want to continue?");
                if (!m_Application.DisplayDialog("updatePackageUsedByOthers", title, message, L10n.Tr("Yes"), L10n.Tr("No")))
                    return false;
            }
        }

        if (!m_OperationDispatcher.Install(targetVersion))
            return false;

        var installRecommended = version.package.versions.recommended == targetVersion ? "Recommended" : "NonRecommended";
        var eventName = $"installUpdate{installRecommended}";
        PackageManagerWindowAnalytics.SendEvent(eventName, targetVersion);
        return true;
    }

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        if (isInProgress)
            return k_InProgressGenericTooltip;

        return string.Format(L10n.Tr("Click to update this {0} to the specified version."), version.GetDescriptor());
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        if (!m_ShowVersion || m_PageManager.activePage.GetSelection().Count > 1)
            return isInProgress ? k_UpdatingToWithoutVersionButtonText : k_UpdateToWithoutVersionButtonText;

        return string.Format(isInProgress ? k_UpdatingToButtonTextFormat : k_UpdateToButtonTextFormat, GetUpdateTarget(version).version);
    }

    public override bool IsInProgress(IPackageVersion version) => m_OperationDispatcher.IsInstallInProgress(GetUpdateTarget(version));

    protected override IEnumerable<DisableCondition> GetAllTemporaryDisableConditions()
    {
        yield return new DisableIfInstallOrEmbedOrUninstallInProgress(m_OperationDispatcher);
        yield return new DisableIfCompiling(m_Application);
    }

    protected override IEnumerable<DisableCondition> GetAllDisableConditions(IPackageVersion version)
    {
        // We need to check the target version so that we don't disable the button in the details header
        yield return new DisableIfVersionDeprecated(GetUpdateTarget(version));
        yield return new DisableIfEntitlementsError(version);
        yield return new DisableIfExportingInProgress(version.package);
    }
}
