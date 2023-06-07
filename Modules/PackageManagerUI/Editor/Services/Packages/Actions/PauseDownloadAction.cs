// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal;

internal class PauseDownloadAction : PackageAction
{
    private readonly PackageOperationDispatcher m_OperationDispatcher;
    private readonly AssetStoreDownloadManager m_AssetStoreDownloadManager;
    private readonly ApplicationProxy m_Application;
    public PauseDownloadAction(PackageOperationDispatcher operationDispatcher, AssetStoreDownloadManager assetStoreDownloadManager, ApplicationProxy application)
    {
        m_OperationDispatcher = operationDispatcher;
        m_AssetStoreDownloadManager = assetStoreDownloadManager;
        m_Application = application;
    }

    public override Icon icon => Icon.Pause;

    protected override bool TriggerActionImplementation(IPackageVersion version)
    {
        m_OperationDispatcher.PauseDownload(version.package);
        PackageManagerWindowAnalytics.SendEvent("pauseDownload", version);
        return true;
    }

    public override bool IsVisible(IPackageVersion version)
    {
        if (version?.HasTag(PackageTag.LegacyFormat) != true)
            return false;

        var operation = m_AssetStoreDownloadManager.GetDownloadOperation(version.package.product?.id);

        // We only want to see two icons at the same time (cancel + resume OR cancel + pause)
        // So we hide the pause button when the resume button is shown, that's why we check the ResumeRequested state
        return operation?.state != DownloadState.ResumeRequested && (operation?.isInProgress == true || operation?.state == DownloadState.Pausing);
    }

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        if (isInProgress)
            return L10n.Tr("The pause request has been sent. Please wait for the download to pause.");
        return string.Format(L10n.Tr("Click to pause the download of this {0}."), version.GetDescriptor());
    }

    public override string GetText(IPackageVersion version, bool isInProgress) => L10n.Tr("Pause");

    public override bool IsInProgress(IPackageVersion version) => m_AssetStoreDownloadManager.GetDownloadOperation(version.package.product?.id).state == DownloadState.Pausing;

    protected override IEnumerable<DisableCondition> GetAllTemporaryDisableConditions()
    {
        yield return new DisableIfCompiling(m_Application);
    }
}
