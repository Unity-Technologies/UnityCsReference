// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDowngradeButton : PackageToolBarDropdownButton
    {
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private AssetStoreCache m_AssetStoreCache;
        private PackageDatabase m_PackageDatabase;
        public PackageDowngradeButton(AssetStoreDownloadManager assetStoreDownloadManager, AssetStoreCache assetStoreCache, PackageDatabase packageDatabase)
        {
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_AssetStoreCache = assetStoreCache;
            m_PackageDatabase = packageDatabase;

            m_Element.SetIcon("warning");
        }

        protected override bool TriggerAction(IPackageVersion version)
        {
            var canDownload = m_PackageDatabase.Download(version.package);
            if (canDownload)
                PackageManagerWindowAnalytics.SendEvent("startDownloadDowngrade", version.packageUniqueId);
            return canDownload;
        }

        protected override bool IsVisible(IPackageVersion version)
        {
            if (version?.HasTag(PackageTag.Downloadable) != true)
                return false;

            var localInfo = m_AssetStoreCache.GetLocalInfo(version.packageUniqueId);
            if (localInfo?.canDowngrade != true)
                return false;

            var operation = m_AssetStoreDownloadManager.GetDownloadOperation(version.packageUniqueId);
            return operation == null || operation.state == DownloadState.DownloadRequested || !operation.isProgressVisible;
        }

        protected override string GetTooltip(IPackageVersion version, bool isInProgress)
        {
            if (isInProgress)
                return L10n.Tr("The download request has been sent. Please wait for the download to start.");

            var localInfo = m_AssetStoreCache.GetLocalInfo(version.packageUniqueId);
            return string.Format(AssetStorePackage.k_IncompatibleWarningMessage, localInfo.supportedVersion);
        }

        protected override string GetText(IPackageVersion version, bool isInProgress)
        {
            return L10n.Tr("Update");
        }

        protected override bool IsInProgress(IPackageVersion version)
        {
            var operation = m_AssetStoreDownloadManager.GetDownloadOperation(version?.packageUniqueId);
            var localInfo = m_AssetStoreCache.GetLocalInfo(version?.packageUniqueId);
            return localInfo?.canDowngrade == true && operation?.isInProgress == true;
        }

        protected override IEnumerable<ButtonDisableCondition> GetDisableConditions(IPackageVersion version)
        {
            yield return new ButtonDisableCondition(() => version?.HasTag(PackageTag.Disabled) ?? false,
                L10n.Tr("This package is no longer available and cannot be downloaded anymore."));
        }

        protected override bool IsHiddenWhenInProgress(IPackageVersion version) => true;
    }
}
