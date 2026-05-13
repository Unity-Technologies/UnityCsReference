// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Experimental;
using UnityEditor.Search;
using UnityEditor.Search.Providers;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Rendering;

namespace UnityEditor.Lighting.LightingSearch
{
    class LightingSearchWindow
    {
        const string k_MenuItemPath = "Window/Rendering/Lighting Search";
        const string k_WindowTitle = "Lighting Search";
        const string k_LightingSearchQueriesFolder = "LightingSearch";
        const string k_QueryTreeRootName = "Scene Lighting Data";

        internal const string k_HDRPAssetTypeName = "HDRenderPipelineAsset";
        internal const string k_URPAssetTypeName = "UniversalRenderPipelineAsset";

        const string k_HDRPLightsQueryPath = "SearchQueries/LightingSearch/HDRP Lights/All Lights.asset";
        const string k_BuiltInLightsQueryPath = "SearchQueries/LightingSearch/Lights/All Lights.asset";

        internal static string GetDefaultQueryPath(string renderPipelineAssetTypeName)
        {
            return renderPipelineAssetTypeName == k_HDRPAssetTypeName
                ? k_HDRPLightsQueryPath
                : k_BuiltInLightsQueryPath;
        }

        static SearchQueryAsset GetDefaultQuery()
        {
            var currentRenderPipeline = GraphicsSettings.currentRenderPipeline;
            var renderPipelineTypeName = currentRenderPipeline?.GetType().Name;
            var queryPath = GetDefaultQueryPath(renderPipelineTypeName);

            var query = EditorResources.Load<UnityEngine.Object>(queryPath) as SearchQueryAsset;

            return query;
        }

        static void OnTrackSelection(SearchItem item)
        {
            if (item == null)
                return;
            if (item.provider?.id == BuiltInSceneObjectsProvider.type)
                SelectItems(item.context?.selection);
            else
                PingItem(item);
        }

        static void PingItem(SearchItem item)
        {
            if (item?.provider?.toObject == null)
            {
                item?.provider?.trackSelection?.Invoke(item, item.context);
                return;
            }
            try
            {
                var obj = item.provider.toObject(item, typeof(UnityEngine.Object));
                if (obj != null)
                    EditorGUIUtility.PingObject(obj);
            }
            catch (System.Exception)
            {
                item?.provider?.trackSelection?.Invoke(item, item.context);
            }
        }

        static void SelectItems(IEnumerable<SearchItem> items)
        {
            if (items == null)
                return;
            var list = new List<UnityEngine.Object>();
            foreach (var item in items)
            {
                if (item?.provider?.toObject == null || item.provider.id != BuiltInSceneObjectsProvider.type)
                    continue;
                try
                {
                    var obj = item.provider.toObject(item, typeof(UnityEngine.Object));
                    if (obj != null)
                        list.Add(obj);
                }
                catch (System.Exception)
                {
                    // Skip items whose provider.toObject throws (e.g. invalid or disposed context).
                }
            }
            if (list.Count > 0)
                Selection.objects = list.ToArray();
        }

        internal static SearchViewState CreateSearchViewState()
        {
            var sceneProvider = UnityEditor.Search.SearchService.GetProvider(BuiltInSceneObjectsProvider.type) ?? BuiltInSceneObjectsProvider.CreateProvider();
            var lightmapProvider = UnityEditor.Search.SearchService.GetProvider(LightmapSearchProvider.ProviderId) ?? LightmapSearchProvider.CreateProvider();
            var projectProvider = UnityEditor.Search.SearchService.GetProvider(AssetProvider.type) ?? AssetProvider.CreateProvider();

            var context = UnityEditor.Search.SearchService.CreateContext(sceneProvider);

            if (sceneProvider != null)
                OverrideOpenAction(sceneProvider);
            if (projectProvider != null)
                OverrideOpenAction(projectProvider);

            context.AddProvider(lightmapProvider);
            context.AddProvider(projectProvider);
            context.options |= SearchFlags.Multiselect;

            var viewState = new SearchViewState(context);
            viewState.trackingHandler = OnTrackSelection;
            viewState.displaySearchErrors = (searchView, group, error) =>
            {
                // Suppress "Unknown filter" errors that occur when using saved queries
                // with filters that have not been indexed in the current project yet.
                // Only suppress errors from the Project tab (asset provider) where indexing occurs.
                if (group == "asset" &&
                    error.reason != null &&
                    error.reason.Contains("Unknown filter", StringComparison.OrdinalIgnoreCase))
                    return false;
                return true;
            };
            return viewState;
        }

        static void OverrideOpenAction(SearchProvider provider)
        {
            var openAction = provider.actions.Find(a => a.id == "open");
            if (openAction != null)
            {
                openAction.closeWindowAfterExecution = false;
            }
        }

        static ISearchQueryNodeHandler LightingQueryTreeNodeHandlerCreator()
        {
            return new LightingSearchQueryTreeNodeHandler(k_LightingSearchQueriesFolder, k_QueryTreeRootName);
        }

        static SearchQueryTreeConfig LightingSearchQueryTreeConfig()
        {
            return new SearchQueryTreeConfig(
                SearchQueryTreeConfig.DefaultUserQueryTreeNodeHandlerCreator,
                SearchQueryTreeConfig.DefaultProjectQueryTreeNodeHandlerCreator,
                LightingQueryTreeNodeHandlerCreator);
        }

        static class LightingCustomPanelSetup
        {
            static void BindCustomPanel(SearchWindowCustomPanelConfig config, ISearchWindow window, ISearchView view, SearchElement rootElement)
            {
                var lightmapExposureSlider = CreateExposureSlider(window);
                // This serves as contextual data for the custom panel, used in the global SearchQueryExecuted event to show/hide the panel.
                config.bindUserData = lightmapExposureSlider;
                rootElement.Add(lightmapExposureSlider);

                // Pass the viewHashCode to ensure that HandleViewStateChanged has a unique ID for registration (since it is a static function)
                // Use SearchElement.On to register a message. this will ensure we only receive events coming from our viewState.
                rootElement.On(SearchEvent.SearchQueryExecuted, HandleViewStateChanged, view.GetHashCode());
                rootElement.On(SearchEvent.SearchTextChanged, HandleViewStateChanged, view.GetHashCode());

                view.Refresh();
            }

            static ExposureSlider CreateExposureSlider(ISearchWindow window)
            {
                var slider = new ExposureSlider(LightingSearchExposureSettings.k_MinExposure, LightingSearchExposureSettings.k_MaxExposure);
                slider.SetValueWithoutNotify(Mathf.Clamp(LightingSearchExposureSettings.CurrentExposure,
                    LightingSearchExposureSettings.k_MinExposure,
                    LightingSearchExposureSettings.k_MaxExposure));

                // Don't show the slider by default - it will be shown/hidden based on the current viewState in HandleQueryExecuted.
                slider.style.display = DisplayStyle.None;

                // Update slider when user changes it
                slider.RegisterValueChangedCallback(evt =>
                {
                    LightingSearchExposureSettings.CurrentExposure = evt.newValue;
                    LightmapSearchProvider.RefreshResultView(window);
                });

                // Update slider when exposure is recalculated (e.g., after bake)
                System.Action onExposureChanged = () =>
                {
                    slider.SetValueWithoutNotify(Mathf.Clamp(LightingSearchExposureSettings.CurrentExposure,
                        LightingSearchExposureSettings.k_MinExposure,
                        LightingSearchExposureSettings.k_MaxExposure));
                    LightmapSearchProvider.RefreshResultView(window);
                };
                LightingSearchExposureSettings.ExposureChanged += onExposureChanged;

                // Store callback for cleanup
                slider.userData = onExposureChanged;

                return slider;
            }

            static void UnbindCustomPanel(SearchWindowCustomPanelConfig config, ISearchWindow window, ISearchView view, SearchElement rootElement)
            {
                // Unsubscribe from exposure changed event
                if (config.bindUserData is ExposureSlider slider && slider.userData is System.Action callback)
                {
                    LightingSearchExposureSettings.ExposureChanged -= callback;
                }

                // The Exposure slider will be destroyed along with the rootElement when unbinding, we don't need to remove it explicitly.
                // Stop listening to Global search events
                rootElement.Off(SearchEvent.SearchQueryExecuted, view.GetHashCode());
                rootElement.Off(SearchEvent.SearchTextChanged, view.GetHashCode());
            }

            static void HandleViewStateChanged(ISearchEvent evt)
            {
                // We need to decide if we show or hide the custom panel based on the columns being displayed in the current viewState.
                // Since SearchEvents are global events and can be triggered by any SearchWindow, we need to make sure we are only affecting a relevant one.

                var sourceViewState = evt.sourceViewState;
                var config = sourceViewState.customPanelConfig;
                if (config == null || config.id != LightmapSearchProvider.ProviderId)
                    return;

                sourceViewState.trackingHandler = OnTrackSelection;

                if (config.bindUserData is not ExposureSlider slider)
                {
                    return;
                }

                var hasActiveLightmapSearchProvider = false;
                foreach (var provider in sourceViewState.context.providers)
                {
                    if (provider.id == LightmapSearchProvider.ProviderId)
                    {
                        hasActiveLightmapSearchProvider = true;
                        break;
                    }
                }

                bool wasVisible = slider.style.display == DisplayStyle.Flex;
                bool shouldBeVisible = hasActiveLightmapSearchProvider;
                slider.style.display = shouldBeVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            public static SearchWindowCustomPanelConfig CreateLightingCustomPanel()
            {
                return new SearchWindowCustomPanelConfig(LightmapSearchProvider.ProviderId)
                {
                    // It is important that the callback be a static function or else it fails serialization.
                    bindPanel = BindCustomPanel,
                    unbindPanel = UnbindCustomPanel,
                    // This means the panel won't be unloaded by other SearchQueries being executed.
                    isLocked = true,
                    // The panel config won't be persisted in the searchQuery asset saved on disk.
                    serializableInQuery = false
                };
            }
        }

        internal static ISearchView ShowLightingSearchWindow()
        {
            var viewState = CreateSearchViewState();

            viewState.queryTreeConfig = LightingSearchQueryTreeConfig();

            SceneProvider sceneProvider = null;
            foreach (var provider in viewState.context.GetProviders())
            {
                if (provider is SceneProvider sp)
                {
                    sceneProvider = sp;
                    break;
                }
            }

            var defaultQuery = GetDefaultQuery();
            if (sceneProvider != null && defaultQuery != null)
            {
                viewState.tableConfig = new SearchTable(defaultQuery.viewState.tableConfig.id, defaultQuery.viewState.tableConfig.columns);
                viewState.text = defaultQuery.searchText;
            }

            viewState.SetDisplayMode(DisplayMode.Table);
            viewState.windowTitle = new GUIContent(k_WindowTitle);
            viewState.customPanelConfig = LightingCustomPanelSetup.CreateLightingCustomPanel();
            viewState.queryBuilderEnabled = true;

            return Search.SearchService.ShowWindow(viewState);
        }

        [MenuItem(k_MenuItemPath, priority = 2, secondaryPriority = 1)]
        internal static void HandleMenuSelection()
        {
            ShowLightingSearchWindow();
        }
    }
}
