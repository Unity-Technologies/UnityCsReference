// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
        PackageManagerWindowAnalytics.SendEvent(k_ExportActionId, version);
        m_ModalManager.ShowExportModal(version);
        return true;
    }

    public override bool IsInProgress(IPackageVersion version) => false;

    public override bool IsVisible(IPackageVersion version) => version.isInstalled && version.HasTag(PackageTag.Local | PackageTag.Custom);

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Export this package to a local tarball file.", null);
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Export", null);
    }

    protected override DisableConditionList<IPackageVersion> CreateDisableConditions() => new(
        new DisableIfPackageIsNotLoaded(),
        new DisableIfPackageIsInInvalidLocation(),
        new DisableIfExportingInProgress()
    );
}
