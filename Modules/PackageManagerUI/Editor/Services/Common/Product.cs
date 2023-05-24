// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class Product : IProduct
    {
        [SerializeField]
        protected long m_Id;
        public long id => m_Id;

        [SerializeField]
        protected bool m_IsHidden;
        public bool isHidden => m_IsHidden;

        [SerializeField]
        protected string m_DisplayName;
        public string displayName => m_DisplayName;

        [SerializeField]
        protected string m_ProductUrl;
        public string productUrl => m_ProductUrl;

        [SerializeField]
        protected string m_Description;
        public string description => m_Description;

        [SerializeField]
        protected string m_PublishNotes;
        public string latestReleaseNotes => m_PublishNotes;

        private static long DateTimeStringToTicks(string dateTimeString)
        {
            return !string.IsNullOrEmpty(dateTimeString) ? DateTime.Parse(dateTimeString).Ticks : 0;
        }

        private static DateTime? TicksToDateTime(long ticks)
        {
            return ticks != 0 ? new DateTime(ticks, DateTimeKind.Utc) : null;
        }

        [SerializeField]
        protected long m_FirstPublishedDateTicks;
        public DateTime? firstPublishedDate => TicksToDateTime(m_FirstPublishedDateTicks);

        [SerializeField]
        protected List<PackageImage> m_Images;
        public IEnumerable<PackageImage> images => m_Images;

        [SerializeField]
        protected long m_PurchasedTimeTicks;
        public DateTime? purchasedTime => TicksToDateTime(m_PurchasedTimeTicks);

        [SerializeField]
        protected List<string> m_Labels;
        public IEnumerable<string> labels => m_Labels;

        private void SetPurchaseInfo(AssetStorePurchaseInfo purchaseInfo)
        {
            m_Labels = purchaseInfo?.tags;
            m_IsHidden = purchaseInfo?.isHidden == true;
            m_PurchasedTimeTicks = DateTimeStringToTicks(purchaseInfo?.purchasedTime);
        }

        public Product(long productId, AssetStorePurchaseInfo purchaseInfo, AssetStoreProductInfo productInfo)
        {
            m_Id = productId;
            m_DisplayName = productInfo?.displayName ?? purchaseInfo?.displayName ?? string.Empty;
            m_Description = productInfo?.description;
            m_PublishNotes = productInfo?.publishNotes;

            m_Images = productInfo?.images ?? new List<PackageImage>();

            m_ProductUrl = productInfo?.assetStoreProductUrl;

            m_FirstPublishedDateTicks = DateTimeStringToTicks(productInfo?.firstPublishedDate);
            SetPurchaseInfo(purchaseInfo);
        }
    }
}
