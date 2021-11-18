// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackagePauseDownloadButton : PackageToolBarRegularButton
    {
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private PackageDatabase m_PackageDatabase;
        public PackagePauseDownloadButton(AssetStoreDownloadManager assetStoreDownloadManager, PackageDatabase packageDatabase)
        {
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_PackageDatabase = packageDatabase;
        }

        protected override bool TriggerAction(IPackageVersion version)
        {
            m_PackageDatabase.PauseDownload(version.package);
            PackageManagerWindowAnalytics.SendEvent("pauseDownload", version.packageUniqueId);
            return true;
        }

        protected override bool IsVisible(IPackageVersion version)
        {
            if (version?.HasTag(PackageTag.Downloadable) != true)
                return false;

            var operation = m_AssetStoreDownloadManager.GetDownloadOperation(version.packageUniqueId);
            return operation?.isInProgress == true || operation?.state == DownloadState.Pausing;
        }

        protected override string GetTooltip(IPackageVersion version, bool isInProgress)
        {
            if (isInProgress)
                return L10n.Tr("The pause request has been sent. Please wait for the download to pause.");
            return string.Format(L10n.Tr("Click to pause the download of this {0}."), version.package.GetDescriptor());
        }

        protected override string GetText(IPackageVersion version, bool isInProgress)
        {
            return L10n.Tr("Pause");
        }

        protected override bool IsInProgress(IPackageVersion version) => m_AssetStoreDownloadManager.GetDownloadOperation(version.packageUniqueId).state == DownloadState.Pausing;
    }
}
