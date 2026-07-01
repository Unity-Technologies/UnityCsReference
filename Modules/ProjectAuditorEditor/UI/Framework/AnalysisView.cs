// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal class AnalysisView : IIssueFilter
    {
        enum ExportMode
        {
            All = 0,
            Filtered = 1,
            Selected
        }

        class NullFilter : IIssueFilter
        {
            public bool Match(ReportItem issue)
            {
                return true;
            }
        }

        protected Draw2D m_2D;
        protected bool m_Dirty = true;
        protected bool m_ColumnWidthsDirty;
        protected SeverityRules m_Rules;
        protected ViewStates m_ViewStates;
        protected ViewDescriptor m_Desc;
        protected List<ReportItem> m_Issues = new List<ReportItem>();
        protected IssueLayout m_Layout;
        protected IssueTable m_Table;
        protected TextFilter m_TextFilter;
        protected ViewManager m_ViewManager;
        protected ProjectAuditorWindow m_Window;

        IIssueFilter m_BaseFilter;
        DependencyView m_DependencyView;
        GUIContent m_HelpButtonContent;
        Utility.DropdownItem[] m_GroupDropdownItems;

        int m_SortPropertyIndex = -1;
        bool m_SortAscending = true;
        int m_TabButtonControlID = -1;
        int m_TabButtonClickedID = -1;
        DateTime m_TabButtonClickedStartTimer;

        Vector2 m_VerticalScrollViewPos;
        Vector2 m_LastVerticalScrollViewSize;
        Vector2 m_DetailsScrollPos;

        public ViewDescriptor Desc => m_Desc;

        public virtual string Description => $"A list of {m_Desc.DisplayName} found in the project.";
        public virtual bool OnlyCriticalIssues() { return false; }

        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        public string DocumentationUrl => Documentation.GetPageUrl(new string(m_Desc.DisplayName.Where(char.IsLetterOrDigit).ToArray()));
#pragma warning restore UA2001

        public int NumIssues => m_Issues.Count;

        public int NumFilteredIssues => m_Table.GetNumMatchingIssues();

        internal ViewManager ViewManager
        {
            get { return m_ViewManager; }
        }

        public Vector2 VerticalScrollViewPos
        {
            get => m_VerticalScrollViewPos;
            set => m_VerticalScrollViewPos = value;
        }

        public Vector2 LastVerticalScrollViewSize
        {
            get => m_LastVerticalScrollViewSize;
            set => m_LastVerticalScrollViewSize = value;
        }

        public virtual bool ShowVerticalScrollView => false;

        public AnalysisView(ViewManager viewManager)
        {
            m_2D = new Draw2D(ProjectAuditor.s_DataPath + "/Shaders/ProjectAuditor.shader");
            m_ViewManager = viewManager;
        }

        public virtual void Create(ViewDescriptor descriptor, IssueLayout layout, SeverityRules rules, ViewStates viewStates, ProjectAuditorWindow window)
        {
            m_Desc = descriptor;
            m_Rules = rules;
            m_ViewStates = viewStates;
            m_Window = window;
            m_BaseFilter = (IIssueFilter)window ?? new NullFilter();
            m_Layout = layout;

            if (layout.Properties == null || layout.Properties.Length == 0)
                return;

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_GroupDropdownItems = m_Layout.Properties.Select(p => new Utility.DropdownItem
#pragma warning restore UA2001
            {
                Content = new GUIContent(p.IsDefaultGroup ? p.Name + " (default)" : p.Name),
                SelectionContent = new GUIContent("Group By: " + p.Name),
                Enabled = p.Format == PropertyFormat.String || p.Format == PropertyFormat.Bool || p.Format == PropertyFormat.Integer,
                UserData = Array.IndexOf(m_Layout.Properties, p)
            }).ToArray();

            if (m_Table != null)
                return;

            var state = new TreeViewState();
            var columns = new MultiColumnHeaderState.Column[layout.Properties.Length];
            for (var i = 0; i < layout.Properties.Length; i++)
            {
                var property = layout.Properties[i];

                var minWidth = 20;
                var width = 80;
                switch (property.Type)
                {
                    case PropertyType.Description:
                        width = 300;
                        if (m_SortPropertyIndex == -1)
                            m_SortPropertyIndex = i;
                        break;
                    case PropertyType.Path:
                        width = 500;
                        break;
                    case PropertyType.LogLevel:
                    case PropertyType.Severity:
                        width = 24;
                        m_SortPropertyIndex = i;
                        break;
                }

                if (property.IsHidden)
                    minWidth = width = 0;

                columns[i] = new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(property.Name, layout.Properties[i].LongName),
                    width = width,
                    minWidth = minWidth,
                    autoResize = true,
                    allowToggleVisibility = (i > 0) && !property.IsHidden // Can't hide the first column (we must have at least one visible column)
                };
            }

            var multiColumnHeader = new PAMultiColumnHeader(new MultiColumnHeaderState(columns));

            // set default sorting column (priority: Severity/logLevel, description or first column)
            multiColumnHeader.SetSorting(m_SortPropertyIndex != -1 ? m_SortPropertyIndex : 0, m_SortAscending);

            m_Table = new IssueTable(state,
                multiColumnHeader,
                m_Desc,
                layout,
                m_Rules,
                this);

            if (m_Desc.ShowDependencyView)
                m_DependencyView = new DependencyView(new TreeViewState(), m_Desc.OnOpenIssue);

            if (m_TextFilter == null)
                m_TextFilter = new TextFilter(layout.Properties);

            var helpButtonTooltip = string.Format("Open Reference for {0}", m_Desc.DisplayName);
            m_HelpButtonContent = Utility.GetIcon(Utility.IconType.Help, helpButtonTooltip);
        }

        public virtual void AddIssues(IEnumerable<ReportItem> allIssues)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var issues = allIssues.Where(i => i.Category == m_Desc.Category).ToArray();
#pragma warning restore UA2001
            if (issues.Length == 0)
                return;

            m_Issues.AddRange(issues);
            m_Table.AddIssues(issues);

            MarkDirty();
        }

        void AdjustColumnWidths()
        {
            bool flatView = m_Table.flatView;
            bool showIgnoredIssues = m_Table.showIgnoredIssues;
            bool needToExpand = flatView == false || showIgnoredIssues == true;

            // Base column widths on the contents of a flat table, skipping ignored issues.
            if (needToExpand)
            {
                m_Table.flatView = true;
                m_Table.showIgnoredIssues = false;
                m_Table.Reload();
            }

            var rows = m_Table.GetRows();

            if (rows == null || rows.Count == 0 || (rows.Count == 1 && rows[0].displayName == "No items"))
                return;

            var header = m_Table.multiColumnHeader;

            for (var columnIndex = 0; columnIndex < m_Layout.Properties.Length; columnIndex++)
            {
                var property = m_Layout.Properties[columnIndex];
                if (property.IsHidden)
                {
                    header.GetColumn(columnIndex).width = 0;
                    continue;
                }

                var propertyType = property.Type;
                var propertyFormat = property.Format;

                string widestPropString = property.Name;
                float widestPropWidth = Utility.EstimateWidth(widestPropString);

                if (propertyFormat != PropertyFormat.Bool) // Just use the column header for bool columns
                {
                    for (int rowIndex = 0; rowIndex < rows.Count; ++rowIndex)
                    {
                        var row = rows[rowIndex];
                        var item = ((IssueTableItem)row);
                        var issue = item.ReportItem;
                        string cellString;

                        if (PropertyTypeUtil.IsCustom(propertyType))
                        {
                            var customPropertyIndex = PropertyTypeUtil.ToCustomIndex(propertyType);

                            switch (propertyFormat)
                            {
                                case PropertyFormat.Bytes:
                                    cellString =
                                        Formatting.FormatSize(issue.GetCustomPropertyUInt64(customPropertyIndex));
                                    break;
                                case PropertyFormat.Time:
                                    cellString =
                                        Formatting.FormatTime(issue.GetCustomPropertyFloat(customPropertyIndex));
                                    break;
                                case PropertyFormat.ULong:
                                    var ulongAsString = issue.GetCustomProperty(customPropertyIndex);
                                    cellString = ulong.TryParse(ulongAsString, out var ulongValue) ? ulongAsString : "";
                                    break;
                                case PropertyFormat.Integer:
                                    var intAsString = issue.GetCustomProperty(customPropertyIndex);
                                    cellString = int.TryParse(intAsString, out var intValue) ? intAsString : "";
                                    break;
                                case PropertyFormat.Percentage:
                                    cellString =
                                        Formatting.FormatPercentage(issue.GetCustomPropertyFloat(customPropertyIndex), 1);
                                    break;
                                default:
                                    cellString = issue.GetProperty(propertyType);
                                    break;
                            }
                        }
                        else
                        {
                            cellString = issue.GetProperty(propertyType);
                        }

                        var propWidth = Utility.EstimateWidth(cellString);

                        if (propWidth > widestPropWidth)
                        {
                            widestPropString = cellString;
                            widestPropWidth = propWidth;
                        }
                    }
                }

                var width = Utility.GetWidth_SlowButAccurate(widestPropString, m_ViewStates.fontSize);

                // Space to accommodate indenting in column 0
                if (columnIndex == 0)
                {
                    width += LayoutSize.CellItemTreeIndent;
                }

                if (widestPropString != property.Name && HasIcon(property))
                    width += LayoutSize.CellItemIconSize;

                // CalcSize underestimates the width of text in cells. Why? No idea. This is a fudge.
                width += LayoutSize.CellWidthPadding;

                var clamp = property.MaxAutoWidth;
                if (clamp != 0 && width > clamp)
                    width = clamp;

                header.GetColumn(columnIndex).width = width;
            }

            if (needToExpand)
            {
                m_Table.flatView = flatView;
                m_Table.showIgnoredIssues = showIgnoredIssues;
                m_Table.Reload();
            }
        }

        bool HasIcon(PropertyDefinition property)
        {
            switch (property.Type)
            {
                case PropertyType.LogLevel:
                case PropertyType.Severity:
                    return true;
                case PropertyType.Description:
                    return m_Desc.DescriptionWithIcon;
                default:
                    if (PropertyTypeUtil.IsCustom(property.Type))
                    {
                        // ULong and Integer actually CAN have icons, but only when they also have empty strings, so the size
                        // of the column is likely to be dictated by some other cell.
                        switch (property.Format)
                        {
                            case PropertyFormat.Bool:
                                return true;
                            case PropertyFormat.ULong:
                                return false;
                            case PropertyFormat.Integer:
                                return false;
                            default:
                                return false;
                        }
                    }
                    return false;
            }
        }

        public virtual void Clear()
        {
            if (m_Table == null)
                return;

            m_Issues.Clear();
            m_Table.Clear();

            MarkDirty();
        }

        /// <summary>
        /// Mark view as dirty. Use this to force a table reload.
        /// </summary>
        public void MarkDirty()
        {
            m_Dirty = true;
        }

        /// <summary>
        /// Mark column widths as dirty. Use this to force the table columns to resize to fit the displayed content.
        /// </summary>
        public void MarkColumnWidthsDirty()
        {
            m_ColumnWidthsDirty = true;
        }

        void RefreshIfDirty()
        {
            if (!m_Dirty)
                return;

            m_Table.Reload();
            m_Dirty = false;
        }

        private bool cacheDiagnosticProperty = true;
        private bool diagnosticResult = false;

        // TODO: remove this method when not used anymore
        public bool IsDiagnostic()
        {
            if (cacheDiagnosticProperty)
            {
                diagnosticResult = Array.Exists(m_Layout.Properties, p => p.Type == PropertyType.Severity);
                cacheDiagnosticProperty = false;
            }

            return diagnosticResult;
        }

        public bool IsValid()
        {
            return m_Layout.Properties.Length == 0 || m_Table != null;
        }

        public virtual void DrawFilters()
        {
        }

        public virtual void DrawContent()
        {
            using (new EditorGUILayout.HorizontalScope(GUI.skin.box, GUILayout.ExpandHeight(true)))
            {
                DrawTable();

                if (m_Desc.ShowDetails)
                {
                    EditorGUILayout.BeginVertical(GUILayout.Width(LayoutSize.DetailsPanelWidth));

                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();

                    var selectedIssues = GetSelection();
                    DrawDetails(selectedIssues);
                    EditorGUILayout.EndVertical();
                }
            }

            if (m_Desc.ShowDependencyView)
            {
                var selectedIssues = GetSelection();
                DrawDependencyView(selectedIssues);
            }
        }

        public void DrawTopPanel()
        {
            if (!m_Desc.ShowInfoPanel)
                return;

            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
            {
                // Add a bit of space to improve readability
                EditorGUILayout.Space();

                m_ViewStates.info = Utility.BoldFoldout(m_ViewStates.info, Contents.InfoFoldout);
                if (m_ViewStates.info)
                {
                    EditorGUI.indentLevel++;

                    DrawInfo();

                    EditorGUI.indentLevel--;
                }
            }

            if (m_Desc.ShowAdditionalInfoPanel?.Invoke(this) ?? false)
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
                {
                    DrawAdditionalInfo();
                }
            }
        }

        protected virtual void DrawInfo()
        {
        }

        protected virtual void DrawAdditionalInfo()
        {
        }

        void DrawTable()
        {
            RefreshIfDirty();

            EditorGUILayout.BeginVertical();

            DrawToolbar();

            // Because adjusting the column width relies on GUIStyle.CalcSize, and because that method can only be called during an OnGUI event, we have to set the widths here.
            if (m_ColumnWidthsDirty)
            {
                AdjustColumnWidths();
                m_ColumnWidthsDirty = false;
            }

            var r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));

            m_Table.OnGUI(r);

            EditorGUILayout.EndVertical();
        }

        public virtual void DrawSearch()
        {
            EditorGUILayout.BeginHorizontal();

            // note that we don't need to detect string changes (with EditorGUI.Begin/EndChangeCheck(), because the TreeViewController already triggers a BuildRows() when the text changes
            EditorGUILayout.LabelField(Contents.SearchStringLabel, ProjectAuditorWindow.LayoutSize.FilterOptionsLabelWidth);

            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            m_TextFilter.searchString = EditorGUILayout.DelayedTextField(m_TextFilter.searchString, GUILayout.Width(280));
            m_Table.searchString = m_TextFilter.searchString;

            EditorGUI.BeginChangeCheck();
            if (EditorGUI.EndChangeCheck())
                MarkDirty();

            GUILayout.FlexibleSpace();

            EditorGUI.indentLevel = oldIndent;

            EditorGUILayout.EndHorizontal();
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            DrawViewOptions();

            EditorGUILayout.Space();

            DrawDataOptions();

            EditorGUILayout.EndHorizontal();
        }

        public virtual void DrawDetails(ReportItem[] selectedIssues)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.EndVertical();
        }

        protected void DrawDetailsHeader(GUIContent title, string copyText, string docsUrl)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(title, SharedStyles.BoldLabel);
                DrawDetailsButtons(copyText, docsUrl);
            }
        }
        protected void DrawDetailsContent(string text, string docsUrl)
        {
            DrawDetailsContent(text, docsUrl, ref m_DetailsScrollPos);
        }

        protected void DrawDetailsContent(string text, string docsUrl, ref Vector2 scrollPos)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));

            GUILayout.TextArea(text, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            DrawDetailsExternalDocsLink(docsUrl);

            EditorGUILayout.EndScrollView();
        }

        protected void DrawDetailsExternalDocsLink(string docsUrl)
        {
            if (!string.IsNullOrEmpty(docsUrl) && !Utility.IsInternalDocsLink(docsUrl))
            {
                if (GUILayout.Button(SharedContents.DocumentationExternal, SharedStyles.LinkLabel))
                    Application.OpenURL(docsUrl); // Only show this icon for external docs

                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            }
        }

        public virtual void DrawViewOptions()
        {
            if (m_ViewManager.OnAnalysisRequested != null && m_ViewManager.Report.IsForCurrentProject())
            {
                GUI.enabled = !m_ViewManager.HasPendingCategories();

                DrawToolbarButtonIcon(Contents.AnalyzeNowButton, () =>
                {
                    if (m_Table.GetNumIgnoredIssues() > 0)
                    {
                        if (EditorUtility.DisplayDialog(k_Discard, k_DiscardQuestion, "Discard Ignored Items", "Cancel"))
                        {
                            m_ViewManager.OnAnalysisRequested(m_Desc.Category);
                        }
                    }
                    else
                    {
                        m_ViewManager.OnAnalysisRequested(m_Desc.Category);
                    }
                });

                GUI.enabled = true;
            }

            m_Table.SetFontSize(m_ViewStates.fontSize);

            if (!m_Layout.IsHierarchy)
            {
                EditorGUI.BeginChangeCheck();
                m_Table.flatView = !GUILayout.Toggle(!m_Table.flatView, Contents.HierarchyButton, EditorStyles.toolbarButton, GUILayout.Width(LayoutSize.ToolbarIconSize));
                if (EditorGUI.EndChangeCheck())
                {
                    MarkDirty();
                }

                using (new EditorGUI.DisabledScope(m_Table.flatView))
                {
                    Utility.ToolbarDropdownList(m_GroupDropdownItems, m_Table.groupPropertyIndex,
                        (data) =>
                        {
                            var groupPropertyIndex = (int)data;
                            if (groupPropertyIndex != m_Table.groupPropertyIndex)
                            {
                                SetRowsExpanded(false);

                                m_Table.groupPropertyIndex = groupPropertyIndex;
                                m_Table.Clear();
                                m_Table.AddIssues(m_Issues);
                                m_Table.Reload();
                            }
                        }, GUILayout.Width(ToolbarButtonSize * 3));

                    // collapse/expand buttons
                    DrawToolbarButton(Contents.CollapseAllButton, () => SetRowsExpanded(false));
                    DrawToolbarButton(Contents.ExpandAllButton, () => SetRowsExpanded(true));
                }
            }
        }

        void DrawDataOptions()
        {
            if (m_Desc.OnDrawToolbar != null)
                m_Desc.OnDrawToolbar(m_ViewManager);

            if (Utility.ToolbarButtonWithDropdownList(Contents.ExportButton, k_ExportModeStrings,
                (data) =>
                {
                    var mode = (ExportMode)data;
                    switch (mode)
                    {
                        case ExportMode.All:
                            Export();
                            return;
                        case ExportMode.Filtered:
                            Export(Match);
                            return;
                        case ExportMode.Selected:
                            var selectedItems = m_Table.GetSelectedItems();
                            Export(issue =>
                            {
                                return selectedItems.Exists(item => item.Find(issue));
                            });
                            return;
                    }
                }, GUILayout.Width(ToolbarButtonSize)))
            {
                Export();

                GUIUtility.ExitGUI();
            }
        }

        void DrawDependencyView(ReportItem[] issues)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(LayoutSize.DependencyViewHeight));

            m_ViewStates.dependencies = Utility.BoldFoldout(m_ViewStates.dependencies, m_Desc.DependencyViewGuiContent != null ? m_Desc.DependencyViewGuiContent : Contents.Dependencies);
            if (m_ViewStates.dependencies)
            {
                if (issues.Length == 0)
                {
                    EditorGUILayout.LabelField(k_NoSelectionText, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                }
                else if (issues.Length > 1)
                {
                    EditorGUILayout.LabelField(k_MultipleSelectionText, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                }
                else// if (issues.Length == 1)
                {
                    var selection = issues[0];
                    var dependencies = selection.Dependencies;

                    m_DependencyView.SetRoot(dependencies);

                    if (dependencies != null)
                    {
                        var r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));

                        m_DependencyView.OnGUI(r);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(k_AnalysisIsRequiredText, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        public void SetSearch(string filter)
        {
            if (m_TextFilter != null)
                m_TextFilter.searchString = filter;
        }

        public int GetSelectionCount()
        {
            var selectedItems = m_Table.GetSelectedItems();
            return selectedItems.Count;
        }

        public ReportItem[] GetSelection()
        {
            return m_Table.GetSelectedReportItems();
        }

        public void SetSelection(Func<ReportItem, bool> predicate)
        {
            RefreshIfDirty();

            // Expand all rows. This is a workaround for the fact that we can't select rows that are not visible.
            SetRowsExpanded(true);

            var rows = m_Table.GetRows();
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var selectedIDs = new List<int>(rows.Select(item => item as IssueTableItem).Where(i => i != null && i.ReportItem != null && predicate(i.ReportItem)).Select(i => i.id));
#pragma warning restore UA2001

            m_Table.SetSelection(selectedIDs);
        }

        public void FrameSelection()
        {
            var selectedItems = m_Table.GetSelectedItems();
            if (selectedItems.Count > 0)
            {
                var firstItem = selectedItems[0];
                m_Table.FrameItem(firstItem.id);
            }
        }

        public void ClearSelection()
        {
            m_Table.ClearSelection();
        }

        void SetRowsExpanded(bool expanded)
        {
            if (expanded)
            {
                var rows = m_Table.GetRows();

                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                m_Table.SetExpanded(rows.Select(r => r.id).ToList());
#pragma warning restore UA2001
            }
            else
            {
                m_Table.SetExpanded(new List<int>());
            }
        }

        protected virtual IReadOnlyCollection<ReportItem> GetIssuesToExport()
        {
            return m_ViewManager.Report.FindByCategory(m_Layout.Category);
        }

        protected virtual void Export(Func<ReportItem, bool> predicate = null)
        {
            var path = EditorUtility.SaveFilePanel("Save to CSV file", UserPreferences.LoadSavePath, string.Format("project-auditor-{0}.csv", m_Desc.Category).ToLower(),
                "csv");
            if (path.Length != 0)
            {
                using (var exporter = new CsvExporter(m_ViewManager.Report))
                {
                    var issues = GetIssuesToExport();
                    exporter.Export(path, m_Layout.Category, issues, (issue) =>
                    {
                        if (!Match(issue))
                            return false;
                        if (predicate != null && !predicate(issue))
                            return false;

                        return true;
                    });
                }

                EditorUtility.RevealInFinder(path);

                if (m_ViewManager.OnViewExportCompleted != null)
                    m_ViewManager.OnViewExportCompleted();

                UserPreferences.LoadSavePath = Path.GetDirectoryName(path);
            }
        }

        public virtual bool Match(ReportItem issue)
        {
            return m_BaseFilter.Match(issue) && m_TextFilter.Match(issue);
        }

        internal void OnEnable()
        {
            LoadSettings();
        }

        public virtual void LoadSettings()
        {
            if (m_Table == null)
                return;

            var columns = m_Table.multiColumnHeader.state.columns;
            for (int i = 0; i < m_Layout.Properties.Length; i++)
            {
                // when reloading we need to make sure visible columns have a width >= minWidth
                columns[i].width = m_Layout.Properties[i].IsHidden ? 0 : Math.Max(columns[i].minWidth, EditorPrefs.GetFloat(GetPrefKey(k_ColumnSizeKey + i), columns[i].width));
            }

            var defaultGroupPropertyIndex = m_Layout.DefaultGroupPropertyIndex;
            m_Table.flatView = EditorPrefs.GetBool(GetPrefKey(k_FlatModeKey), defaultGroupPropertyIndex == -1);
            m_Table.showIgnoredIssues = EditorPrefs.GetBool(GetPrefKey(k_ShowIgnoredIssuesKey), false);
            m_Table.groupPropertyIndex = EditorPrefs.GetInt(GetPrefKey(k_GroupPropertyIndexKey), defaultGroupPropertyIndex);
            m_SortPropertyIndex = EditorPrefs.GetInt(GetPrefKey(k_SortPropertyIndexKey), 0);
            m_SortAscending = EditorPrefs.GetBool(GetPrefKey(k_SortAscendingKey), true);
            m_Table.multiColumnHeader.SetSorting(m_SortPropertyIndex, m_SortAscending);

            m_TextFilter.searchDependencies = EditorPrefs.GetBool(GetPrefKey(k_SearchDepsKey), false);
            m_TextFilter.ignoreCase = EditorPrefs.GetBool(GetPrefKey(k_SearchIgnoreCaseKey), true);
            m_TextFilter.searchString = EditorPrefs.GetString(GetPrefKey(k_SearchStringKey));
        }

        public virtual void SaveSettings()
        {
            if (m_Table == null)
                return;

            var columns = m_Table.multiColumnHeader.state.columns;
            for (int i = 0; i < m_Layout.Properties.Length; i++)
            {
                EditorPrefs.SetFloat(GetPrefKey(k_ColumnSizeKey + i), columns[i].width);
            }
            EditorPrefs.SetBool(GetPrefKey(k_FlatModeKey), m_Table.flatView);
            EditorPrefs.SetBool(GetPrefKey(k_ShowIgnoredIssuesKey), m_Table.showIgnoredIssues);
            EditorPrefs.SetInt(GetPrefKey(k_GroupPropertyIndexKey), m_Table.groupPropertyIndex);

            EditorPrefs.SetInt(GetPrefKey(k_SortPropertyIndexKey), m_SortPropertyIndex);
            EditorPrefs.SetBool(GetPrefKey(k_SortAscendingKey), m_SortAscending);

            EditorPrefs.SetBool(GetPrefKey(k_SearchDepsKey), m_TextFilter.searchDependencies);
            EditorPrefs.SetBool(GetPrefKey(k_SearchIgnoreCaseKey), m_TextFilter.ignoreCase);
            EditorPrefs.SetString(GetPrefKey(k_SearchStringKey), m_TextFilter.searchString);
        }

        string GetPrefKey(string key)
        {
            return $"{k_PrefKeyPrefix}.{m_Desc.DisplayName}.{key}";
        }

        public static void DrawActionButton(GUIContent guiContent, Action onClick)
        {
            if (GUILayout.Button(guiContent, GUILayout.Height(LayoutSize.ActionButtonHeight)))
            {
                onClick();
            }
        }

        public static void DrawToolbarButton(GUIContent guiContent, Action onClick)
        {
            if (GUILayout.Button(
                guiContent, EditorStyles.toolbarButton,
                GUILayout.Width(ToolbarButtonSize)))
            {
                onClick();
            }
        }

        public static void DrawToolbarLargeButton(GUIContent guiContent, Action onClick)
        {
            if (GUILayout.Button(
                guiContent, EditorStyles.toolbarButton,
                GUILayout.Width(LayoutSize.ToolbarLargeButtonSize)))
            {
                onClick();
            }
        }

        public static void DrawToolbarButtonIcon(GUIContent guiContent, Action onClick)
        {
            if (GUILayout.Button(
                guiContent, EditorStyles.toolbarButton,
                GUILayout.Width(LayoutSize.ToolbarIconSize)))
            {
                onClick();
            }
        }

        void DrawDetailsButtons(string copyText, string docsUrl)
        {
            int size = LayoutSize.TabButtonSize;

            if (!string.IsNullOrEmpty(docsUrl))
            {
                if (Utility.IsInternalDocsLink(docsUrl)) // Only show this icon for internal docs
                {
                    var rect = GUILayoutUtility.GetRect(SharedContents.DocumentationInternal, SharedStyles.DocumentationButton, GUILayout.Width(size), GUILayout.Height(size));
                    bool result = GUI.Button(rect, SharedContents.DocumentationInternal, SharedStyles.DocumentationButton);
                    EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

                    if (result)
                        Application.OpenURL(Utility.GetVersionedDocsUrl(docsUrl));
                }
            }

            if (copyText != null)
            {
                var evt = Event.current;
                var content = SharedContents.CopyToClipboard;
                int id = GUIUtility.GetControlID(content, FocusType.Passive);
                var styleToUse = (m_TabButtonClickedID == id) ? SharedStyles.CopyToClipboardButtonClicked : SharedStyles.CopyToClipboardButton;
                var rect = GUILayoutUtility.GetRect(content, styleToUse, GUILayout.Width(size), GUILayout.Height(size));
                var isHoverState = rect.Contains(evt.mousePosition);
                bool result = GUI.Button(rect, content, styleToUse);

                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

                if (result)
                    EditorInterop.CopyToClipboard(Formatting.StripRichTextTags(copyText));

                if (evt.type == EventType.MouseMove)
                {
                    if (isHoverState)
                    {
                        if (m_TabButtonControlID != id)
                        {
                            m_TabButtonControlID = id;
                            m_Window?.Repaint();
                            return;
                        }
                    }
                    else
                    {
                        if (m_TabButtonControlID == id)
                        {
                            m_TabButtonControlID = 0;
                            m_Window?.Repaint();
                            return;
                        }
                    }
                }

                // Display the clicked state for 1 second
                if (result)
                {
                    m_TabButtonClickedStartTimer = DateTime.UtcNow;
                    m_TabButtonClickedID = id;
                }
                if (evt.type == EventType.Layout)
                {
                    if (m_TabButtonClickedID == id)
                    {
                        var endTime = m_TabButtonClickedStartTimer + TimeSpan.FromSeconds(1);
                        if (DateTime.UtcNow < endTime)
                            m_Window?.Repaint();
                        else
                            m_TabButtonClickedID = 0;
                    }
                }
            }
        }

        // pref keys
        const string k_PrefKeyPrefix = "ProjectAuditor.AnalysisView.";
        const string k_ColumnSizeKey = "ColumnSize";
        const string k_FlatModeKey = "FlatMode";
        const string k_ShowIgnoredIssuesKey = "ShowIgnoredIssues";
        const string k_GroupPropertyIndexKey = "GroupPropertyIndex";
        const string k_SortPropertyIndexKey = "SortPropertyIndex";
        const string k_SortAscendingKey = "SortAscending";
        const string k_SearchDepsKey = "SearchDeps";
        const string k_SearchIgnoreCaseKey = "SearchIgnoreCase";
        const string k_SearchStringKey = "SearchString";

        // UI strings
        protected const string k_NoSelectionText = "<No selection>";
        protected const string k_AnalysisIsRequiredText = "<Missing Data: Please Analyze>";
        protected const string k_MultipleSelectionText = "<Multiple selection>";

        const string k_Discard = "Analyze Now";
        const string k_DiscardQuestion = "If you analyze this section, your currently ignored items will be discarded.";

        public static int ToolbarButtonSize => LayoutSize.ToolbarButtonSize;
        public static int ToolbarIconSize => LayoutSize.ToolbarIconSize;

        static readonly string[] k_ExportModeStrings =
        {
            "All",
            "Filtered",
            "Selected"
        };

        protected static class LayoutSize
        {
            public static readonly int FoldoutWidth = 260;
            public static readonly int FoldoutMaxHeight = 220;
            public static readonly int DependencyViewHeight = 200;
            public static readonly int DetailsPanelWidth = 200;
            public static readonly int ToolbarButtonSize = 80;
            public static readonly int ToolbarLargeButtonSize = 120;
            public static readonly int ToolbarIconSize = 32;
            public static readonly int ActionButtonHeight = 30;
            public static readonly int TabButtonSize = 16;
            public static readonly int CellItemIconSize = 16;
            public static readonly int CellWidthPadding = 6;
            public static readonly int CellItemTreeIndent = 30;
        }

        static class Contents
        {
            public static readonly GUIContent AnalyzeNowButton = Utility.GetIcon(Utility.IconType.Refresh, "Analyze Now!");
            public static readonly GUIContent HierarchyButton = Utility.GetIcon(Utility.IconType.Hierarchy, "Show/Hide Hierarchy");

            public static readonly GUIContent ExportButton = new GUIContent("Export", "Export current view to .csv file");
            public static readonly GUIContent ExpandAllButton = new GUIContent("Expand All");
            public static readonly GUIContent CollapseAllButton = new GUIContent("Collapse All");

            public static readonly GUIContent InfoFoldout = new GUIContent("Information");
            public static readonly GUIContent SearchStringLabel = new GUIContent("Search: ", "Text search options");
            public static readonly GUIContent Dependencies = new GUIContent("Dependencies");
        }
        protected static class SharedContents
        {
            public static readonly GUIContent Details = new GUIContent("Details", "Issue Details");
            public static readonly GUIContent Recommendation = new GUIContent("Recommendation", "Recommendation on how to solve the issue");
            public static readonly GUIContent CopyToClipboard = EditorGUIUtility.TrTextContent(string.Empty, "Copy to Clipboard");
            public static readonly GUIContent QuickFix = new GUIContent("Quick Fix", "Automatically fix the issue");
            public static readonly GUIContent QuickFixDone = new GUIContent("Fixed", "Quick fix applied");
            public static readonly GUIContent DocumentationInternal = EditorGUIUtility.TrTextContent(string.Empty, "Open the Unity documentation");
            public static readonly GUIContent DocumentationExternal = new GUIContent("Learn More", "Open external documentation");
            public static readonly GUIContent Show = new GUIContent("Show:");
            public static readonly GUIContent ShowIgnoredIssues = new GUIContent("Show Ignored Issues");
        }
    }
}
