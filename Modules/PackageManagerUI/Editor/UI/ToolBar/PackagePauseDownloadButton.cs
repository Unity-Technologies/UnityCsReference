// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackagePauseDownloadButton : PackageToolBarRegularButton
    {
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private PackageOperationDispatcher m_OperationDispatcher;
        private bool m_IsIconButton;
        public PackagePauseDownloadButton(AssetStoreDownloadManager assetStoreDownloadManager, PackageOperationDispatcher operationDispatcher, bool isIconButton = false)
        {
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_OperationDispatcher = operationDispatcher;
            m_IsIconButton = isIconButton;
            if (isIconButton)
            {
                element.AddToClassList("pauseIcon");
                element.AddToClassList("icon");
            }
        }

        protected override bool TriggerAction(IPackageVersion version)
        {
            m_OperationDispatcher.PauseDownload(version.package);
            PackageManagerWindowAnalytics.SendEvent("pauseDownload", version);
            return true;
        }

        protected override bool IsVisible(IPackageVersion version)
        {
            if (version?.HasTag(PackageTag.LegacyFormat) != true)
                return false;

            var operation = m_AssetStoreDownloadManager.GetDownloadOperation(version.package.product?.id);

            // We only want to see two icons at the same time (cancel + resume OR cancel + pause)
            // So we hide the pause button when the resume button is shown, that's why we check the ResumeRequested state
            return operation?.state != DownloadState.ResumeRequested && (operation?.isInProgress == true || operation?.state == DownloadState.Pausing);
        }

        protected override string GetTooltip(IPackageVersion version, bool isInProgress)
        {
            if (isInProgress)
                return L10n.Tr("The pause request has been sent. Please wait for the download to pause.");
            return string.Format(L10n.Tr("Click to pause the download of this {0}."), version.GetDescriptor());
        }

        protected override string GetText(IPackageVersion version, bool isInProgress) => m_IsIconButton ? string.Empty : L10n.Tr("Pause");

        protected override bool IsInProgress(IPackageVersion version) => m_AssetStoreDownloadManager.GetDownloadOperation(version.package.product?.id).state == DownloadState.Pausing;
    }
}
