// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class RemoveCustomAction : PackageAction
{
    private readonly IPackageOperationDispatcher m_OperationDispatcher;
    private readonly IApplicationProxy m_Application;
    public RemoveCustomAction(IPackageOperationDispatcher operationDispatcher, IApplicationProxy applicationProxy)
    {
        m_OperationDispatcher = operationDispatcher;
        m_Application = applicationProxy;
    }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        if (version.HasTag(PackageTag.Custom))
        {
            if (!m_Application.DisplayDialog("removeEmbeddedPackage", L10n.Tr("Removing package in development", null), L10n.Tr("You will lose all your changes (if any) if you delete a package in development. Are you sure?", null), L10n.Tr("Yes", null), L10n.Tr("No", null)))
                return false;

            m_OperationDispatcher.RemoveEmbedded(version.package);
            PackageManagerWindowAnalytics.SendEvent("removeEmbedded", version.uniqueId);
            return true;
        }

        return false;
    }

    public override bool IsVisible(IPackageVersion version)
    {
        var installed = version?.package.versions.installed;
        return installed != null
               && version.HasTag(PackageTag.UpmFormat)
               && version.HasTag(PackageTag.Custom)
               && (installed == version || version.IsRequestedButOverriddenVersion);
    }

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        if (isInProgress)
            return k_InProgressGenericTooltip;
        return string.Format(L10n.Tr("Click to remove this {0} from your project.", null), version.GetDescriptor());
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        return isInProgress ? L10n.Tr("Removing", null) : L10n.Tr("Remove", null);
    }

    public override bool IsInProgress(IPackageVersion version) => m_OperationDispatcher.IsUninstallInProgress(version.package);

    protected override DisableConditionList<IPackageVersion> CreateDisableConditions() => new(
        new DisableIfExportingInProgress()
    );

    protected override DisableConditionList<IPackageVersion> CreateTemporaryDisableConditions() => new(
        new DisableIfInstallOrEmbedOrUninstallInProgress(m_OperationDispatcher),
        new DisableIfCompiling(m_Application)
    );
}
