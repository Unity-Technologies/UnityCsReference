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
        private PackageOperationDispatcher m_OperationDispatcher;
        private UnityConnectProxy m_UnityConnect;
        public PackageRedownloadButton(AssetStoreDownloadManager assetStoreDownloadManager, AssetStoreCache assetStoreCache, PackageOperationDispatcher operationDispatcher, UnityConnectProxy unityConnect)
        {
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_AssetStoreCache = assetStoreCache;
            m_OperationDispatcher = operationDispatcher;
            m_UnityConnect = unityConnect;
        }

        protected override bool TriggerAction(IPackageVersion version)
        {
            var canDownload = m_OperationDispatcher.Download(version.package);
            if (canDownload)
                PackageManagerWindowAnalytics.SendEvent("startReDownload", version);
            return canDownload;
        }

        protected override bool IsVisible(IPackageVersion version)
        {
            if (!m_UnityConnect.isUserLoggedIn)
                return false;

            if (version?.HasTag(PackageTag.LegacyFormat) != true)
                return false;

            var productId = version.package.product?.id;
            var localInfo = m_AssetStoreCache.GetLocalInfo(productId);
            var updateInfo = m_AssetStoreCache.GetUpdateInfo(productId);
            var operation = m_AssetStoreDownloadManager.GetDownloadOperation(productId);
            return localInfo != null && updateInfo?.canUpdateOrDowngrade != true
                && (operation == null || operation.state == DownloadState.DownloadRequested || !operation.isProgressVisible);
        }

        protected override string GetTooltip(IPackageVersion version, bool isInProgress)
        {
            if (isInProgress)
                return L10n.Tr("The re-download request has been sent. Please wait for the re-download to start.");

            return string.Format(L10n.Tr("Click to re-download this {0} to get the current editor's version."), version.GetDescriptor());
        }

        protected override string GetText(IPackageVersion version, bool isInProgress)
        {
            return L10n.Tr("Re-Download");
        }

        protected override bool IsInProgress(IPackageVersion version)
        {
            var productId = version?.package.product?.id;
            var operation = m_AssetStoreDownloadManager.GetDownloadOperation(productId);
            var localInfo = m_AssetStoreCache.GetLocalInfo(productId);
            var updateInfo = m_AssetStoreCache.GetUpdateInfo(productId);
            return localInfo != null && updateInfo?.canUpdateOrDowngrade != true && operation?.isInProgress == true;
        }

        protected override IEnumerable<ButtonDisableCondition> GetDisableConditions(IPackageVersion version)
        {
            yield return new ButtonDisableCondition(() => version?.HasTag(PackageTag.Disabled) ?? false,
                L10n.Tr("This package is no longer available and cannot be downloaded anymore."));
        }

        protected override bool IsHiddenWhenInProgress(IPackageVersion version) => true;
    }
}
