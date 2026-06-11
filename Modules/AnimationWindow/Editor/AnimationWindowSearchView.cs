// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Pool;
using SearchUtils = UnityEditor.Search.SearchUtils;

namespace UnityEditorInternal
{
    sealed class AnimationWindowSearchView : ISearchView
    {
        SearchViewState m_ViewState;
        AnimationWindowState m_State;

        public ISearchList results { get; set; }
        public SearchContext context => m_ViewState.context;
        public SearchViewState state => m_ViewState;

        public AnimationWindowSearchView(AnimationWindowState state)
        {
            m_State = state;
            m_ViewState = SearchViewState.LoadDefaults();

            // Register animation provider (follows Hierarchy pattern)
            var providers = new[]
            {
                CreateAnimationProvider()
            };

            m_ViewState.context = SearchService.CreateContext(providers, "");
            results = new SortedSearchList(m_ViewState.context);
            context.searchView = this;
        }

        SearchProvider CreateAnimationProvider()
        {
            return new SearchProvider("animation", "Animation")
            {
                isExplicitProvider = false,
                priority = 100,
                active = true,
                fetchPropositions = (context, options) => FetchAnimationPropositions(context, options)
            };
        }

        IEnumerable<SearchProposition> FetchAnimationPropositions(SearchContext context, SearchPropositionOptions options)
        {
            // Return both components and properties
            // QuickSearch UI will organize them by category

            // Collect unique component types and property names from curves
            using var pooledObject = DictionaryPool<Type, HashSet<(string, string)>>.Get(out var propertiesByType);
            foreach (var curve in m_State.allCurves)
            {
                var propertyName = curve.propertyName;
                var propertyGroupName = AnimationWindowUtility.GetPropertyGroupName(propertyName);

                var nicePropertyName = AnimationWindowUtility.GetNicePropertyDisplayName(curve.type, propertyName);

                if (propertiesByType.ContainsKey(curve.type))
                {
                    propertiesByType[curve.type].Add((propertyName, nicePropertyName));
                    if (propertyName != propertyGroupName)
                        propertiesByType[curve.type].Add((propertyGroupName, AnimationWindowUtility.GetNicePropertyDisplayName(curve.type, propertyGroupName)));
                }
                else
                {
                    var propertyNames = HashSetPool<(string, string)>.Get();

                    propertyNames.Add((propertyName, nicePropertyName));
                    if (propertyName != propertyGroupName)
                        propertyNames.Add((propertyGroupName, AnimationWindowUtility.GetNicePropertyDisplayName(curve.type, propertyGroupName)));

                    propertiesByType.Add(curve.type, propertyNames);
                }
            }

            foreach (var proposition in GetComponentPropositions(propertiesByType))
                yield return proposition;

            foreach (var proposition in GetPropertyPropositions(propertiesByType))
                yield return proposition;

            foreach (var (_, propertyNames) in propertiesByType)
            {
                HashSetPool<(string, string)>.Release(propertyNames);
            }

        }

        IEnumerable<SearchProposition> GetComponentPropositions(Dictionary<Type, HashSet<(string, string)>> propertiesByType)
        {
            if (m_State == null || m_State.allCurves == null)
                yield break;

            foreach (var (componentType, _) in propertiesByType)
            {
                var typeName = componentType.Name;
                var icon = SearchUtils.GetTypeIcon(componentType);

                yield return new SearchProposition(
                    category: "Components",
                    label: typeName,
                    replacement: $"t={typeName}",
                    help: $"Filter by {typeName} component",
                    icon: icon,
                    priority: 0,
                    color: QueryColors.type
                );
            }
        }

        IEnumerable<SearchProposition> GetPropertyPropositions(Dictionary<Type, HashSet<(string, string)>> propertiesByType)
        {
            if (m_State == null || m_State.allCurves == null)
                yield break;

            // Create propositions organized by component (hierarchical categories)
            foreach (var (componentType, propertyNames) in propertiesByType)
            {
                foreach (var (propertyName, nicePropertyName) in propertyNames)
                {
                    var componentName = componentType.Name;
                    var componentIcon = SearchUtils.GetTypeIcon(componentType);

                    yield return new SearchProposition(
                        category: $"Properties/{componentName}", // Hierarchical category
                        label: nicePropertyName,
                        replacement: $"p=\"{componentName}.{propertyName}\"",
                        help: $"Filter by {propertyName}",
                        icon: componentIcon, // Icon inherited from component
                        priority: 0,
                        color: QueryColors.filter
                    );
                }
            }
        }

        void ISearchView.SetSearchText(string searchText, TextCursorPlacement moveCursor)
        {
            ((ISearchView)this).SetSearchText(searchText, moveCursor, 0);
        }

        void ISearchView.SetSearchText(string searchText, TextCursorPlacement moveCursor, int cursorInsertPosition)
        {
            if (string.Equals(context.searchText.Trim(), searchText.Trim(), StringComparison.Ordinal))
                return;

            context.searchText = searchText;
            m_State.searchFilter = context.searchQuery;
        }

        void ISearchView.SetSelection(params int[] selection)
        {
            // Used by SearchField to clear selection when query changes.
        }

        IEnumerable<SearchQueryError> ISearchView.GetAllVisibleErrors()
        {
            yield break;
        }

        Rect ISearchView.position => new Rect(0, 0, 400, 20);
        void ISearchView.Repaint() { }
        string ISearchView.currentGroup { get => null; set { } }
        bool ISearchView.IsPicker() => false;

        #region NotImplemented
        SearchSelection ISearchView.selection => throw new NotSupportedException();
        float ISearchView.itemIconSize { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        string ISearchView.currentResultViewId { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        DisplayMode ISearchView.displayMode => throw new NotSupportedException();
        bool ISearchView.multiselect { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        bool ISearchView.searchInProgress => throw new NotSupportedException();
        Action<SearchItem, bool> ISearchView.selectCallback => throw new NotSupportedException();
        Func<SearchItem, bool> ISearchView.filterCallback => throw new NotSupportedException();
        Action<SearchItem> ISearchView.trackingCallback => throw new NotSupportedException();
        int ISearchView.totalCount => throw new NotSupportedException();
        bool ISearchView.syncSearch { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        SearchPreviewManager ISearchView.previewManager => throw new NotSupportedException();

        void ISearchView.AddSelection(params int[] selection) => throw new NotSupportedException();
        void ISearchView.Refresh(RefreshFlags reason) => throw new NotSupportedException();
        void ISearchView.ExecuteAction(SearchAction action, SearchItem[] items, bool endSearch) => throw new NotSupportedException();
        void ISearchView.ExecuteSelection() => throw new NotSupportedException();
        void ISearchView.ShowItemContextualMenu(SearchItem item, Rect contextualActionPosition) => throw new NotSupportedException();
        void ISearchView.SelectSearch() => throw new NotSupportedException();
        void ISearchView.FocusSearch() => throw new NotSupportedException();
        void ISearchView.SetColumns(IEnumerable<SearchColumn> columns) => throw new NotSupportedException();
        EntityId ISearchView.GetViewId() => throw new NotSupportedException();
        IEnumerable<IGroup> ISearchView.EnumerateGroups() => throw new NotSupportedException();
        void ISearchView.SetupColumns(IList<SearchField> fields) => throw new NotSupportedException();
        void IDisposable.Dispose() => throw new NotSupportedException();
        void ISearchView.Focus() => throw new NotSupportedException();
        void ISearchView.Close() => throw new NotSupportedException();
        #endregion
    }
}
