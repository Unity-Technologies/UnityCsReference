// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Connect;

namespace UnityEditor.PackageManager.UI.Internal;

internal class ExportAction : PackageAction
{
    private const string k_ExportActionId = "export";

    private readonly IModalManager m_ModalManager;
    private readonly IUnityConnectProxy m_UnityConnect;
    public ExportAction(IModalManager modalManager, IUnityConnectProxy unityConnect)
    {
        m_ModalManager = modalManager;
        m_UnityConnect = unityConnect;
    }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        m_UnityConnect.ParseOrganizationInfosAsync((organizationInfo) =>
        {
            m_ModalManager.ShowExportModal(version, organizationInfo);
            PackageManagerWindowAnalytics.SendEvent(k_ExportActionId, version);
        });
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
