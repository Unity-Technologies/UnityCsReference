// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Search
{
    class QuerySelectorItemGUI : AdvancedDropdownGUI
    {
        class Styles
        {
            public const float minSinglelineTextHeight = 20f;

            public static readonly GUIStyle itemStyle = new GUIStyle("DD LargeItemStyle")
            {
                fixedHeight = 22
            };

            public static readonly Color labelColor;
            public static readonly GUIStyle label;
            public static readonly GUIStyle textStyle;
            public static readonly GUIStyle ellipsisTextStyle;

            static Styles()
            {
                ColorUtility.TryParseHtmlString("#202427", out labelColor);

                label = new GUIStyle("ToolbarLabel")
                {
                    richText = true,
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(6, 6, 0, 0),
                    normal = new GUIStyleState { textColor = labelColor },
                    hover = new GUIStyleState { textColor = labelColor }
                };
                textStyle = new GUIStyle(label)
                {
                    padding = new RectOffset(0, 0, 0, 0),
                    alignment = TextAnchor.MiddleCenter
                };
                ellipsisTextStyle = new GUIStyle(label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    clipping = TextClipping.Ellipsis
                };
            }

            public static readonly GUIStyle propositionIcon = new GUIStyle("label")
            {
                fixedWidth = 18f,
                fixedHeight = 18f,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };

            public static readonly GUIStyle lineSeparator = new GUIStyle("DefaultLineSeparator")
            {
                margin = new RectOffset(25, 25, 10, 5)
            };

            public static readonly Vector4 borderWidth4 = new Vector4(1, 1, 1, 1);
            public static readonly Vector4 borderRadius4 = new Vector4(8f, 8f, 8f, 8f);

            public static readonly float sepColor = (EditorGUIUtility.isProSkin) ? 0.43f : 0.6f;
        }

        internal QuerySelector host;
        internal override GUIStyle lineStyle => Styles.itemStyle;
        internal override Vector2 iconSize => new Vector2(Styles.propositionIcon.fixedWidth, Styles.propositionIcon.fixedHeight);

        public QuerySelectorItemGUI(AdvancedDropdownDataSource dataSource, QuerySelector host)
            : base(dataSource)
        {
            this.host = host;
        }

        internal override Rect GetItemRect(in GUIContent content)
        {
            return GUILayoutUtility.GetRect(host.window.position.width - 18f, QueryBlock.blockHeight + 2, lineStyle, GUILayout.ExpandWidth(true));
        }

        internal override Vector2 CalcItemSize(GUIContent content)
        {
            return lineStyle.CalcSize(GUIContent.Temp(content.text)) + new Vector2(Styles.propositionIcon.fixedWidth + 40f, 0);
        }

        internal override float CalcItemHeight(GUIContent content, float width)
        {
            return Styles.minSinglelineTextHeight + 2;
        }

        internal override void DrawLineSeparator()
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, Styles.lineSeparator, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint)
                return;

            Color orgColor = GUI.color;
            Color tintColor = new Color(Styles.sepColor, Styles.sepColor, Styles.sepColor, 1.0f);
            GUI.color = GUI.color * tintColor;
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
            GUI.color = orgColor;
        }

        internal override void DrawItemContent(AdvancedDropdownItem item, Rect rect, GUIContent content, bool isHover, bool isActive, bool on, bool hasKeyboardFocus)
        {
            if (item.hasChildren || item.userData is not SearchProposition proposition)
            {
                base.DrawItemContent(item, rect, content, isHover, isActive, on, hasKeyboardFocus);
                return;
            }

            var bgColor = proposition.color;
            if (bgColor == Color.clear)
                bgColor = QueryColors.filter;

            // Add spacing between items
            rect.y += 2;
            rect.yMax -= 3;

            // Left margin
            rect.x += 2;

            var iconRect = new Rect(rect.x, rect.y, iconSize.x, iconSize.y);
            if (content.image != null)
            {
                // Draw icon if needed. If no icon, rect is already offsetted.
                Styles.propositionIcon.Draw(iconRect, content.image, false, false, false, false);
                rect.xMin += iconSize.x + 2;
            }
            else
            {
                iconRect = new Rect(rect.x, rect.y, 0, 0);
            }

            var textContent = GUIContent.Temp(content.text);
            var size = Styles.textStyle.CalcSize(textContent).x;
            var backgroundRect = new Rect(iconRect.xMax + 2f, rect.yMin, size + 10f, rect.height);

            var selected = isHover || on;
            var color = selected ? bgColor * QueryColors.selectedTint : bgColor;

            var maxWidth = m_HeaderRect.width - iconRect.width - 18f; // offset for the icon and paddings.
            var isTextTooLong = backgroundRect.width > maxWidth;
            var textStyle = isTextTooLong ? Styles.ellipsisTextStyle : Styles.textStyle;
            backgroundRect.width = isTextTooLong ? maxWidth : backgroundRect.width;

            // Draw block background
            GUI.DrawTexture(backgroundRect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 0f, color, Vector4.zero, Styles.borderRadius4);

            // Draw Text
            textStyle.Draw(backgroundRect, textContent, false, false, false, false);

            if (selected)
            {
                // Draw border
                GUI.DrawTexture(backgroundRect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 0f, QueryColors.selectedBorderColor, Styles.borderWidth4, Styles.borderRadius4);
            }
        }
    }

    class QuerySelector : AdvancedDropdown, IBlockEditor
    {
        private readonly string m_Title;
        private readonly IBlockSource m_BlockSource;
        private readonly IEnumerable<SearchProposition> m_Propositions;

        public SearchContext context => m_BlockSource.context;
        public EditorWindow window => m_WindowInstance;

        public QuerySelector(Rect rect, IBlockSource dataSource, string title = null)
            : base(new AdvancedDropdownState())
        {
            m_BlockSource = dataSource;
            m_Title = title ?? m_BlockSource.editorTitle ?? m_BlockSource.name ?? string.Empty;
            m_Propositions = m_BlockSource.FetchPropositions().Where(p => p.valid);

            minimumSize = new Vector2(Mathf.Max(rect.width, 250f), 350f);
            maximumSize = new Vector2(Mathf.Max(rect.width, 400f), 450f);

            m_DataSource = new CallbackDataSource(BuildRoot);
            m_DataSource.CurrentFolderContextualSearch = true;
            m_Gui = new QuerySelectorItemGUI(m_DataSource, this);
        }

        public static QuerySelector Open(Rect r, IBlockSource source, string title = null)
        {
            var w = new QuerySelector(r, source, title);
            w.Show(r);
            w.Bind();
            return w;
        }

        readonly struct ItemPropositionComparer : IComparer<AdvancedDropdownItem>
        {
            public int Compare(AdvancedDropdownItem x, AdvancedDropdownItem y)
            {
                if (x.userData is SearchProposition px && y.userData is SearchProposition py)
                    return px.priority.CompareTo(py.priority);
                return x.CompareTo(y);
            }
        }

        private void Bind()
        {
            m_WindowInstance.windowClosed += OnClose;
            m_WindowInstance.selectionCanceled += OnSelectionCanceled;
            m_DataSource.searchMatchItem = OnSearchItemMatch;
            m_DataSource.searchMatchItemComparer = new ItemPropositionComparer();
        }

        struct ItemSearchData
        {
            public string data;
            public int lastIndex;
            public bool isValid => lastIndex != -1;
        }

        static ItemSearchData[] s_ItemSearchDatas = new ItemSearchData[3];
        internal static bool DoSearchItemMatch(in AdvancedDropdownItem item, in string[] words, out bool didMatchStart)
        {
            void PrepareItemData(int index, string data)
            {
                s_ItemSearchDatas[index].data = data;
                s_ItemSearchDatas[index].lastIndex = -1;
                if (!string.IsNullOrEmpty(data))
                {
                    s_ItemSearchDatas[index].lastIndex = data.LastIndexOf('(');
                    s_ItemSearchDatas[index].lastIndex = s_ItemSearchDatas[index].lastIndex == -1 ? data.Length : s_ItemSearchDatas[index].lastIndex;
                }
            }

            // We should only have items with valid SearchPropositions. However, it can happen that
            // this method is called with an AdvancedDropdownItem that has no userData (or that userData is not a SearchProposition),
            // which is the case when there is no propositions so the root item becomes a searchable item. In that case, we should
            // just return false so that the item is not shown in the search results.
            if (item.userData is not SearchProposition proposition)
            {
                didMatchStart = false;
                return false;
            }

            var itemDataCount = 0;
            PrepareItemData(itemDataCount++, item.displayName);
            PrepareItemData(itemDataCount++, item.name);
            PrepareItemData(itemDataCount++, proposition.replacement);

            didMatchStart = false;
            var fp = -1;
            foreach (var w in words)
            {
                for (var i = 0; i < itemDataCount; ++i)
                {
                    if (s_ItemSearchDatas[i].isValid)
                    {
                        fp = s_ItemSearchDatas[i].data.IndexOf(w, 0, s_ItemSearchDatas[i].lastIndex, StringComparison.OrdinalIgnoreCase);
                        didMatchStart |= fp != -1 && (fp == 0 || s_ItemSearchDatas[i].data[fp - 1] == ' ');
                        if (fp != -1)
                            break;
                    }
                }

                if (fp == -1)
                    return false;
            }
            return fp != -1;
        }

        internal static AdvancedDropdownItem CreateItem(SearchProposition p, bool formatNames, out string path, out string name, out string prefix)
        {
            path = p.path;
            name = p.label;
            prefix = p.category;
            if (name.LastIndexOf('/') != -1)
            {
                var ls = path.LastIndexOf('/');
                name = path.Substring(ls + 1);
                prefix = path.Substring(0, ls);
            }

            var displayName = formatNames ? ObjectNames.NicifyVariableName(name) : name;
            var newItem = new AdvancedDropdownItem(path)
            {
                displayName = displayName,
                icon = p.icon ?? Icons.quicksearch,
                tooltip = string.IsNullOrEmpty(p.help) ? $"Search {displayName}" : p.help,
                userData = p
            };
            return newItem;
        }

        private bool OnSearchItemMatch(in AdvancedDropdownItem item, in string[] words, out bool didMatchStart)
        {
            return DoSearchItemMatch(item, words, out didMatchStart);
        }

        private void OnClose(AdvancedDropdownWindow w = null)
        {
            m_BlockSource.CloseEditor();
        }

        private void OnSelectionCanceled()
        {
            OnClose();
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var rootItem = new AdvancedDropdownItem(m_Title);
            var formatNames = m_BlockSource.formatNames;
            foreach (var p in m_Propositions)
            {
                var newItem = CreateItem(p, formatNames, out var path, out var name, out var prefix);
                var parent = rootItem;
                if (prefix != null)
                    parent = MakeParents(prefix, p, rootItem);

                if (p.isSeparator)
                {
                    (parent ?? rootItem).AddSeparator();
                }
                else
                {
                    var fit = FindItem(name, parent);
                    if (fit == null)
                        parent.AddChild(newItem);
                    else if (p.icon)
                        fit.icon = p.icon;
                }
            }

            return rootItem;
        }

        private AdvancedDropdownItem FindItem(string path, AdvancedDropdownItem root)
        {
            var pos = path.IndexOf('/');
            var name = pos == -1 ? path : path.Substring(0, pos);
            var suffix = pos == -1 ? null : path.Substring(pos + 1);

            foreach (var c in root.children)
            {
                if (suffix == null && string.Equals(c.name, name, StringComparison.Ordinal))
                    return c;

                if (suffix == null)
                    continue;

                var f = FindItem(suffix, c);
                if (f != null)
                    return f;
            }

            return null;
        }

        private AdvancedDropdownItem MakeParents(string prefix, in SearchProposition proposition, AdvancedDropdownItem parent)
        {
            var parts = prefix.Split('/');

            foreach (var p in parts)
            {
                var f = FindItem(p, parent);
                if (f != null)
                {
                    if (f.icon == null)
                        f.icon = proposition.icon;
                    parent = f;
                }
                else
                {
                    var newItem = new AdvancedDropdownItem(p) { icon = proposition.icon };
                    parent.AddChild(newItem);
                    parent = newItem;
                }
            }

            return parent;
        }

        protected override void ItemSelected(AdvancedDropdownItem i)
        {
            if (i.userData is SearchProposition sp)
                m_BlockSource.Apply(sp);
        }
    }
}
