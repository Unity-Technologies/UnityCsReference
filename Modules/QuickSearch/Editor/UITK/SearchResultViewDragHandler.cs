// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    sealed class SearchResultViewDragHandler
    {
        readonly ISearchView m_ViewModel;
        readonly VisualElement m_DragSourceElement;

        bool m_InitiateDrag;
        Vector3 m_InitiateDragPosition;
        SearchItem m_DraggedItem;

        public Func<PointerDownEvent, bool> CanStartDrag { get; set; }
        public Func<PointerDownEvent, SearchItem> GetDraggedItem { get; set; }
        public Action<SearchItem> StartDrag { get; set; }

        public SearchResultViewDragHandler(ISearchView viewModel, VisualElement dragSourceElement)
        {
            m_ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            m_DragSourceElement = dragSourceElement ?? throw new ArgumentNullException(nameof(dragSourceElement));
        }

        public void RegisterDragCallbacks()
        {
            m_DragSourceElement.RegisterCallback<PointerDownEvent>(OnItemPointerDown);
            m_DragSourceElement.RegisterCallback<PointerUpEvent>(OnItemPointerUp);
            m_DragSourceElement.RegisterCallback<DragExitedEvent>(OnDragExited);
        }

        public void UnregisterDragCallbacks()
        {
            m_DragSourceElement.UnregisterCallback<PointerDownEvent>(OnItemPointerDown);
            m_DragSourceElement.UnregisterCallback<PointerUpEvent>(OnItemPointerUp);
            m_DragSourceElement.UnregisterCallback<DragExitedEvent>(OnDragExited);

            ResetDrag();
        }

        public void ResetDrag()
        {
            m_InitiateDrag = false;
            m_DraggedItem = null;
            m_DragSourceElement.UnregisterCallback<PointerMoveEvent>(OnItemPointerMove);
            m_DragSourceElement.UnregisterCallback<PointerLeaveEvent>(OnItemPointerLeave);
        }

        void OnItemPointerDown(PointerDownEvent evt)
        {
            // dragging is initiated only by left mouse clicks
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            var canDrag = !m_ViewModel.IsPicker() && (CanStartDrag?.Invoke(evt) ?? false);
            if (!canDrag)
                return;

            m_DraggedItem = GetDraggedItem?.Invoke(evt);
            m_InitiateDrag = true;
            m_InitiateDragPosition = evt.localPosition;

            m_DragSourceElement.UnregisterCallback<PointerMoveEvent>(OnItemPointerMove);
            m_DragSourceElement.RegisterCallback<PointerMoveEvent>(OnItemPointerMove);

            m_DragSourceElement.UnregisterCallback<PointerLeaveEvent>(OnItemPointerLeave);
            m_DragSourceElement.RegisterCallback<PointerLeaveEvent>(OnItemPointerLeave);
        }

        void OnItemPointerLeave(PointerLeaveEvent evt)
        {
            // If we enter here, it means the mouse left the element before any mouse
            // move, so the item jumped around to be repositioned in the window.
            // This will cause an issue with drag and drop
            ResetDrag();
        }

        void OnItemPointerMove(PointerMoveEvent evt)
        {
            if (!m_InitiateDrag)
                return;

            if ((evt.localPosition - m_InitiateDragPosition).sqrMagnitude < 5f)
                return;

            StartDrag?.Invoke(m_DraggedItem);
            ResetDrag();
        }

        void OnDragExited(DragExitedEvent evt) => ResetDrag();
        void OnItemPointerUp(PointerUpEvent evt) => ResetDrag();
    }
}
