// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    public static class ItemSelectors
    {
        internal static class Styles
        {
            private static readonly RectOffset paddingNone = new RectOffset(0, 0, 0, 0);

            public static readonly GUIStyle itemLabel = new GUIStyle(EditorStyles.label)
            {
                name = "quick-search-item-label",
                richText = true,
                wordWrap = false,
                margin = new RectOffset(8, 4, 4, 2),
                padding = paddingNone
            };

            public static readonly GUIStyle itemLabelLeftAligned = new GUIStyle(itemLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(2, 2, 0, 0)
            };
            public static readonly GUIStyle itemLabelCenterAligned = new GUIStyle(itemLabelLeftAligned) { alignment = TextAnchor.MiddleCenter };
            public static readonly GUIStyle itemLabelrightAligned = new GUIStyle(itemLabelLeftAligned) { alignment = TextAnchor.MiddleRight };
        }

        [SearchSelector("id", priority: 1)] static object GetSearchItemID(SearchItem item) => item.id;
        [SearchSelector("label", priority: 1)] static object GetSearchItemLabel(SearchItem item) => item.GetLabel(item.context);
        [SearchSelector("description", priority: 1)] static object GetSearchItemDesc(SearchItem item) => item.GetDescription(item.context);
        [SearchSelector("value", priority: 1)] static object GetSearchItemValue(SearchItem item) => item.value;
        [SearchSelector("provider", priority: 9)] static object GetSearchItemProvider(SearchItem item) => item.provider?.name;
        [SearchSelector("score", priority: 9)] static object GetSearchItemScore(SearchItem item) => item.score;
        [SearchSelector("options", priority: 9)] static object GetSearchItemOptions(SearchItem item) => item.options;
        [SearchSelector("data", priority: 9)] static object GetSearchItemData(SearchItem item) => item.data?.ToString();
        [SearchSelector("thumbnail", priority: 9, cacheable = false)] static object GetSearchItemThumbnail(SearchItem item) => item.GetThumbnail(item.context, cacheThumbnail: false);
        [SearchSelector("preview", priority: 9, cacheable = false)] static object GetSearchItemPreview(SearchItem item) => item.GetPreview(item.context, new Vector2(64, 64), FetchPreviewOptions.Normal, cacheThumbnail: false);

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
                yield return CreateColumn("Thumbnail", "thumbnail", "Texture2D");
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

        private static VisualElement CreateVisualElement(SearchColumn column)
        {
            var image = new Image();
            var label = new Label { style = { unityTextAlign = GetItemTextAlignment(column) } };
            var container = new VisualElement();
            container.AddToClassList("search-table-cell__item-name");
            container.Add(image);
            container.Add(label);
            return container;
        }

        private static void BindName(SearchColumnEventArgs args, VisualElement ve)
        {
            var text = string.Empty;
            Texture2D thumbnail = null;

            if (args.value is Object obj)
            {
                text = obj.name;
                thumbnail = AssetPreview.GetMiniThumbnail(obj);
            }
            else if (args.value != null)
            {
                var item = args.item;
                text = args.value.ToString();
                thumbnail = item.GetThumbnail(item.context ?? args.context);
            }

            ve.Q<Label>().text = text;
            ve.Q<Image>().image = thumbnail;
        }

        [SearchColumnProvider("Default")]
        internal static void InitializeObjectPathColumn(SearchColumn column)
        {
            column.getter = SearchColumn.DefaultSelect;
            column.setter = null;
            column.drawer = null;
            column.comparer = null;
            column.binder = null;
            column.cellCreator = null;
        }

        [SearchColumnProvider("Name")]
        internal static void InitializeItemNameColumn(SearchColumn column)
        {
            column.getter = GetName;
            column.cellCreator = CreateVisualElement;
            column.binder = BindName;
        }

        public static GUIStyle GetItemContentStyle(SearchColumn column)
        {
            if (column.options.HasAny(SearchColumnFlags.TextAlignmentCenter))
                return Styles.itemLabelCenterAligned;
            if (column.options.HasAny(SearchColumnFlags.TextAlignmentRight))
                return Styles.itemLabelrightAligned;
            return Styles.itemLabelLeftAligned;
        }

        internal static TextAnchor GetItemTextAlignment(SearchColumn column)
        {
            if (column.options.HasAny(SearchColumnFlags.TextAlignmentCenter))
                return TextAnchor.MiddleCenter;
            if (column.options.HasAny(SearchColumnFlags.TextAlignmentRight))
                return TextAnchor.MiddleRight;
            return TextAnchor.MiddleLeft;
        }

        [SearchColumnProvider("size")]
        internal static void InitializeItemSizeColumn(SearchColumn column)
        {
            column.binder = (SearchColumnEventArgs args, VisualElement ve) =>
            {
                var label = (TextElement)ve;
                string text = string.Empty;
                if (Utils.TryGetNumber(args.value, out var n))
                    text = Utils.FormatBytes((long)n);
                else if (args.value != null)
                    text = args.value.ToString();
                label.text = text;
            };
        }

        [SearchColumnProvider("count")]
        internal static void InitializeCountColumn(SearchColumn column)
        {
            column.binder = (SearchColumnEventArgs args, VisualElement ve) =>
            {
                var label = (TextElement)ve;
                string text = string.Empty;
                if (Utils.TryGetNumber(args.value, out var n))
                    text = Utils.FormatCount((ulong)n);
                else if (args.value != null)
                    text = args.value.ToString();
                label.text = text;
            };
        }

        [SearchColumnProvider("selectable")]
        internal static void InitializeSelectableColumn(SearchColumn column)
        {
            column.binder = (args, ve) =>
            {
                var label = (TextElement)ve;
                label.text = args.value?.ToString() ?? string.Empty;
                label.selection.isSelectable = true;
            };
        }
    }
}
