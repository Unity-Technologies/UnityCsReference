// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageResumeDownloadButton : PackageToolBarRegularButton
    {
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private PackageDatabase m_PackageDatabase;
        public PackageResumeDownloadButton(AssetStoreDownloadManager assetStoreDownloadManager,
                                           PackageDatabase packageDatabase)
        {
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_PackageDatabase = packageDatabase;
        }

        protected override bool TriggerAction()
        {
            m_PackageDatabase.ResumeDownload(m_Package);
            PackageManagerWindowAnalytics.SendEvent("resumeDownload", m_Package.uniqueId);
            return true;
        }

        protected override bool isVisible
        {
            get
            {
                if (m_Version?.HasTag(PackageTag.Downloadable) != true)
                    return false;

                var operation = m_AssetStoreDownloadManager.GetDownloadOperation(m_Version.packageUniqueId);
                return operation?.state == DownloadState.Paused || operation?.state == DownloadState.ResumeRequested;
            }
        }

        protected override string GetTooltip(bool isInProgress)
        {
            if (isInProgress)
                return L10n.Tr("The resume request has been sent. Please wait for the download to resume.");
            return string.Format(L10n.Tr("Click to resume the download of this {0}."), m_Package.GetDescriptor());
        }

        protected override string GetText(bool isInProgress)
        {
            return L10n.Tr("Resume");
        }

        protected override bool isInProgress => m_AssetStoreDownloadManager.GetDownloadOperation(m_Version.packageUniqueId)?.state == DownloadState.ResumeRequested;
    }
}
