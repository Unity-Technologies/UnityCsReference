// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Lighting.LightingSearch
{
    /// <summary>
    /// Data container for baked lightmap preview column, containing the lightmap index and exposed texture.
    /// </summary>
    internal struct BakedLightmapPreviewData
    {
        public int index;
        public Texture2D lightmapColor;
    }

    class LightmapSearchProvider : SearchProvider
    {
        internal const string ProviderId = "lightmaps";
        internal const string FilterId = "lm:";
        const string k_BakedLightmapPreviewColumnProvider = "Baked Lightmap Preview";
        const string k_LightmapTextureColumnProvider = "Lightmap Texture";
        const string k_ProviderDisplayName = "Lightmaps";
        const string k_LightingSearchUSSPath = "StyleSheets/LightingSearch.uss";

        const float k_ItemHeight = 110f;
        static StyleSheet s_CachedLightmapTextureStyleSheet;

        static class QuerySelectors
        {
            public const string k_LightmapSelectorPath = "Lightmaps";
            public const string k_BakedLightmapPreviewSelector = k_LightmapSelectorPath + "/Baked Lightmap";
            public const string k_ColorSelector = k_LightmapSelectorPath + "/Color";
            public const string k_DirectionalitySelector = k_LightmapSelectorPath + "/Directionality";
            public const string k_ShadowMaskSelector = k_LightmapSelectorPath + "/Shadowmask";
            public const string k_IndexSelector = k_LightmapSelectorPath + "/Index";
            public const string k_SizeSelector = k_LightmapSelectorPath + "/Size";
            public const string k_FormatSelector = k_LightmapSelectorPath + "/Format";
            public const string k_CompressionSelector = k_LightmapSelectorPath + "/Compression";
            public const string k_WidthSelector = k_LightmapSelectorPath + "/Width";
            public const string k_HeightSelector = k_LightmapSelectorPath + "/Height";
            public const string k_LightingDataAssetSelector = k_LightmapSelectorPath + "/Lighting Data Asset";

            public const string k_IndexFilter = "lightmaps.index";
            public const string k_SizeFilter = "lightmaps.size";
        }

        readonly QueryEngine<LightmapDataWrapper> m_QueryEngine;

        internal class LightmapDataWrapper
        {
            public int index;
            public LightmapData data;
        }

        public LightmapSearchProvider()
            : base(ProviderId, k_ProviderDisplayName)
        {
            m_QueryEngine = BuildQueryEngine();

            filterId = FilterId;
            isExplicitProvider = true;
            fetchColumns = FetchColumns;
            tableConfig = GetDefaultTableConfig;
            fetchItems = FetchItems;
            fetchThumbnail = FetchThumbnail;
            fetchPropositions = (_, _) => m_QueryEngine.GetPropositions();
            actions = GetActions();
            onEnable = () =>
            {
                Lightmapping.bakeCompleted += OnBakeCompleted;
            };
            onDisable = () =>
            {
                Lightmapping.bakeCompleted -= OnBakeCompleted;
            };
        }

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new LightmapSearchProvider();
        }

        static QueryEngine<LightmapDataWrapper> BuildQueryEngine()
        {
            var queryEngineOptions = new QueryValidationOptions
            {
                validateFilters = false,
                skipNestedQueries = true,
                skipUnknownFilters = true
            };
            var qe = new QueryEngine<LightmapDataWrapper>(queryEngineOptions);

            var supportedOperators = new[] { "=", "!=", ">", ">=", "<", "<=" };

            qe.SetFilter(QuerySelectors.k_IndexFilter, wrapper => wrapper.index, supportedOperators)
                .AddOrUpdatePropositionData(
                    category: k_ProviderDisplayName,
                    label: "Index",
                    replacement: $"{QuerySelectors.k_IndexFilter}>0",
                    help: "Search by index",
                    color: QueryColors.filter);

            qe.SetFilter(QuerySelectors.k_SizeFilter, wrapper =>
                {
                    var texture = wrapper.data.lightmapColor;
                    if (texture == null)
                        return 0;
                    return System.Math.Max(texture.width, texture.height);
                }, supportedOperators)
                .AddOrUpdatePropositionData(
                    category: k_ProviderDisplayName,
                    label: "Size",
                    replacement: $"{QuerySelectors.k_SizeFilter}>512",
                    help: "Search by size (max dimension)",
                    color: QueryColors.filter);

            return qe;
        }

        IEnumerator FetchItems(SearchContext context, List<SearchItem> items, SearchProvider provider)
        {
            // If the query is empty, we return all the lightmaps
            var isEmptyQuery = context.searchQuery == "";
            var query = m_QueryEngine.ParseQuery(context.searchQuery);
            if (!isEmptyQuery && !query.valid)
                yield break;

            LightmapData[] lightmaps = LightmapSettings.lightmaps;

            // Initialize exposure when lightmaps are first displayed
            if (lightmaps.Length > 0 && !LightingSearchExposureSettings.IsInitialized)
            {
                LightingSearchExposureSettings.ResetToDefault();
            }
            
            for (var i = 0; i < lightmaps.Length; ++i)
            {
                var lm = lightmaps[i];
                var currentIndex = i; // Capture the index to avoid closure issues
                var lightmapEntityId = lm.lightmapColor != null ? lm.lightmapColor.GetEntityId() : EntityId.None;
                var data = new LightmapDataWrapper { index = currentIndex, data = lm };

                if (!isEmptyQuery && !query.Test(data))
                    continue;

                yield return provider.CreateItem(context,
                    lightmapEntityId.ToString(), 0,
                    lm.lightmapColor != null ? lm.lightmapColor.name : string.Empty,
                    "",
                    thumbnail: null, data);
            }
        }

        static Texture2D FetchThumbnail(SearchItem item, SearchContext context)
        {
            var dataWrapper = GetLightmapData(item);
            Texture2D sourceTexture = dataWrapper.data.lightmapColor;
            if (sourceTexture == null)
                return null;

            var textureEntityId = sourceTexture.GetEntityId();
            return LightmapTextureCache.GetOrCreate(textureEntityId, sourceTexture);
        }

        static void OnBakeCompleted()
        {
            // Recalculate a new auto exposure value
            LightingSearchExposureSettings.ResetToDefault();
            // Ensure cache is cleared even if exposure didn't change
            LightmapTextureCache.Clear();
            RefreshAllLightmapSearchWindowResultViews();
        }

        static void RefreshAllLightmapSearchWindowResultViews()
        {
            var windows = Resources.FindObjectsOfTypeAll<SearchWindow>();
            if (windows == null)
                return;

            foreach (var win in windows)
            {
                if (win.IsPicker())
                    continue;

                // Check if this window is showing lightmap provider
                var context = win.context;
                if (context != null)
                {
                    foreach (var provider in context.providers)
                    {
                        if (provider.id == ProviderId)
                        {
                            RefreshResultView(win);
                            break;
                        }
                    }
                }
            }
        }

        static void RefreshWindow()
        {
            UnityEditor.Search.SearchService.RefreshWindows();
        }

        // Refreshes the result view without refetching items
        internal static void RefreshResultView(ISearchWindow window)
        {
            var searchWindow = (window as SearchWindow);
            if (searchWindow != null)
            {
                var searchView = searchWindow.searchView;
                searchView.resultView.UpdateView();
            }
        }

        static LightmapDataWrapper GetLightmapData(SearchItem item)
        {
            var lightingDataWrapper = item.data as LightmapDataWrapper;
            return lightingDataWrapper;
        }

        static object GetLightmapData(SearchSelectorArgs args)
        {
            if (args.current.data is LightmapDataWrapper wrapper)
            {
                var lightmapColor = wrapper.data.lightmapColor;

                switch (args.path)
                {
                    case QuerySelectors.k_BakedLightmapPreviewSelector: return new BakedLightmapPreviewData { index = wrapper.index, lightmapColor = lightmapColor };
                    case QuerySelectors.k_ColorSelector: return lightmapColor;
                    case QuerySelectors.k_DirectionalitySelector: return wrapper.data.lightmapDir;
                    case QuerySelectors.k_ShadowMaskSelector: return wrapper.data.shadowMask;
                    case QuerySelectors.k_IndexSelector: return wrapper.index;
                    case QuerySelectors.k_SizeSelector: return lightmapColor != null ? $"{lightmapColor.width}x{lightmapColor.height}" : null;
                    case QuerySelectors.k_FormatSelector: return lightmapColor != null ? lightmapColor.format.ToString() : null;
                    case QuerySelectors.k_CompressionSelector: return Lightmapping.GetLightingSettingsOrDefaultsFallback().lightmapCompression == LightmapCompression.None ? " Uncompressed" : "Compressed";
                    case QuerySelectors.k_LightingDataAssetSelector: return Lightmapping.lightingDataAsset;
                }
            }

            return null;
        }

        static BakedLightmapPreviewData CreateBakedLightmapPreviewData(LightmapDataWrapper wrapper)
        {
            if (wrapper == null)
                return default;

            var sourceTexture = wrapper.data.lightmapColor;
            var exposedTexture = sourceTexture != null
                ? LightmapTextureCache.GetOrCreate(sourceTexture.GetEntityId(), sourceTexture)
                : null;
            return new BakedLightmapPreviewData { index = wrapper.index, lightmapColor = exposedTexture };
        }

        [SearchSelector(QuerySelectors.k_IndexSelector, cacheable = false)]
        static object GetIndex(SearchSelectorArgs args) => GetLightmapData(args);

        [SearchSelector(QuerySelectors.k_BakedLightmapPreviewSelector, cacheable = false)]
        static object GetBakedLightmapPreview(SearchSelectorArgs args)
        {
            return CreateBakedLightmapPreviewData(args.current.data as LightmapDataWrapper);
        }

        [SearchSelector(QuerySelectors.k_ColorSelector, cacheable = false)]
        static object GetColor(SearchSelectorArgs args)
        {
            if (GetLightmapData(args) is Texture2D sourceTexture)
                return LightmapTextureCache.GetOrCreate(sourceTexture.GetEntityId(), sourceTexture);
            return null;
        }

        [SearchSelector(QuerySelectors.k_DirectionalitySelector, cacheable = false)]
        static object GetDirectionality(SearchSelectorArgs args) => GetLightmapData(args);

        [SearchSelector(QuerySelectors.k_ShadowMaskSelector, cacheable = false)]
        static object GetShadowmask(SearchSelectorArgs args) => GetLightmapData(args);

        [SearchSelector(QuerySelectors.k_SizeSelector, cacheable = false)]
        static object GetSize(SearchSelectorArgs args) => GetLightmapData(args);

        [SearchSelector(QuerySelectors.k_FormatSelector, cacheable = false)]
        static object GetFormat(SearchSelectorArgs args) => GetLightmapData(args);

        [SearchSelector(QuerySelectors.k_CompressionSelector, cacheable = false)]
        static object GetCompression(SearchSelectorArgs args) => GetLightmapData(args);

        [SearchSelector(QuerySelectors.k_LightingDataAssetSelector, cacheable = false)]
        static object GetLightingDataAsset(SearchSelectorArgs args) => GetLightmapData(args);

        static IEnumerable<SearchColumn> FetchColumns(SearchContext context, IEnumerable<SearchItem> items)
        {
            yield return new SearchColumn($"{k_ProviderDisplayName}/Index", QuerySelectors.k_IndexSelector);
            yield return new SearchColumn($"{k_ProviderDisplayName}/{k_BakedLightmapPreviewColumnProvider}", QuerySelectors.k_BakedLightmapPreviewSelector, $"{k_ProviderDisplayName}/{k_BakedLightmapPreviewColumnProvider}");
            yield return new SearchColumn($"{k_ProviderDisplayName}/Color", QuerySelectors.k_ColorSelector, $"{k_ProviderDisplayName}/{k_LightmapTextureColumnProvider}");
            yield return new SearchColumn($"{k_ProviderDisplayName}/Directionality", QuerySelectors.k_DirectionalitySelector, $"{k_ProviderDisplayName}/{k_LightmapTextureColumnProvider}");
            yield return new SearchColumn($"{k_ProviderDisplayName}/Shadowmask", QuerySelectors.k_ShadowMaskSelector, $"{k_ProviderDisplayName}/{k_LightmapTextureColumnProvider}");
            yield return new SearchColumn($"{k_ProviderDisplayName}/Size", QuerySelectors.k_SizeSelector);
            yield return new SearchColumn($"{k_ProviderDisplayName}/Format", QuerySelectors.k_FormatSelector);
            yield return new SearchColumn($"{k_ProviderDisplayName}/Compression", QuerySelectors.k_CompressionSelector);
            yield return new SearchColumn($"{k_ProviderDisplayName}/Lighting Data Asset", QuerySelectors.k_LightingDataAssetSelector, "Object");
        }

        internal static SearchTable GetDefaultTableConfig(SearchContext context)
        {
            var defaultQuery = EditorResources.Load<UnityEngine.Object>("SearchQueries/LightingSearch/Lightmaps/All Lightmaps.asset") as SearchQueryAsset;

            // Clone the table config from the asset to avoid modifying the original asset data
            return defaultQuery != null ? new SearchTable(defaultQuery.viewState.tableConfig.id, defaultQuery.viewState.tableConfig.columns) { itemHeight = k_ItemHeight } : null;
        }

        static List<SearchAction> GetActions()
        {
            return new List<SearchAction>
            {
                new("open", "Select renderer(s) in lightmap", OpenLightmapsInHierarchy)
                {
                    closeWindowAfterExecution = false
                },
                new SearchAction("preview", "Open lightmap preview window", OpenPreviewWindow)
                {
                    closeWindowAfterExecution = false
                }
            };
        }

        static void OpenLightmapsInHierarchy(SearchItem[] items)
        {
            var rendererDict = new Dictionary<int, List<GameObject>>();
            var allRenderers = Resources.FindObjectsOfTypeAll<MeshRenderer>();
            foreach (var renderer in allRenderers)
            {
                if (!EditorUtility.IsPersistent(renderer.gameObject))
                {
                    var index = renderer.lightmapIndex;
                    if (!rendererDict.TryGetValue(index, out var list))
                    {
                        list = new List<GameObject>();
                        rendererDict[index] = list;
                    }
                    list.Add(renderer.gameObject);
                }
            }

            var terrainDict = new Dictionary<int, List<GameObject>>();
            var allTerrains = Resources.FindObjectsOfTypeAll<Terrain>();
            foreach (var terrain in allTerrains)
            {
                if (!EditorUtility.IsPersistent(terrain.gameObject))
                {
                    var index = terrain.lightmapIndex;
                    if (!terrainDict.TryGetValue(index, out var list))
                    {
                        list = new List<GameObject>();
                        terrainDict[index] = list;
                    }
                    list.Add(terrain.gameObject);
                }
            }

            var lightmapGameObjects = new HashSet<GameObject>();

            foreach (var searchItem in items)
            {
                var data = GetLightmapData(searchItem);

                if (rendererDict.TryGetValue(data.index, out var rendererGOs))
                    foreach (var go in rendererGOs)
                        lightmapGameObjects.Add(go);

                if (terrainDict.TryGetValue(data.index, out var terrainGOs))
                    foreach (var go in terrainGOs)
                        lightmapGameObjects.Add(go);
            }

            if (lightmapGameObjects.Count > 0)
            {
                var objectsArray = new GameObject[lightmapGameObjects.Count];
                lightmapGameObjects.CopyTo(objectsArray);
                Selection.objects = objectsArray;

                using (var enumerator = lightmapGameObjects.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        EditorGUIUtility.PingObject(enumerator.Current);
                    }
                }
            }
        }

        static void OpenPreviewWindow(SearchItem[] items)
        {
            foreach (var searchItem in items)
            {
                var data = GetLightmapData(searchItem);
                LightmapPreviewWindow.CreateLightmapPreviewWindowIndexedWithExposure(data.index, false, false, LightingSearchExposureSettings.CurrentExposure);
            }
        }

        [SearchColumnProvider($"{k_ProviderDisplayName}/{k_BakedLightmapPreviewColumnProvider}")]
        static void BakedLightmapPreviewSearchColumnProvider(SearchColumn column)
        {
            column.cellCreator = _ => new LightmapPreviewField();
            column.binder = (args, ve) =>
            {
                var lightmapField = (LightmapPreviewField)ve;
                var data = args.value as BakedLightmapPreviewData?;

                lightmapField.lightmapIndex = data?.index ?? -1;
                lightmapField.lightmapTexture = data?.lightmapColor;
                lightmapField.exposure = LightingSearchExposureSettings.CurrentExposure;
            };
            column.getter = args => CreateBakedLightmapPreviewData(args.item.data as LightmapDataWrapper);
            column.options &= ~SearchColumnFlags.CanSort;
        }

        [SearchColumnProvider($"{k_ProviderDisplayName}/{k_LightmapTextureColumnProvider}")]
        static void LightmapTextureSearchColumnProvider(SearchColumn column)
        {
            if (s_CachedLightmapTextureStyleSheet == null)
            {
                s_CachedLightmapTextureStyleSheet = EditorResources.Load<UnityEngine.Object>(k_LightingSearchUSSPath) as StyleSheet;
            }

            column.cellCreator = _ =>
            {
                var image = new Image();
                if (s_CachedLightmapTextureStyleSheet != null)
                    image.styleSheets.Add(s_CachedLightmapTextureStyleSheet);
                image.AddToClassList("lighting-search-lightmap-texture--column");
                return image;
            };
            column.binder = (args, ve) =>
            {
                var image = (Image)ve;
                image.image = args.value as Texture2D;
            };
            column.options &= ~SearchColumnFlags.CanSort;
        }

    }
}
