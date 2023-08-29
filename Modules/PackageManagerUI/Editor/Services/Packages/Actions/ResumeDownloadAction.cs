// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal;

internal class ResumeDownloadAction : PackageAction
{
    private readonly IPackageOperationDispatcher m_OperationDispatcher;
    private readonly IAssetStoreDownloadManager m_AssetStoreDownloadManager;
    private readonly IApplicationProxy m_Application;
    public ResumeDownloadAction(IPackageOperationDispatcher operationDispatcher, IAssetStoreDownloadManager assetStoreDownloadManager, IApplicationProxy application)
    {
        m_OperationDispatcher = operationDispatcher;
        m_AssetStoreDownloadManager = assetStoreDownloadManager;
        m_Application = application;
    }

    public override Icon icon => Icon.Resume;

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        m_OperationDispatcher.ResumeDownload(version.package);
        PackageManagerWindowAnalytics.SendEvent("resumeDownload", version);
        return true;
    }

    public override bool IsVisible(IPackageVersion version)
    {
        if (version?.HasTag(PackageTag.LegacyFormat) != true)
            return false;

        var operation = m_AssetStoreDownloadManager.GetDownloadOperation(version.package.product?.id);
        return operation?.state == DownloadState.Paused || operation?.state == DownloadState.ResumeRequested;
    }

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        if (isInProgress)
            return L10n.Tr("The resume request has been sent. Please wait for the download to resume.");
        return string.Format(L10n.Tr("Click to resume the download of this {0}."), version.GetDescriptor());
    }

    public override string GetText(IPackageVersion version, bool isInProgress) => L10n.Tr("Resume");

    public override bool IsInProgress(IPackageVersion version) => m_AssetStoreDownloadManager.GetDownloadOperation(version.package.product?.id)?.state == DownloadState.ResumeRequested;

    protected override IEnumerable<DisableCondition> GetAllTemporaryDisableConditions()
    {
        yield return new DisableIfNoNetwork(m_Application);
        yield return new DisableIfCompiling(m_Application);
    }
}
