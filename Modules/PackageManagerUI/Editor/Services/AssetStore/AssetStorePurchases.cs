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
            status = filters?.status ?? Status.None;
            sortOption = filters?.sortOption ?? PageSortOption.NameAsc;
            categories = filters?.categories ?? new List<string>();
            labels = filters?.labels ?? new List<string>();
        }

        public new PurchasesQueryArgs Clone()
        {
            return (PurchasesQueryArgs)MemberwiseClone();
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder($"?offset={startIndex}&limit={limit}", 512);
            var statusQuery = ToQueryString(status);
            if (!string.IsNullOrEmpty(statusQuery))
                stringBuilder.Append($"&{statusQuery}");
            if (!string.IsNullOrEmpty(searchText))
                stringBuilder.Append($"&query={Uri.EscapeDataString(searchText)}");
            var orderBy = ToQueryString(sortOption);
            if (!string.IsNullOrEmpty(orderBy))
                stringBuilder.Append($"&{orderBy}");
            if (labels?.Count > 0)
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                stringBuilder.Append($"&tagging={string.Join(",", labels.Select(label => Uri.EscapeDataString(label)).ToArray())}");
#pragma warning restore RS0030
            if (categories?.Count > 0)
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                stringBuilder.Append($"&categories={string.Join(",", categories.Select(cat => Uri.EscapeDataString(cat)).ToArray())}");
#pragma warning restore RS0030
            if (productIds?.Count > 0)
                stringBuilder.Append($"&ids={string.Join(",", productIds.ToArray())}");
            return stringBuilder.ToString();
        }

        public static string ToQueryString(Status value)
        {
            return value switch
            {
                Status.Unlabeled => "status=unlabeled",
                Status.Hidden => "status=hidden",
                Status.Deprecated => "status=deprecated",
                Status.Downloaded => string.Empty,
                Status.Imported => string.Empty,
                Status.UpdateAvailable => string.Empty,
                Status.SubscriptionBased => string.Empty,
                Status.None => string.Empty,
                _ => string.Empty
            };
        }

        public static string ToQueryString(PageSortOption value)
        {
            return value switch
            {
                PageSortOption.NameAsc => "orderBy=name&order=asc",
                PageSortOption.NameDesc => "orderBy=name&order=desc",
                PageSortOption.UpdateDateDesc => "orderBy=update_date&order=desc",
                PageSortOption.PurchasedDateDesc => "orderBy=purchased_date&order=desc",
                PageSortOption.PublishedDateDesc => string.Empty,
                _ => string.Empty
            };
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
        public List<AssetStorePurchaseInfo> list = new();

        public string searchText => queryArgs?.searchText;
        public int startIndex => queryArgs?.startIndex ?? 0;

        #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        public IEnumerable<long> productIds => list.Select(p => p.productId);
#pragma warning restore RS0030

        public List<Category> categories = new();

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
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var matchingCategory = categories.FirstOrDefault(c => c.name == category.name);
#pragma warning restore RS0030
                if (matchingCategory != null)
                    matchingCategory.count += category.count;
                else
                    categories.Add(new Category { name = category.name, count = category.count });
            }
        }
    }

    internal partial class JsonParser
    {
        public AssetStorePurchases ParsePurchases(IDictionary<string, object> rawList)
        {
            var purchases = new AssetStorePurchases();
            purchases.total = (long)rawList["total"];

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var results = rawList.GetList<Dictionary<string, object>>("results") ?? Enumerable.Empty<Dictionary<string, object>>();
#pragma warning restore RS0030
            foreach (var item in results)
            {
                var purchase = ParsePurchaseInfo(item);
                if (purchase != null)
                    purchases.list.Add(purchase);
            }

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var categories = rawList.GetList<Dictionary<string, object>>("category") ?? Enumerable.Empty<Dictionary<string, object>>();
#pragma warning restore RS0030
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
