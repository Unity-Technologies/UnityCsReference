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
        const string k_SavedWindowPositionPrefKey = "picker_window_position_offset";
        const string k_PickerVisibilityFlags = "picker_visibility_flags";
        const string k_PickerItemSize = "picker_item_size";
        const string k_PickerInspector = "picker_inspector";
        const string k_PickerCurrentGroup = "picker_current_group";
        const int k_UniquePickerHash = 123456789;

        public override bool IsPicker()
        {
            return true;
        }

        protected override void LoadSessionSettings(SearchViewState args)
        {
            var visibilityFlags = (SearchFlags)SearchSettings.GetScopeValue(k_PickerVisibilityFlags, m_ContextHash, (int)(SearchFlags.WantsMore | SearchFlags.Packages));
            args.context.options |= visibilityFlags;
            args.itemSize = SearchSettings.GetScopeValue(k_PickerItemSize, m_ContextHash, (int)DisplayMode.Grid);
            if (SearchSettings.GetScopeValue(k_PickerInspector, m_ContextHash, 0) != 0)
            {
                args.flags |= SearchViewFlags.OpenInspectorPreview;
            }
            RestoreSearchText(args);

            args.group = SearchSettings.GetScopeValue(k_PickerCurrentGroup, m_ContextHash, GroupedSearchList.allGroupId);

            UpdateViewState(args);
        }

        protected override void SaveSessionSettings()
        {
            if (m_LastFocusedWindow && !Utils.IsRunningTests())
            {
                var offset = new Rect(position.position - m_LastFocusedWindow.position.position, position.size);
                SearchSettings.SetScopeValue(k_SavedWindowPositionPrefKey, m_ContextHash, offset);
            }

            SearchSettings.SetScopeValue(k_PickerVisibilityFlags, m_ContextHash, (int)(context.options & (SearchFlags.WantsMore | SearchFlags.Packages)));
            SearchSettings.SetScopeValue(k_PickerItemSize, m_ContextHash, itemIconSize);
            SearchSettings.SetScopeValue(k_PickerInspector, m_ContextHash, viewState.flags.HasAny(SearchViewFlags.OpenInspectorPreview) ? 1 : 0);
            SearchSettings.SetScopeValue(k_PickerCurrentGroup, m_ContextHash, viewState.group);

            SearchSettings.Save();
        }

        protected override void RestoreSearchText(SearchViewState viewState)
        {
            // We do not restore any text.
        }

        protected override void ComputeContextHash()
        {
            m_ContextHash = k_UniquePickerHash;
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

        public static SearchPickerWindow ShowPicker(SearchViewState args)
        {
            var qs = Create<SearchPickerWindow>(args);
            qs.titleContent.text = $"Select {args.title ?? "item"}...";

            if (!qs.viewState.queryBuilderEnabled && !string.IsNullOrEmpty(qs.context.searchText))
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
