// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal;

internal class RemoveImportedAction : PackageAction
{
    private readonly PackageOperationDispatcher m_OperationDispatcher;
    private readonly ApplicationProxy m_Application;
    public RemoveImportedAction(PackageOperationDispatcher operationDispatcher, ApplicationProxy application)
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

    protected override bool TriggerActionImplementation(IList<IPackageVersion> versions)
    {
        if (!m_Application.DisplayDialog("removeMultiImported", L10n.Tr("Removing imported packages"),
                L10n.Tr("Remove all assets from these packages?\nAny changes you made to the assets will be lost."),
                L10n.Tr("Remove"), L10n.Tr("Cancel")))
            return false;

        m_OperationDispatcher.RemoveImportedAssets(versions);
        PackageManagerWindowAnalytics.SendEvent("removeImported", versions);
        return true;
    }

    public override bool IsVisible(IPackageVersion version)
    {
        return version?.importedAssets?.Any() == true;
    }

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        if (isInProgress)
            return k_InProgressGenericTooltip;
        return string.Format(L10n.Tr("Remove this {0}'s imported assets from your project."), version.GetDescriptor());
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Remove assets from project");
    }

    public override string GetMultiSelectText(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Remove");
    }

    public override bool IsInProgress(IPackageVersion version) => false;

    protected override IEnumerable<DisableCondition> GetAllTemporaryDisableConditions()
    {
        yield return new DisableIfCompiling(m_Application);
    }
}
