// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class DownloadUpdateAction : DownloadActionBase
{
    public DownloadUpdateAction(PackageOperationDispatcher operationDispatcher,
        AssetStoreDownloadManager assetStoreDownloadManager,
        UnityConnectProxy unityConnect,
        ApplicationProxy application) : base(operationDispatcher, assetStoreDownloadManager, unityConnect, application)
    {
    }

    protected override string analyticEventName => "startDownloadUpdate";

    public override bool isRecommended => true;

    public override Icon icon => Icon.Download;

    public override bool IsVisible(IPackageVersion version)
    {
        return base.IsVisible(version) && IsUpdateAvailable(version);
    }

    public override string GetTooltip(IPackageVersion version, bool isInProgress)
    {
        if (isInProgress)
            return L10n.Tr("The download request has been sent. Please wait for the download to start.");

        var result = string.Format(L10n.Tr("Click to download the recommended version of this {0}."), version.GetDescriptor());
        if (IsAdaptedPackageUpdate(version.package.versions?.recommended, version.package.versions?.importAvailable))
            result += L10n.Tr("\n*This package update has been adapted for this current version of Unity.");
        return result;
    }

    public override string GetText(IPackageVersion version, bool isInProgress)
    {
        var recommended = version.package.versions.recommended;
        if (string.IsNullOrEmpty(recommended?.versionString))
            return L10n.Tr("Download update");
        return string.Format(IsAdaptedPackageUpdate(recommended, version.package.versions.importAvailable) ? L10n.Tr("Download update {0}*") : L10n.Tr("Download update {0}"), recommended.versionString);
    }

    public override string GetMultiSelectText(IPackageVersion version, bool isInProgress)
    {
        return L10n.Tr("Update");
    }

    public override bool IsInProgress(IPackageVersion version)
    {
        return base.IsInProgress(version) && IsUpdateAvailable(version);
    }

    // Adapted package update refers to the edge case where a publisher can publish different packages for different unity versions, resulting us
    // sometimes recommending user to update to a package with the same version string (or even lower version string)
    private static bool IsAdaptedPackageUpdate(IPackageVersion recommended, IPackageVersion importAvailable)
    {
        return recommended?.versionString == importAvailable?.versionString || recommended?.uploadId < importAvailable?.uploadId;
    }

    private static bool IsUpdateAvailable(IPackageVersion version)
    {
        var importAvailable = version.package.versions.importAvailable;
        var recommended = version.package.versions.recommended;
        return importAvailable != null && recommended != null && recommended.uploadId != importAvailable.uploadId;
    }
}
