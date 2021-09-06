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

        protected override bool TriggerAction()
        {
            m_PackageDatabase.PauseDownload(m_Package);
            PackageManagerWindowAnalytics.SendEvent("pauseDownload", m_Package.uniqueId);
            return true;
        }

        protected override bool isVisible
        {
            get
            {
                if (m_Version?.HasTag(PackageTag.Downloadable) != true)
                    return false;

                var operation = m_AssetStoreDownloadManager.GetDownloadOperation(m_Version.packageUniqueId);
                return operation?.isInProgress == true || operation?.state == DownloadState.Pausing;
            }
        }

        protected override string GetTooltip(bool isInProgress)
        {
            if (isInProgress)
                return L10n.Tr("The pause request has been sent. Please wait for the download to pause.");
            return string.Format(L10n.Tr("Click to pause the download of this {0}."), m_Package.GetDescriptor());
        }

        protected override string GetText(bool isInProgress)
        {
            return L10n.Tr("Pause");
        }

        protected override bool isInProgress => m_AssetStoreDownloadManager.GetDownloadOperation(m_Version.packageUniqueId).state == DownloadState.Pausing;
    }
}
