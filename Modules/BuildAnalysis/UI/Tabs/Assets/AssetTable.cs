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
    internal class AssetTable : VisualElement
    {
        private const string k_AllExtensions = "All";
        private const int k_NoExtensionId = -1;
        private const string k_UxmlPath = "BuildAnalysis/UXML/AssetTable.uxml";

        public event Action<BuildAnalysisAsset?> SelectionChanged;

        private readonly DropdownField m_ViewDropdown;
        private readonly ToolbarSearchField m_SearchField;
        private readonly MultiColumnListView m_ListView;
        private readonly Label m_FooterCountLabel;
        private readonly ZebraEmptyBody m_EmptyBody;

        private BuildAnalysisAsset[] m_Assets = Array.Empty<BuildAnalysisAsset>();
        private string[] m_Names = Array.Empty<string>();
        private string[] m_FormattedSizes = Array.Empty<string>();

        // Deduplicated extension pool sorted alphabetically, used directly as dropdown choices (after the "All" prefix).
        private string[] m_ExtensionPool = Array.Empty<string>();
        // Per-asset index into m_ExtensionPool, or k_NoExtensionId for paths with no extension.
        private int[] m_ExtensionIds = Array.Empty<int>();

        private readonly List<int> m_FilteredIndices = new List<int>();
        private IVisualElementScheduledItem m_SearchDebounce;
        // k_NoExtensionId means "All" (no filter); otherwise an index into m_ExtensionPool.
        private int m_ViewFilterId = k_NoExtensionId;
        private string m_SearchText = string.Empty;

        public AssetTable()
        {
            AddToClassList("section");

            var template = EditorGUIUtility.LoadRequired(k_UxmlPath) as VisualTreeAsset;
            template.CloneTree(this);

            m_ViewDropdown = this.Q<DropdownField>("view-dropdown");
            m_SearchField = this.Q<ToolbarSearchField>("search-field");
            m_ListView = this.Q<MultiColumnListView>("asset-list-view");
            m_FooterCountLabel = this.Q<Label>("footer-count-label");

            m_EmptyBody = new ZebraEmptyBody(m_ListView);
            m_ListView.parent.Add(m_EmptyBody);

            m_ViewDropdown.choices = new List<string> { k_AllExtensions };
            m_ViewDropdown.SetValueWithoutNotify(k_AllExtensions);

            m_ListView.itemsSource = m_FilteredIndices;
            m_ListView.columns["column-name"].makeCell = MakeNameCell;
            m_ListView.columns["column-name"].bindCell = BindNameCell;
            m_ListView.columns["column-file-extension"].makeCell = MakeFileExtensionCell;
            m_ListView.columns["column-file-extension"].bindCell = BindFileExtensionCell;
            m_ListView.columns["column-output-size"].makeCell = MakeOutputSizeCell;
            m_ListView.columns["column-output-size"].bindCell = BindOutputSizeCell;

            m_ViewDropdown.RegisterValueChangedCallback(evt => SetViewFilter(evt.newValue));
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
            BuildAnalysisAsset? asset = null;
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item is int assetIdx and >= 0 && assetIdx < m_Assets.Length)
                    {
                        asset = m_Assets[assetIdx];
                        break;
                    }
                }
            }
            SelectionChanged?.Invoke(asset);
        }

        public void Bind(BuildAnalysis analysis)
        {
            m_Assets = analysis.Tables.Assets;
            var n = m_Assets.Length;

            // Reuse parallel arrays across binds when possible; only grow when capacity is exceeded.
            EnsureCapacity(ref m_Names, n);
            EnsureCapacity(ref m_FormattedSizes, n);
            if (m_ExtensionIds.Length < n)
                m_ExtensionIds = new int[n];

            BuildExtensionPoolAndNames(n);
            for (int i = 0; i < n; i++)
                m_FormattedSizes[i] = FormatUtility.FormatSize(m_Assets[i].OutputSizeBytes);
            BuildViewDropdownChoices();
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

        private void BuildExtensionPoolAndNames(int n)
        {
            var insertionMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var insertionPool = new List<string>();

            for (int i = 0; i < n; i++)
            {
                var path = m_Assets[i].Path ?? string.Empty;
                m_Names[i] = Path.GetFileNameWithoutExtension(path);
                var ext = Path.GetExtension(path);

                if (string.IsNullOrEmpty(ext))
                {
                    m_ExtensionIds[i] = k_NoExtensionId;
                    continue;
                }

                // Lookup is case-insensitive (insertionMap uses OrdinalIgnoreCase), so we only need
                // to allocate the lowercased form on first insertion.
                if (!insertionMap.TryGetValue(ext, out var id))
                {
                    id = insertionPool.Count;
                    var lowerExt = ext.ToLowerInvariant();
                    insertionPool.Add(lowerExt);
                    insertionMap[lowerExt] = id;
                }
                m_ExtensionIds[i] = id;
            }

            // Sort the pool alphabetically and build a remap from insertion-id to sorted-id.
            var sortedPool = new List<string>(insertionPool);
            sortedPool.Sort(StringComparer.Ordinal);
            var remap = new int[insertionPool.Count];
            for (int sortedId = 0; sortedId < sortedPool.Count; sortedId++)
                remap[insertionMap[sortedPool[sortedId]]] = sortedId;

            for (int i = 0; i < n; i++)
            {
                if (m_ExtensionIds[i] != k_NoExtensionId)
                    m_ExtensionIds[i] = remap[m_ExtensionIds[i]];
            }

            m_ExtensionPool = sortedPool.ToArray();
        }

        private void BuildViewDropdownChoices()
        {
            var choices = new List<string>(m_ExtensionPool.Length + 1) { k_AllExtensions };
            choices.AddRange(m_ExtensionPool);
            m_ViewDropdown.choices = choices;
            m_ViewDropdown.SetValueWithoutNotify(k_AllExtensions);
        }

        private void SetViewFilter(string value)
        {
            var newId = string.IsNullOrEmpty(value) || string.Equals(value, k_AllExtensions, StringComparison.Ordinal)
                ? k_NoExtensionId
                : Array.IndexOf(m_ExtensionPool, value.ToLowerInvariant());

            if (newId == m_ViewFilterId)
                return;

            m_ViewFilterId = newId;
            m_ViewDropdown.SetValueWithoutNotify(newId == k_NoExtensionId ? k_AllExtensions : m_ExtensionPool[newId]);
            ApplyFilters();
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
            m_ViewFilterId = k_NoExtensionId;
            m_SearchText = string.Empty;
            m_SearchField.SetValueWithoutNotify(string.Empty);
            m_ViewDropdown.SetValueWithoutNotify(k_AllExtensions);
        }

        private void ApplyFilters()
        {
            m_FilteredIndices.Clear();
            if (m_FilteredIndices.Capacity < m_Assets.Length)
                m_FilteredIndices.Capacity = m_Assets.Length;

            var hasViewFilter = m_ViewFilterId != k_NoExtensionId;
            var hasSearch = !string.IsNullOrEmpty(m_SearchText);

            for (int i = 0; i < m_Assets.Length; i++)
            {
                if (hasViewFilter && m_ExtensionIds[i] != m_ViewFilterId)
                    continue;
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
            // Read the active sort. If none is set fall back to the output size descending.
            string columnName = "column-output-size";
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
            Comparison<int> cmp = columnName switch
            {
                "column-name"        => CompareByName,
                "column-file-extension"   => CompareByExtension,
                "column-output-size" => CompareByOutputSize,
                _                    => null
            };
            if (cmp == null) return;
            m_FilteredIndices.Sort(ascending ? cmp : (a, b) => -cmp(a, b));
        }

        private int CompareByName(int a, int b) =>
            string.Compare(m_Names[a], m_Names[b], StringComparison.OrdinalIgnoreCase);

        private int CompareByExtension(int a, int b)
        {
            var ea = m_ExtensionIds[a];
            var eb = m_ExtensionIds[b];
            if (ea == eb) return 0;
            // Empty extensions sort before any concrete extension.
            if (ea == k_NoExtensionId) return -1;
            if (eb == k_NoExtensionId) return 1;
            // Pool is alphabetically sorted, so id order == display order.
            return ea.CompareTo(eb);
        }

        private int CompareByOutputSize(int a, int b) =>
            m_Assets[a].OutputSizeBytes.CompareTo(m_Assets[b].OutputSizeBytes);

        private void UpdateFooter()
        {
            m_FooterCountLabel.text = $"Showing {m_FilteredIndices.Count}/{m_Assets.Length}";
        }

        private static VisualElement MakeNameCell()
        {
            var container = new VisualElement();
            container.AddToClassList("asset-table-cell__name-container");
            var icon = new Image { scaleMode = ScaleMode.ScaleToFit };
            icon.AddToClassList("asset-table-cell__name-icon");
            container.Add(icon);
            var label = new Label();
            label.AddToClassList("asset-table-cell__name-label");
            container.Add(label);
            AssetRowContextMenu.AttachTo(container);
            return container;
        }

        private void BindNameCell(VisualElement ve, int row)
        {
            var idx = m_FilteredIndices[row];
            var path = m_Assets[idx].Path;
            ((Image)ve.ElementAt(0)).image = IconUtility.GetAssetIcon(path);
            ((Label)ve.ElementAt(1)).text = m_Names[idx];
            ve.tooltip = path;
            ve.userData = path;
        }

        private static VisualElement MakeFileExtensionCell()
        {
            var label = new Label();
            label.AddToClassList("asset-table-cell__file-extension");
            AssetRowContextMenu.AttachTo(label);
            return label;
        }

        private void BindFileExtensionCell(VisualElement ve, int row)
        {
            var idx = m_FilteredIndices[row];
            var extId = m_ExtensionIds[idx];
            ((Label)ve).text = extId == k_NoExtensionId ? string.Empty : m_ExtensionPool[extId];
            var path = m_Assets[idx].Path;
            ve.tooltip = path;
            ve.userData = path;
        }

        private static VisualElement MakeOutputSizeCell()
        {
            var label = new Label();
            label.AddToClassList("asset-table-cell__output-size");
            AssetRowContextMenu.AttachTo(label);
            return label;
        }

        private void BindOutputSizeCell(VisualElement ve, int row)
        {
            var idx = m_FilteredIndices[row];
            ((Label)ve).text = m_FormattedSizes[idx];
            var path = m_Assets[idx].Path;
            ve.tooltip = path;
            ve.userData = path;
        }
    }
}
