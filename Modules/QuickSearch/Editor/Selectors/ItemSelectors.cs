// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Search
{
    static class ItemSelectors
    {
        [SearchSelector("id", priority: 1)] static object GetSearchItemID(SearchItem item) => item.id;
        [SearchSelector("label", priority: 1)] static object GetSearchItemLabel(SearchItem item) => item.GetLabel(item.context);
        [SearchSelector("description", priority: 1)] static object GetSearchItemDesc(SearchItem item) => item.GetDescription(item.context);
        [SearchSelector("value", priority: 1)] static object GetSearchItemValue(SearchItem item) => item.value;
        [SearchSelector("provider", priority: 9)] static object GetSearchItemProvider(SearchItem item) => item.provider?.name;
        [SearchSelector("score", priority: 9)] static object GetSearchItemScore(SearchItem item) => item.score;
        [SearchSelector("options", priority: 9)] static object GetSearchItemOptions(SearchItem item) => item.options;
        [SearchSelector("data", priority: 9)] static object GetSearchItemData(SearchItem item) => item.data?.ToString();
        [SearchSelector("thumbnail", priority: 9)] static object GetSearchItemThumbnail(SearchItem item) => item.GetThumbnail(item.context, cacheThumbnail: false);

        [SearchSelector("Field/(?<fieldName>.+)", priority: 9, printable: false)]
        static object GetSearchItemFieldValue(SearchSelectorArgs args) => args.current.GetValue(args["fieldName"].ToString());

        public static SearchColumn CreateColumn(string path, string selector = null, string provider = null, SearchColumnFlags options = SearchColumnFlags.Default)
        {
            var pname = SearchColumn.ParseName(path);
            return new SearchColumn(path, selector ?? path, provider, new GUIContent(pname), options);
        }

        public static IEnumerable<SearchColumn> Enumerate(IEnumerable<SearchItem> items = null)
        {
            yield return CreateColumn("Label", null, "Name");
            yield return CreateColumn("Description");

            if (items != null)
            {
                yield return CreateColumn("ID");
                yield return CreateColumn("Name", null, "Name");
                yield return CreateColumn("Value");
                yield return CreateColumn("Thumbnail");
                yield return CreateColumn("Default/Path", "path");
                yield return CreateColumn("Default/Type", "type");
                yield return CreateColumn("Default/Provider", "provider");
                yield return CreateColumn("Default/Score", "score");
                yield return CreateColumn("Default/Options", "options");
                yield return CreateColumn("Default/Data", "data");

                var firstItem = items.FirstOrDefault();
                if (firstItem != null && firstItem.GetFieldCount() > 0)
                {
                    foreach (var f in firstItem.GetFields())
                        yield return CreateColumn($"Field/{f.label}", f.name);
                }
            }
        }

        private static object GetName(SearchColumnEventArgs args)
        {
            var value = args.column.SelectValue(args.item, args.item.context ?? args.context);
            if (value is Object obj)
                return obj;
            return (value ?? args.value)?.ToString();
        }

        private static object DrawName(SearchColumnEventArgs args)
        {
            if (args.value is Object obj)
            {
                GUI.Label(args.rect, Utils.GUIContentTemp(obj.name, AssetPreview.GetMiniThumbnail(obj)), GetItemContentStyle(args.column));
            }
            else if (args.value != null)
            {
                var item = args.item;
                var thumbnail = item.GetThumbnail(item.context ?? args.context);
                GUI.Label(args.rect, Utils.GUIContentTemp(args.value.ToString(), thumbnail), GetItemContentStyle(args.column));
            }
            return args.value;
        }

        [SearchColumnProvider("Name")]
        internal static void InitializeItemNameColumn(SearchColumn column)
        {
            column.getter = GetName;
            column.drawer = DrawName;
        }

        public static GUIStyle GetItemContentStyle(SearchColumn column)
        {
            if (column.options.HasAny(SearchColumnFlags.TextAlignmentCenter))
                return Styles.itemLabelCenterAligned;
            if (column.options.HasAny(SearchColumnFlags.TextAlignmentRight))
                return Styles.itemLabelrightAligned;
            return Styles.itemLabelLeftAligned;
        }

        [SearchColumnProvider("size")]
        internal static void InitializeItemSizeColumn(SearchColumn column)
        {
            column.drawer = args =>
            {
                var itemStyle = GetItemContentStyle(args.column);
                if (Utils.TryGetNumber(args.value, out var n))
                    GUI.Label(args.rect, Utils.FormatBytes((long)n), itemStyle);
                else
                    GUI.Label(args.rect, args.value?.ToString() ?? string.Empty, itemStyle);
                return args.value;
            };
        }

        [SearchColumnProvider("count")]
        internal static void InitializeCountColumn(SearchColumn column)
        {
            column.drawer = args =>
            {
                var itemStyle = GetItemContentStyle(args.column);
                if (Utils.TryGetNumber(args.value, out var n))
                    GUI.Label(args.rect, Utils.FormatCount((ulong)n), itemStyle);
                else
                    GUI.Label(args.rect, args.value?.ToString() ?? string.Empty, itemStyle);
                return args.value;
            };
        }

        [SearchColumnProvider("selectable")]
        internal static void InitializeSelectableColumn(SearchColumn column)
        {
            column.drawer = args =>
            {
                var itemStyle = GetItemContentStyle(args.column);
                EditorGUI.SelectableLabel(args.rect, args.value?.ToString() ?? string.Empty, itemStyle);
                return args.value;
            };
        }
    }
}
