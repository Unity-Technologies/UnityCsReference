// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageRedownloadButton : PackageToolBarRegularButton
    {
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private AssetStoreCache m_AssetStoreCache;
        private PackageDatabase m_PackageDatabase;
        public PackageRedownloadButton(AssetStoreDownloadManager assetStoreDownloadManager, AssetStoreCache assetStoreCache, PackageDatabase packageDatabase)
        {
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_AssetStoreCache = assetStoreCache;
            m_PackageDatabase = packageDatabase;
        }

        protected override bool TriggerAction(IPackageVersion version)
        {
            var canDownload = m_PackageDatabase.Download(version.package);
            if (canDownload)
            {
                PackageManagerWindowAnalytics.SendEvent("startReDownload", version.packageUniqueId);
                return true;
            }
            return false;
        }

        protected override bool IsVisible(IPackageVersion version)
        {
            if (version?.HasTag(PackageTag.Downloadable) != true)
                return false;

            var localInfo = m_AssetStoreCache.GetLocalInfo(version.packageUniqueId);
            var operation = m_AssetStoreDownloadManager.GetDownloadOperation(version.packageUniqueId);
            return localInfo?.canUpdate == false
                && (operation == null || operation.state == DownloadState.DownloadRequested || !operation.isProgressVisible);
        }

        protected override string GetTooltip(IPackageVersion version, bool isInProgress)
        {
            if (isInProgress)
                return L10n.Tr("The re-download request has been sent. Please wait for the re-download to start.");

            return string.Format(L10n.Tr("Click to re-download this {0} to get the current editor's version."), version.package.GetDescriptor());
        }

        protected override string GetText(IPackageVersion version, bool isInProgress)
        {
            return L10n.Tr("Re-Download");
        }

        protected override bool IsInProgress(IPackageVersion version)
        {
            var operation = m_AssetStoreDownloadManager.GetDownloadOperation(version?.packageUniqueId);
            var localInfo = m_AssetStoreCache.GetLocalInfo(version?.packageUniqueId);
            return localInfo?.canUpdate == false
                && operation != null
                && operation.state != DownloadState.Aborted
                && operation.state != DownloadState.Error
                && operation.state != DownloadState.Completed
                && operation.state != DownloadState.None;
        }

        protected override IEnumerable<ButtonDisableCondition> GetDisableConditions(IPackageVersion version)
        {
            yield return new ButtonDisableCondition(() => version?.HasTag(PackageTag.Disabled) ?? false,
                L10n.Tr("This package is no longer available and cannot be downloaded anymore."));
        }

        protected override bool IsHiddenWhenInProgress(IPackageVersion version) => true;
    }
}
