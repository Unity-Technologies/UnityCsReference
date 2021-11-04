// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Search
{
    [EditorWindowTitle(title = "Search")]
    class SearchPickerWindow : QuickSearch
    {
        protected override bool IsPicker()
        {
            return true;
        }

        public override void ExecuteSelection()
        {
            if (selectCallback == null || selection.Count == 0)
                return;
            selectCallback(selection.First(), false);
            selectCallback = null;
            CloseSearchWindow();
        }

        protected override void LoadSessionSettings(SearchViewState args)
        {
            RefreshSearch();
            UpdateViewState(args);
        }

        protected override void SaveSessionSettings()
        {
            SaveGlobalSettings();
        }

        protected override IEnumerable<SearchItem> FetchItems()
        {
            if (!viewState.excludeNoneItem)
                yield return SearchItem.none;
            foreach (var item in SearchService.GetItems(context))
            {
                if (filterCallback != null && !filterCallback(item))
                    continue;
                yield return item;
            }
        }

        protected override void OnAsyncItemsReceived(SearchContext context, IEnumerable<SearchItem> items)
        {
            var filteredItems = items;
            if (filterCallback != null)
                filteredItems = filteredItems.Where(item => filterCallback(item));
            base.OnAsyncItemsReceived(context, filteredItems);
        }

        protected override void UpdateWindowTitle()
        {
            if (!titleContent.image)
                titleContent.image = Icons.quickSearchWindow;
        }

        public override void Refresh(RefreshFlags flags = RefreshFlags.Default)
        {
            if (flags != RefreshFlags.Default)
                base.Refresh(flags);
        }

        protected override void UpdateFocusState(TextEditor te)
        {
            te.MoveLineEnd();
        }

        internal override SearchContext CreateQueryContext(ISearchQuery query)
        {
            return SearchService.CreateContext(context?.GetProviders(), query.searchText, context?.options ?? SearchFlags.Default);
        }

        protected override void DrawSyncSearchButton()
        {
            // Do nothing
        }

        protected override bool IsSavedSearchQueryEnabled()
        {
            return m_ViewState.HasFlag(SearchViewFlags.EnableSearchQuery);
        }


        public static QuickSearch ShowPicker(SearchViewState args)
        {
            var qs = Create<SearchPickerWindow>(args.LoadDefaults(SearchFlags.OpenPicker));
            qs.searchEventStatus = SearchEventStatus.WaitForEvent;
            qs.titleContent.text = $"Select {args.title ?? "item"}...";

            if (args.context.options.HasAny(SearchFlags.Dockable))
                qs.Show();
            else
                qs.ShowAuxWindow();

            // The window position can only be set one frame later.
            Utils.CallDelayed(() =>
            {
                if (args.HasFlag(SearchViewFlags.Centered))
                    qs.position = args.position = Utils.GetMainWindowCenteredPosition(args.hasWindowSize ? args.windowSize : qs.position.size);
                qs.Focus();
            });
            return qs;
        }
    }
}
