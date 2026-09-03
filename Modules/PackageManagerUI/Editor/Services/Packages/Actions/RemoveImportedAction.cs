// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal;

internal class RemoveImportedAction : PackageAction
{
    private readonly IPackageOperationDispatcher m_OperationDispatcher;
    private readonly IApplicationProxy m_Application;
    public RemoveImportedAction(IPackageOperationDispatcher operationDispatcher, IApplicationProxy application)
    {
        m_OperationDispatcher = operationDispatcher;
        m_Application = application;
    }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        m_OperationDispatcher.RemoveImportedAssets(version.package);
        PackageManagerWindowAnalytics.SendEvent("removeImported", version);
        return true;
    }

    protected override bool TriggerActionImplementation(IReadOnlyCollection<IPackage> packages)
    {
        if (!m_Application.DisplayDialog("removeMultiImported", L10n.Tr("Removing imported packages", null),
                L10n.Tr("Remove all assets from these packages?\nAny changes you made to the assets will be lost.", null),
                L10n.Tr("Remove", null), L10n.Tr("Cancel", null)))
            return false;

        m_OperationDispatcher.RemoveImportedAssets(packages);
        PackageManagerWindowAnalytics.SendEvent("removeImported", packages);
        return true;
    }

    public override bool IsVisible(IPackageVersion version)
    {
        return version?.importedAssets?.Count >0;
    }

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        if (isInProgress)
            return k_InProgressGenericTooltip;
        return string.Format(L10n.Tr("Remove this {0}'s imported assets from your project.", null), version.GetDescriptor());
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Remove assets from project", null);
    }

    public override string GetMultiSelectText(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Remove", null);
    }

    public override bool IsInProgress(IPackageVersion version) => false;

    protected override DisableConditionList<IPackageVersion> CreateTemporaryDisableConditions() => new(
        new DisableIfCompiling(m_Application)
    );
}
