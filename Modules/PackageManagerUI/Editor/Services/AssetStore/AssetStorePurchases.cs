// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class PurchasesQueryArgs : PageFilters
    {
        public int startIndex;
        public int limit;
        public List<long> productIds;

        public override string ToString()
        {
            var limit = this.limit;
            var startIndex = this.startIndex > 0 ? this.startIndex : 0;
            var stringBuilder = new StringBuilder($"?offset={startIndex}&limit={limit}", 512);
            if (!string.IsNullOrEmpty(searchText))
                stringBuilder.Append($"&query={Uri.EscapeDataString(searchText)}");
            if (statuses?.Any() ?? false)
                stringBuilder.Append($"&status={statuses.FirstOrDefault()}");
            if (!string.IsNullOrEmpty(orderBy))
            {
                stringBuilder.Append($"&orderBy={orderBy}");
                stringBuilder.Append(isReverseOrder ? "&order=desc" : "&order=asc");
            }
            if (labels?.Any() ?? false)
                stringBuilder.Append($"&tagging={string.Join(",", labels.Select(label => Uri.EscapeDataString(label)).ToArray())}");
            if (categories?.Any() ?? false)
                stringBuilder.Append($"&categories={string.Join(",", categories.Select(cat => Uri.EscapeDataString(cat)).ToArray())}");
            if (productIds?.Any() ?? false)
                stringBuilder.Append($"&ids={string.Join(",", productIds.Select(id => id.ToString()).ToArray())}");
            return stringBuilder.ToString();
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

        public void ParsePurchases(IDictionary<string, object> rawList)
        {
            total = (long)rawList["total"];
            if (total <= 0)
                return;

            var results = rawList.GetList<Dictionary<string, object>>("results") ?? Enumerable.Empty<Dictionary<string, object>>();
            foreach (var item in results)
                list.Add(AssetStorePurchaseInfo.ParsePurchaseInfo(item));

            var categories = rawList.GetList<Dictionary<string, object>>("category") ?? Enumerable.Empty<Dictionary<string, object>>();
            foreach (var item in categories)
                this.categories.Add(new Category { name = item.GetString("name"), count = (long)item["count"] });
        }
    }
}
