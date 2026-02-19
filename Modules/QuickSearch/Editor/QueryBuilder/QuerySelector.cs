// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
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
                fixedWidth = 16f,
                fixedHeight = 16f,
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
        Dictionary<int, AdvancedDropdownItem> m_PathIdToItem;
        public SearchContext context => m_BlockSource.context;
        public EditorWindow window => m_WindowInstance;
        private IEnumerator<SearchProposition> m_PropositionEnumerator;
        private int m_PropositionsCount;
        private AdvancedDropdownItem m_RootItem;
        private TimeSpan m_PropositionsIterationTimeInSeconds;

        static readonly TimeSpan k_PropositionsIterationTimes = TimeSpan.FromSeconds(0.02);

        public QuerySelector(Rect rect, IBlockSource dataSource, string title = null)
            : this(rect, dataSource, title, k_PropositionsIterationTimes)
        {
        }

        QuerySelector(Rect rect, IBlockSource dataSource, string title, TimeSpan propositionsIterationTimes)
            : base(new AdvancedDropdownState())
        {
            m_BlockSource = dataSource;
            m_Title = title ?? m_BlockSource.editorTitle ?? m_BlockSource.name ?? string.Empty;
            m_PathIdToItem = new();
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_Propositions = m_BlockSource.FetchPropositions().Where(p => p.valid);
#pragma warning restore UA2001

            minimumSize = new Vector2(Mathf.Max(rect.width, 250f), 350f);
            maximumSize = new Vector2(Mathf.Max(rect.width, 400f), 450f);

            m_PropositionsIterationTimeInSeconds = propositionsIterationTimes;
            m_DataSource = new CallbackDataSource(BuildRoot);
            m_DataSource.CurrentFolderContextualSearch = true;
            m_Gui = new QuerySelectorItemGUI(m_DataSource, this);
        }

        public static QuerySelector Open(Rect r, IBlockSource source, string title = null)
        {
            return Open(r, source, title, k_PropositionsIterationTimes);
        }

        internal static QuerySelector Open(Rect r, IBlockSource source, string title, TimeSpan propositionsIterationTimes)
        {
            var w = new QuerySelector(r, source, title, propositionsIterationTimes);
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

        internal static bool DoSearchItemMatch(in AdvancedDropdownItem item, in string[] words, out bool didMatchStart)
        {
            bool DoMatch(string data, string word, ref bool didMatchStart)
            {
                var fp = data.IndexOf(word, StringComparison.OrdinalIgnoreCase);
                didMatchStart |= fp != -1 && (fp == 0 || data[fp - 1] == ' ');
                return fp != -1;
            }

            didMatchStart = false;
            // We should only have items with valid SearchPropositions. However, it can happen that
            // this method is called with an AdvancedDropdownItem that has no userData (or that userData is not a SearchProposition),
            // which is the case when there is no propositions so the root item becomes a searchable item. In that case, we should
            // just return false so that the item is not shown in the search results.
            if (item.userData is not SearchProposition proposition)
            {
                return false;
            }

            // Algo:
            // if any word doesn't match: return false
            // try to find a word that match and we didStart
            foreach (var w in words)
            {
                var matchItem = DoMatch(item.displayName, w, ref didMatchStart);
                matchItem = DoMatch(item.name, w, ref didMatchStart) || matchItem;

                // The word doesn't match display or name: no match
                if (!matchItem)
                    return false;
            }

            // At this point a match was found:
            return true;
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

            // UUM-119687: Skip NicifyVariableName for propositions with SkipNicifyVariableName flag
            var shouldNicify = formatNames && !p.generationOptions.HasAny(SearchPropositionGenerationOptions.SkipNicifyVariableName);
            var displayName = shouldNicify ? ObjectNames.NicifyVariableName(name) : name;
            var newItem = new AdvancedDropdownItem(path, displayName)
            {
                icon = p.icon ?? Icons.quicksearch,
                tooltip = p.help,
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
            var startIteration = EditorApplication.timeSinceStartup;
            if (m_PropositionEnumerator == null)
            {
                m_PathIdToItem.Clear();
                m_RootItem = new AdvancedDropdownItem(m_Title);
                m_PropositionEnumerator = m_Propositions.GetEnumerator();
                m_PropositionsCount = 0;
            }

            var formatNames = m_BlockSource.formatNames;
            var hasData = m_PropositionEnumerator.MoveNext();
            while ((m_PropositionsIterationTimeInSeconds == TimeSpan.Zero || TimeSpan.FromSeconds(EditorApplication.timeSinceStartup - startIteration) < m_PropositionsIterationTimeInSeconds)
                   && hasData)
            {
                var p = m_PropositionEnumerator.Current;
                var newItem = CreateItem(p, formatNames, out var path, out var name, out var prefix);
                var parent = m_RootItem;
                if (prefix != null)
                    parent = MakeParents(prefix, p, m_RootItem);

                if (p.isSeparator)
                {
                    (parent ?? m_RootItem).AddSeparator();
                }
                else
                {
                    var itemAlreadyInTree = FindItem(name, parent);
                    if (itemAlreadyInTree == null)
                    {
                        parent.AddChild(newItem);
                        m_PathIdToItem[path.GetHashCode()] = newItem;
                    }
                    else if (p.icon)
                        itemAlreadyInTree.icon = p.icon;
                }
                hasData = m_PropositionEnumerator.MoveNext();
                m_PropositionsCount++;
            }

            if (hasData)
            {
                m_WindowInstance.SetDataSourceDirty();
            }
            else
            {
                // All propositions have been added to the Dialog.
                m_PathIdToItem.Clear();
                m_PropositionEnumerator.Dispose();
                m_PropositionEnumerator = null;
            }

            return m_RootItem;
        }

        private AdvancedDropdownItem FindItem(string path, AdvancedDropdownItem root)
        {
            if (m_PathIdToItem.TryGetValue(path.GetHashCode(), out var item))
            {
                return item;
            }
            return null;
        }

        static readonly string[] k_Tokens = new string[10];
        private AdvancedDropdownItem MakeParents(string prefix, in SearchProposition proposition, AdvancedDropdownItem parent)
        {
            var tokenCount = SearchUtils.SplitTokens(prefix,  '/', k_Tokens);
            for (var i = 0; i < tokenCount; ++i)
            {
                var p = k_Tokens[i];
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
                    m_PathIdToItem[p.GetHashCode()] = newItem;
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
