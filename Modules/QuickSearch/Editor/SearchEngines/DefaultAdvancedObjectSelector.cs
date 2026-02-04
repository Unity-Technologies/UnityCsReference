// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.Search.Providers;
using UnityEditor.SearchService;
using UnityEngine;

namespace UnityEditor.Search
{
    static class DefaultAdvancedObjectSelector
    {
        internal const string defaultAdvancedObjectSelectorId = "default_advanced_selector";
        static SearchWindow s_Window;

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
            var searchFlags = SearchFlags.OpenPicker | SearchFlags.UseSessionSettings;
            if (Utils.IsRunningTests())
                searchFlags |= SearchFlags.Dockable;

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var requiredTypes = selectContext.requiredTypes.ToArray();
#pragma warning restore UA2001
            var searchQuery = SearchUtils.BuildUnionTypeQuery(requiredTypes) ?? "";
            var selectHandler = parameters.selectorClosedHandler;
            var trackingHandler = parameters.trackingHandler;

            var viewState = SearchViewState.CreatePickerState("",
                SearchService.CreateContext(GetObjectSelectorProviders(selectContext), searchQuery, searchFlags), selectHandler, trackingHandler,
                requiredTypes);
            if (parameters.context.currentObject)
                viewState.selectedIds = new EntityId[] { parameters.context.currentObject.GetEntityId()};
            viewState.context.runtimeContext = new RuntimeSearchContext() {
                searchEngineContext = selectContext,
                pickerType = SearchPickerType.AdvancedSearchPicker };
            s_Window = SearchService.ShowPicker(viewState) as SearchWindow;

            // Notify the window is shown like in the legacy ObjectSelector
            ObjectSelector.InvokeWindowShown(s_Window);
        }

        static void EndSession(in AdvancedObjectSelectorParameters parameters)
        {
            if ((parameters.context.endSessionModes & ObjectSelectorSearchEndSessionModes.CloseSelector) != 0)
            {
                s_Window?.Close();
            }
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
