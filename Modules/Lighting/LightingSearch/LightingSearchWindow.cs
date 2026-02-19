// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Search;
using UnityEditor.Search.Providers;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Lighting.LightingSearch
{
    class LightingSearchWindow
    {
        const string k_MenuItemPath = "Window/Rendering/Lighting Search (new Light Explorer)";
        const string k_WindowTitle = "Lighting Search";

        internal static SearchViewState CreateSearchViewState()
        {
            var sceneProvider = BuiltInSceneObjectsProvider.CreateProvider();
            var lightmapProvider = UnityEditor.Search.SearchService.GetProvider(LightmapSearchProvider.ProviderId) ?? LightmapSearchProvider.CreateProvider();
            var projectProvider = UnityEditor.Search.SearchService.GetProvider(AssetProvider.type) ?? AssetProvider.CreateProvider();

            var context = UnityEditor.Search.SearchService.CreateContext(sceneProvider);

            if (sceneProvider != null)
            {
                sceneProvider.trackSelection = (item, _) =>
                {
                    var selectedObjects = new System.Collections.Generic.List<Object>();
                    foreach (var searchItem in context.selection)
                    {
                        var obj = searchItem.provider.toObject(searchItem, typeof(GameObject));
                        if (obj != null)
                            selectedObjects.Add(obj);
                    }
                    if (selectedObjects.Count > 0)
                        Selection.objects = selectedObjects.ToArray();
                };

                OverrideOpenAction(sceneProvider);
            }

            if (projectProvider != null)
            {
                OverrideOpenAction(projectProvider);
            }

            context.AddProvider(lightmapProvider);
            context.AddProvider(projectProvider);
            context.options |= SearchFlags.Multiselect;

            return new SearchViewState(context);
        }

        /// <summary>
        /// Keep the window open after executing "open" action to maintain consistency with Light Explorer behavior.
        /// This allows continued interaction with the organized lighting view instead of closing after each selection.
        /// </summary>
        static void OverrideOpenAction(SearchProvider provider)
        {
            var openAction = provider.actions.Find(a => a.id == "open");
            if (openAction != null)
            {
                openAction.closeWindowAfterExecution = false;
            }
        }

        static class LightingCustomPanelSetup
        {
            static ISearchWindow s_Window;

            static void BindCustomPanel(SearchWindowCustomPanelConfig config, ISearchWindow window, ISearchView view, SearchElement rootElement)
            {
                s_Window = window;

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
                s_Window = null;

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
                if (config == null)
                {
                    return;
                }

                if (config.id != LightmapSearchProvider.ProviderId)
                {
                    return;
                }

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
                slider.style.display = shouldBeVisible  ? DisplayStyle.Flex : DisplayStyle.None;

                // Repaint the window if the visibility of the panel changed.
                if (wasVisible != shouldBeVisible)
                {
                    if (s_Window is EditorWindow editorWindow)
                    {
                        editorWindow.Repaint();
                    }
                }
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

            viewState.itemSize = (float)DisplayMode.Table;
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
