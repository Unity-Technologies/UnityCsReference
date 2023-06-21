// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class ReDownloadAction : DownloadActionBase
{
    public ReDownloadAction(PackageOperationDispatcher operationDispatcher,
        AssetStoreDownloadManager assetStoreDownloadManager,
        UnityConnectProxy unityConnect,
        ApplicationProxy application) : base(operationDispatcher, assetStoreDownloadManager, unityConnect, application)
    {
    }

    protected override string analyticEventName => "startReDownload";

    // Re-download action covers all the `download` scenarios that's not covered by `DownloadNew` and `DownloadUpdate` actions
    public override bool IsVisible(IPackageVersion version)
    {
        return base.IsVisible(version) && IsUpToDateOrNoUpdateFound(version);
    }

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        if (isInProgress)
            return L10n.Tr("The re-download request has been sent. Please wait for the re-download to start.");
        return string.Format(L10n.Tr("Click to re-download this {0} to get the current editor's version."), version.GetDescriptor());
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        var importAvailableVersionString = version.package.versions.importAvailable?.versionString;
        return !string.IsNullOrEmpty(importAvailableVersionString) ? string.Format(L10n.Tr("Re-download {0}"), importAvailableVersionString) : L10n.Tr("Re-download");
    }

    public override bool IsInProgress(IPackageVersion version)
    {
        return base.IsInProgress(version) && IsUpToDateOrNoUpdateFound(version);
    }

    private static bool IsUpToDateOrNoUpdateFound(IPackageVersion version)
    {
        var recommended = version.package.versions?.recommended;
        var importAvailable = version.package.versions?.importAvailable;
        return importAvailable != null && (recommended == null || recommended.uploadId == importAvailable.uploadId);
    }
}
