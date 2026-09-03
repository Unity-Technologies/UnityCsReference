// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

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

    protected override bool TriggerActionImplementation(IReadOnlyCollection<IPackage> packages)
    {
        m_OperationDispatcher.AbortDownload(packages);
        PackageManagerWindowAnalytics.SendEvent("abortDownload", packages);
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
        return string.Format(L10n.Tr("Click to cancel the download of this {0}.", null), version.GetDescriptor());
    }

    public override string GetText(IPackageVersion version, bool isInProgress) => L10n.Tr("Cancel", null);

    public override bool IsInProgress(IPackageVersion version) => false;

    internal class DisableIfResumeRequestSent : IDisableCondition<IPackageVersion>
    {
        private static readonly string k_Tooltip = L10n.Tr("A resume request has been sent. You cannot cancel this download until it is resumed.", null);
        private readonly IAssetStoreDownloadManager m_DownloadManager;
        public DisableIfResumeRequestSent(IAssetStoreDownloadManager downloadManager)
        {
            m_DownloadManager = downloadManager;
        }

        public bool IsActive(IPackageVersion version, out string tooltip)
        {
            tooltip = k_Tooltip;
            return m_DownloadManager.GetDownloadOperation(version?.package.product?.id).state == DownloadState.ResumeRequested;
        }
    }

    protected override DisableConditionList<IPackageVersion> CreateTemporaryDisableConditions() => new(
        new DisableIfCompiling(m_Application)
    );

    protected override DisableConditionList<IPackageVersion> CreateDisableConditions() => new(
        new DisableIfResumeRequestSent(m_AssetStoreDownloadManager)
    );
}
