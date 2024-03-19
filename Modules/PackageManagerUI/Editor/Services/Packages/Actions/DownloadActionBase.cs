// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal;

internal abstract class DownloadActionBase : PackageAction
{
    protected readonly IPackageOperationDispatcher m_OperationDispatcher;
    protected readonly IAssetStoreDownloadManager m_AssetStoreDownloadManager;
    protected readonly IUnityConnectProxy m_UnityConnect;
    protected readonly IApplicationProxy m_Application;

    protected DownloadActionBase(IPackageOperationDispatcher operationDispatcher,
        IAssetStoreDownloadManager assetStoreDownloadManager,
        IUnityConnectProxy unityConnect,
        IApplicationProxy application)
    {
        m_OperationDispatcher = operationDispatcher;
        m_AssetStoreDownloadManager = assetStoreDownloadManager;
        m_UnityConnect = unityConnect;
        m_Application = application;
    }

    protected abstract string analyticEventName { get; }

    protected override bool TriggerActionImplementation(IList<IPackage> packages)
    {
        var canDownload = m_OperationDispatcher.Download(packages);
        if (canDownload)
            PackageManagerWindowAnalytics.SendEvent(analyticEventName, packages.Select(p => p.versions.primary));
        return canDownload;
    }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        var canDownload = m_OperationDispatcher.Download(version.package);
        if (canDownload)
            PackageManagerWindowAnalytics.SendEvent(analyticEventName, version);
        return canDownload;
    }

    public override bool IsVisible(IPackageVersion version)
    {
        if (!m_UnityConnect.isUserLoggedIn || version?.HasTag(PackageTag.LegacyFormat) != true)
            return false;

        var operation = m_AssetStoreDownloadManager.GetDownloadOperation(version.package.product?.id);
        return operation == null || operation.state == DownloadState.DownloadRequested || !operation.isProgressVisible;
    }

    public override bool IsInProgress(IPackageVersion version)
    {
        return m_AssetStoreDownloadManager.GetDownloadOperation(version?.package.product?.id)?.isInProgress == true;
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
