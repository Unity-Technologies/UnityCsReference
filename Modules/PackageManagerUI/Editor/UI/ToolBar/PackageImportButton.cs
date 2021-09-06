// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageImportButton : PackageToolBarRegularButton
    {
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private PackageDatabase m_PackageDatabase;
        public PackageImportButton(AssetStoreDownloadManager assetStoreDownloadManager, PackageDatabase packageDatabase)
        {
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_PackageDatabase = packageDatabase;
        }

        protected override bool TriggerAction()
        {
            m_PackageDatabase.Import(m_Package);
            PackageManagerWindowAnalytics.SendEvent("import", m_Package.uniqueId);
            return true;
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

                return !isDownloadOperationInProgress
                    && m_Version.HasTag(PackageTag.Importable)
                    && m_Version.isAvailableOnDisk
                    && m_Package.state != PackageState.InProgress;
            }
        }

        protected override string GetTooltip(bool isInProgress)
        {
            return string.Format(L10n.Tr("Click to import assets from the {0} into your project."), m_Package.GetDescriptor());
        }

        protected override string GetText(bool isInProgress)
        {
            return L10n.Tr("Import");
        }

        protected override bool isInProgress => false;
    }
}
