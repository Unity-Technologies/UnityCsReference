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

        static string BuildInitialQuery(in ObjectSelectorSearchContext selectContext)
        {
            var query = string.Empty;
            var types = selectContext.requiredTypes.ToArray();
            var typeNames = selectContext.requiredTypeNames.ToArray();
            for (int i = 0; i < types.Length; ++i)
            {
                var name = types[i]?.Name ?? typeNames[i];
                if (query.Length != 0)
                    query += ' ';
                query += $"t:{name}";
            }
            return query;
        }
        
        static void SelectObject(in AdvancedObjectSelectorParameters parameters)
        {
            var selectContext = parameters.context;
            var searchFlags = SearchFlags.OpenPicker;
            if (Utils.IsRunningTests())
                searchFlags |= SearchFlags.Dockable;

            var searchQuery = BuildInitialQuery(selectContext) ?? "";
            var selectHandler = parameters.selectorClosedHandler;
            var trackingHandler = parameters.trackingHandler;

            var viewState = SearchViewState.CreatePickerState(null, 
                SearchService.CreateContext(GetObjectSelectorProviders(selectContext), searchQuery, searchFlags), selectHandler, trackingHandler,
                selectContext.requiredTypeNames.First(), selectContext.requiredTypes.First());
            if (parameters.context.currentObject)
                viewState.selectedIds = new int[] { parameters.context.currentObject.GetInstanceID()};
            viewState.context.runtimeContext = new RuntimeSearchContext() { searchEngineContext = selectContext, pickerType = SearchPickerType.AdvancedSearchPicker };

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
