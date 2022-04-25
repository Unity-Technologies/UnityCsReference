// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.Search.Providers;
using UnityEditor.SearchService;

namespace UnityEditor.Search
{
    static class DefaultAdvancedObjectSelector
    {
        internal const string defaultAdvancedObjectSelectorId = "default_advanced_selector";
        static QuickSearch s_Window;

        [AdvancedObjectSelectorValidator(defaultAdvancedObjectSelectorId)]
        static bool CanOpenSelector(ObjectSelectorSearchContext context)
        {
            // The default advanced object selector can always open for any context
            return true;
        }

        [AdvancedObjectSelector(defaultAdvancedObjectSelectorId, "Default Advanced Selector", 9999, true)]
        static void HandleAdvancedObjectSelectorEvents(AdvancedObjectSelectorEventType eventType, in AdvancedObjectSelectorParameters parameters)
        {
            switch (eventType)
            {
                case AdvancedObjectSelectorEventType.EndSession:
                    EndSession(parameters);
                    break;
                case AdvancedObjectSelectorEventType.OpenAndSearch:
                    SelectObject(parameters);
                    break;
                case AdvancedObjectSelectorEventType.SetSearchFilter:
                    SetSearchFilter(parameters);
                    break;
            }
        }

        internal static IEnumerable<SearchProvider> GetObjectSelectorProviders(ObjectSelectorSearchContext context)
        {
            bool allowAssetObjects = (context.visibleObjects & VisibleObjects.Assets) == VisibleObjects.Assets;
            bool allowSceneObjects = (context.visibleObjects & VisibleObjects.Scene) == VisibleObjects.Scene;

            if (allowAssetObjects)
            {
                yield return SearchService.GetProvider(AdbProvider.type);
                yield return SearchService.GetProvider(AssetProvider.type);
            }
            if (allowSceneObjects)
                yield return SearchService.GetProvider(BuiltInSceneObjectsProvider.type);
        }

        static void SelectObject(in AdvancedObjectSelectorParameters parameters)
        {
            var selectContext = parameters.context;
            var viewFlags = SearchFlags.OpenPicker;
            if (Utils.IsRunningTests())
                viewFlags |= SearchFlags.Dockable;

            var searchQuery = string.Join(" ", selectContext.requiredTypeNames.Select(tn => tn == null ? "" : $"t:{tn.ToLowerInvariant()}"));
            if (string.IsNullOrEmpty(searchQuery))
                searchQuery = "";
            else
                searchQuery += " ";
            var selectHandler = parameters.selectorClosedHandler;
            var trackingHandler = parameters.trackingHandler;
            var viewState = new SearchViewState(
                SearchService.CreateContext(GetObjectSelectorProviders(selectContext), searchQuery, viewFlags), selectHandler, trackingHandler,
                selectContext.requiredTypeNames.First(), selectContext.requiredTypes.First());

            s_Window = SearchService.ShowPicker(viewState) as QuickSearch;
        }

        static void EndSession(in AdvancedObjectSelectorParameters parameters)
        {
            s_Window = null;
        }

        static void SetSearchFilter(in AdvancedObjectSelectorParameters parameters)
        {
            if (!s_Window)
                return;
            s_Window.SetSearchText(parameters.searchFilter);
        }
    }
}
