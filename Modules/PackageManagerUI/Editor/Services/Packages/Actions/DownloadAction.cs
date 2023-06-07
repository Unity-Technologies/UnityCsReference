// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal;

internal class DownloadAction : PackageAction
{
    private readonly PackageOperationDispatcher m_OperationDispatcher;
    private readonly AssetStoreDownloadManager m_AssetStoreDownloadManager;
    private readonly AssetStoreCache m_AssetStoreCache;
    private readonly UnityConnectProxy m_UnityConnect;
    private readonly ApplicationProxy m_Application;
    public DownloadAction(PackageOperationDispatcher operationDispatcher,
        AssetStoreDownloadManager assetStoreDownloadManager,
        AssetStoreCache assetStoreCache,
        UnityConnectProxy unityConnect,
        ApplicationProxy application)
    {
        m_OperationDispatcher = operationDispatcher;
        m_AssetStoreDownloadManager = assetStoreDownloadManager;
        m_AssetStoreCache = assetStoreCache;
        m_UnityConnect = unityConnect;
        m_Application = application;
    }

    public override bool isRecommended => true;

    public override Icon icon => Icon.Download;

    protected override bool TriggerActionImplementation(IList<IPackageVersion> versions)
    {
        var canDownload = m_OperationDispatcher.Download(versions.Select(v => v.package));
        if (canDownload)
            PackageManagerWindowAnalytics.SendEvent("startDownloadNew", versions);
        return canDownload;
    }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        var canDownload = m_OperationDispatcher.Download(version.package);
        if (canDownload)
            PackageManagerWindowAnalytics.SendEvent("startDownloadNew", version);
        return canDownload;
    }

    public override bool IsVisible(IPackageVersion version)
    {
        if (!m_UnityConnect.isUserLoggedIn)
            return false;

        if (version?.HasTag(PackageTag.LegacyFormat) != true)
            return false;

        var productId = version.package.product?.id;
        var localInfo = m_AssetStoreCache.GetLocalInfo(productId);
        if (localInfo != null)
            return false;

        var operation = m_AssetStoreDownloadManager.GetDownloadOperation(productId);
        return operation == null || operation.state == DownloadState.DownloadRequested || !operation.isProgressVisible;

    }

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        if (isInProgress)
            return L10n.Tr("The download request has been sent. Please wait for the download to start.");
        return string.Format(L10n.Tr("Click to download this {0} for later use."), version.GetDescriptor());
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Download");
    }

    public override bool IsInProgress(IPackageVersion version)
    {
        var productId = version?.package.product?.id;
        var operation = m_AssetStoreDownloadManager.GetDownloadOperation(productId);
        var localInfo = m_AssetStoreCache.GetLocalInfo(productId);
        return localInfo == null && operation?.isInProgress == true;
    }

    protected override IEnumerable<DisableCondition> GetAllTemporaryDisableConditions()
    {
        yield return new DisableIfNoNetwork(m_Application);
        yield return new DisableIfCompiling(m_Application);
    }

    protected override IEnumerable<DisableCondition> GetAllDisableConditions(IPackageVersion version)
    {
        yield return new DisableIfPackageDisabled(version);
    }

    protected override bool IsHiddenWhenInProgress(IPackageVersion version) => true;
}
