// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal;

internal class ExportAction : PackageAction
{
    private const string k_ExportActionId = "export";

    private readonly IModalManager m_ModalManager;
    public ExportAction(IModalManager modalManager)
    {
        m_ModalManager = modalManager;
    }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        m_ModalManager.ShowExportModal(version);
        PackageManagerWindowAnalytics.SendEvent(k_ExportActionId, version);

        return true;
    }

    public override bool IsInProgress(IPackageVersion version) => false;

    public override bool IsVisible(IPackageVersion version) => version.isInstalled && version.HasTag(PackageTag.Local | PackageTag.Custom);

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Export this package to a local tarball file.");
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Export");
    }

    protected override IEnumerable<DisableCondition> GetAllDisableConditions(IPackageVersion version)
    {
        yield return new DisableIfExportingInProgress(version.package);
    }
}
