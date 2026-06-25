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
    /// <summary>
    /// Right-docked side panel that shows properties of the asset currently selected in the
    /// Asset Table or Root Asset Table.
    /// </summary>
    internal sealed class AssetInspector : VisualElement
    {
        private const string k_UssPath = "BuildAnalysis/StyleSheets/AssetInspector.uss";

        internal enum Mode { Empty, Asset, Root }

        private readonly InspectorHeader m_Header = new InspectorHeader();
        private readonly AssetInspectorBody m_AssetBody = new AssetInspectorBody();
        private readonly RootAssetInspectorBody m_RootBody = new RootAssetInspectorBody();

        internal Mode CurrentMode { get; private set; }

        public AssetInspector()
        {
            AddToClassList("inspector");

            styleSheets.Add(EditorGUIUtility.LoadRequired(k_UssPath) as StyleSheet);

            Add(m_Header);
            Add(m_AssetBody);
            Add(m_RootBody);

            ShowEmpty();
        }

        public void ShowAsset(BuildAnalysisAsset asset, BuildAnalysisImporterType? importer)
        {
            var path = asset.Path ?? string.Empty;
            m_Header.Bind(IconUtility.GetAssetIcon(path), Path.GetFileNameWithoutExtension(path), path);
            m_AssetBody.Bind(asset, importer);
            SetMode(Mode.Asset);
        }

        public void ShowRootAsset(BuildAnalysisRootAsset root, BuildAnalysisAsset rootAsset,
            BuildAnalysisAsset[] assets)
        {
            var path = rootAsset.Path ?? string.Empty;
            // Root names keep the extension to match the Root Asset Table's Name column.
            var title = string.IsNullOrEmpty(path) ? string.Empty : Path.GetFileName(path);
            m_Header.Bind(IconUtility.GetAssetIcon(path), title, path);
            m_RootBody.Bind(root, rootAsset, assets);
            SetMode(Mode.Root);
        }

        public void ShowEmpty() => SetMode(Mode.Empty);

        private void SetMode(Mode mode)
        {
            CurrentMode = mode;
            m_Header.style.display = mode == Mode.Empty ? DisplayStyle.None : DisplayStyle.Flex;
            m_AssetBody.style.display = mode == Mode.Asset ? DisplayStyle.Flex : DisplayStyle.None;
            m_RootBody.style.display = mode == Mode.Root ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    /// <summary>Asset icon + filename + a right-aligned Select button (= Show in Project).</summary>
    internal sealed class InspectorHeader : VisualElement
    {
        private readonly Image m_Icon;
        private readonly Label m_Name;
        private string m_Path;

        public InspectorHeader()
        {
            AddToClassList("inspector__header");

            m_Icon = new Image { scaleMode = ScaleMode.ScaleToFit };
            m_Icon.AddToClassList("inspector__header-icon");
            Add(m_Icon);

            m_Name = new Label();
            m_Name.AddToClassList("inspector__header-name");
            Add(m_Name);

            var select = new Button(OnSelectClicked) { text = "Select" };
            select.AddToClassList("inspector__select-button");
            Add(select);
        }

        internal string Title => m_Name.text;

        public void Bind(Texture icon, string title, string assetPath)
        {
            m_Icon.image = icon;
            m_Name.text = title;
            m_Path = assetPath;
        }

        private void OnSelectClicked() => AssetActions.ShowInProject(m_Path);
    }

    /// <summary>One label/value row: label left, value right-aligned, selectable, ellipsis on overflow.</summary>
    internal sealed class InspectorField : VisualElement
    {
        private readonly Label m_Value;

        public InspectorField(string label)
        {
            AddToClassList("inspector__field");

            var labelEl = new Label(label);
            labelEl.AddToClassList("inspector__field-label");
            Add(labelEl);

            m_Value = new Label();
            m_Value.AddToClassList("inspector__field-value");
            m_Value.selection.isSelectable = true;
            Add(m_Value);
        }

        internal string Value { get => m_Value.text; set => m_Value.text = value; }
        internal string ValueTooltip { get => m_Value.tooltip; set => m_Value.tooltip = value; }
        internal bool ValueSelectable => m_Value.selection.isSelectable;
    }

    /// <summary>Body for an Asset Table selection: the asset's own properties.</summary>
    internal sealed class AssetInspectorBody : VisualElement
    {
        internal readonly InspectorField OutputSize = new InspectorField("Output size");
        internal readonly InspectorField ImporterType = new InspectorField("Importer type");
        internal readonly InspectorField ObjectsCount = new InspectorField("Objects count");
        internal readonly InspectorField ResourcesFiles = new InspectorField("Resources files");
        internal readonly InspectorField AssetPath = new InspectorField("Asset path");

        public AssetInspectorBody()
        {
            AddToClassList("inspector__body");
            Add(OutputSize);
            Add(ImporterType);
            Add(ObjectsCount);
            Add(ResourcesFiles);
            Add(AssetPath);
        }

        public void Bind(BuildAnalysisAsset asset, BuildAnalysisImporterType? importer)
        {
            var path = asset.Path ?? string.Empty;
            OutputSize.Value = FormatUtility.FormatSize(asset.OutputSizeBytes);
            ImporterType.Value = importer?.Name ?? string.Empty;
            ObjectsCount.Value = asset.ObjectCount.ToString();
            ResourcesFiles.Value = asset.ResourceCount.ToString();
            AssetPath.Value = path;
            AssetPath.ValueTooltip = path;
        }
    }

    /// <summary>Body for a Root Asset Table selection: reachability stats + a "References" list.</summary>
    internal sealed class RootAssetInspectorBody : VisualElement
    {
        internal readonly InspectorField DirectSize = new InspectorField("Direct size");
        internal readonly InspectorField DirectAssets = new InspectorField("Direct assets");
        internal readonly InspectorField TotalSize = new InspectorField("Total size");
        internal readonly InspectorField TotalAssets = new InspectorField("Total assets");
        internal readonly InspectorField OutputSize = new InspectorField("Output size");
        internal readonly InspectorField AssetPath = new InspectorField("Asset path");
        internal readonly ReferencesView References = new ReferencesView();

        public RootAssetInspectorBody()
        {
            AddToClassList("inspector__body");
            Add(DirectSize);
            Add(DirectAssets);
            Add(TotalSize);
            Add(TotalAssets);
            Add(OutputSize);
            Add(AssetPath);
            Add(References);
        }

        public void Bind(BuildAnalysisRootAsset root, BuildAnalysisAsset rootAsset,
            BuildAnalysisAsset[] assets)
        {
            var path = rootAsset.Path ?? string.Empty;
            DirectSize.Value = FormatUtility.FormatSize(root.DirectSizeBytes);
            DirectAssets.Value = FormatUtility.FormatCount(root.DirectAssetCount);
            TotalSize.Value = FormatUtility.FormatSize(root.TotalSizeBytes);
            TotalAssets.Value = FormatUtility.FormatCount(root.TotalAssetCount);
            OutputSize.Value = FormatUtility.FormatSize(rootAsset.OutputSizeBytes);
            AssetPath.Value = path;
            AssetPath.ValueTooltip = path;
            References.Bind(root.ReferencedAssetIds, assets);
        }
    }

    /// <summary>
    /// Read-only "References" list of all assets a root pulls in (its full reachable set),
    /// sorted by size descending.
    /// Rows are precomputed in <see cref="Bind"/> so <c>bindItem</c> stays allocation-free.
    /// </summary>
    internal sealed class ReferencesView : VisualElement
    {
        private const string k_AllExtensions = "All";
        private const int k_NoExtensionId = -1;

        private struct Row
        {
            public string Name;
            public string Path;
            public string Size;
            public ulong SizeBytes;
            public Texture Icon;
            public int ExtensionId;
        }

        private readonly Label m_Header;
        private readonly DropdownField m_ViewDropdown;
        private readonly ToolbarSearchField m_SearchField;
        private readonly ListView m_List;
        private readonly ZebraEmptyBody m_EmptyBody;
        private readonly Label m_FooterCount;

        private readonly List<Row> m_Rows = new List<Row>();
        // Indices into m_Rows (kept in m_Rows' size-sorted order) that pass the active filters.
        private readonly List<int> m_FilteredIndices = new List<int>();
        private string[] m_ExtensionPool = Array.Empty<string>();

        private IVisualElementScheduledItem m_SearchDebounce;
        private int m_ViewFilterId = k_NoExtensionId;
        private string m_SearchText = string.Empty;

        public ReferencesView()
        {
            AddToClassList("inspector__references");

            m_Header = new Label("References");
            m_Header.AddToClassList("inspector__references-header");
            Add(m_Header);

            var toolbar = new VisualElement();
            toolbar.AddToClassList("data-list__toolbar");
            toolbar.AddToClassList("inspector__references-toolbar");

            m_ViewDropdown = new DropdownField { label = "View:", tooltip = "Filter by file extension", name = "view-dropdown" };
            m_ViewDropdown.AddToClassList("data-list__toolbar-control");
            m_ViewDropdown.AddToClassList("data-list__toolbar-popup");
            m_ViewDropdown.choices = new List<string> { k_AllExtensions };
            m_ViewDropdown.SetValueWithoutNotify(k_AllExtensions);
            toolbar.Add(m_ViewDropdown);

            var spacer = new VisualElement();
            spacer.AddToClassList("data-list__toolbar-spacer");
            toolbar.Add(spacer);

            m_SearchField = new ToolbarSearchField { name = "search-field", tooltip = "Search asset name" };
            m_SearchField.AddToClassList("data-list__toolbar-search");
            toolbar.Add(m_SearchField);
            Add(toolbar);

            m_List = new ListView
            {
                itemsSource = m_FilteredIndices,
                fixedItemHeight = 20,
                selectionType = SelectionType.None,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                makeItem = MakeItem,
                bindItem = BindItem,
            };
            m_List.AddToClassList("inspector__references-list");
            Add(m_List);

            var footer = new VisualElement();
            footer.AddToClassList("data-list__footer");
            footer.AddToClassList("inspector__references-footer");
            m_FooterCount = new Label { name = "footer-count-label" };
            m_FooterCount.AddToClassList("data-list__footer-count");
            footer.Add(m_FooterCount);
            Add(footer);

            // Continue the zebra pattern past the last row into the empty body, matching the tables.
            m_EmptyBody = new ZebraEmptyBody(m_List);
            Add(m_EmptyBody);

            m_ViewDropdown.RegisterValueChangedCallback(evt => SetViewFilter(evt.newValue));
            m_SearchField.RegisterValueChangedCallback(evt =>
            {
                var newText = evt.newValue ?? string.Empty;
                m_SearchDebounce?.Pause();
                m_SearchDebounce = schedule.Execute(() => SetSearchText(newText)).StartingIn(200);
            });
        }

        // Visible (post-filter) row count.
        internal int Count => m_FilteredIndices.Count;
        internal string HeaderText => m_Header.text;

        public void Bind(int[] referencedAssetIds, BuildAnalysisAsset[] assets)
        {
            BuildRows(referencedAssetIds, assets);
            BuildViewDropdownChoices();
            ResetFilterState();
            ApplyFilters();
        }

        private void BuildRows(int[] referencedAssetIds, BuildAnalysisAsset[] assets)
        {
            m_Rows.Clear();

            // Build a deduplicated extension pool while creating rows (insertion order first), then
            // sort the pool alphabetically and remap each row's id to its sorted position.
            var insertionMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var insertionPool = new List<string>();

            if (referencedAssetIds != null && assets != null)
            {
                foreach (var id in referencedAssetIds)
                {
                    if (id < 0 || id >= assets.Length)
                        continue;
                    var a = assets[id];
                    var p = a.Path ?? string.Empty;

                    var ext = Path.GetExtension(p);
                    int extId;
                    if (string.IsNullOrEmpty(ext))
                    {
                        extId = k_NoExtensionId;
                    }
                    else if (!insertionMap.TryGetValue(ext, out extId))
                    {
                        extId = insertionPool.Count;
                        var lowerExt = ext.ToLowerInvariant();
                        insertionPool.Add(lowerExt);
                        insertionMap[lowerExt] = extId;
                    }

                    m_Rows.Add(new Row
                    {
                        Name = string.IsNullOrEmpty(p) ? string.Empty : Path.GetFileName(p),
                        Path = p,
                        Size = FormatUtility.FormatSize(a.OutputSizeBytes),
                        SizeBytes = a.OutputSizeBytes,
                        Icon = IconUtility.GetAssetIcon(p),
                        ExtensionId = extId,
                    });
                }
            }

            var sortedPool = new List<string>(insertionPool);
            sortedPool.Sort(StringComparer.Ordinal);
            if (insertionPool.Count > 0)
            {
                var remap = new int[insertionPool.Count];
                for (int sortedId = 0; sortedId < sortedPool.Count; sortedId++)
                    remap[insertionMap[sortedPool[sortedId]]] = sortedId;
                for (int i = 0; i < m_Rows.Count; i++)
                {
                    var row = m_Rows[i];
                    if (row.ExtensionId != k_NoExtensionId)
                    {
                        row.ExtensionId = remap[row.ExtensionId];
                        m_Rows[i] = row;
                    }
                }
            }
            m_ExtensionPool = sortedPool.ToArray();

            // Largest contributors first — the reason a user opens this list. Name then path break
            // ties so equal-size rows keep a deterministic order (List.Sort is unstable).
            m_Rows.Sort((x, y) =>
            {
                var bySize = y.SizeBytes.CompareTo(x.SizeBytes);
                if (bySize != 0)
                    return bySize;
                var byName = string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
                return byName != 0 ? byName : string.Compare(x.Path, y.Path, StringComparison.Ordinal);
            });
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
            if (m_FilteredIndices.Capacity < m_Rows.Count)
                m_FilteredIndices.Capacity = m_Rows.Count;

            var hasViewFilter = m_ViewFilterId != k_NoExtensionId;
            var hasSearch = !string.IsNullOrEmpty(m_SearchText);

            for (int i = 0; i < m_Rows.Count; i++)
            {
                var row = m_Rows[i];
                if (hasViewFilter && row.ExtensionId != m_ViewFilterId)
                    continue;
                if (hasSearch && row.Name.IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                m_FilteredIndices.Add(i);
            }

            m_List.RefreshItems();
            m_FooterCount.text = $"Showing {m_FilteredIndices.Count}/{m_Rows.Count}";
            m_EmptyBody.Refresh();
        }

        private static VisualElement MakeItem()
        {
            var row = new VisualElement();
            row.AddToClassList("inspector__references-item");
            var icon = new Image { scaleMode = ScaleMode.ScaleToFit };
            icon.AddToClassList("inspector__references-item-icon");
            row.Add(icon);
            var name = new Label();
            name.AddToClassList("inspector__references-item-name");
            row.Add(name);
            var size = new Label();
            size.AddToClassList("inspector__references-item-size");
            row.Add(size);
            return row;
        }

        private void BindItem(VisualElement ve, int index)
        {
            var row = m_Rows[m_FilteredIndices[index]];
            ((Image)ve.ElementAt(0)).image = row.Icon;
            var name = (Label)ve.ElementAt(1);
            name.text = row.Name;
            name.tooltip = row.Path;
            ((Label)ve.ElementAt(2)).text = row.Size;
        }
    }
}
