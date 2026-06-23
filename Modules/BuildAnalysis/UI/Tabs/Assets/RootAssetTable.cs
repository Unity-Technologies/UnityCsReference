// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    internal class RootAssetTable : VisualElement
    {
        private const string k_UxmlPath = "BuildAnalysis/UXML/RootAssetTable.uxml";

        public event Action<BuildAnalysisRootAsset?> SelectionChanged;

        private readonly ToolbarSearchField m_SearchField;
        private readonly MultiColumnListView m_ListView;
        private readonly Label m_FooterCountLabel;
        private readonly ZebraEmptyBody m_EmptyBody;

        private BuildAnalysisRootAsset[] m_RootAssets = Array.Empty<BuildAnalysisRootAsset>();
        private BuildAnalysisAsset[] m_Assets = Array.Empty<BuildAnalysisAsset>();

        // Parallel pre-formatted arrays — indexed by root asset row. Grown only when capacity
        // is exceeded so re-binds with smaller payloads don't realloc.
        private string[] m_Names = Array.Empty<string>();
        private string[] m_Paths = Array.Empty<string>();
        private string[] m_FormattedDirectSizes = Array.Empty<string>();
        private string[] m_FormattedDirectCounts = Array.Empty<string>();
        private string[] m_FormattedTotalSizes = Array.Empty<string>();
        private string[] m_FormattedTotalCounts = Array.Empty<string>();

        private readonly List<int> m_FilteredIndices = new List<int>();
        private IVisualElementScheduledItem m_SearchDebounce;
        private string m_SearchText = string.Empty;

        public RootAssetTable()
        {
            AddToClassList("section");

            var template = EditorGUIUtility.LoadRequired(k_UxmlPath) as VisualTreeAsset;
            template.CloneTree(this);

            m_SearchField = this.Q<ToolbarSearchField>("search-field");
            m_ListView = this.Q<MultiColumnListView>("root-asset-list-view");
            m_FooterCountLabel = this.Q<Label>("footer-count-label");

            m_EmptyBody = new ZebraEmptyBody(m_ListView);
            m_ListView.parent.Add(m_EmptyBody);

            m_ListView.itemsSource = m_FilteredIndices;
            m_ListView.columns["column-name"].makeCell = MakeNameCell;
            m_ListView.columns["column-name"].bindCell = BindNameCell;
            m_ListView.columns["column-direct-size"].makeCell = MakeNumericCell;
            m_ListView.columns["column-direct-size"].bindCell = BindDirectSizeCell;
            m_ListView.columns["column-direct-assets"].makeCell = MakeNumericCell;
            m_ListView.columns["column-direct-assets"].bindCell = BindDirectCountCell;
            m_ListView.columns["column-total-size"].makeCell = MakeNumericCell;
            m_ListView.columns["column-total-size"].bindCell = BindTotalSizeCell;
            m_ListView.columns["column-total-assets"].makeCell = MakeNumericCell;
            m_ListView.columns["column-total-assets"].bindCell = BindTotalCountCell;

            // Column header tooltips
            m_ListView.Q<VisualElement>("column-direct-size").tooltip = "Build output size for this root and what loads immediately with it. Excludes on-demand loadable content.";
            m_ListView.Q<VisualElement>("column-direct-assets").tooltip = "Count of source assets that load immediately with this root. Excludes on-demand loadables.";
            m_ListView.Q<VisualElement>("column-total-size").tooltip = "Build output size for everything reachable from this root, including on-demand loadable content.";
            m_ListView.Q<VisualElement>("column-total-assets").tooltip = "Count of source assets reachable from this root, including on-demand loadables.";

            m_SearchField.RegisterValueChangedCallback(evt =>
            {
                var newText = evt.newValue ?? string.Empty;
                m_SearchDebounce?.Pause();
                m_SearchDebounce = schedule.Execute(() => SetSearchText(newText)).StartingIn(200);
            });
            m_ListView.columnSortingChanged += () =>
            {
                ApplySort();
                m_ListView.RefreshItems();
            };
            m_ListView.selectionChanged += OnListSelectionChanged;
        }

        public void ClearSelection()
        {
            m_ListView.selectedIndex = -1;
        }

        private void OnListSelectionChanged(IEnumerable<object> items)
        {
            BuildAnalysisRootAsset? root = null;
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item is int rootIdx and >= 0 && rootIdx < m_RootAssets.Length)
                    {
                        root = m_RootAssets[rootIdx];
                        break;
                    }
                }
            }
            SelectionChanged?.Invoke(root);
        }

        public void Bind(BuildAnalysis analysis)
        {
            m_RootAssets = analysis.Tables.RootAssets;
            m_Assets = analysis.Tables.Assets;
            var n = m_RootAssets.Length;

            EnsureCapacity(ref m_Names, n);
            EnsureCapacity(ref m_Paths, n);
            EnsureCapacity(ref m_FormattedDirectSizes, n);
            EnsureCapacity(ref m_FormattedDirectCounts, n);
            EnsureCapacity(ref m_FormattedTotalSizes, n);
            EnsureCapacity(ref m_FormattedTotalCounts, n);

            for (int i = 0; i < n; i++)
            {
                var root = m_RootAssets[i];
                var path = ResolveAssetPath(root.AssetId);
                m_Paths[i] = path;
                m_Names[i] = string.IsNullOrEmpty(path) ? string.Empty : Path.GetFileName(path);
                m_FormattedDirectSizes[i] = FormatUtility.FormatSize(root.DirectSizeBytes);
                m_FormattedDirectCounts[i] = FormatUtility.FormatCount(root.DirectAssetCount);
                m_FormattedTotalSizes[i] = FormatUtility.FormatSize(root.TotalSizeBytes);
                m_FormattedTotalCounts[i] = FormatUtility.FormatCount(root.TotalAssetCount);
            }

            ResetFilterState();
            ApplyFilters();
        }

        private static void EnsureCapacity(ref string[] array, int min)
        {
            if (array.Length < min)
                array = new string[min];
            else
                // Drop strong refs to strings from a previous larger bind so they can be GC'd.
                Array.Clear(array, min, array.Length - min);
        }

        private string ResolveAssetPath(int assetId)
        {
            // BuildAnalyzer drops root assets whose path can't be resolved, so we expect assetId to be in-bounds.
            Debug.Assert(assetId >= 0 && assetId < m_Assets.Length);
            return m_Assets[assetId].Path ?? string.Empty;
        }

        private void SetSearchText(string value)
        {
            var newText = value ?? string.Empty;
            if (string.Equals(newText, m_SearchText, StringComparison.Ordinal))
                return;
            m_SearchText = newText;
            ApplyFilters();
        }

        private void ResetFilterState()
        {
            m_SearchDebounce?.Pause();
            m_SearchText = string.Empty;
            m_SearchField.SetValueWithoutNotify(string.Empty);
        }

        private void ApplyFilters()
        {
            m_FilteredIndices.Clear();
            if (m_FilteredIndices.Capacity < m_RootAssets.Length)
                m_FilteredIndices.Capacity = m_RootAssets.Length;

            var hasSearch = !string.IsNullOrEmpty(m_SearchText);

            for (int i = 0; i < m_RootAssets.Length; i++)
            {
                if (hasSearch && m_Names[i].IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                m_FilteredIndices.Add(i);
            }

            ApplySort();

            // Clear selection when filters change so a stale highlight doesn't survive.
            m_ListView.selectedIndex = -1;

            m_ListView.RefreshItems();
            UpdateFooter();
            m_EmptyBody.Refresh();
        }

        private void ApplySort()
        {
            // Read the active sort. If none is set fall back to Direct Size descending.
            string columnName = "column-direct-size";
            bool ascending = false;
            using (var enumerator = m_ListView.sortedColumns.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    var sort = enumerator.Current;
                    columnName = sort.columnName;
                    ascending = sort.direction == SortDirection.Ascending;
                }
            }

            SortFiltered(columnName, ascending);
        }

        private void SortFiltered(string columnName, bool ascending)
        {
            Comparison<int> primary = columnName switch
            {
                "column-name"           => CompareByName,
                "column-direct-size"    => CompareByDirectSize,
                "column-direct-assets"  => CompareByDirectCount,
                "column-total-size"     => CompareByTotalSize,
                "column-total-assets"   => CompareByTotalCount,
                _                       => null
            };
            if (primary == null) return;

            // List<T>.Sort is unstable. Without a tie-break, rows with equal primary keys visibly
            // swap on re-sort. Break ties by name ascending (then by row index as a final
            // deterministic key); tie-break direction is independent of the primary direction
            // so the secondary order doesn't flip with the user's clicks.
            m_FilteredIndices.Sort((a, b) =>
            {
                var r = primary(a, b);
                if (!ascending) r = -r;
                if (r != 0) return r;
                var byName = string.Compare(m_Names[a], m_Names[b], StringComparison.OrdinalIgnoreCase);
                return byName != 0 ? byName : a.CompareTo(b);
            });
        }

        private int CompareByName(int a, int b) =>
            string.Compare(m_Names[a], m_Names[b], StringComparison.OrdinalIgnoreCase);

        private int CompareByDirectSize(int a, int b) =>
            m_RootAssets[a].DirectSizeBytes.CompareTo(m_RootAssets[b].DirectSizeBytes);

        private int CompareByDirectCount(int a, int b) =>
            m_RootAssets[a].DirectAssetCount.CompareTo(m_RootAssets[b].DirectAssetCount);

        private int CompareByTotalSize(int a, int b) =>
            m_RootAssets[a].TotalSizeBytes.CompareTo(m_RootAssets[b].TotalSizeBytes);

        private int CompareByTotalCount(int a, int b) =>
            m_RootAssets[a].TotalAssetCount.CompareTo(m_RootAssets[b].TotalAssetCount);

        private void UpdateFooter()
        {
            m_FooterCountLabel.text = $"Showing {m_FilteredIndices.Count}/{m_RootAssets.Length}";
        }

        private static VisualElement MakeNameCell()
        {
            var container = new VisualElement();
            container.AddToClassList("root-asset-table-cell__name-container");
            var icon = new VisualElement();
            icon.AddToClassList("root-asset-table-cell__name-icon");
            container.Add(icon);
            var label = new Label();
            label.AddToClassList("root-asset-table-cell__name-label");
            container.Add(label);
            AssetRowContextMenu.AttachTo(container);
            return container;
        }

        private void BindNameCell(VisualElement ve, int row)
        {
            var idx = m_FilteredIndices[row];
            ((Label)ve.ElementAt(1)).text = m_Names[idx];
            ve.tooltip = m_Paths[idx];
            ve.userData = m_Paths[idx];
        }

        private static VisualElement MakeNumericCell()
        {
            var label = new Label();
            label.AddToClassList("root-asset-table-cell__numeric");
            AssetRowContextMenu.AttachTo(label);
            return label;
        }

        private void BindDirectSizeCell(VisualElement ve, int row)
        {
            var idx = m_FilteredIndices[row];
            ((Label)ve).text = m_FormattedDirectSizes[idx];
            ve.tooltip = m_Paths[idx];
            ve.userData = m_Paths[idx];
        }

        private void BindDirectCountCell(VisualElement ve, int row)
        {
            var idx = m_FilteredIndices[row];
            ((Label)ve).text = m_FormattedDirectCounts[idx];
            ve.tooltip = m_Paths[idx];
            ve.userData = m_Paths[idx];
        }

        private void BindTotalSizeCell(VisualElement ve, int row)
        {
            var idx = m_FilteredIndices[row];
            ((Label)ve).text = m_FormattedTotalSizes[idx];
            ve.tooltip = m_Paths[idx];
            ve.userData = m_Paths[idx];
        }

        private void BindTotalCountCell(VisualElement ve, int row)
        {
            var idx = m_FilteredIndices[row];
            ((Label)ve).text = m_FormattedTotalCounts[idx];
            ve.tooltip = m_Paths[idx];
            ve.userData = m_Paths[idx];
        }
    }
}
