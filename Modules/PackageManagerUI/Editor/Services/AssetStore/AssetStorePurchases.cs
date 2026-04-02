// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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
        public long[] productIds;

        public override bool isFilterSet => base.isFilterSet || !string.IsNullOrEmpty(searchText);

        public PurchasesQueryArgs(int startIndex = 0, int limit = 0, string searchText = null, IPageFilters filters = null) : base(filters)
        {
            this.startIndex = startIndex;
            this.limit = limit;
            this.searchText = searchText ?? string.Empty;
        }

        public PurchasesQueryArgs Clone()
        {
            return (PurchasesQueryArgs)MemberwiseClone();
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder($"?offset={startIndex}&limit={limit}", 512);
            stringBuilder.Append(ToQueryString(status));
            if (!string.IsNullOrEmpty(searchText))
            {
                stringBuilder.Append("&query=");
                stringBuilder.Append(Uri.EscapeDataString(searchText));
            }
            stringBuilder.Append(ToQueryString(sortOption));
            if (labels?.Count > 0)
            {
                stringBuilder.Append("&tagging=");
                stringBuilder.AppendJoin(',', labels.SelectAsEnumerable(Uri.EscapeDataString));
            }
            if (categories?.Count > 0)
            {
                stringBuilder.Append("&categories=");
                stringBuilder.AppendJoin(',', categories.SelectAsEnumerable(Uri.EscapeDataString));
            }
            if (productIds?.Length > 0)
            {
                stringBuilder.Append("&ids=");
                stringBuilder.AppendJoin(',', productIds);
            }
            return stringBuilder.ToString();
        }

        public static string ToQueryString(PageFilterStatus value)
        {
            return value switch
            {
                PageFilterStatus.Unlabeled => "&status=unlabeled",
                PageFilterStatus.Hidden => "&status=hidden",
                PageFilterStatus.Deprecated => "&status=deprecated",
                _ => string.Empty
            };
        }

        public static string ToQueryString(PageSortOption value)
        {
            return value switch
            {
                PageSortOption.NameAsc => "&orderBy=name&order=asc",
                PageSortOption.NameDesc => "&orderBy=name&order=desc",
                PageSortOption.UpdateDateDesc => "&orderBy=update_date&order=desc",
                PageSortOption.PurchasedDateDesc => "&orderBy=purchased_date&order=desc",
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
                var matchingCategory = categories.FirstMatch(c => c.name == category.name);
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
            var purchases = new AssetStorePurchases { total = (long)rawList["total"] };
            var results = rawList.GetEnumerable<Dictionary<string, object>>("results") ?? Array.Empty<Dictionary<string, object>>();
            foreach (var item in results)
            {
                var purchase = ParsePurchaseInfo(item);
                if (purchase != null)
                    purchases.list.Add(purchase);
            }

            var categories = rawList.GetEnumerable<Dictionary<string, object>>("category") ?? Array.Empty<Dictionary<string, object>>();
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
