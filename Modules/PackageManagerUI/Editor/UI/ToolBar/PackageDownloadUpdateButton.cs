// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDownloadUpdateButton : PackageToolBarRegularButton
    {
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private AssetStoreCache m_AssetStoreCache;
        private PackageOperationDispatcher m_OperationDispatcher;
        public PackageDownloadUpdateButton(AssetStoreDownloadManager assetStoreDownloadManager, AssetStoreCache assetStoreCache, PackageOperationDispatcher operationDispatcher)
        {
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_AssetStoreCache = assetStoreCache;
            m_OperationDispatcher = operationDispatcher;
        }

        protected override bool TriggerAction(IList<IPackageVersion> versions)
        {
            var canDownload = m_OperationDispatcher.Download(versions.Select(v => v.package));
            if (canDownload)
                PackageManagerWindowAnalytics.SendEvent("startDownloadUpdate", versions);
            return true;
        }

        protected override bool TriggerAction(IPackageVersion version)
        {
            var canDownload = m_OperationDispatcher.Download(version.package);
            if (canDownload)
                PackageManagerWindowAnalytics.SendEvent("startDownloadUpdate", version);
            return canDownload;
        }

        protected override bool IsVisible(IPackageVersion version)
        {
            if (version?.HasTag(PackageTag.LegacyFormat) != true)
                return false;

            var productId = version.package.product?.id;
            var updateInfo = m_AssetStoreCache.GetUpdateInfo(productId);
            if (updateInfo?.canUpdate != true)
                return false;

            var operation = m_AssetStoreDownloadManager.GetDownloadOperation(productId);
            return operation == null || operation.state == DownloadState.DownloadRequested || !operation.isProgressVisible;
        }

        protected override string GetTooltip(IPackageVersion version, bool isInProgress)
        {
            if (isInProgress)
                return L10n.Tr("The download request has been sent. Please wait for the download to start.");

            return string.Format(L10n.Tr("Click to download the latest version of this {0}."), version.GetDescriptor());
        }

        protected override string GetText(IPackageVersion version, bool isInProgress)
        {
            return L10n.Tr("Update");
        }

        protected override bool IsInProgress(IPackageVersion version)
        {
            var productId = version?.package.product?.id;
            var operation = m_AssetStoreDownloadManager.GetDownloadOperation(productId);
            var updateInfo =  m_AssetStoreCache.GetUpdateInfo(productId);
            return updateInfo?.canUpdate == true && operation?.isInProgress == true;
        }

        protected override IEnumerable<ButtonDisableCondition> GetDisableConditions(IPackageVersion version)
        {
            yield return new ButtonDisableCondition(() => version?.HasTag(PackageTag.Disabled) ?? false,
                L10n.Tr("This package is no longer available and cannot be updated anymore."));
        }

        protected override bool IsHiddenWhenInProgress(IPackageVersion version) => true;
    }
}
