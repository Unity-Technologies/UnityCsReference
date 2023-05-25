// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class AssetStorePackage : BasePackage
    {
        [SerializeField]
        private string m_ProductId;
        public override string uniqueId => m_ProductId;
        public override string productId => m_ProductId;

        [SerializeField]
        private AssetStoreVersionList m_VersionList;

        [SerializeField]
        private UpmVersionList m_UpmVersionList;

        [SerializeField]
        private PlaceholderVersionList m_PlaceholderVersionList;

        public override IVersionList versions
        {
            get
            {
                if (m_PlaceholderVersionList?.Any() == true)
                    return m_PlaceholderVersionList;
                if (string.IsNullOrEmpty(name))
                    return m_VersionList;
                return m_UpmVersionList;
            }
        }

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

        private AssetStorePackage(AssetStorePurchaseInfo purchaseInfo, AssetStoreProductInfo productInfo)
        {
            m_Errors = new List<UIError>();
            m_Progress = PackageProgress.None;
            m_Type = PackageType.AssetStore;
            m_Name = productInfo?.packageName;
            m_ProductId = productInfo?.id.ToString();
            m_ProductDisplayName = productInfo?.displayName;
            m_ProductDescription = productInfo?.description;
            m_PublishNotes = productInfo?.publishNotes;
            m_PublisherName = productInfo?.publisherName;
            m_PublisherLink = productInfo?.publisherLink;

            m_Images = productInfo?.images ?? new List<PackageImage>();
            m_Links = productInfo?.links ?? new List<PackageLink>();

            m_Labels = purchaseInfo?.tags;
            m_PurchasedTimeTicks = !string.IsNullOrEmpty(purchaseInfo?.purchasedTime) ? DateTime.Parse(purchaseInfo?.purchasedTime).Ticks : 0;

            var firstPublishedDateString = productInfo?.firstPublishedDate ?? string.Empty;
            m_FirstPublishedDateTicks = !string.IsNullOrEmpty(firstPublishedDateString) ? DateTime.Parse(firstPublishedDateString).Ticks : 0;

            m_AssetStoreLink = productInfo?.assetStoreLink.url;
        }

        public AssetStorePackage(AssetStorePurchaseInfo purchaseInfo, AssetStoreProductInfo productInfo, AssetStoreVersionList versionList)
            : this(purchaseInfo, productInfo)
        {
            m_VersionList = versionList;
            RefreshPackageTypeFromVersions();
            LinkPackageAndVersions();
        }

        // We are passing packageName and productId in this particular constructor because for regular AssetStore packages
        // those two fields are inferred from productInfo, in the case of Upm on AssetStore package, productInfo could be null sometimes
        public AssetStorePackage(string packageName, string productId, AssetStorePurchaseInfo purchaseInfo, AssetStoreProductInfo productInfo, UpmVersionList upmVersionList)
            : this(purchaseInfo, productInfo)
        {
            m_Name = packageName;
            m_ProductId = productId;
            m_UpmVersionList = upmVersionList;
            m_Type |= PackageType.Upm;

            RefreshPackageTypeFromVersions();
            LinkPackageAndVersions();
        }

        public AssetStorePackage(string packageName, string productId, AssetStorePurchaseInfo purchaseInfo, AssetStoreProductInfo productInfo, PlaceholderVersionList placeholderVersionList)
            : this(purchaseInfo, productInfo)
        {
            m_Name = packageName;
            m_ProductId = productId;
            m_PlaceholderVersionList = placeholderVersionList;
            if (!string.IsNullOrEmpty(packageName))
                m_Type |= PackageType.Upm;

            RefreshPackageTypeFromVersions();
            LinkPackageAndVersions();
        }

        public override IPackage Clone()
        {
            return (IPackage)MemberwiseClone();
        }
    }
}
