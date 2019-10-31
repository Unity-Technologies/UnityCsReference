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
    internal class PurchasesQueryArgs
    {
        private const int k_DefaultLimit = 100;

        public int startIndex;
        public int limit;
        public string searchText;
        public string status;
        public List<string> tags;
        public List<long> productIds;
        public string orderBy;
        public bool isReverseOrder;

        public override string ToString()
        {
            var limit = this.limit > 0 ? this.limit : k_DefaultLimit;
            var startIndex = this.startIndex > 0 ? this.startIndex : 0;
            var stringBuilder = new StringBuilder($"?offset={startIndex}&limit={limit}", 512);
            if (!string.IsNullOrEmpty(searchText))
                stringBuilder.Append($"&query={Uri.EscapeDataString(searchText)}");
            if (!string.IsNullOrEmpty(status))
                stringBuilder.Append($"&status={status}");
            if (!string.IsNullOrEmpty(orderBy))
                stringBuilder.Append($"&orderBy={orderBy}");
            if (isReverseOrder)
                stringBuilder.Append("&order=desc");
            if (tags?.Any() ?? false)
                stringBuilder.Append($"&tagging={string.Join(",", tags.Select(tag => Uri.EscapeDataString(tag)).ToArray())}");
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
