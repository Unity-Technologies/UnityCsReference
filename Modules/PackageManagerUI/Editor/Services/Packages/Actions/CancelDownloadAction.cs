// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal;

internal class CancelDownloadAction : PackageAction
{
    private readonly IPackageOperationDispatcher m_OperationDispatcher;
    private readonly IAssetStoreDownloadManager m_AssetStoreDownloadManager;
    private readonly IApplicationProxy m_Application;
    public CancelDownloadAction(IPackageOperationDispatcher operationDispatcher, IAssetStoreDownloadManager assetStoreDownloadManager, IApplicationProxy application)
    {
        m_OperationDispatcher = operationDispatcher;
        m_AssetStoreDownloadManager = assetStoreDownloadManager;
        m_Application = application;
    }

    public override Icon icon => Icon.Cancel;

    protected override bool TriggerActionImplementation(IList<IPackageVersion> versions)
    {
        m_OperationDispatcher.AbortDownload(versions.Select(v => v.package));
        PackageManagerWindowAnalytics.SendEvent("abortDownload", versions);
        return true;
    }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        m_OperationDispatcher.AbortDownload(version.package);
        PackageManagerWindowAnalytics.SendEvent("abortDownload", version);
        return true;
    }

    public override bool IsVisible(IPackageVersion version)
    {
        if (version?.HasTag(PackageTag.LegacyFormat) != true)
            return false;

        var operation = m_AssetStoreDownloadManager.GetDownloadOperation(version.package.product?.id);
        return operation?.isProgressVisible == true;
    }

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        return string.Format(L10n.Tr("Click to cancel the download of this {0}."), version.GetDescriptor());
    }

    public override string GetText(IPackageVersion version, bool isInProgress) => L10n.Tr("Cancel");

    public override bool IsInProgress(IPackageVersion version) => false;

    internal class DisableIfResumeRequestSent : DisableCondition
    {
        private static readonly string k_Tooltip = L10n.Tr("A resume request has been sent. You cannot cancel this download until it is resumed.");
        public DisableIfResumeRequestSent(IAssetStoreDownloadManager downloadManager, IPackageVersion version)
        {
            active = downloadManager.GetDownloadOperation(version?.package.product?.id).state == DownloadState.ResumeRequested;
            tooltip = k_Tooltip;
        }
    }

    protected override IEnumerable<DisableCondition> GetAllTemporaryDisableConditions()
    {
        yield return new DisableIfCompiling(m_Application);
    }

    protected override IEnumerable<DisableCondition> GetAllDisableConditions(IPackageVersion version)
    {
        yield return new DisableIfResumeRequestSent(m_AssetStoreDownloadManager, version);
    }
}
