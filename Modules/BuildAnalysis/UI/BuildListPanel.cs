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
        private const double k_FooterWarnThreshold = 0.8;
        private const string k_FooterWarningTooltip = "Build limit approaching. To edit the limit, go to Project Settings > Build Pipeline.";
        private const string k_FooterExceededTooltip = "Build limit exceeded. Older builds will be removed on the next build. To edit the limit, go to Project Settings > Build Pipeline.";

        public event Action<BuildEntry> SelectionChanged;

        private readonly IBuildListActions m_Actions;
        private readonly ToolbarSearchField m_SearchField;
        private readonly ToolbarMenu m_SettingsMenu;
        private readonly ListView m_BuildListView;
        private readonly VisualElement m_EmptyState;
        private readonly Label m_EmptyStateTitle;
        private readonly Label m_EmptyStateDescription;
        private readonly VisualElement m_FooterRoot;
        private readonly Label m_FooterLabel;

        private BuildEntry[] m_AllBuilds = Array.Empty<BuildEntry>();
        private string[] m_AllPlatformNames = Array.Empty<string>();
        private BuildEntry[] m_FilteredBuilds = Array.Empty<BuildEntry>();
        private string m_CurrentSearchText = string.Empty;
        private BuildEntry m_SelectedBuild;

        public BuildEntry SelectedBuild => m_SelectedBuild;

        public BuildListPanel(IBuildListActions actions)
        {
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));

            var template = EditorGUIUtility.LoadRequired(k_UxmlPath) as VisualTreeAsset;
            template.CloneTree(this);

            m_SearchField = this.Q<ToolbarSearchField>("build-search");
            m_SettingsMenu = this.Q<ToolbarMenu>("build-settings-menu");
            m_BuildListView = this.Q<ListView>("build-list");
            m_EmptyState = this.Q<VisualElement>("empty-state");
            m_EmptyStateTitle = this.Q<Label>("empty-state-title");
            m_EmptyStateDescription = this.Q<Label>("empty-state-description");
            m_FooterRoot = this.Q<VisualElement>("build-list-footer");
            m_FooterLabel = this.Q<Label>("build-list-footer__label");

            SetupBuildListView();
            SetupSettingsMenu();
            m_SearchField.RegisterValueChangedCallback(evt => ApplyFilter(evt.newValue));

            // Initial empty state.
            UpdateEmptyState();
            UpdateFooter(0, 0);
        }

        public void SetBuilds(BuildEntry[] builds, int buildHistoryLimit)
        {
            m_AllBuilds = builds ?? Array.Empty<BuildEntry>();
            if (m_AllPlatformNames.Length < m_AllBuilds.Length)
                m_AllPlatformNames = new string[m_AllBuilds.Length];
            for (var i = 0; i < m_AllBuilds.Length; i++)
                m_AllPlatformNames[i] = m_AllBuilds[i].Platform.ToString();
            ApplyFilter(m_CurrentSearchText);
            UpdateFooter(m_AllBuilds.Length, buildHistoryLimit);

            // After re-binding, try to restore a previously-persisted selection if
            // nothing is currently selected
            if (m_SelectedBuild == null)
                RestoreSelectionFromPrefs();
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
            m_BuildListView.makeItem = MakeListItem;
            m_BuildListView.bindItem = BindListItem;
            m_BuildListView.selectionType = SelectionType.Single;
            m_BuildListView.fixedItemHeight = 45;
            m_BuildListView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            m_BuildListView.selectionChanged += OnListSelectionChanged;
            m_BuildListView.AddManipulator(new ContextualMenuManipulator(
                evt => PopulateContextMenu(evt.menu, m_SelectedBuild, Unsupported.IsDeveloperMode())));
        }

        private void SetupSettingsMenu()
        {
            m_SettingsMenu.menu.AppendAction("Delete All Builds...", _ => m_Actions.DeleteAllBuilds());
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
            if (index < 0 || index >= m_FilteredBuilds.Length)
                return;

            var build = m_FilteredBuilds[index];
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
                m_BuildListView.itemsSource = m_FilteredBuilds;
                m_BuildListView.Rebuild();
                ReapplySelectionAfterRebuild();
            }
            finally
            {
                m_BuildListView.selectionChanged += OnListSelectionChanged;
            }

            UpdateEmptyState();
        }

        private void ReapplySelectionAfterRebuild()
        {
            if (m_SelectedBuild == null)
                return;

            var selectedGuid = m_SelectedBuild.BuildSessionGUID;
            for (var i = 0; i < m_FilteredBuilds.Length; i++)
            {
                if (m_FilteredBuilds[i].BuildSessionGUID == selectedGuid)
                {
                    // Refresh the cached reference so consumers see the latest metadata
                    // for this GUID (size/duration/result may have changed across rebuilds).
                    m_SelectedBuild = m_FilteredBuilds[i];
                    m_BuildListView.SetSelectionWithoutNotify((IEnumerable<int>)new[] { i });
                    return;
                }
            }

            // Selected build is no longer in the filtered view. Clear and notify.
            m_SelectedBuild = null;
            m_BuildListView.SetSelectionWithoutNotify((IEnumerable<int>)Array.Empty<int>());
            SelectionChanged?.Invoke(null);
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

            SelectionChanged?.Invoke(newSelection);
        }

        private void RestoreSelectionFromPrefs()
        {
            var storedGuidString = EditorPrefs.GetString(k_SelectedBuildKey, string.Empty);
            if (string.IsNullOrEmpty(storedGuidString) || !GUID.TryParse(storedGuidString, out var storedGuid))
                return;

            for (var i = 0; i < m_FilteredBuilds.Length; i++)
            {
                if (m_FilteredBuilds[i].BuildSessionGUID == storedGuid)
                {
                    m_BuildListView.SetSelection(i); // fires OnListSelectionChanged → SelectionChanged event
                    return;
                }
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
                m_EmptyStateTitle.text = "No builds available";
                m_EmptyStateDescription.text = "Builds will appear here automatically\nafter you build your project.";
            }
            else
            {
                m_EmptyStateTitle.text = "No matching builds";
                m_EmptyStateDescription.text = $"No builds match '{m_CurrentSearchText}'";
            }
        }

        private void UpdateFooter(int count, int limit)
        {
            if (limit <= 0)
            {
                m_FooterLabel.text = $"{count} builds";
                m_FooterRoot.RemoveFromClassList(k_FooterWarningClass);
                m_FooterRoot.tooltip = string.Empty;
                return;
            }

            m_FooterLabel.text = $"{count} of {limit} builds";
            var exceeded = count > limit;
            var approaching = !exceeded && count >= (int)Math.Ceiling(limit * k_FooterWarnThreshold);
            m_FooterRoot.EnableInClassList(k_FooterWarningClass, exceeded || approaching);
            m_FooterRoot.tooltip = exceeded ? k_FooterExceededTooltip
                                 : approaching ? k_FooterWarningTooltip
                                 : string.Empty;
        }

        internal void PopulateContextMenu(DropdownMenu menu, BuildEntry selection, bool isDeveloperMode)
        {
            var status = selection != null
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled;

            menu.AppendAction("Show in Explorer", _ => m_Actions.ShowInExplorer(selection), status);
            menu.AppendAction("Copy Path", _ => m_Actions.CopyPath(selection), status);

            if (isDeveloperMode)
                menu.AppendAction("Run Analysis Again", _ => m_Actions.RegenerateAnalysis(selection), status);

            menu.AppendSeparator();

            menu.AppendAction("Delete", _ => m_Actions.DeleteBuild(selection), status);
        }
    }
}
