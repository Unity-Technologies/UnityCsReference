// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    internal class AssetsTabView : IBuildAnalysisTabView
    {
        private const string k_UxmlPath = "BuildAnalysis/UXML/AssetsTab.uxml";
        private const int k_InspectorPaneIndex = 1;

        public event Action InspectorOpenRequested;

        private readonly VisualElement m_Root = new VisualElement();
        private VisualElement m_NoSelection;
        private VisualElement m_Body;
        private TwoPaneSplitView m_InspectorSplit;
        private VisualElement m_InspectorHost;
        private BuildHeaderController m_Header;
        private AssetTable m_AssetTable;
        private RootAssetTable m_RootAssetTable;
        private AssetInspector m_AssetInspector;
        private Label m_ScenesValue;
        private Label m_AssetsValue;
        private VisualElement m_RootAssetsCard;
        private Label m_RootAssetsValue;

        private bool m_HasLaidOut;
        private bool m_InspectorOpen;
        private bool m_SuppressSelectionClear;

        private BuildAnalysisImporterType[] m_CachedImporterTypes = Array.Empty<BuildAnalysisImporterType>();
        private BuildAnalysisAsset[] m_CachedAssets = Array.Empty<BuildAnalysisAsset>();

        public VisualElement Root => m_Root;

        public void Initialize()
        {
            Debug.Assert(m_Root.childCount == 0, "AssetsTabView.Initialize() should only be called once.");
            m_Root.style.flexGrow = 1;

            var template = EditorGUIUtility.LoadRequired(k_UxmlPath) as VisualTreeAsset;
            template.CloneTree(m_Root);

            m_NoSelection = m_Root.Q<VisualElement>("no-selection");
            m_Body = m_Root.Q<VisualElement>("assets-body");
            m_InspectorSplit = m_Root.Q<TwoPaneSplitView>("assets-inspector-split");
            m_InspectorHost = m_Root.Q<VisualElement>("asset-inspector-host");
            m_Header = new BuildHeaderController(m_Root.Q<VisualElement>("build-header"));
            m_ScenesValue = m_Root.Q<VisualElement>("stat-card-scenes").Q<Label>("value");
            m_AssetsValue = m_Root.Q<VisualElement>("stat-card-assets").Q<Label>("value");
            m_RootAssetsCard = m_Root.Q<VisualElement>("stat-card-root-assets");
            m_RootAssetsValue = m_RootAssetsCard.Q<Label>("value");

            var sections = m_Root.Q<VisualElement>("assets-sections");
            m_RootAssetTable = new RootAssetTable();
            sections.Add(m_RootAssetTable);
            m_AssetTable = new AssetTable();
            sections.Add(m_AssetTable);

            m_AssetInspector = new AssetInspector();
            m_InspectorHost.Add(m_AssetInspector);

            m_AssetTable.SelectionChanged += OnAssetSelectionChanged;
            m_RootAssetTable.SelectionChanged += OnRootAssetSelectionChanged;

            // Defer first CollapseChild call until after first layout — TwoPaneSplitView
            // throws if collapsed before its initial geometry is computed.
            m_InspectorSplit.RegisterCallback<GeometryChangedEvent>(OnFirstGeometry);

            SetSelection(null, null);
        }

        private void OnAssetSelectionChanged(BuildAnalysisAsset? asset)
        {
            if (asset.HasValue)
            {
                m_SuppressSelectionClear = true;
                m_RootAssetTable.ClearSelection();
                m_SuppressSelectionClear = false;

                m_AssetInspector.ShowAsset(asset.Value, ResolveImporterType(asset.Value.ImporterTypeId));
                InspectorOpenRequested?.Invoke();
                return;
            }

            if (m_SuppressSelectionClear || m_AssetInspector.CurrentMode != AssetInspector.Mode.Asset)
                return;
            m_AssetInspector.ShowEmpty();
        }

        private void OnRootAssetSelectionChanged(BuildAnalysisRootAsset? root)
        {
            if (root.HasValue)
            {
                m_SuppressSelectionClear = true;
                m_AssetTable.ClearSelection();
                m_SuppressSelectionClear = false;

                var r = root.Value;
                var rootAsset = ResolveAsset(r.AssetId);

                m_AssetInspector.ShowRootAsset(r, rootAsset ?? default, m_CachedAssets);
                InspectorOpenRequested?.Invoke();
                return;
            }

            if (m_SuppressSelectionClear || m_AssetInspector.CurrentMode != AssetInspector.Mode.Root)
                return;
            m_AssetInspector.ShowEmpty();
        }

        private void ResetInspector() => m_AssetInspector.ShowEmpty();

        private BuildAnalysisImporterType? ResolveImporterType(int id)
        {
            if (id < 0 || id >= m_CachedImporterTypes.Length)
                return null;
            return m_CachedImporterTypes[id];
        }

        private BuildAnalysisAsset? ResolveAsset(int assetId)
        {
            if (assetId < 0 || assetId >= m_CachedAssets.Length)
                return null;
            return m_CachedAssets[assetId];
        }

        public void SetSelection(BuildEntry selection, BuildAnalysis analysis)
        {
            var hasSelection = selection != null && analysis != null;
            m_NoSelection.style.display = hasSelection ? DisplayStyle.None : DisplayStyle.Flex;
            m_Body.style.display = hasSelection ? DisplayStyle.Flex : DisplayStyle.None;

            ResetInspector();

            if (!hasSelection)
            {
                m_CachedImporterTypes = Array.Empty<BuildAnalysisImporterType>();
                m_CachedAssets = Array.Empty<BuildAnalysisAsset>();
                m_RootAssetsCard.style.display = DisplayStyle.None;
                m_RootAssetTable.style.display = DisplayStyle.None;
                return;
            }

            m_CachedImporterTypes = analysis.Tables.ImporterTypes;
            m_CachedAssets = analysis.Tables.Assets;

            m_Header.Bind(selection, analysis);
            var counts = analysis.Computed.Counts;
            m_ScenesValue.text = counts.SceneCount.ToString();
            m_AssetsValue.text = counts.AssetCount.ToString();

            var isContentDirectory = selection.BuildType == BuildType.ContentDirectory;
            m_RootAssetsCard.style.display = isContentDirectory ? DisplayStyle.Flex : DisplayStyle.None;
            m_RootAssetTable.style.display = isContentDirectory ? DisplayStyle.Flex : DisplayStyle.None;
            if (isContentDirectory)
            {
                m_RootAssetsValue.text = counts.RootAssetCount.ToString();
                m_RootAssetTable.Bind(analysis);
            }

            m_AssetTable.Bind(analysis);
        }

        public void OnTabVisibilityChanged(bool isVisible)
        {
            // Selection is only valid for the exact table state in which it was made.
            // Clearing on tab return forces the user to re-pick a row
            if (isVisible)
            {
                m_AssetTable.ClearSelection();
                m_RootAssetTable.ClearSelection();
                ResetInspector();
            }
        }

        public void OnInspectorVisibilityChanged(bool isOpen)
        {
            m_InspectorOpen = isOpen;
            if (!m_HasLaidOut)
                return;

            ApplyInspectorVisibility();
        }

        private void OnFirstGeometry(GeometryChangedEvent evt)
        {
            m_InspectorSplit.UnregisterCallback<GeometryChangedEvent>(OnFirstGeometry);
            m_HasLaidOut = true;
            ApplyInspectorVisibility();
        }

        private void ApplyInspectorVisibility()
        {
            if (m_InspectorOpen)
                m_InspectorSplit.UnCollapse();
            else
                m_InspectorSplit.CollapseChild(k_InspectorPaneIndex);
        }
    }
}
