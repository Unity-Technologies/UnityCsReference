// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class PurchasesQueryArgs : PageFilters
    {
        private static readonly string k_DownloadedStatus = "downloaded";

        public int startIndex;
        public int limit;
        public List<string> productIds;

        public string status => statuses?.FirstOrDefault() ?? string.Empty;
        public bool downloadedOnly => k_DownloadedStatus.Equals(status, StringComparison.OrdinalIgnoreCase);

        public new PurchasesQueryArgs Clone()
        {
            return (PurchasesQueryArgs)MemberwiseClone();
        }
    }

    [Serializable]
    internal class AssetStorePurchases
    {
        [Serializable]
        public class Category
        {
            public string name;
            public long count;
        }

        public long total;
        public PurchasesQueryArgs queryArgs;
        public List<AssetStorePurchaseInfo> list = new List<AssetStorePurchaseInfo>();

        public string searchText => queryArgs?.searchText;
        public int startIndex => queryArgs?.startIndex ?? 0;

        public IEnumerable<long> productIds => list.Select(p => p.productId);

        public List<Category> categories = new List<Category>();

        public AssetStorePurchases(PurchasesQueryArgs queryArgs = null)
        {
            this.queryArgs = queryArgs ?? new PurchasesQueryArgs();
        }

        public void AppendPurchases(IDictionary<string, object> rawList)
        {
            var parsedTotal = (long)rawList["total"];
            if (parsedTotal <= 0)
                return;
            total = parsedTotal;

            var results = rawList.GetList<Dictionary<string, object>>("results") ?? Enumerable.Empty<Dictionary<string, object>>();
            foreach (var item in results)
                list.Add(AssetStorePurchaseInfo.ParsePurchaseInfo(item));

            var categories = rawList.GetList<Dictionary<string, object>>("category") ?? Enumerable.Empty<Dictionary<string, object>>();
            foreach (var item in categories)
            {
                var categoryName = item.GetString("name");
                var matchingCategory = this.categories.FirstOrDefault(c => c.name == categoryName);
                if (matchingCategory != null)
                    matchingCategory.count += (long)item["count"];
                else
                    this.categories.Add(new Category { name = categoryName, count = (long)item["count"] });
            }
        }
    }
}
