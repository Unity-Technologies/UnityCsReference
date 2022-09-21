// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class PurchasesQueryArgs : PageFilters
    {
        public int startIndex;
        public long limit;
        public string searchText;
        public List<long> productIds;

        public override bool isFilterSet => base.isFilterSet || !string.IsNullOrEmpty(searchText);

        public PurchasesQueryArgs(int startIndex = 0, int limit = 0, string searchText = null, PageFilters filters = null)
        {
            this.startIndex = startIndex;
            this.limit = limit;
            this.searchText = searchText ?? string.Empty;
            status = filters?.status ?? string.Empty;
            categories = filters?.categories ?? new List<string>();
            labels = filters?.labels ?? new List<string>();
            orderBy = filters?.orderBy ?? string.Empty;
            isReverseOrder = filters?.isReverseOrder ?? false;
        }

        public new PurchasesQueryArgs Clone()
        {
            return (PurchasesQueryArgs)MemberwiseClone();
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder($"?offset={startIndex}&limit={limit}", 512);
            if (!string.IsNullOrEmpty(status))
                stringBuilder.Append($"&status={status}");

            if (!string.IsNullOrEmpty(searchText))
                stringBuilder.Append($"&query={Uri.EscapeDataString(searchText)}");
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
                stringBuilder.Append($"&ids={string.Join(",", productIds.ToArray())}");
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

        public void AppendPurchases(AssetStorePurchases purchases)
        {
            if (purchases.total <= 0)
                return;

            total = Math.Max(total, purchases.total);

            list.AddRange(purchases.list);

            foreach (var category in purchases.categories)
            {
                var matchingCategory = categories.FirstOrDefault(c => c.name == category.name);
                if (matchingCategory != null)
                    matchingCategory.count += category.count;
                else
                    categories.Add(new Category { name = category.name, count = category.count });
            }
        }
    }

    internal partial class JsonParser
    {
        public virtual AssetStorePurchases ParsePurchases(IDictionary<string, object> rawList)
        {
            var purchases = new AssetStorePurchases();
            purchases.total = (long)rawList["total"];

            var results = rawList.GetList<Dictionary<string, object>>("results") ?? Enumerable.Empty<Dictionary<string, object>>();
            foreach (var item in results)
            {
                var purchase = ParsePurchaseInfo(item);
                if (purchase != null)
                    purchases.list.Add(purchase);
            }

            var categories = rawList.GetList<Dictionary<string, object>>("category") ?? Enumerable.Empty<Dictionary<string, object>>();
            foreach (var item in categories)
            {
                var categoryName = item.GetString("name");
                if (!string.IsNullOrEmpty(categoryName))
                    purchases.categories.Add(new AssetStorePurchases.Category { name = categoryName, count = (long)item["count"] });
            }
            return purchases;
        }
    }
}
