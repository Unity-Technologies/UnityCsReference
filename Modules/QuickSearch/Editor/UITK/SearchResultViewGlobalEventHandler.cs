// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    sealed class SearchResultViewGlobalEventHandler
    {
        public delegate void AddToSelectionHandler(ReadOnlySpan<int> newSelection); // Using delegate to be able to use ReadOnlySpan
        public SearchElement ResultView { get; set; }
        public VisualElement TargetEventHandler { get; set; }
        public Func<IKeyboardEvent, bool> IsValidKey { get; set; }
        public Func<int> GetCurrentIndex { get; set; }
        public Func<int> GetItemCount { get; set; }
        public Action<int> SetSelectedIndex { get; set; }
        public Func<int, bool> SelectionContains { get; set; }
        public AddToSelectionHandler AddToSelection { get; set; }
        public Action<int> RemoveFromSelection { get; set; }
        public Action<int> Frame { get; set; }
        public Func<int> GetVisibleItemCount { get; set; }
        public Func<KeyDownEvent, KeyDownEvent> GenerateLocalKeyDownEvent { get; set; }

        public SearchResultViewGlobalEventHandler(SearchElement resultView,
            VisualElement targetEventHandler,
            Func<IKeyboardEvent, bool> isValidKey,
            Func<int> getCurrentIndex,
            Func<int> getItemCount,
            Action<int> setSelectedIndex,
            Func<int, bool> selectionContains,
            AddToSelectionHandler addToSelection,
            Action<int> removeFromSelection,
            Action<int> frame,
            Func<int> getVisibleItemCount,
            Func<KeyDownEvent, KeyDownEvent> generateLocalKeyDownEvent)
        {
            ResultView = resultView;
            TargetEventHandler = targetEventHandler;
            IsValidKey = isValidKey ?? DefaultIsValidKey;
            GetCurrentIndex = getCurrentIndex ?? (() => -1);
            GetItemCount = getItemCount ?? (() => 0);
            SetSelectedIndex = setSelectedIndex ?? (i => { });
            SelectionContains = selectionContains ?? (i => false);
            AddToSelection = addToSelection ?? (ints => { });
            RemoveFromSelection = removeFromSelection ?? (i => { });
            Frame = frame ?? (i => { });
            GetVisibleItemCount = getVisibleItemCount ?? (() => 0);
            GenerateLocalKeyDownEvent = generateLocalKeyDownEvent ?? DefaultGenerateLocalKeyDownEvent;
        }

        public SearchResultViewGlobalEventHandler(SearchElement resultView, VisualElement targetEventHandler)
            : this(resultView, targetEventHandler, null, null, null,
                null, null, null, null, null,
                null, null)
        { }

        public void RegisterGlobalEventHandlers()
        {
            ResultView.RegisterGlobalEventHandler<KeyDownEvent>(OnGlobalKeyDownEvent, 20);
            ResultView.RegisterGlobalEventHandler<NavigationSubmitEvent>(OnGlobalNavigationSubmitEvent, 20);
        }

        public void UnregisterGlobalEventHandler()
        {
            ResultView.UnregisterGlobalEventHandler<KeyDownEvent>(OnGlobalKeyDownEvent);
            ResultView.UnregisterGlobalEventHandler<NavigationSubmitEvent>(OnGlobalNavigationSubmitEvent);
        }

        SearchGlobalEventHandlerResult OnGlobalNavigationSubmitEvent(NavigationSubmitEvent evt)
        {
            if (evt.target is not VisualElement ve)
                return false;

            if (ve != ResultView && !ResultView.Contains(ve))
                return false;

            var currentIndex = GetCurrentIndex();
            if (currentIndex == -1)
                return false;

            ExecuteActionForSelection(ResultView.viewModel.selection, executeSecondary: evt.altKey);
            return true;
        }

        SearchGlobalEventHandlerResult OnGlobalKeyDownEvent(KeyDownEvent evt)
        {
            if (evt.target is not VisualElement ve || !IsValidKey(evt))
                return false;

            var currentIndex = GetCurrentIndex();
            var itemCount = GetItemCount();

            // If the event target is from the Result View, do not handle it.
            if (ve == ResultView || ResultView.Contains(ve))
                return false;

            if ((evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) && currentIndex != -1)
            {
                ExecuteActionForSelection(ResultView.viewModel.selection, executeSecondary: evt.altKey);
                return true;
            }

            // UITK collections have a selection, and can handle the event directly
            if (currentIndex != -1)
            {
                using var pooled = GenerateLocalKeyDownEvent(evt);
                pooled.target = TargetEventHandler;
                pooled.propagationPhase = PropagationPhase.BubbleUp;
                TargetEventHandler.SendEvent(pooled, DispatchMode.Immediate);
                return new SearchGlobalEventHandlerResult(true, true);
            }

            var nextSelectedIndex = -1;
            var visibleItemCount = GetVisibleItemCount();
            switch (evt.keyCode)
            {
                case KeyCode.DownArrow:
                    nextSelectedIndex = 0;
                    break;
                case KeyCode.UpArrow:
                    nextSelectedIndex = itemCount - 1;
                    break;
                case KeyCode.PageDown:
                {
                    if (itemCount > 0 && visibleItemCount > 0)
                        nextSelectedIndex = visibleItemCount - 1;
                    break;
                }
                case KeyCode.PageUp:
                {
                    if (itemCount > 0)
                        nextSelectedIndex = itemCount - 1;
                    break;
                }
            }

            return VerifySelectionChanged(currentIndex, nextSelectedIndex, evt);
        }

        void ExecuteActionForSelection(SearchSelection selection, bool executeSecondary)
        {
            var action = executeSecondary ? SearchView.GetSecondaryAction(selection, null) : SearchView.GetDefaultAction(selection, null);
            ResultView.viewModel.ExecuteAction(action, selection.ToArray(), true);
        }

        static bool DefaultIsValidKey(IKeyboardEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                case KeyCode.DownArrow:
                case KeyCode.UpArrow:
                case KeyCode.PageUp:
                case KeyCode.PageDown:
                    return true;
            }
            return false;
        }

        bool VerifySelectionChanged(int currentIndex, int nextSelectedIndex, EventBase evt)
        {
            var selectionHasChanged = currentIndex != nextSelectedIndex;
            if (selectionHasChanged && nextSelectedIndex != -1)
            {
                var shiftKey = evt is KeyDownEvent { shiftKey: true } or INavigationEvent { shiftKey: true };
                if (!shiftKey)
                    SetSelectedIndex(nextSelectedIndex);
                else
                {
                    if (!SelectionContains(nextSelectedIndex))
                    {
                        var count = Math.Abs(nextSelectedIndex - currentIndex);
                        using var pool = new RentSpanUnmanaged<int>(count);
                        var index = 0;
                        if (nextSelectedIndex > currentIndex)
                        {
                            for (int i = ++currentIndex; i <= nextSelectedIndex; ++i)
                                pool.Span[index++] = i;
                        }
                        else
                        {
                            for (int i = --currentIndex; i >= nextSelectedIndex; --i)
                                pool.Span[index++] = i;
                        }
                        AddToSelection(pool.Span);
                    }
                    else
                    {
                        if (nextSelectedIndex > currentIndex)
                        {
                            for (int i = currentIndex; i < nextSelectedIndex; ++i)
                                RemoveFromSelection(i);
                        }
                        else
                        {
                            for (int i = currentIndex; i > nextSelectedIndex; --i)
                                RemoveFromSelection(i);
                        }

                        RemoveFromSelection(currentIndex);
                    }
                }

                Frame(nextSelectedIndex);
            }

            return selectionHasChanged;
        }

        static KeyDownEvent DefaultGenerateLocalKeyDownEvent(KeyDownEvent evt)
        {
            return KeyboardEventBase<KeyDownEvent>.GetPooled(evt.character, evt.keyCode, evt.modifiers);
        }
    }
}
