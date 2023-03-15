// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageResumeDownloadButton : PackageToolBarRegularButton
    {
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private PackageOperationDispatcher m_OperationDispatcher;
        private bool m_IsIconButton;
        public PackageResumeDownloadButton(AssetStoreDownloadManager assetStoreDownloadManager, PackageOperationDispatcher operationDispatcher, bool isIconButton = false)
        {
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_OperationDispatcher = operationDispatcher;
            m_IsIconButton = isIconButton;
            if (isIconButton)
            {
                element.AddToClassList("resumeIcon");
                element.AddToClassList("icon");
            }
        }

        protected override bool TriggerAction(IPackageVersion version)
        {
            m_OperationDispatcher.ResumeDownload(version.package);
            PackageManagerWindowAnalytics.SendEvent("resumeDownload", version);
            return true;
        }

        protected override bool IsVisible(IPackageVersion version)
        {
            if (version?.HasTag(PackageTag.LegacyFormat) != true)
                return false;

            var operation = m_AssetStoreDownloadManager.GetDownloadOperation(version.package.product?.id);
            return operation?.state == DownloadState.Paused || operation?.state == DownloadState.ResumeRequested;
        }

        protected override string GetTooltip(IPackageVersion version, bool isInProgress)
        {
            if (isInProgress)
                return L10n.Tr("The resume request has been sent. Please wait for the download to resume.");
            return string.Format(L10n.Tr("Click to resume the download of this {0}."), version.GetDescriptor());
        }

        protected override string GetText(IPackageVersion version, bool isInProgress) => m_IsIconButton ? string.Empty : L10n.Tr("Resume");

        protected override bool IsInProgress(IPackageVersion version) => m_AssetStoreDownloadManager.GetDownloadOperation(version.package.product?.id)?.state == DownloadState.ResumeRequested;
    }
}
