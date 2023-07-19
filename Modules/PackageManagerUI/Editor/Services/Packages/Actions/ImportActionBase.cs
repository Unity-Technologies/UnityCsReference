// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal;

internal abstract class ImportActionBase : PackageAction
{
    protected readonly PackageOperationDispatcher m_OperationDispatcher;
    protected readonly AssetStoreDownloadManager m_AssetStoreDownloadManager;
    protected readonly ApplicationProxy m_Application;
    protected readonly UnityConnectProxy m_UnityConnect;

    protected ImportActionBase(PackageOperationDispatcher operationDispatcher,
                               AssetStoreDownloadManager assetStoreDownloadManager,
                               ApplicationProxy application,
                               UnityConnectProxy unityConnect)
    {
        m_OperationDispatcher = operationDispatcher;
        m_AssetStoreDownloadManager = assetStoreDownloadManager;
        m_Application = application;
        m_UnityConnect = unityConnect;
    }

    protected abstract string analyticEventName { get; }

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        m_OperationDispatcher.Import(version.package);
        PackageManagerWindowAnalytics.SendEvent(analyticEventName, version);
        return true;
    }

    public override bool IsVisible(IPackageVersion version)
    {
        return m_UnityConnect.isUserLoggedIn
            && version.HasTag(PackageTag.LegacyFormat)
            && version.package.versions.importAvailable != null
            && version.package.progress == PackageProgress.None
            && m_AssetStoreDownloadManager.GetDownloadOperation(version.package.product?.id)?.isProgressVisible != true;
    }

    public override bool IsInProgress(IPackageVersion version) => false;

    protected override IEnumerable<DisableCondition> GetAllTemporaryDisableConditions()
    {
        yield return new DisableIfCompiling(m_Application);
    }

    protected override IEnumerable<DisableCondition> GetAllDisableConditions(IPackageVersion version)
    {
        yield return new DisableIfPackageDisabled(version);
    }
}
