// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageCancelDownloadButton : PackageToolBarRegularButton
    {
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private PackageDatabase m_PackageDatabase;
        public PackageCancelDownloadButton(AssetStoreDownloadManager assetStoreDownloadManager, PackageDatabase packageDatabase)
        {
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_PackageDatabase = packageDatabase;
        }

        protected override bool TriggerAction()
        {
            m_PackageDatabase.AbortDownload(m_Package);

            PackageManagerWindowAnalytics.SendEvent("abortDownload", m_Package.uniqueId);
            return true;
        }

        protected override bool isVisible
        {
            get
            {
                if (m_Version?.HasTag(PackageTag.Downloadable) != true)
                    return false;

                var operation = m_AssetStoreDownloadManager.GetDownloadOperation(m_Version.packageUniqueId);
                return operation?.isInProgress == true
                    || operation?.state == DownloadState.Pausing
                    || operation?.state == DownloadState.Paused
                    || operation?.state == DownloadState.ResumeRequested;
            }
        }

        protected override IEnumerable<ButtonDisableCondition> GetDisableConditions()
        {
            var operation = m_AssetStoreDownloadManager.GetDownloadOperation(m_Version.packageUniqueId);
            var resumeRequsted = operation?.state == DownloadState.ResumeRequested;
            yield return new ButtonDisableCondition(resumeRequsted,
                L10n.Tr("A resume request has been sent. You cannot cancel this download until it is resumed."));
        }

        protected override string GetTooltip(bool isInProgress)
        {
            return string.Format(L10n.Tr("Click to cancel the download of this {0}."), m_Package.GetDescriptor());
        }

        protected override string GetText(bool isInProgress)
        {
            return L10n.Tr("Cancel");
        }

        protected override bool isInProgress => false;
    }
}
