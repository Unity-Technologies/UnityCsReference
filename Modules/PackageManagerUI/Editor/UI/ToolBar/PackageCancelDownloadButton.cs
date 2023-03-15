// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageCancelDownloadButton : PackageToolBarRegularButton
    {
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private PackageOperationDispatcher m_OperationDispatcher;
        private bool m_IsIconButton;
        public PackageCancelDownloadButton(AssetStoreDownloadManager assetStoreDownloadManager, PackageOperationDispatcher operationDispatcher, bool isIconButton = false)
        {
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_OperationDispatcher = operationDispatcher;
            m_IsIconButton = isIconButton;
            if (isIconButton)
            {
                element.AddToClassList("cancelIcon");
                element.AddToClassList("icon");
            }
        }

        protected override bool TriggerAction(IList<IPackageVersion> versions)
        {
            m_OperationDispatcher.AbortDownload(versions.Select(v => v.package));
            PackageManagerWindowAnalytics.SendEvent("abortDownload", versions);
            return true;
        }

        protected override bool TriggerAction(IPackageVersion version)
        {
            m_OperationDispatcher.AbortDownload(version.package);
            PackageManagerWindowAnalytics.SendEvent("abortDownload", version);
            return true;
        }

        protected override bool IsVisible(IPackageVersion version)
        {
            if (version?.HasTag(PackageTag.LegacyFormat) != true)
                return false;

            var operation = m_AssetStoreDownloadManager.GetDownloadOperation(version.package.product?.id);
            return operation?.isProgressVisible == true;
        }

        protected override IEnumerable<ButtonDisableCondition> GetDisableConditions(IPackageVersion version)
        {
            var operation = m_AssetStoreDownloadManager.GetDownloadOperation(version.package.product?.id);
            var resumeRequested = operation?.state == DownloadState.ResumeRequested;
            yield return new ButtonDisableCondition(resumeRequested,
                L10n.Tr("A resume request has been sent. You cannot cancel this download until it is resumed."));
        }

        protected override string GetTooltip(IPackageVersion version, bool isInProgress)
        {
            return string.Format(L10n.Tr("Click to cancel the download of this {0}."), version.GetDescriptor());
        }

        protected override string GetText(IPackageVersion version, bool isInProgress) => m_IsIconButton ? string.Empty : L10n.Tr("Cancel");

        protected override bool IsInProgress(IPackageVersion version) => false;
    }
}
