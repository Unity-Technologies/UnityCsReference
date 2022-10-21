// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Search;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    [EditorWindowTitle(title = "Search")]
    class SearchPickerWindow : SearchWindow
    {
        internal const string k_SavedSearchTextPrefKey = "picker_search_text";
        const string k_SavedWindowPositionPrefKey = "picker_window_position_offset";

        public override bool IsPicker()
        {
            return true;
        }

        internal override void OnEnable()
        {
            base.OnEnable();
        }

        internal override void OnDisable()
        {
            if (m_LastFocusedWindow && !Utils.IsRunningTests())
            {
                var offset = new Rect(position.position - m_LastFocusedWindow.position.position, position.size);
                SearchSettings.SetScopeValue(k_SavedWindowPositionPrefKey, m_ContextHash, offset);
            }
            SearchSettings.SetScopeValue(k_SavedSearchTextPrefKey, m_ContextHash, context.searchText);

            base.OnDisable();
        }

        protected override void RestoreSearchText(SearchViewState viewState)
        {
            if (Utils.IsRunningTests() || viewState.context == null)
                return;

            var previousSearchText = SearchSettings.GetScopeValue(k_SavedSearchTextPrefKey, m_ContextHash, viewState.initialQuery);
            if (string.CompareOrdinal(viewState.searchText.Trim(), previousSearchText.Trim()) != 0)
                viewState.text = previousSearchText.Trim();
        }

        protected override void HandleEscapeKeyDown(EventBase evt)
        {
            if (string.CompareOrdinal(context.searchText, viewState.initialQuery) != 0)
            {
                ClearSearch();
                return;
            }

            SendEvent(SearchAnalytics.GenericEventType.QuickSearchDismissEsc);
            selectCallback?.Invoke(null, true);
            selectCallback = null;
            CloseSearchWindow();
        }

        public override void ExecuteSelection()
        {
            if (selectCallback == null || selection.Count == 0)
                return;
            selectCallback(selection.First(), false);
            selectCallback = null;
            CloseSearchWindow();
        }

        protected override IEnumerable<SearchItem> FetchItems()
        {
            if (!viewState.excludeClearItem)
                yield return SearchItem.clear;
        }

        protected override void UpdateWindowTitle()
        {
            if (!titleContent.image)
                titleContent.image = Icons.quickSearchWindow;
        }

        protected override SearchContext CreateQueryContext(ISearchQuery query)
        {
            return SearchService.CreateContext(context?.GetProviders(), query.searchText, context?.options ?? SearchFlags.Default);
        }

        internal protected override bool IsSavedSearchQueryEnabled()
        {
            return m_ViewState.HasFlag(SearchViewFlags.EnableSearchQuery);
        }

        public static SearchPickerWindow ShowPicker(SearchViewState args)
        {
            var qs = Create<SearchPickerWindow>(args.LoadDefaults(SearchFlags.OpenPicker));
            qs.titleContent.text = $"Select {args.title ?? "item"}...";

            if (qs.viewState.flags.HasNone(SearchViewFlags.OpenInBuilderMode) && !string.IsNullOrEmpty(qs.context.searchText))
            {
                if (!qs.viewState.text.EndsWith(' '))
                    qs.viewState.text += ' ';
                if (!qs.viewState.initialQuery.EndsWith(' '))
                    qs.viewState.initialQuery += ' ';
            }

            InjectDefaultRuntimeContext(qs.context);

            if (args.context.options.HasAny(SearchFlags.Dockable))
                qs.Show(immediateDisplay: true);
            else
                qs.ShowAuxWindow();

            // The window position can only be set one frame later.
            qs.RestoreWindowPosition(args);
            qs.Focus();
            return qs;
        }

        private Vector2 GetInitialWindowSize()
        {
            if (Utils.IsRunningTests())
                return new Vector2(400, 600);
            return position.size;
        }

        private Rect RestoreWindowPosition(SearchViewState viewState)
        {
            Rect windowPosition = default;
            if (Utils.IsRunningTests() || viewState.HasFlag(SearchViewFlags.Centered))
            {
                windowPosition = Utils.GetMainWindowCenteredPosition(viewState.hasWindowSize ? viewState.windowSize : GetInitialWindowSize());
            }
            else if (m_LastFocusedWindow && !Utils.IsRunningTests())
            {
                var pickerOffset = SearchSettings.GetScopeValue(k_SavedWindowPositionPrefKey, m_ContextHash, default(Rect));
                if (pickerOffset != default)
                {
                    windowPosition.position = m_LastFocusedWindow.position.position + pickerOffset.position;
                    windowPosition.size = pickerOffset.size;
                }
            }

            if (windowPosition != default)
                position = viewState.position = windowPosition;

            return windowPosition;
        }

        static void InjectDefaultRuntimeContext(SearchContext context)
        {
            context.runtimeContext ??= new RuntimeSearchContext();
            if (context.runtimeContext.pickerType == SearchPickerType.None)
                context.runtimeContext.pickerType = SearchPickerType.AdvancedSearchPicker;
            if (context.runtimeContext.requiredTypes == null && context.runtimeContext.requiredTypeNames == null && context.filterType != null)
            {
                context.runtimeContext.requiredTypes = new[] { context.filterType };
                context.runtimeContext.requiredTypeNames = new[] { context.filterType.Name };
            }
        }
    }
}
