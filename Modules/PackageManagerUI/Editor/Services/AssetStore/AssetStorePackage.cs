// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class AssetStorePackage : BasePackage
    {
        [SerializeField]
        private string m_ProductId;
        public override string uniqueId => m_ProductId;

        [SerializeField]
        private AssetStoreVersionList m_VersionList;

        [SerializeField]
        private UpmVersionList m_UpmVersionList;

        public override IVersionList versions => string.IsNullOrEmpty(name) ? m_VersionList as IVersionList : m_UpmVersionList as IVersionList;

        [SerializeField]
        private List<PackageImage> m_Images;
        [SerializeField]
        private List<PackageLink> m_Links;
        [SerializeField]
        private string m_AssetStoreLink;

        public string assetStoreLink => m_AssetStoreLink;

        public override IEnumerable<PackageImage> images => m_Images;

        public override IEnumerable<PackageLink> links => m_Links;

        [SerializeField]
        private List<string> m_Labels;
        public override IEnumerable<string> labels => m_Labels;

        [SerializeField]
        protected long m_PurchasedTimeTicks;
        public override DateTime? purchasedTime => m_PurchasedTimeTicks == 0 ? (DateTime?)null : new DateTime(m_PurchasedTimeTicks, DateTimeKind.Utc);

        public void ResolveDependencies(AssetStoreUtils assetStoreUtils, IOProxy ioProxy)
        {
            m_VersionList?.ResolveDependencies(assetStoreUtils, ioProxy);
        }

        public AssetStorePackage(AssetStoreUtils assetStoreUtils, IOProxy ioProxy, string productId, UIError error)
        {
            ResolveDependencies(assetStoreUtils, ioProxy);

            m_Errors = new List<UIError> { error };
            m_Progress = PackageProgress.None;
            m_Type = PackageType.AssetStore;
            m_Name = string.Empty;
            m_ProductId = productId;

            m_Images = new List<PackageImage>();
            m_Links = new List<PackageLink>();
            m_VersionList = new AssetStoreVersionList(assetStoreUtils, ioProxy);
            m_UpmVersionList = new UpmVersionList();

            m_Labels = new List<string>();
            m_PurchasedTimeTicks = 0;
        }

        public AssetStorePackage(AssetStoreUtils assetStoreUtils, IOProxy ioProxy, AssetStorePurchaseInfo purchaseInfo, AssetStoreProductInfo productInfo, AssetStoreLocalInfo localInfo = null)
        {
            ResolveDependencies(assetStoreUtils, ioProxy);

            m_Errors = new List<UIError>();
            m_Progress = PackageProgress.None;
            m_Type = PackageType.AssetStore;
            m_Name = string.Empty;
            m_ProductId = productInfo?.id.ToString();
            m_Images = productInfo?.images ?? new List<PackageImage>();
            m_Links = productInfo?.links ?? new List<PackageLink>();
            m_VersionList = new AssetStoreVersionList(assetStoreUtils, ioProxy);
            m_UpmVersionList = new UpmVersionList();
            m_AssetStoreLink = productInfo?.assetStoreLink.url;

            var firstPublishedDateString = productInfo?.firstPublishedDate ?? string.Empty;
            m_FirstPublishedDateTicks = !string.IsNullOrEmpty(firstPublishedDateString) ? DateTime.Parse(firstPublishedDateString).Ticks : 0;

            m_Labels = purchaseInfo?.tags;
            m_PurchasedTimeTicks = !string.IsNullOrEmpty(purchaseInfo?.purchasedTime) ? DateTime.Parse(purchaseInfo?.purchasedTime).Ticks : 0;

            if (purchaseInfo == null)
            {
                var errorMessage = L10n.Tr("Unable to get purchase details because you may not have purchased this package.");
                AddError(new UIError(UIErrorCode.AssetStorePackageError, errorMessage));
            }
            if (string.IsNullOrEmpty(productInfo?.id) || string.IsNullOrEmpty(productInfo?.versionId))
            {
                AddError(new UIError(UIErrorCode.AssetStorePackageError, L10n.Tr("Invalid product details.")));
            }
            else if (localInfo == null)
            {
                m_VersionList.AddVersion(new AssetStorePackageVersion(assetStoreUtils, ioProxy, productInfo));
            }
            else
            {
                m_VersionList.AddVersion(new AssetStorePackageVersion(assetStoreUtils, ioProxy, productInfo, localInfo));
                if (localInfo.canUpdate && (localInfo.versionId != productInfo.versionId || localInfo.versionString != productInfo.versionString))
                    m_VersionList.AddVersion(new AssetStorePackageVersion(assetStoreUtils, ioProxy, productInfo));
            }
        }

        public AssetStorePackage(AssetStoreUtils assetStoreUtils, IOProxy ioProxy, AssetStorePurchaseInfo purchaseInfo, AssetStoreProductInfo productInfo, UpmPackage package)
        {
            ResolveDependencies(assetStoreUtils, ioProxy);

            m_Errors = new List<UIError>();
            m_Progress = PackageProgress.None;
            m_Type = PackageType.AssetStore;
            m_Name = package?.name ?? string.Empty;
            m_ProductId = productInfo?.id.ToString();

            m_Images = productInfo?.images ?? new List<PackageImage>();
            m_Links = productInfo?.links ?? new List<PackageLink>();
            m_VersionList = new AssetStoreVersionList(assetStoreUtils, ioProxy);

            m_Labels = purchaseInfo?.tags;
            m_PurchasedTimeTicks = !string.IsNullOrEmpty(purchaseInfo?.purchasedTime) ? DateTime.Parse(purchaseInfo?.purchasedTime).Ticks : 0;

            m_UpmVersionList = package?.versions as UpmVersionList ?? new UpmVersionList();
            if (productInfo != null)
            {
                foreach (var version in m_UpmVersionList.Cast<UpmPackageVersion>())
                    version.UpdateProductInfo(productInfo);
            }

            m_AssetStoreLink = productInfo?.assetStoreLink.url;

            var firstPublishedDateString = productInfo?.firstPublishedDate ?? string.Empty;
            m_FirstPublishedDateTicks = !string.IsNullOrEmpty(firstPublishedDateString) ? DateTime.Parse(firstPublishedDateString).Ticks : 0;

            if (purchaseInfo == null)
            {
                var errorMessage = L10n.Tr("Unable to get asset purchase details because you may not have purchased this package.");
                AddError(new UIError(UIErrorCode.AssetStorePackageError, errorMessage));
            }
            if (string.IsNullOrEmpty(productInfo?.id) || string.IsNullOrEmpty(productInfo?.versionId))
                AddError(new UIError(UIErrorCode.AssetStorePackageError, L10n.Tr("Unable to retrieve asset product details.")));
            else if (string.IsNullOrEmpty(package?.name))
                AddError(new UIError(UIErrorCode.AssetStorePackageError, L10n.Tr("Unable to retrieve asset package info.")));
        }

        public override IPackage Clone()
        {
            return (IPackage)MemberwiseClone();
        }
    }
}
