// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class DownloadNewAction : DownloadActionBase
{
    public DownloadNewAction(IPackageOperationDispatcher operationDispatcher,
        IAssetStoreDownloadManager assetStoreDownloadManager,
        IUnityConnectProxy unityConnect,
        IApplicationProxy application) : base(operationDispatcher, assetStoreDownloadManager, unityConnect, application)
    {
    }

    protected override string analyticEventName => "startDownloadNew";

    public override bool isRecommended => true;

    public override Icon icon => Icon.Download;

    public override bool IsVisible(IPackageVersion version)
    {
        return base.IsVisible(version) && version?.package.versions.importAvailable == null;
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
        return base.IsInProgress(version) && version?.package.versions.importAvailable == null;
    }
}
