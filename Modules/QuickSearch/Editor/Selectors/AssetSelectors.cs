// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Search.Providers.AssetProvider;

namespace UnityEditor.Search
{
    class AssetSelectors
    {
        [SearchSelector("name", provider: type, priority: 99)]
        static object GetAssetName(SearchItem item)
        {
            var obj = item.ToObject();
            return obj?.name;
        }

        [SearchSelector("filename", provider: type, priority: 99)]
        static object GetAssetFilename(SearchItem item)
        {
            return Path.GetFileName(GetAssetPath(item));
        }

        [SearchSelector("type", priority: 99)]
        static string GetAssetType(SearchItem item)
        {
            if (!(SearchUtils.GetAssetPath(item) is string assetPath))
                return null;
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (assetType == typeof(GameObject))
                return "Prefab";
            return assetType?.Name;
        }

        [SearchSelector("extension", provider: type, priority: 99)]
        static string GetAssetExtension(SearchItem item)
        {
            if (GetAssetPath(item) is string assetPath)
                return Path.GetExtension(assetPath).Substring(1);
            return null;
        }

        [SearchSelector("size", priority: 99)]
        static object GetAssetFileSize(SearchItem item)
        {
            if (SearchUtils.GetAssetPath(item) is string assetPath && !string.IsNullOrEmpty(assetPath))
            {
                var fi = new FileInfo(assetPath);
                return fi.Exists ? fi.Length : 0;
            }
            return null;
        }

        public static IEnumerable<SearchColumn> Enumerate(IEnumerable<SearchItem> items)
        {
            return PropertySelectors.Enumerate(FilterItems(items, 5))
                .Concat(MaterialSelectors.Enumerate(FilterItems(items, 20)));
        }

        static IEnumerable<SearchItem> FilterItems(IEnumerable<SearchItem> items, int count)
        {
            return items.Where(e => e.provider.type == type).Take(count);
        }
    }
}
