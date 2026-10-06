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
        private VisualElement m_ContentMain;
        private HelpBox m_AssetSourceBanner;
        private HelpBox m_AssetsEmptyState;

        private bool m_HasLaidOut;
        private bool m_InspectorOpen;
        private bool m_SuppressSelectionClear;

        private BuildAnalysisImporterType[] m_CachedImporterTypes = Array.Empty<BuildAnalysisImporterType>();
        private BuildAnalysisAsset[] m_CachedAssets = Array.Empty<BuildAnalysisAsset>();

        private readonly IDependencyGraphService m_GraphService;

        // The dependency graph is per build, not per asset: Apply prefetches it once and asset
        // clicks bind synchronously from m_GraphResult (null = still loading, section shows the
        // spinner). m_GraphSeq is a miniature of the window's SelectionGate — Apply bumps it, and
        // a load continuation that comes back under an older seq drops itself.
        private int m_GraphSeq;
        private DependencyGraphResult m_GraphResult;
        private int m_InspectedAssetId = -1;
        private bool m_IsContentDirectory;
        private GUID m_AppliedBuildGuid;

        public AssetsTabView(IDependencyGraphService graphService)
        {
            m_GraphService = graphService ?? throw new ArgumentNullException(nameof(graphService));
        }

        public VisualElement Root => m_Root;

        public void Initialize()
        {
            Debug.Assert(m_Root.childCount == 0, "AssetsTabView.Initialize() should only be called once.");
            m_Root.style.flexGrow = 1;

            var template = EditorGUIUtility.LoadRequired(k_UxmlPath) as VisualTreeAsset;
            template.CloneTree(m_Root);

            m_NoSelection = m_Root.Q<VisualElement>("no-selection");
            m_Body = m_Root.Q<VisualElement>("assets-body");
            KeyboardNavigation.ScrollFocusedIntoView(m_Root.Q<ScrollView>("assets-content"));
            m_InspectorSplit = m_Root.Q<TwoPaneSplitView>("assets-inspector-split");
            m_InspectorHost = m_Root.Q<VisualElement>("asset-inspector-host");
            m_Header = new BuildHeaderController(m_Root.Q<VisualElement>("build-header"));
            m_ScenesValue = m_Root.Q<VisualElement>("stat-card-scenes").Q<Label>("value");
            m_AssetsValue = m_Root.Q<VisualElement>("stat-card-assets").Q<Label>("value");
            m_RootAssetsCard = m_Root.Q<VisualElement>("stat-card-root-assets");
            m_RootAssetsValue = m_RootAssetsCard.Q<Label>("value");
            m_ContentMain = m_Root.Q<VisualElement>("assets-content-main");
            m_AssetSourceBanner = m_Root.Q<HelpBox>("asset-source-banner");
            m_AssetsEmptyState = m_Root.Q<HelpBox>("assets-empty-state");

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

            Apply(null);
        }

        private void OnAssetSelectionChanged(BuildAnalysisAsset? asset)
        {
            if (asset.HasValue)
            {
                m_SuppressSelectionClear = true;
                m_RootAssetTable.ClearSelection();
                m_SuppressSelectionClear = false;

                m_AssetInspector.ShowAsset(asset.Value, ResolveImporterType(asset.Value.ImporterTypeId),
                    showReferences: m_IsContentDirectory);
                if (m_IsContentDirectory)
                {
                    m_InspectedAssetId = asset.Value.Id;
                    BindReferenceSection(m_InspectedAssetId);
                }
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

        private void ResetInspector()
        {
            m_InspectedAssetId = -1;
            m_AssetInspector.ShowEmpty();
        }

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

        public void Apply(BuildAnalysisView view)
        {
            // Whatever graph load is in flight is for the previous selection now.
            m_GraphSeq++;
            m_GraphResult = null;

            var hasSelection = view?.Entry != null && view.Analysis != null;
            m_NoSelection.style.display = hasSelection ? DisplayStyle.None : DisplayStyle.Flex;
            m_Body.style.display = hasSelection ? DisplayStyle.Flex : DisplayStyle.None;

            if (!hasSelection)
            {
                ResetInspector();

                m_CachedImporterTypes = Array.Empty<BuildAnalysisImporterType>();
                m_CachedAssets = Array.Empty<BuildAnalysisAsset>();
                m_IsContentDirectory = false;
                m_AppliedBuildGuid = default;
                m_RootAssetsCard.style.display = DisplayStyle.None;
                m_RootAssetTable.style.display = DisplayStyle.None;
                m_AssetSourceBanner.style.display = DisplayStyle.None;
                m_AssetsEmptyState.style.display = DisplayStyle.None;
                return;
            }

            // The active reference tab is interactive state, kept across re-applies of the same
            // build (a regenerate) and reset only on a genuinely new selection.
            if (view.Entry.BuildSessionGUID != m_AppliedBuildGuid)
                m_AssetInspector.References.ResetActiveTab();
            m_AppliedBuildGuid = view.Entry.BuildSessionGUID;

            // Set before the tables rebind so any selection event they raise sees the new value.
            m_IsContentDirectory = view.Entry.BuildType == BuildType.ContentDirectory;

            // A new selection discards the inspector's contents: it was showing an asset from the old build.
            ResetInspector();
            BindRootAssets(view.Entry, view.Analysis);
            BindAssets(view.Entry, view.Analysis);

            // Prefetch the dependency graph so asset clicks usually bind synchronously. Skipped
            // for build types that never record one (the section is hidden for those anyway).
            if (m_IsContentDirectory)
                LoadGraphAsync(view);
        }

        // Awaiting an already-completed task continues synchronously, so a cached graph is bound
        // before Apply returns and no loading state is ever shown for it.
        private async void LoadGraphAsync(BuildAnalysisView view)
        {
            var seq = m_GraphSeq;
            var result = await m_GraphService.LoadAsync(view.Entry, view.Analysis);
            if (seq != m_GraphSeq || m_Root.panel == null)
                return;

            m_GraphResult = result;
            if (m_AssetInspector.CurrentMode == AssetInspector.Mode.Asset && m_InspectedAssetId >= 0)
                BindReferenceSection(m_InspectedAssetId);
        }

        private void BindReferenceSection(int assetId)
        {
            var section = m_AssetInspector.References;
            if (m_GraphResult == null)
                section.SetLoading();
            else if (m_GraphResult.Status == DependencyGraphStatus.Loaded)
                section.Bind(m_GraphResult.References, assetId, m_CachedAssets);
            else
                section.SetUnavailable(m_GraphResult.Status);
        }

        private void BindRootAssets(BuildEntry selection, BuildAnalysis analysis)
        {
            var isContentDirectory = selection.BuildType == BuildType.ContentDirectory;
            m_RootAssetsCard.style.display = isContentDirectory ? DisplayStyle.Flex : DisplayStyle.None;
            m_RootAssetTable.style.display = isContentDirectory ? DisplayStyle.Flex : DisplayStyle.None;
            if (!isContentDirectory)
                return;

            m_RootAssetsValue.text = analysis.Computed.Counts.RootAssetCount.ToString();
            m_RootAssetTable.Bind(analysis);
        }

        private void BindAssets(BuildEntry selection, BuildAnalysis analysis)
        {
            m_CachedImporterTypes = analysis.Tables.ImporterTypes;
            m_CachedAssets = analysis.Tables.Assets;

            m_Header.Bind(selection);
            var counts = analysis.Computed.Counts;
            m_ScenesValue.text = counts.SceneCount.ToString();
            m_AssetsValue.text = counts.AssetCount.ToString();

            m_AssetTable.Bind(analysis);

            // Assets-less builds (scripts-only / incremental-clean) borrow the table from an earlier build.
            // A build whose recorded content source couldn't be resolved hides the table and shows the empty state instead.
            // Reflect that state instead of a bare empty grid.
            var unavailable = analysis.AssetSource.SourceUnavailable;
            m_ContentMain.style.display = unavailable ? DisplayStyle.None : DisplayStyle.Flex;

            m_AssetsEmptyState.style.display = unavailable ? DisplayStyle.Flex : DisplayStyle.None;
            if (unavailable)
                m_AssetsEmptyState.text = "No asset data was found for this build. " +
                                          "Scripts-only and incremental builds show assets from an earlier complete build, but none was found. Run a complete build to record asset data.";

            var borrowed = analysis.AssetSource.IsBorrowed;
            m_AssetSourceBanner.style.display = borrowed ? DisplayStyle.Flex : DisplayStyle.None;
            if (borrowed)
                m_AssetSourceBanner.text = BuildBorrowedBannerText(analysis.AssetSource);
        }

        private static string BuildBorrowedBannerText(BuildAnalysisAssetSource source)
        {
            var date = FormatUtility.TryParseBuildTimestamp(source.BuildStartedAtUtc, out var parsed)
                ? $" ({FormatUtility.FormatBuildDate(parsed.ToLocalTime().DateTime)})"
                : string.Empty;
            return $"The asset data shown is from an earlier complete build{date}. This build did not record any of its own.";
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
