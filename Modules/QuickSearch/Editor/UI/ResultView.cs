// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Search
{
    [Serializable]
    class ResultViewState
    {
        public bool isValid => displayMode != DisplayMode.None;
        public float itemSize;
        public DisplayMode displayMode;
        public string group;
        public SearchTable tableConfig;

        public ResultViewState(DisplayMode mode, SearchTable tableConfig)
        {
            displayMode = mode;
            itemSize = (float)mode;
            group = null;
            this.tableConfig = tableConfig;
        }

        public ResultViewState(DisplayMode mode, float itemSize)
        {
            displayMode = mode;
            this.itemSize = itemSize;
            group = null;
            tableConfig = null;
        }
    }

    /// <summary>
    /// A view able to display a <see cref="ISearchList"/> of <see cref="SearchItem"/>s.
    /// </summary>
    interface IResultView : IDisposable
    {
        ISearchList items { get; }
        float itemSize { get; }
        Vector2 scrollPosition { get; }
        ISearchView searchView { get; }
        Rect rect { get; }

        bool focusSelectedItem { get; set; }
        bool scrollbarVisible { get; }

        void Draw(Rect rect, ICollection<int> selection);
        void Draw(ICollection<int> selection, float viewWidth);
        int GetDisplayItemCount();
        void HandleInputEvent(Event evt, List<int> selection);
        void DrawControlLayout(float viewWidth);
        void Refresh(RefreshFlags flags = RefreshFlags.Default);
        ResultViewState SaveViewState(string name);
        void SetViewState(ResultViewState viewState);

        void OnGroupChanged(string prevGroupId, string newGroupId);
    }

    abstract class ResultView : IResultView
    {
        const int k_ResetSelectionIndex = -1;

        protected Vector2 m_ScrollPosition;
        protected bool m_PrepareDrag;
        protected Vector3 m_DragStartPosition;
        protected int m_MouseDownItemIndex;
        protected double m_ClickTime = 0;
        protected Rect m_DrawItemsRect = Rect.zero;

        public ResultView(ISearchView hostView)
        {
            searchView = hostView;
        }

        public ISearchList items => searchView.results;
        public float itemSize => searchView.itemIconSize;
        public Vector2 scrollPosition { get => m_ScrollPosition; set => m_ScrollPosition = value; }
        public ISearchView searchView { get; private set; }
        public SearchContext context => searchView.context;
        public Rect rect => m_DrawItemsRect;
        public bool focusSelectedItem { get; set; }
        protected bool compactView => itemSize == 0;
        public bool scrollbarVisible { get; protected set; }

        public abstract void Draw(Rect rect, ICollection<int> selection);

        public virtual int GetDisplayItemCount()
        {
            return items?.Count ?? 0;
        }

        public void Draw(ICollection<int> selection, float viewWidth)
        {
            GUILayout.Box(string.Empty, Styles.resultview, GUILayout.ExpandHeight(true), GUILayout.Width(viewWidth));
            if (Event.current.type == EventType.Repaint)
                m_DrawItemsRect = GUILayoutUtility.GetLastRect();

            Draw(m_DrawItemsRect, selection);
        }

        public virtual void DrawControlLayout(float viewWidth)
        {
            // Do nothing
        }

        public virtual void OnGroupChanged(string prevGroupId, string newGroupId)
        {
            // Do nothing
        }

        public virtual ResultViewState SaveViewState(string name)
        {
            return new ResultViewState(searchView.displayMode, itemSize);
        }

        public virtual void SetViewState(ResultViewState viewState)
        {
            // Do nothing
        }

        protected bool IsDragClicked(Event evt)
        {
            if (evt.type != EventType.DragExited)
                return false;
            return IsDragClicked(evt.mousePosition);
        }

        protected bool IsDragClicked(Vector2 mousePosition)
        {
            var dragDistance = Vector2.Distance(m_DragStartPosition, mousePosition);
            return dragDistance < 15;
        }

        protected void HandleMouseDown(int clickedItemIndex)
        {
            var evt = Event.current;
            m_PrepareDrag = true;
            m_MouseDownItemIndex = clickedItemIndex;
            m_DragStartPosition = evt.mousePosition;

            if (AutoComplete.IsHovered(evt.mousePosition))
                return;

            if (evt.button == 1)
            {
                var item = items.ElementAt(clickedItemIndex);
                if (!searchView.selection.Contains(item))
                    searchView.SetSelection(clickedItemIndex);
            }
        }

        protected void HandleMouseUp(int clickedItemIndex, int itemTotalCount)
        {
            var evt = Event.current;
            var mouseDownItemIndex = m_MouseDownItemIndex;
            m_MouseDownItemIndex = -1;

            if (AutoComplete.IsHovered(evt.mousePosition))
                return;

            if (clickedItemIndex >= 0 && clickedItemIndex < itemTotalCount)
            {
                if (evt.button == 0 && mouseDownItemIndex == clickedItemIndex)
                {
                    var ctrl = evt.control || evt.command;
                    var now = EditorApplication.timeSinceStartup;
                    if (searchView.multiselect && evt.modifiers.HasAny(EventModifiers.Shift))
                    {
                        int min = searchView.selection.MinIndex();
                        int max = searchView.selection.MaxIndex();

                        if (clickedItemIndex > min)
                        {
                            if (ctrl && clickedItemIndex > max)
                            {
                                var r = 0;
                                var range = new int[clickedItemIndex - max];
                                for (int i = max + 1; i <= clickedItemIndex; ++i)
                                    range[r++] = i;
                                searchView.AddSelection(range);
                            }
                            else
                            {
                                var r = 0;
                                var range = new int[clickedItemIndex - min + 1];
                                for (int i = min; i <= clickedItemIndex; ++i)
                                    range[r++] = i;
                                searchView.SetSelection(range);
                            }
                        }
                        else if (clickedItemIndex < max)
                        {
                            if (ctrl && clickedItemIndex < min)
                            {
                                var r = 0;
                                var range = new int[min - clickedItemIndex];
                                for (int i = min - 1; i >= clickedItemIndex; --i)
                                    range[r++] = i;
                                searchView.AddSelection(range);
                            }
                            else
                            {
                                var r = 0;
                                var range = new int[max - clickedItemIndex + 1];
                                for (int i = max; i >= clickedItemIndex; --i)
                                    range[r++] = i;
                                searchView.SetSelection(range);
                            }
                        }
                    }
                    else if (searchView.multiselect && ctrl)
                    {
                        searchView.AddSelection(clickedItemIndex);
                    }
                    else
                        searchView.SetSelection(clickedItemIndex);

                    if ((now - m_ClickTime) < 0.3)
                    {
                        var item = items.ElementAt(clickedItemIndex);
                        if (item.provider.actions.Count > 0)
                            searchView.ExecuteAction(item.provider.actions[0], new[] {item}, !SearchSettings.keepOpen);
                        GUIUtility.ExitGUI();
                    }
                    SearchField.Focus();
                    evt.Use();
                    m_ClickTime = now;
                }
                else if (evt.button == 1)
                {
                    var item = items.ElementAt(clickedItemIndex);
                    var contextRect = new Rect(evt.mousePosition, new Vector2(1, 1));
                    var selection = searchView.selection;
                    if (selection.Count <= 1)
                        searchView.ShowItemContextualMenu(item, contextRect);
                }
            }

            // Reset drag content
            m_PrepareDrag = false;
            DragAndDrop.PrepareStartDrag();
        }

        protected void HandleMouseDrag(int dragIndex, int itemTotalCount)
        {
            if (IsDragClicked(Event.current.mousePosition))
                return;
            if (dragIndex >= 0 && dragIndex < itemTotalCount)
            {
                var item = items.ElementAt(dragIndex);
                if (item.provider?.startDrag != null)
                {
                    if (searchView is QuickSearch search)
                    {
                        search.SendEvent(SearchAnalytics.GenericEventType.QuickSearchDragItem, item.provider.id);
                    }
                    item.provider.startDrag(item, searchView.context);
                    m_PrepareDrag = false;

                    Event.current.Use();
                }
            }
        }

        protected virtual void HandleKeyEvent(Event evt, List<int> selection)
        {
            var ctrl = evt.control || evt.command;
            var selectedIndex = selection.Count == 0 ? k_ResetSelectionIndex : selection.Last();
            var firstIndex = selection.Count == 0 ? k_ResetSelectionIndex : selection.First();
            var lastIndex = selectedIndex;
            var multiselect = searchView.multiselect;
            var results = searchView.results;
            if (evt.keyCode == KeyCode.DownArrow)
            {
                if (multiselect && evt.modifiers.HasAny(EventModifiers.Shift) && selection.Count > 0)
                {
                    if (lastIndex >= firstIndex)
                    {
                        if (lastIndex < results.Count - 1)
                            searchView.AddSelection(lastIndex + 1);
                    }
                    else
                        selection.Remove(lastIndex);
                }
                else
                {
                    if (lastIndex < results.Count - 1)
                        searchView.SetSelection(selectedIndex + 1);
                    else
                        searchView.SetSelection(0);
                }
                evt.Use();
            }
            else if (evt.keyCode == KeyCode.UpArrow)
            {
                if (selectedIndex >= 0)
                {
                    if (multiselect && evt.modifiers.HasAny(EventModifiers.Shift) && selection.Count > 0)
                    {
                        if (firstIndex < lastIndex)
                            selection.Remove(lastIndex);
                        else if (lastIndex > 0)
                            searchView.AddSelection(lastIndex - 1);
                    }
                    else
                    {
                        if (selectedIndex > 0)
                            searchView.SetSelection(selectedIndex - 1);
                        else
                            searchView.SetSelection(results.Count - 1);
                    }
                    evt.Use();
                }
            }
            else if (ctrl && evt.keyCode == KeyCode.End || evt.keyCode == KeyCode.PageDown)
            {
                HandlePageDown(evt, selection);
            }
            else if (ctrl && evt.keyCode == KeyCode.Home || evt.keyCode == KeyCode.PageUp)
            {
                HandlePageUp(evt, selection);
            }
            else if (evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Return)
            {
                if (selectedIndex == -1 && results.Count > 0)
                    selectedIndex = 0;

                if (selectedIndex != -1)
                {
                    var item = results.ElementAt(selectedIndex);
                    if (item.provider.actions.Count > 0)
                    {
                        SearchAction action = item.provider.actions[0];
                        if (evt.modifiers.HasAny(EventModifiers.Alt))
                        {
                            var actionIndex = 1;
                            if (evt.modifiers.HasAny(EventModifiers.Control))
                            {
                                actionIndex = 2;
                                if (evt.modifiers.HasAny(EventModifiers.Shift))
                                    actionIndex = 3;
                            }
                            action = item.provider.actions[Math.Max(0, Math.Min(actionIndex, item.provider.actions.Count - 1))];
                        }

                        if (action != null)
                        {
                            evt.Use();
                            searchView.ExecuteAction(action, searchView.selection.ToArray(), !SearchSettings.keepOpen);
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
            else if (!EditorGUIUtility.editingTextField)
                SearchField.Focus();

            var newSelection = selection.Count == 0 ? k_ResetSelectionIndex : selection.Last();
            if (selectedIndex != newSelection)
                focusSelectedItem = true;
        }

        protected virtual void HandlePageDown(Event evt, List<int> selection)
        {
            var ctrl = evt.control || evt.command;
            var jumpAtIndex = k_ResetSelectionIndex;

            if (evt.keyCode == KeyCode.PageDown)
            {
                jumpAtIndex = GetLastVisibleItemIndex();
                if (selection.Count > 0 && selection.Last() == jumpAtIndex)
                    jumpAtIndex += GetDisplayItemCount();
                jumpAtIndex = Math.Min(jumpAtIndex, searchView.results.Count - 1);
            }
            else if (ctrl && evt.keyCode == KeyCode.End)
            {
                jumpAtIndex = searchView.results.Count - 1;
            }

            if (searchView.multiselect && evt.modifiers.HasAny(EventModifiers.Shift) && selection.Count > 0)
            {
                var selectedIndex = selection.Count == 0 ? k_ResetSelectionIndex : selection.First();
                searchView.SetSelection(GenerateSelectionRange(selectedIndex, jumpAtIndex));
            }
            else
            {
                searchView.SetSelection(jumpAtIndex);
            }
            evt.Use();
        }

        protected virtual void HandlePageUp(Event evt, List<int> selection)
        {
            var ctrl = evt.control || evt.command;
            var jumpAtIndex = k_ResetSelectionIndex;

            if (evt.keyCode == KeyCode.PageUp)
            {
                jumpAtIndex = GetFirstVisibleItemIndex();
                if (selection.Count > 0 && selection.Last() == jumpAtIndex)
                    jumpAtIndex -= GetDisplayItemCount();
                jumpAtIndex = Math.Max(0, jumpAtIndex);
            }
            else if (ctrl && evt.keyCode == KeyCode.Home)
            {
                jumpAtIndex = 0;
            }

            if (searchView.multiselect && evt.modifiers.HasAny(EventModifiers.Shift) && selection.Count > 0)
            {
                var selectedIndex = selection.Count == 0 ? k_ResetSelectionIndex : selection.First();
                searchView.SetSelection(GenerateSelectionRange(selectedIndex, jumpAtIndex));
            }
            else
            {
                searchView.SetSelection(jumpAtIndex);
            }
            evt.Use();
        }

        private int[] GenerateSelectionRange(int first, int last)
        {
            int r = 0;
            var diff = last - first;
            var range = new int[Math.Abs(diff) + 1];
            if (diff >= 0)
            {
                for (int i = first; i <= last; ++i)
                    range[r++] = i;
            }
            else
            {
                for (int i = first; i >= last; --i)
                    range[r++] = i;
            }
            return range;
        }

        protected virtual float GetRowHeight()
        {
            return compactView ? Styles.itemRowHeight / 2.0f : Styles.itemRowHeight;
        }

        protected virtual int GetFirstVisibleItemIndex()
        {
            var rowHeight = GetRowHeight();
            return Math.Max(0, Mathf.CeilToInt(m_ScrollPosition.y / rowHeight));
        }

        protected virtual int GetLastVisibleItemIndex()
        {
            var firstVisibleIndex = GetFirstVisibleItemIndex();
            var itemVisibleCount = GetDisplayItemCount();
            return firstVisibleIndex + itemVisibleCount - 1;
        }

        public void HandleInputEvent(Event evt, List<int> selection)
        {
            if (evt.type == EventType.KeyDown)
                HandleKeyEvent(evt, selection);
        }

        public virtual void Refresh(RefreshFlags flags)
        {
            // Nothing to refresh by default
        }

        public virtual void Dispose()
        {
        }
    }
}
