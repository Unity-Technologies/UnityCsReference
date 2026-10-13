// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Build.Reporting;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    internal sealed class BuildListPanel : VisualElement
    {
        private const string k_UxmlPath = "BuildAnalysis/UXML/BuildListPanel.uxml";
        internal const string k_SelectedBuildKey = "BuildAnalysisWindow.SelectedBuildSessionGUID";

        internal const string k_FooterWarningClass = "build-list-footer--warning";
        private const double k_FooterWarnThreshold = 0.7;
        internal const string k_FooterWarningTooltip = "Build limit approaching. Older builds will be automatically deleted once the limit is hit. To edit the limit, go to Project Settings > Build Pipeline.";
        internal const string k_FooterExceededTooltip = "Build limit exceeded. Older builds will be automatically deleted on the next build. To edit the limit, go to Project Settings > Build Pipeline.";

        private const int k_NoItemId = -1;

        public event Action<BuildEntry> SelectionChanged;

        private readonly IBuildListActions m_Actions;
        private readonly ToolbarSearchField m_SearchField;
        private readonly ToolbarMenu m_SettingsMenu;
        private readonly TreeView m_BuildListView;
        private readonly VisualElement m_EmptyState;
        private readonly Label m_EmptyStateDescription;
        private readonly VisualElement m_FooterRoot;
        private readonly VisualElement m_FooterLimit;
        private readonly Label m_FooterLabel;
        private readonly Label m_FooterSearchCount;

        // One id per GUID, never reused, so expanded state survives a refresh.
        private readonly Dictionary<GUID, int> m_ItemIdByGuid = new Dictionary<GUID, int>();

        // SetRootItems forgets collapsed rows, so the user's collapses are reapplied.
        private readonly HashSet<int> m_CollapsedItemIds = new HashSet<int>();
        private bool m_RestoringExpandedState;

        private BuildEntry[] m_AllBuilds = Array.Empty<BuildEntry>();
        private int m_BuildHistoryLimit;
        private string[] m_AllPlatformNames = Array.Empty<string>();
        private BuildEntry[] m_FilteredBuilds = Array.Empty<BuildEntry>();
        private BuildRelationships m_Relationships = new BuildRelationships();
        private string m_CurrentSearchText = string.Empty;
        private BuildEntry m_SelectedBuild;
        private int m_PendingScrollId = k_NoItemId;

        public BuildEntry SelectedBuild => m_SelectedBuild;

        public BuildListPanel(IBuildListActions actions)
        {
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));

            var template = EditorGUIUtility.LoadRequired(k_UxmlPath) as VisualTreeAsset;
            template.CloneTree(this);

            m_SearchField = this.Q<ToolbarSearchField>("build-search");
            m_SettingsMenu = this.Q<ToolbarMenu>("build-settings-menu");
            m_BuildListView = this.Q<TreeView>("build-list");
            m_EmptyState = this.Q<VisualElement>("empty-state");
            m_EmptyStateDescription = this.Q<Label>("empty-state-description");
            m_FooterRoot = this.Q<VisualElement>("build-list-footer");
            m_FooterLimit = this.Q<VisualElement>("build-list-footer__limit");
            m_FooterLabel = this.Q<Label>("build-list-footer__label");
            m_FooterSearchCount = this.Q<Label>("build-list-footer__search-count");

            SetupBuildListView();
            SetupSettingsMenu();
            m_SearchField.RegisterValueChangedCallback(evt => ApplyFilter(evt.newValue));

            // Initial empty state.
            UpdateEmptyState();
            UpdateFooter();
        }

        public void SetBuilds(BuildEntry[] builds, int buildHistoryLimit)
        {
            SetBuilds(builds, new BuildRelationships(), buildHistoryLimit);
        }

        public void SetBuilds(BuildEntry[] builds, BuildRelationships relationships, int buildHistoryLimit)
        {
            m_AllBuilds = builds ?? Array.Empty<BuildEntry>();
            m_BuildHistoryLimit = buildHistoryLimit;
            m_Relationships = relationships ?? new BuildRelationships();
            if (m_AllPlatformNames.Length < m_AllBuilds.Length)
                m_AllPlatformNames = new string[m_AllBuilds.Length];
            for (var i = 0; i < m_AllBuilds.Length; i++)
                m_AllPlatformNames[i] = m_AllBuilds[i].Platform.ToString();
            ApplyFilter(m_CurrentSearchText);

            // After re-binding, try to restore a previously-persisted selection if
            // nothing is currently selected
            if (m_SelectedBuild == null)
                RestoreSelectionFromPrefs();
        }

        public bool SelectBuild(GUID buildSessionGUID)
        {
            if (buildSessionGUID.Empty())
                return false;

            if (FindIndex(m_AllBuilds, buildSessionGUID) < 0)
                return false; // No build with this GUID.

            var index = FindIndex(m_FilteredBuilds, buildSessionGUID);
            if (index < 0)
            {
                // An active search filter is hiding the target.
                // Clear it so the re-query below sees the rebuilt list so the build becomes selectable.
                m_SearchField.SetValueWithoutNotify(string.Empty);
                ApplyFilter(string.Empty);
                index = FindIndex(m_FilteredBuilds, buildSessionGUID);
            }

            if (index >= 0)
            {
                // SetSelectionById expands collapsed ancestors.
                var itemId = GetItemId(buildSessionGUID);
                m_BuildListView.SetSelectionById(itemId); // fires OnListSelectionChanged → SelectionChanged
                ScrollToItemId(itemId);                   // SetSelectionById does not scroll the row into view
            }

            return true;
        }

        public void ClearSelection()
        {
            var hadSelection = m_SelectedBuild != null;
            m_SelectedBuild = null;
            EditorPrefs.DeleteKey(k_SelectedBuildKey);

            // ClearSelection may fire OnListSelectionChanged with empty items, but
            // m_SelectedBuild is already null so the equality guard suppresses the
            // double-notify.
            m_BuildListView.ClearSelection();
            if (hadSelection)
                SelectionChanged?.Invoke(null);
        }

        private void SetupBuildListView()
        {
            m_BuildListView.viewDataKey = null;
            m_BuildListView.makeItem = MakeListItem;
            m_BuildListView.bindItem = BindListItem;
            m_BuildListView.selectionType = SelectionType.Single;
            m_BuildListView.fixedItemHeight = 45;
            m_BuildListView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            m_BuildListView.selectionChanged += OnListSelectionChanged;
            m_BuildListView.itemExpandedChanged += OnItemExpandedChanged;
            m_BuildListView.AddManipulator(new ContextualMenuManipulator(
                evt => PopulateContextMenu(evt.menu, m_SelectedBuild, Unsupported.IsDeveloperMode())));
        }

        private void SetupSettingsMenu()
        {
            // The status callback runs each time the menu opens, so the item tracks the current build count.
            m_SettingsMenu.menu.AppendAction("Delete All...", _ => m_Actions.DeleteAllBuilds(),
                _ => m_AllBuilds.Length > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        private VisualElement MakeListItem()
        {
            var itemContainer = new VisualElement();
            itemContainer.AddToClassList("build-list-item-container");

            var item = new VisualElement();
            item.AddToClassList("build-list-item");

            var row1 = new VisualElement();
            row1.AddToClassList("build-list-item__row1");

            var platformIcon = new Image { name = "platform-icon" };
            platformIcon.AddToClassList("platform-icon");
            row1.Add(platformIcon);

            var buildName = new Label { name = "build-name" };
            buildName.AddToClassList("build-name");
            row1.Add(buildName);

            var statusIcon = new VisualElement { name = "status-icon" };
            statusIcon.AddToClassList("status-icon");
            row1.Add(statusIcon);

            var metaRow = new VisualElement();
            metaRow.AddToClassList("build-list-item__meta-row");

            var row2 = new Label { name = "size-duration" };
            row2.AddToClassList("build-list-item__row2");

            var row3 = new Label { name = "timestamp" };
            row3.AddToClassList("build-list-item__row3");

            item.Add(row1);
            metaRow.Add(row2);
            metaRow.Add(row3);
            item.Add(metaRow);

            itemContainer.Add(item);
            return itemContainer;
        }

        private void BindListItem(VisualElement element, int index)
        {
            var build = m_BuildListView.GetItemDataForIndex<BuildEntry>(index);
            if (build == null)
                return;

            element.Q<Label>("build-name").text = build.BuildName;
            element.Q<Label>("size-duration").text =
                $"{FormatUtility.FormatSize(build.TotalSizeBytes)} • {FormatUtility.FormatDuration(build.TotalTimeMs)}";
            element.Q<Label>("timestamp").text = FormatUtility.FormatBuildDate(build.BuildStartedAt);

            var statusIcon = element.Q<VisualElement>("status-icon");
            if (build.BuildResult == BuildResult.Succeeded)
            {
                statusIcon.AddToClassList("status-icon--success");
                statusIcon.RemoveFromClassList("status-icon--failed");
            }
            else
            {
                statusIcon.AddToClassList("status-icon--failed");
                statusIcon.RemoveFromClassList("status-icon--success");
            }

            var platformIcon = element.Q<Image>("platform-icon");
            platformIcon.image = IconUtility.GetPlatformIcon(build.Platform);
        }

        private void ApplyFilter(string searchText)
        {
            m_CurrentSearchText = searchText ?? string.Empty;

            if (string.IsNullOrEmpty(m_CurrentSearchText))
            {
                m_FilteredBuilds = m_AllBuilds;
            }
            else
            {
                var filteredBuilds = new List<BuildEntry>(m_AllBuilds.Length);
                for (var i = 0; i < m_AllBuilds.Length; i++)
                {
                    var build = m_AllBuilds[i];
                    if (build.BuildName.IndexOf(m_CurrentSearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        m_AllPlatformNames[i].IndexOf(m_CurrentSearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        filteredBuilds.Add(build);
                    }
                }
                m_FilteredBuilds = filteredBuilds.ToArray();
            }

            m_BuildListView.selectionChanged -= OnListSelectionChanged;
            try
            {
                // A held selection id would expand its ancestors; ReapplySelectionAfterRebuild restores it.
                m_BuildListView.ClearSelection();
                m_BuildListView.SetRootItems(BuildTreeItems());
                RestoreExpandedState();
                m_BuildListView.Rebuild();
                ReapplySelectionAfterRebuild();
            }
            finally
            {
                m_BuildListView.selectionChanged += OnListSelectionChanged;
            }

            UpdateEmptyState();
            UpdateFooter();
        }

        private List<TreeViewItemData<BuildEntry>> BuildTreeItems()
        {
            var items = new List<TreeViewItemData<BuildEntry>>(m_FilteredBuilds.Length);

            // Search results are flat, since a match's parent may not match. With no nesting, there is no tree to build.
            if (!string.IsNullOrEmpty(m_CurrentSearchText) || !m_Relationships.HasAny)
            {
                foreach (var build in m_FilteredBuilds)
                    items.Add(new TreeViewItemData<BuildEntry>(GetItemId(build.BuildSessionGUID), build));
                return items;
            }

            var buildsByGuid = new Dictionary<GUID, BuildEntry>(m_FilteredBuilds.Length);
            foreach (var build in m_FilteredBuilds)
                buildsByGuid[build.BuildSessionGUID] = build;

            var addedToTree = new HashSet<GUID>();
            foreach (var build in m_FilteredBuilds)
            {
                // A build whose parent isn't listed, e.g. deleted by retention, is shown unnested.
                if (m_Relationships.TryGetParent(build.BuildSessionGUID, out var parent)
                    && buildsByGuid.ContainsKey(parent))
                    continue;

                // A top-level row, with its nested builds added beneath it.
                items.Add(CreateTreeItem(build, buildsByGuid, addedToTree));
            }

            // Catch anything that was never added and show it unnested.
            foreach (var build in m_FilteredBuilds)
            {
                if (addedToTree.Add(build.BuildSessionGUID))
                    items.Add(new TreeViewItemData<BuildEntry>(GetItemId(build.BuildSessionGUID), build));
            }

            return items;
        }

        private TreeViewItemData<BuildEntry> CreateTreeItem(
            BuildEntry build, Dictionary<GUID, BuildEntry> buildsByGuid, HashSet<GUID> addedToTree)
        {
            var itemId = GetItemId(build.BuildSessionGUID);
            addedToTree.Add(build.BuildSessionGUID);

            var childGuids = m_Relationships.GetChildren(build.BuildSessionGUID);

            // Nothing is nested in this build, so show it as a leaf.
            if (childGuids.Count == 0)
                return new TreeViewItemData<BuildEntry>(itemId, build);

            var children = new List<TreeViewItemData<BuildEntry>>(childGuids.Count);
            for (var i = 0; i < childGuids.Count; i++)
            {
                // Skip children that aren't listed, and any already added so a parent loop can't recurse forever.
                if (buildsByGuid.TryGetValue(childGuids[i], out var child) && !addedToTree.Contains(childGuids[i]))
                    children.Add(CreateTreeItem(child, buildsByGuid, addedToTree));
            }

            // Every child was skipped, so show the build as a leaf rather than with an empty, expandable group.
            if (children.Count == 0)
                return new TreeViewItemData<BuildEntry>(itemId, build);

            // The build with its nested builds beneath it.
            return new TreeViewItemData<BuildEntry>(itemId, build, children);
        }

        // Expand everything, then re-collapse what the user collapsed.
        private void RestoreExpandedState()
        {
            m_RestoringExpandedState = true;
            try
            {
                m_BuildListView.ExpandAll();
                foreach (var itemId in m_CollapsedItemIds)
                    m_BuildListView.CollapseItem(itemId, collapseAllChildren: false, refresh: false);
            }
            finally
            {
                m_RestoringExpandedState = false;
            }
        }

        private void OnItemExpandedChanged(TreeViewExpansionChangedArgs args)
        {
            // An args.id of -1 is a whole-tree change, which only RestoreExpandedState makes.
            if (m_RestoringExpandedState || args.id < 0)
                return;

            if (args.isExpanded)
                m_CollapsedItemIds.Remove(args.id);
            else
                m_CollapsedItemIds.Add(args.id);
        }

        private int GetItemId(GUID buildSessionGUID)
        {
            // IDs go 0, 1, 2... as builds are first seen. Entries are never removed, so Count is always the next free ID.
            if (!m_ItemIdByGuid.TryGetValue(buildSessionGUID, out var itemId))
            {
                itemId = m_ItemIdByGuid.Count;
                m_ItemIdByGuid[buildSessionGUID] = itemId;
            }
            return itemId;
        }

        private void ReapplySelectionAfterRebuild()
        {
            if (m_SelectedBuild == null)
                return;

            var selectedGUID = m_SelectedBuild.BuildSessionGUID;
            var index = FindIndex(m_FilteredBuilds, selectedGUID);
            if (index >= 0)
            {
                // Refresh the cached reference so consumers see the latest metadata
                // for this GUID (size/duration/result may have changed across rebuilds).
                m_SelectedBuild = m_FilteredBuilds[index];

                m_BuildListView.SetSelectionByIdWithoutNotify(new[] { GetItemId(selectedGUID) });
                return;
            }

            // The selected build is not in the filtered view, either because the search filter
            // hides it or because it has left the build history.
            var allIndex = FindIndex(m_AllBuilds, selectedGUID);
            var leftHistory = allIndex < 0;
            m_SelectedBuild = leftHistory ? null : m_AllBuilds[allIndex];

            m_BuildListView.ClearSelection();

            if (leftHistory)
                SelectionChanged?.Invoke(null);
        }

        private static int FindIndex(BuildEntry[] builds, GUID buildSessionGUID)
        {
            for (var i = 0; i < builds.Length; i++)
            {
                if (builds[i].BuildSessionGUID == buildSessionGUID)
                    return i;
            }
            return -1;
        }

        // Before the first layout ScrollToItemById cannot position, so defer to the first geometry pass.
        private void ScrollToItemId(int itemId)
        {
            if (m_BuildListView.layout.height > 0f)
            {
                m_BuildListView.ScrollToItemById(itemId);
                return;
            }

            var alreadyPending = m_PendingScrollId != k_NoItemId;
            m_PendingScrollId = itemId;
            if (!alreadyPending)
                m_BuildListView.RegisterCallback<GeometryChangedEvent>(OnScrollOnGeometryChanged);
        }

        private void OnScrollOnGeometryChanged(GeometryChangedEvent _)
        {
            m_BuildListView.UnregisterCallback<GeometryChangedEvent>(OnScrollOnGeometryChanged);
            if (m_PendingScrollId != k_NoItemId)
            {
                m_BuildListView.ScrollToItemById(m_PendingScrollId);
                m_PendingScrollId = k_NoItemId;
            }
        }

        private void OnListSelectionChanged(IEnumerable<object> selectedItems)
        {
            BuildEntry newSelection = null;
            foreach (var item in selectedItems)
            {
                if (item is BuildEntry selectedBuild)
                {
                    newSelection = selectedBuild;
                    break;
                }
            }

            if (m_SelectedBuild == newSelection)
                return;

            m_SelectedBuild = newSelection;
            if (newSelection != null)
                EditorPrefs.SetString(k_SelectedBuildKey, newSelection.BuildSessionGUID.ToString());
            else
                EditorPrefs.DeleteKey(k_SelectedBuildKey); // otherwise the next refresh would restore it

            SelectionChanged?.Invoke(newSelection);
        }

        private void RestoreSelectionFromPrefs()
        {
            var storedGuidString = EditorPrefs.GetString(k_SelectedBuildKey, string.Empty);
            if (string.IsNullOrEmpty(storedGuidString) || !GUID.TryParse(storedGuidString, out var storedGuid))
                return;

            var index = FindIndex(m_FilteredBuilds, storedGuid);
            if (index >= 0)
            {
                var itemId = GetItemId(storedGuid);
                m_BuildListView.SetSelectionById(itemId); // fires OnListSelectionChanged → SelectionChanged event
                ScrollToItemId(itemId);
            }
        }

        private void UpdateEmptyState()
        {
            var hasBuilds = m_FilteredBuilds.Length > 0;
            m_BuildListView.style.display = hasBuilds ? DisplayStyle.Flex : DisplayStyle.None;
            m_EmptyState.style.display = hasBuilds ? DisplayStyle.None : DisplayStyle.Flex;

            if (hasBuilds)
                return;

            if (m_AllBuilds.Length == 0)
            {
                m_EmptyStateDescription.text = "No Build Reports available.\nReports will be listed here\nafter a player or content build\nhas been created.";
            }
            else
            {
                m_EmptyStateDescription.text = $"No builds match '{m_CurrentSearchText}'.";
            }
        }

        private void UpdateFooter()
        {
            var count = m_AllBuilds.Length;
            var limit = m_BuildHistoryLimit;
            var exceeded = limit > 0 && count > limit;
            var approaching = limit > 0 && !exceeded && count >= (int)Math.Ceiling(limit * k_FooterWarnThreshold);
            var showLimit = exceeded || approaching;
            var searching = !string.IsNullOrEmpty(m_CurrentSearchText);

            m_FooterRoot.EnableInClassList(k_FooterWarningClass, showLimit);
            m_FooterLabel.text = showLimit ? $"{count} of {limit} limit" : string.Empty;
            m_FooterLimit.tooltip = exceeded ? k_FooterExceededTooltip
                                  : approaching ? k_FooterWarningTooltip
                                  : string.Empty;

            // Always filled, so the hidden count still gives the footer row its height.
            m_FooterSearchCount.text = $"Showing {m_FilteredBuilds.Length}/{count}";
            m_FooterSearchCount.style.visibility = searching ? Visibility.Visible : Visibility.Hidden;

            // Hidden rather than removed, so the list does not resize when a search starts or clears.
            m_FooterRoot.style.visibility = showLimit || searching ? Visibility.Visible : Visibility.Hidden;
        }

        internal void PopulateContextMenu(DropdownMenu menu, BuildEntry selection, bool isDeveloperMode)
        {
            var status = selection != null
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled;

            menu.AppendAction(BuildAnalysisConstants.k_ShowInFileBrowserLabel, _ => m_Actions.ShowInExplorer(selection), status);
            menu.AppendAction("Copy Path", _ => m_Actions.CopyPath(selection), status);

            if (isDeveloperMode)
                menu.AppendAction("Run Analysis Again", _ => m_Actions.RegenerateAnalysis(selection), status);

            menu.AppendSeparator();

            menu.AppendAction("Delete Build Report Directory", _ => m_Actions.DeleteBuild(selection), status);
        }
    }
}
