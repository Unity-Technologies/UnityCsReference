// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDownloadButton : PackageToolBarRegularButton
    {
        public static readonly string k_DownloadButtonText = L10n.Tr("Download");
        public static readonly string k_UpdateButtonText = L10n.Tr("Update");

        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private PackageDatabase m_PackageDatabase;
        public PackageDownloadButton(AssetStoreDownloadManager assetStoreDownloadManager, PackageDatabase packageDatabase)
        {
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_PackageDatabase = packageDatabase;
        }

        protected override bool TriggerAction()
        {
            var canDownload = m_PackageDatabase.Download(m_Package);
            if (canDownload)
            {
                var isUpdate = m_Package.state == PackageState.UpdateAvailable;
                var eventName = isUpdate ? "startDownloadUpdate" : "startDownloadNew";
                PackageManagerWindowAnalytics.SendEvent(eventName, m_Package.uniqueId);
                return true;
            }
            return false;
        }

        protected override bool isVisible
        {
            get
            {
                if (m_Version?.HasTag(PackageTag.Downloadable) != true)
                    return false;

                var operation = m_AssetStoreDownloadManager.GetDownloadOperation(m_Version.packageUniqueId);
                var isDownloadOperationInProgress = operation?.isInProgress == true
                    || operation?.state == DownloadState.Pausing
                    || operation?.state == DownloadState.Paused
                    || operation?.state == DownloadState.ResumeRequested;

                var isAvailableOnDisk = m_Version?.isAvailableOnDisk ?? false;
                var hasUpdateAvailable = m_Package.state == PackageState.UpdateAvailable;
                var isLatestVersionOnDisk = isAvailableOnDisk && !hasUpdateAvailable;
                var isDownloadRequested = operation?.state == DownloadState.DownloadRequested;
                return !isLatestVersionOnDisk && (isDownloadRequested || operation == null || !isDownloadOperationInProgress);
            }
        }

        protected override string GetTooltip(bool isInProgress)
        {
            if (isInProgress)
                return L10n.Tr("The download request has been sent. Please wait for the download to start.");

            if (m_Package.state == PackageState.UpdateAvailable)
                return string.Format(L10n.Tr("Click to download the latest version of this {0}."), m_Package.GetDescriptor());

            return string.Format(L10n.Tr("Click to download this {0} for later use."), m_Package.GetDescriptor());
        }

        protected override string GetText(bool isInProgress)
        {
            return m_Package.state == PackageState.UpdateAvailable ? k_UpdateButtonText : k_DownloadButtonText;
        }

        protected override bool isInProgress => m_AssetStoreDownloadManager.GetDownloadOperation(m_Version.packageUniqueId)?.state == DownloadState.DownloadRequested;
    }
}
