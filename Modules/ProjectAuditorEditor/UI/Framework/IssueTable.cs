// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    class PAMultiColumnHeader : MultiColumnHeader
    {
        public PAMultiColumnHeader(MultiColumnHeaderState state) : base(state)
        {
        }

        private void MyToggleVisibility(object userData)
        {
            ToggleVisibility((int)userData);
        }

        protected override void AddColumnHeaderContextMenuItems(GenericMenu menu)
        {
            menu.AddItem(EditorGUIUtility.TrTextContent("Resize to Fit"), false, new GenericMenu.MenuFunction(ResizeToFit));
            menu.AddSeparator("");
            for (int userData = 0; userData < state.columns.Length; ++userData)
            {
                MultiColumnHeaderState.Column column = state.columns[userData];
                string text = string.IsNullOrEmpty(column.contextMenuText) ? column.headerContent.text : column.contextMenuText;
                if (column.allowToggleVisibility)
                    menu.AddItem(new GUIContent(text), state.visibleColumns.Contains(userData), new GenericMenu.MenuFunction2(MyToggleVisibility), userData);
            }
        }
    }

    class IssueTable : TreeView
    {
        static readonly int k_DefaultRowHeight = 18;
        static readonly int k_FirstId = 1;

        readonly SeverityRules m_Rules;
        readonly ViewDescriptor m_Desc;
        readonly AnalysisView m_View;
        readonly IssueLayout m_Layout;
        readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>(100);

        private Dictionary<string, IssueTableItem>
            m_TreeViewItemGroupsLookup = new Dictionary<string, IssueTableItem>();
        Dictionary<int, IssueTableItem> m_TreeViewItemIssues;
        List<IssueTableItem> m_SelectedIssues = new List<IssueTableItem>();
        ReportItem[] m_SelectedReportItems;
        bool m_SelectionChanged = true;
        bool m_SelectionChangedReportItems = true;
        int m_NextId;
        int m_NumMatchingIssues;
        bool m_FlatView;
        bool m_ShowIgnoredIssues;
        int m_GroupPropertyIndex;

        public bool flatView
        {
            get => m_FlatView;
            set => m_FlatView = value;
        }

        public bool showIgnoredIssues
        {
            get => m_ShowIgnoredIssues;
            set => m_ShowIgnoredIssues = value;
        }

        public int groupPropertyIndex
        {
            get => m_GroupPropertyIndex;
            set
            {
                if (value >= m_Layout.Properties.Length)
                    return;
                if (value >= 0)
                    m_GroupPropertyIndex = value;
            }
        }

        public IssueTable(TreeViewState state, MultiColumnHeader multicolumnHeader,
                          ViewDescriptor desc, IssueLayout layout, SeverityRules rules,
                          AnalysisView view) : base(state,
                                                    multicolumnHeader)
        {
            m_Rules = rules;
            m_View = view;
            m_Desc = desc;
            m_Layout = layout;
            m_FlatView = true; // by default, don't use groups

            var propertyIndex = m_Layout.DefaultGroupPropertyIndex;
            if (propertyIndex != -1)
            {
                m_FlatView = false;
                groupPropertyIndex = propertyIndex;
            }

            multicolumnHeader.sortingChanged += OnSortingChanged;
            showAlternatingRowBackgrounds = true;

            Clear();
        }

        public void AddIssues(IReadOnlyCollection<ReportItem> issues)
        {
            // update groups
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var groupNames = issues.Select(i => i.GetPropertyGroup(m_Layout.Properties[groupPropertyIndex])).Distinct().ToArray();
#pragma warning restore UA2001
            foreach (var name in groupNames)
            {
                // if necessary, create a group
                if (!m_TreeViewItemGroupsLookup.ContainsKey(name))
                    m_TreeViewItemGroupsLookup[name] = new IssueTableItem(m_NextId++, 0, name);
            }

            var items = new Dictionary<int, IssueTableItem>(issues.Count);
            if (m_TreeViewItemIssues != null)
            {
                foreach (var issuesPair in m_TreeViewItemIssues)
                {
                    items.Add(issuesPair.Value.id, issuesPair.Value);
                }
            }

            foreach (var issue in issues)
            {
                var depth = 1;
                if (m_Layout.IsHierarchy)
                {
                    if (m_Desc.Category == IssueCategory.BuildStep)
                    {
                        depth = issue.GetCustomPropertyInt32(BuildReportStepProperty.Depth);
                    }
                    else
                        depth = 0;
                }

                var item = new IssueTableItem(m_NextId++, depth, issue.Description, issue, issue.GetPropertyGroup(m_Layout.Properties[groupPropertyIndex]));
                items.Add(item.id, item);
            }

            m_TreeViewItemIssues = items;
        }

        public void Clear()
        {
            m_NextId = k_FirstId;
            m_TreeViewItemGroupsLookup.Clear();
            m_TreeViewItemIssues = new Dictionary<int, IssueTableItem>();
            ClearSelection();
        }

        protected override TreeViewItem BuildRoot()
        {
            var idForHiddenRoot = -1;
            var depthForHiddenRoot = -1;
            var root = new TreeViewItem(idForHiddenRoot, depthForHiddenRoot, "root");

            foreach (var item in m_TreeViewItemGroupsLookup.Values)
            {
                root.AddChild(item);
            }

            return root;
        }

        Dictionary<string, List<IssueTableItem>> groupNameItemLookup = new Dictionary<string, List<IssueTableItem>>();
        Dictionary<string, List<IssueTableItem>> groupNameItemLookupIgnored = new Dictionary<string, List<IssueTableItem>>();

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            m_Rows.Clear();

            // find all issues matching the filters and make an array out of them
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var allIssues = m_TreeViewItemIssues.Values.ToArray();
            var filteredItems = allIssues.Where(item => m_View.Match(item.ReportItem)).ToArray();
#pragma warning restore UA2001

            m_NumMatchingIssues = filteredItems.Length;
            if (m_NumMatchingIssues == 0)
            {
                m_Rows.Add(new TreeViewItem(0, 0, "No items"));
                return m_Rows;
            }

            foreach (var group in m_TreeViewItemGroupsLookup.Values)
            {
                if (group.children != null)
                    group.children.Clear();
            }

            if (!hasSearch && !m_FlatView)
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var groupedItemQuery = allIssues.GroupBy(i => i.ReportItem.GetPropertyGroup(m_Layout.Properties[groupPropertyIndex]));
#pragma warning restore UA2001

                groupNameItemLookup.Clear();
                groupNameItemLookupIgnored.Clear();

                foreach (var filteredItem in filteredItems)
                {
                    string filteredItemName = filteredItem.GroupName;
                    if (!groupNameItemLookup.ContainsKey(filteredItemName))
                    {
                        groupNameItemLookup[filteredItemName] = new List<IssueTableItem>();
                    }

                    groupNameItemLookup[filteredItemName].Add(filteredItem);
                }

                foreach (var issue in allIssues)
                {
                    string filteredItemName = issue.GroupName;
                    if (!groupNameItemLookupIgnored.ContainsKey(filteredItemName))
                    {
                        groupNameItemLookupIgnored[filteredItemName] = new List<IssueTableItem>();
                    }

                    if (issue.ReportItem.IsIgnored)
                        groupNameItemLookupIgnored[filteredItemName].Add(issue);
                }

                foreach (var groupedItems in groupedItemQuery)
                {
                    var groupName = groupedItems.Key;
                    var group = m_TreeViewItemGroupsLookup[groupName];

                    if (!groupNameItemLookup.TryGetValue(groupName, out var children))
                        children = new List<IssueTableItem>();

                    int ignoredChildrenCount = 0;
                    if (groupNameItemLookupIgnored.TryGetValue(groupName, out var ignored_children))
                        ignoredChildrenCount = ignored_children.Count;

                    int childrenCount = children.Count;

                    if (childrenCount == 0 && ignoredChildrenCount == 0)
                        continue;

                    m_Rows.Add(group);

                    var groupIsExpanded = state.expandedIDs.Contains(group.id);

                    group.NumVisibleChildren = childrenCount;
                    group.NumIgnoredChildren = ignoredChildrenCount;
                    group.DisplayName = groupName;

                    foreach (var child in children)
                    {
                        if (groupIsExpanded)
                            m_Rows.Add(child);
                        group.AddChild(child);
                    }
                }
            }
            else
            {
                foreach (var item in filteredItems)
                {
                    var group = m_TreeViewItemGroupsLookup[item.GroupName];
                    group.AddChild(item);

                    m_Rows.Add(item);
                }
            }
            SortIfNeeded(m_Rows);

            return m_Rows;
        }

        protected override IList<int> GetAncestors(int id)
        {
            if (m_TreeViewItemIssues == null || m_TreeViewItemIssues.Count == 0)
                return new List<int>();
            return base.GetAncestors(id);
        }

        protected override IList<int> GetDescendantsThatHaveChildren(int id)
        {
            if (m_TreeViewItemIssues == null || m_TreeViewItemIssues.Count == 0)
                return new List<int>();
            return base.GetDescendantsThatHaveChildren(id);
        }

        public void SetFontSize(int fontSize)
        {
            rowHeight = k_DefaultRowHeight * fontSize / ViewStates.DefaultMinFontSize;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                CellGUI(args.GetCellRect(i), args.item, args.GetColumn(i), ref args);
        }

        public string GetCustomGroupPropertyCellString(IssueTableItem item, PropertyDefinition property)
        {
            string label = null;
            var customPropertyIndex = PropertyTypeUtil.ToCustomIndex(property.Type);
            if (property.Format == PropertyFormat.Bytes || property.Format == PropertyFormat.Time || property.Format == PropertyFormat.Percentage)
            {
                if (property.Format == PropertyFormat.Bytes)
                {
                    ulong sum = 0;
                    foreach (var childItem in item.children)
                    {
                        var issueTableItem = childItem as IssueTableItem;
                        var value = issueTableItem.ReportItem.GetCustomPropertyUInt64(customPropertyIndex);
                        sum += value;
                    }

                    label = Formatting.FormatSize(sum);
                }
                else
                {
                    float sum = 0;
                    foreach (var childItem in item.children)
                    {
                        var issueTableItem = childItem as IssueTableItem;
                        var value = issueTableItem.ReportItem.GetCustomPropertyFloat(customPropertyIndex);
                        sum += value;
                    }
                    label = property.Format == PropertyFormat.Time ? Formatting.FormatTime(sum) : Formatting.FormatPercentage(sum, 1);
                }
            }

            return label;
        }

        void CellGUI(Rect cellRect, TreeViewItem treeViewItem, int columnIndex, ref RowGUIArgs args)
        {
            var property = m_Layout.Properties[columnIndex];
            if (property.IsHidden)
                return;

            var propertyType = property.Type;
            var labelStyle = SharedStyles.LabelWithDynamicSize;
            var item = treeViewItem as IssueTableItem;

            if (item == null)
            {
                if (propertyType == PropertyType.Description)
                    EditorGUI.LabelField(cellRect, new GUIContent(treeViewItem.displayName, treeViewItem.displayName), labelStyle);
                return;
            }

            var contentIndent = GetContentIndent(treeViewItem);
            // indent first column, if necessary
            if (columnIndex == 0 && !hasSearch && !m_FlatView)
            {
                var indent = contentIndent + extraSpaceBeforeIconAndLabel;
                cellRect.xMin += indent;
                CenterRectUsingSingleLineHeight(ref cellRect);
            }
            else if (m_Layout.IsHierarchy && property.Type == PropertyType.Description)
            {
                var indent = contentIndent;
                cellRect.xMin += indent;
                CenterRectUsingSingleLineHeight(ref cellRect);
            }

            if (item.IsGroup())
            {
                if (columnIndex == 0)
                {
                    var guiContent = new GUIContent(item.GetDisplayName());
                    EditorGUI.LabelField(cellRect, guiContent, labelStyle);

                    cellRect.xMax -= labelStyle.CalcSize(guiContent).x;

                    if (showIgnoredIssues)
                    {
                        string label = item.NumIgnoredChildren > 0 ? $"({item.NumVisibleChildren} Item(s), including {item.NumIgnoredChildren} Ignored)" : $"({item.NumVisibleChildren} Item(s))";
                        EditorGUI.LabelField(new Rect(cellRect)
                        {
                            x = labelStyle.CalcSize(guiContent).x + contentIndent
                        }, label, SharedStyles.LabelDarkWithDynamicSize);
                    }
                    else
                    {
                        string label = item.NumIgnoredChildren > 0 ? $"({item.NumVisibleChildren} Item(s), {item.NumIgnoredChildren} Ignored and hidden)" : $"({item.NumVisibleChildren} Item(s))";
                        EditorGUI.LabelField(new Rect(cellRect)
                        {
                            x = labelStyle.CalcSize(guiContent).x + contentIndent
                        }, label, SharedStyles.LabelDarkWithDynamicSize);
                    }
                }
                else if (PropertyTypeUtil.IsCustom(property.Type))
                {
                    string label = GetCustomGroupPropertyCellString(item, property);

                    if (!string.IsNullOrEmpty(label))
                    {
                        EditorGUI.LabelField(cellRect, label, labelStyle);
                    }
                }
            }
            else
            {
                Rule rule = null;
                var issue = item.ReportItem;
                if (issue.Id.IsValid())
                {
                    if (issue.IsIgnored)
                        GUI.enabled = false;

                    /* var id = issue.Id;
                     rule = m_Rules.GetRule(id, issue.GetContext());
                     if (rule == null)
                         rule = m_Rules.GetRule(id); // try to find non-specific rule
                     if (rule != null && rule.Severity == Severity.None)
                         GUI.enabled = false;*/
                }

                switch (propertyType)
                {
                    case PropertyType.LogLevel:
                        {
                            if (issue.Severity != Severity.Hidden)
                            {
                                var icon = Utility.GetLogLevelIcon(issue.LogLevel);
                                if (icon != null)
                                {
                                    EditorGUI.LabelField(cellRect, icon, labelStyle);
                                }
                            }
                        }
                        break;

                    case PropertyType.Severity:
                        {
                            EditorGUI.LabelField(cellRect, Utility.GetSeverityIconWithText(issue.Severity), labelStyle);
                        }
                        break;

                    case PropertyType.Areas:
                        var areaNames = issue.Id.GetDescriptor().GetAreasSummary();
                        EditorGUI.LabelField(cellRect, new GUIContent(areaNames, Tooltip.Area), labelStyle);
                        break;
                    case PropertyType.Description:
                        GUIContent guiContent = null;
                        if (issue.Location != null && m_Desc.DescriptionWithIcon)
                        {
                            guiContent =
                                Utility.GetTextContentWithAssetIcon(item.GetDisplayName(), issue.Location.Path);
                        }
                        else
                        {
                            guiContent = new GUIContent(item.GetDisplayName(), item.GetDisplayName());
                        }
                        EditorGUI.LabelField(cellRect, guiContent, labelStyle);
                        break;

                    case PropertyType.Filename:
                        // display fullpath as tooltip
                        EditorGUI.LabelField(cellRect, new GUIContent(issue.GetProperty(PropertyType.Filename), issue.GetProperty(PropertyType.Path)), labelStyle);
                        break;

                    default:
                        if (PropertyTypeUtil.IsCustom(propertyType))
                        {
                            var customPropertyIndex = PropertyTypeUtil.ToCustomIndex(propertyType);

                            switch (property.Format)
                            {
                                case PropertyFormat.Bool:
                                    var boolAsString = issue.GetCustomProperty(customPropertyIndex);
                                    var boolValue = false;
                                    if (!bool.TryParse(boolAsString, out boolValue))
                                        EditorGUI.LabelField(cellRect, Utility.GetIcon(Utility.IconType.Info, boolAsString), labelStyle);
                                    else if (boolValue)
                                        EditorGUI.LabelField(cellRect, Utility.GetIcon(Utility.IconType.WhiteCheckMark), labelStyle);
                                    break;
                                case PropertyFormat.Bytes:
                                    EditorGUI.LabelField(cellRect, Formatting.FormatSize(issue.GetCustomPropertyUInt64(customPropertyIndex)), labelStyle);
                                    break;
                                case PropertyFormat.Time:
                                    EditorGUI.LabelField(cellRect, Formatting.FormatTime(issue.GetCustomPropertyFloat(customPropertyIndex)), labelStyle);
                                    break;
                                case PropertyFormat.ULong:
                                    var ulongAsString = issue.GetCustomProperty(customPropertyIndex);
                                    var ulongValue = (ulong)0;
                                    if (!ulong.TryParse(ulongAsString, out ulongValue))
                                        EditorGUI.LabelField(cellRect, Utility.GetIcon(Utility.IconType.Info, ulongAsString), labelStyle);
                                    else
                                        EditorGUI.LabelField(cellRect, new GUIContent(ulongAsString, ulongAsString), labelStyle);
                                    break;
                                case PropertyFormat.Integer:
                                    var intAsString = issue.GetCustomProperty(customPropertyIndex);
                                    var intValue = 0;
                                    if (!int.TryParse(intAsString, out intValue))
                                        EditorGUI.LabelField(cellRect, Utility.GetIcon(Utility.IconType.Info, intAsString), labelStyle);
                                    else
                                        EditorGUI.LabelField(cellRect, new GUIContent(intAsString, intAsString), labelStyle);
                                    break;
                                case PropertyFormat.Percentage:
                                    EditorGUI.LabelField(cellRect, Formatting.FormatPercentage(issue.GetCustomPropertyFloat(customPropertyIndex), 1), labelStyle);
                                    break;
                                default:
                                    var value = issue.GetCustomProperty(customPropertyIndex);
                                    EditorGUI.LabelField(cellRect, new GUIContent(value, value), labelStyle);
                                    break;
                            }
                        }
                        else
                        {
                            EditorGUI.LabelField(cellRect, new GUIContent(issue.GetProperty(propertyType)), labelStyle);
                        }

                        break;
                }
                if (issue.WasFixed)
                    GUI.enabled = true;
                else if (rule != null && rule.Severity == Severity.None)
                    GUI.enabled = true;
                else if (issue.IsIgnored)
                    GUI.enabled = true;
            }

            ShowContextMenu(cellRect, item, propertyType, args.GetNumVisibleColumns());
        }

        new void CenterRectUsingSingleLineHeight(ref Rect rect)
        {
            float singleLineHeight = rowHeight;
            if (rect.height > singleLineHeight)
            {
                rect.y += (rect.height - singleLineHeight) * 0.5f;
                rect.height = singleLineHeight;
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            if (m_Desc.OnOpenIssue == null)
                return;

            var rows = FindRows(new[] { id });
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var item = rows.FirstOrDefault();
#pragma warning restore UA2001

            if (item == null)
                return;

            var tableItem = item as IssueTableItem;

            if (tableItem == null)
                return;

            var issue = tableItem.ReportItem;
            if (issue != null && issue.Location != null && issue.Location.IsValid)
            {
                m_Desc.OnOpenIssue(issue.Location);
            }
        }

        protected override void SearchChanged(string newSearch)
        {
            // auto-expand groups containing selected items
            foreach (var id in state.selectedIDs)
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var item = m_TreeViewItemIssues.FirstOrDefault(issue => issue.Value.id == id && issue.Value.parent != null);
#pragma warning restore UA2001
                if (item.Value != null && !state.expandedIDs.Contains(item.Value.parent.id))
                {
                    state.expandedIDs.Add(item.Value.parent.id);
                }
            }
        }

        public int GetNumMatchingIssues()
        {
            return m_NumMatchingIssues;
        }

        public int GetNumIgnoredIssues()
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return m_TreeViewItemIssues.Count(item => item.Value.ReportItem.IsIgnored);
#pragma warning restore UA2001
        }

        public ReportItem[] GetSelectedReportItems()
        {
            if (!m_SelectionChangedReportItems)
            {
                return m_SelectedReportItems;
            }

            m_SelectionChangedReportItems = false;

            var selectedItems = GetSelectedItems();
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_SelectedReportItems = selectedItems.Where(item => item.parent != null).Select(i => i.ReportItem).ToArray();
#pragma warning restore UA2001

            return m_SelectedReportItems;
        }

        public List<IssueTableItem> GetSelectedItems()
        {
            if (!m_SelectionChanged)
            {
                return m_SelectedIssues;
            }

            m_SelectionChanged = false;

            var ids = GetSelection();

            m_SelectedIssues.Clear();

            var count = ids.Count;
            if (count > 0)
            {
                for (int i = 0; i < count; ++i)
                {
                    // Skip group rows that are not in the dictionary
                    if (m_TreeViewItemIssues.TryGetValue(ids[i], out var item))
                        m_SelectedIssues.Add(item);
                }

                return m_SelectedIssues;
            }

            return m_SelectedIssues;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            m_SelectionChanged = true;
            m_SelectionChangedReportItems = true;
        }

        void OnSortingChanged(MultiColumnHeader _multiColumnHeader)
        {
            if (m_Layout.IsHierarchy)
                return;

            SortIfNeeded(GetRows());
        }

        void ShowContextMenu(Rect cellRect, IssueTableItem item, PropertyType propertyType, int numVisibleColumns)
        {
            var current = Event.current;
            if (cellRect.Contains(current.mousePosition) && current.type == EventType.ContextClick)
            {
                var menu = new GenericMenu();

                menu.AddItem(Utility.ClearSelection, false, ClearSelection);

                if (item.ReportItem != null)
                {
                    if (state.selectedIDs.Count == 1)
                    {
                        if (m_View.ViewManager.Report.IsForCurrentProject())
                        {
                            if (m_Desc.OnOpenIssue != null && item.ReportItem.Location != null)
                            {
                                menu.AddItem(Utility.OpenIssue, false,
                                    () => { m_Desc.OnOpenIssue(item.ReportItem.Location); });
                            }
                        }

                        if (m_Desc.ShowFilters)
                        {
                            menu.AddItem(new GUIContent($"Filter by Selected Issue"), false,
                                () => { m_View.SetSearch(item.ReportItem.Description); });
                        }
                    }
                }

                if (m_View.ViewManager.Report.IsForCurrentProject())
                {
                    if (m_Desc.OnOpenIssue != null && item.ReportItem != null && item.ReportItem.Location != null)
                    {
                        menu.AddItem(Utility.OpenIssue, false, () => { m_Desc.OnOpenIssue(item.ReportItem.Location); });
                    }

                    var desc = item.ReportItem != null && item.ReportItem.Id.IsValid() ? item.ReportItem.Id.GetDescriptor() : null;
                    if (m_Desc.OnOpenManual != null && desc != null && desc.Type.StartsWith("UnityEngine."))
                    {
                        menu.AddItem(Utility.OpenScriptReference, false, () =>
                        {
                            m_Desc.OnOpenManual(desc);
                        });
                    }
                }

                if (m_Desc.OnContextMenu != null)
                {
                    menu.AddSeparator("");
                    m_Desc.OnContextMenu(menu, m_View.ViewManager, item.ReportItem);
                }

                menu.AddSeparator("");
                menu.AddItem(Utility.CopyRowToClipboard, false, () => CopySelectionToClipboard(item, propertyType, numVisibleColumns, CopyType.Row));
                menu.AddItem(Utility.CopyCellToClipboard, false, () => CopySelectionToClipboard(item, propertyType, numVisibleColumns, CopyType.Cell));

                menu.ShowAsContext();

                current.Use();
            }
        }

        enum CopyType
        {
            Row,
            Cell
        }

        void CopySelectionToClipboard(IssueTableItem item, PropertyType propertyType, int numVisibleColumns, CopyType copyType)
        {
            var sortedSelectedIDs = new List<int>(SortItemIDsInRowOrder(state.selectedIDs));

            var text = new StringBuilder();
#pragma warning disable UA2006 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            bool indentItems = !flatView && sortedSelectedIDs.Exists(id => m_TreeViewItemGroupsLookup.Values.Any(g => g.id == id));
#pragma warning restore UA2006
            foreach (var id in sortedSelectedIDs)
            {
                if (text.Length > 0)
                    text.Append("\n");

                if (!m_TreeViewItemIssues.TryGetValue(id, out var currentItem))
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    currentItem = m_TreeViewItemGroupsLookup.Values.First(g => g.id == id); // Group name
#pragma warning restore UA2001
                else if (indentItems)
                    text.Append("\t");  // If showing in groups, indent the items

                if (currentItem.IsGroup())
                {
                    text.Append(currentItem.GetDisplayName());
                }
                else if (copyType == CopyType.Row)
                {
                    for (int columnIndex = 0; columnIndex < numVisibleColumns; columnIndex++)
                    {
                        if (columnIndex > 0)
                            text.Append(", ");
                        var property = m_Layout.Properties[columnIndex];
                        text.Append(currentItem.ReportItem.GetProperty(property.Type));
                    }
                }
                else
                {
                    text.Append(currentItem.ReportItem.GetProperty(propertyType));
                }
            }

            EditorInterop.CopyToClipboard(text.ToString());
        }

        public void ClearSelection()
        {
            state.selectedIDs.Clear();

            m_SelectionChanged = true;
            m_SelectionChangedReportItems = true;
        }

        void SortIfNeeded(IList<TreeViewItem> rows)
        {
            if (rows == null || rows.Count <= 1)
                return;

            if (multiColumnHeader.sortedColumnIndex == -1)
                return; // No column to sort for (just use the order the data are in)

            SortByMultipleColumns(rows);
            Repaint();
        }

        void SortByMultipleColumns(IList<TreeViewItem> rows)
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;
            if (sortedColumns.Length == 0)
                return;

            var columnAscending = new bool[sortedColumns.Length];
            for (var i = 0; i < sortedColumns.Length; i++)
                columnAscending[i] = multiColumnHeader.IsSortedAscending(sortedColumns[i]);

            var root = new ItemTree(null, m_Layout);
            var stack = new Stack<ItemTree>();
            stack.Push(root);
            foreach (var row in rows)
            {
                var r = row as IssueTableItem;
                if (r == null)
                    continue;

                var activeParentDepth = stack.Peek().Depth;

                while (row.depth <= activeParentDepth)
                {
                    stack.Pop();
                    activeParentDepth = stack.Peek().Depth;
                }

                if (row.depth > activeParentDepth)
                {
                    var t = new ItemTree(r, m_Layout);
                    stack.Peek().AddChild(t);
                    stack.Push(t);
                }
            }

            root.Sort(sortedColumns, columnAscending, groupPropertyIndex);

            // convert back to rows
            var newRows = new List<TreeViewItem>(rows.Count);
            root.ToList(newRows);
            rows.Clear();
            foreach (var treeViewItem in newRows)
                rows.Add(treeViewItem);
        }

        internal class ItemTree
        {
            readonly List<ItemTree> m_Children;
            readonly IssueTableItem m_Item;
            readonly IssueLayout m_Layout;

            public ItemTree(IssueTableItem i, IssueLayout layout)
            {
                m_Item = i;
                m_Children = new List<ItemTree>();
                m_Layout = layout;
            }

            public int Depth
            {
                get { return m_Item == null ? -1 : m_Item.depth; }
            }

            public void AddChild(ItemTree item)
            {
                m_Children.Add(item);
            }

            public void Sort(int[] columnSortOrder, bool[] isColumnAscending, int groupProperty)
            {
                m_Children.Sort(delegate (ItemTree a, ItemTree b)
                {
                    var rtn = 0;

                    for (var i = 0; i < columnSortOrder.Length; i++)
                    {
                        var order = isColumnAscending[i] ? 1 : -1;

                        if (a.m_Item.IsGroup() && b.m_Item.IsGroup())
                            rtn = order * CompareGroupItemTo(a.m_Item, b.m_Item, columnSortOrder[i], groupProperty);
                        else
                            rtn = order * ProjectIssueExtensions.CompareTo(a.m_Item?.ReportItem, b.m_Item?.ReportItem, m_Layout.Properties[columnSortOrder[i]].Type);

                        if (rtn == 0)
                            continue;

                        return rtn;
                    }

                    return rtn;
                });

                foreach (var child in m_Children)
                    child.Sort(columnSortOrder, isColumnAscending, groupProperty);
            }

            int CompareGroupItemTo(IssueTableItem itemA, IssueTableItem itemB, int columnIndex, int groupProperty)
            {
                if (itemA.children == null || itemB.children == null)
                    return 0;

                if (itemA.children.Count == 0 || itemB.children.Count == 0)
                    return 0;

                if (columnIndex == 0)
                {
                    return ProjectIssueExtensions.CompareTo(((IssueTableItem)itemA.children[0]).ReportItem,
                        ((IssueTableItem)itemB.children[0]).ReportItem,
                        m_Layout.Properties[groupProperty].Type);
                }

                var property = m_Layout.Properties[columnIndex];
                if (property.IsHidden)
                    return 0;

                var propertyType = property.Type;

                if (PropertyTypeUtil.IsCustom(propertyType))
                {
                    var customPropertyIndex = PropertyTypeUtil.ToCustomIndex(propertyType);
                    if (property.Format == PropertyFormat.Bytes)
                    {
                        var valueA = GetGroupColumnSumUlong(itemA, customPropertyIndex);
                        var valueB = GetGroupColumnSumUlong(itemB, customPropertyIndex);

                        return valueA > valueB ? 1 : (valueA < valueB ? -1 : 0);
                    }
                    if (property.Format == PropertyFormat.Time ||
                        property.Format == PropertyFormat.Percentage)
                    {
                        var valueA = GetGroupColumnSumFloat(itemA, customPropertyIndex);
                        var valueB = GetGroupColumnSumFloat(itemB, customPropertyIndex);

                        return valueA > valueB ? 1 : (valueA < valueB ? -1 : 0);
                    }

                    var stringA = GetGroupFirstChildCustomProperty(itemA, customPropertyIndex);
                    var stringB = GetGroupFirstChildCustomProperty(itemB, customPropertyIndex);
                    return ProjectIssueExtensions.StringCompareWithLongIntSupport(stringA, stringB);
                }
                else
                {
                    var stringA = GetGroupFirstChildProperty(itemA, property.Type);
                    var stringB = GetGroupFirstChildProperty(itemB, property.Type);
                    return ProjectIssueExtensions.StringCompareWithLongIntSupport(stringA, stringB);
                }
            }

            string GetGroupFirstChildCustomProperty(IssueTableItem item, int customPropertyIndex)
            {
                if (item.children.Count == 0)
                    return string.Empty;

                var issueTableItem = item.children[0] as IssueTableItem;
                return issueTableItem.ReportItem.GetCustomProperty(customPropertyIndex);
            }

            string GetGroupFirstChildProperty(IssueTableItem item, PropertyType propertyType)
            {
                if (item.children.Count == 0)
                    return string.Empty;

                var issueTableItem = item.children[0] as IssueTableItem;
                return issueTableItem.ReportItem.GetProperty(propertyType);
            }

            ulong GetGroupColumnSumUlong(IssueTableItem item, int customPropertyIndex)
            {
                ulong sum = 0;
                foreach (var childItem in item.children)
                {
                    var issueTableItem = childItem as IssueTableItem;
                    var value = issueTableItem.ReportItem.GetCustomPropertyUInt64(customPropertyIndex);
                    sum += value;
                }

                return sum;
            }

            float GetGroupColumnSumFloat(IssueTableItem item, int customPropertyIndex)
            {
                float sum = 0;
                foreach (var childItem in item.children)
                {
                    var issueTableItem = childItem as IssueTableItem;
                    var value = issueTableItem.ReportItem.GetCustomPropertyFloat(customPropertyIndex);
                    sum += value;
                }

                return sum;
            }

            public void ToList(List<TreeViewItem> list)
            {
                // TODO be good to optimise this, rarely used, so not required
                if (m_Item != null)
                    list.Add(m_Item);
                foreach (var child in m_Children)
                    child.ToList(list);
            }
        }

        static class Tooltip
        {
            public static string Area = "Areas that this issue might have an impact on";
            public static string HotPath = "Potential hot-path";
        }
    }
}
