// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Toolbars;
using UnityEngine.Search;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Search
{
    static partial class MainToolbarMenuItemPicker
    {
        [OnCodeLoaded]
        static void RegisterMainToolbarMenuItemSearch()
        {
            MainToolbarWindow.menuItemSearchRequested += OpenMenuItemsPicker;
        }

        internal static void OpenMenuItemsPicker()
        {
            var searchContext = SearchService.CreateContext("menu", string.Empty);
            var state = SearchViewState.CreatePickerState(L10n.Tr("Menu Items", null), searchContext, OnMenuItemSelected);
            state.excludeClearItem = true;
            state.resultViewDescriptorList = new SearchResultViewDescriptorList(new[] { SearchTreeView.GetDescriptor() });
            var view = SearchService.ShowPicker(state);
            if (view is SearchWindow window)
                window.searchView.SetSearchItemComparer(new SortByDescriptionComparer());
        }

        internal static void OnMenuItemSelected(SearchItem item, bool canceled)
        {
            if (canceled)
                return;

            MainToolbar.PinMenuItem(item.id);
        }
    }
}
