// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class SearchResultView : ISearchView
    {
        private readonly IResultView m_View;
        private readonly List<int> m_Selection = new List<int>();
        private ISearchList m_Results;

        public ISearchList results
        {
            get => m_Results;
            set => m_Results = value;
        }

        public SearchContext context => m_Results.context;
        public SearchSelection selection => new SearchSelection(m_Selection, results);
        public float itemIconSize { get; set; } = 1f;
        public DisplayMode displayMode => DisplayMode.List;
        public bool multiselect { get; set; } = false;
        public Action<SearchItem, bool> selectCallback => OnItemSelected;
        public Func<SearchItem, bool> filterCallback => null;
        public Action<SearchItem> trackingCallback => null;

        public SearchResultView(ISearchList results)
        {
            m_Results = results;
            m_View = new ListView(this);
        }

        public void AddSelection(params int[] newSelection)
        {
            m_Selection.AddRange(newSelection.Where(s => !m_Selection.Contains(s)));
        }

        public void ExecuteAction(SearchAction action, SearchItem[] items, bool endSearch = true)
        {
            selectCallback?.Invoke(items.FirstOrDefault(), false);
        }

        public void Focus()
        {
            // Not needed
        }

        public void Refresh()
        {
            Repaint();
        }

        public void Repaint()
        {
            // Not needed
        }

        public void SetSearchText(string searchText, TextCursorPlacement moveCursor = TextCursorPlacement.MoveLineEnd)
        {
            context.searchText = searchText;
            Refresh();
        }

        public void SetSelection(params int[] newSelection)
        {
            m_Selection.Clear();
            m_Selection.AddRange(newSelection);
        }

        public void ShowItemContextualMenu(SearchItem item, Rect contextualActionPosition)
        {
            // Nothing to do.
        }

        public void Close()
        {
            // Nothing to do.
        }

        private void OnItemSelected(SearchItem selectedItem, bool canceled)
        {
            if (selectedItem == null || canceled)
                return;

            var provider = selectedItem.provider;
            var selectAction = provider.actions.FirstOrDefault(a => a.id == "select");
            if (selectAction != null && selectAction.handler != null)
                selectAction.handler(selectedItem);
            else if (selectAction != null && selectAction.execute != null)
                selectAction.execute(new SearchItem[] { selectedItem });
            else
                selectedItem.provider?.trackSelection?.Invoke(selectedItem, context);
        }

        public void OnGUI(float width)
        {
            results.context.searchView = this;
            if (results.Count > 0)
            {
                m_View.HandleInputEvent(Event.current, m_Selection);
                m_View.Draw(m_Selection, width);
            }
            else
            {
                GUILayout.Label("No results");
            }
        }

        public void OnGUI(Rect previewArea)
        {
            results.context.searchView = this;
            if (results.Count > 0)
            {
                m_View.HandleInputEvent(Event.current, m_Selection);
                // GUI.Box(previewArea, string.Empty, Styles.resultview);
                m_View.Draw(previewArea, m_Selection);
            }
            else
            {
                GUI.Label(previewArea, "No results");
            }
        }

        public void Draw(Rect previewArea)
        {
            if (results.Count > 0)
            {
                results.context.searchView = this;
                m_View.Draw(previewArea, m_Selection);
            }
        }

        public string GetPreviewString()
        {
            if (results.pending)
                return $"Still searching but we've already found {results.Count} results...";
            if (results.Count == 0)
                return "No result";
            return $"Found {results.Count} results.";
        }

        public void SelectSearch()
        {
            // Nothing to select
        }

        public void Dispose()
        {
            // Not needed
        }
    }

    class SearchResultViewContainer : IMGUIContainer
    {
        SearchResultView m_ResultView;

        public SearchResultViewContainer(ISearchList results)
        {
            m_ResultView = new SearchResultView(results);
            style.overflow = Overflow.Hidden;
            onGUIHandler = () => m_ResultView.OnGUI(resolvedStyle.width);
        }

        public void Repaint()
        {
            panel?.visualTree.MarkDirtyRepaint();
        }

        public void Refresh()
        {
            Repaint();
        }

        public SearchResultView resultView => m_ResultView;
    }
}
