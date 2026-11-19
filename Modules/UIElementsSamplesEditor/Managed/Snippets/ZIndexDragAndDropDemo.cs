// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class ZIndexDragAndDropDemo : ElementSnippet<ZIndexDragAndDropDemo>
    {
        internal override void Apply(VisualElement container)
        {
            container.AddToClassList(EditorGUIUtility.isProSkin ? "dark" : "light");

            Initialize(container);
        }

        /// <sample>
        #region sample
        VisualElement m_DragArea;
        VisualElement m_DraggedItem;
        Vector2 m_PointerOffsetInDragArea;
        int m_PointerId;

        void Initialize(VisualElement root)
        {
            m_DragArea = root.Q("drag-area");

            var grid = root.Q("inventory-grid");
            foreach (var slot in grid.Children())
            {
                var item = slot.ElementAt(0);
                item.RegisterCallback<PointerDownEvent>(evt => BeginDrag(evt, item));
            }
        }

        void BeginDrag(PointerDownEvent evt, VisualElement item)
        {
            if (m_DraggedItem != null) return;

            m_DraggedItem = item;
            m_PointerId = evt.pointerId;
            m_PointerOffsetInDragArea = m_DragArea.WorldToLocal(evt.position);

            item.AddToClassList("dragging");
            item.CapturePointer(evt.pointerId);

            item.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            item.RegisterCallback<PointerUpEvent>(OnPointerUp);
            item.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            item.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (m_DraggedItem == null) return;
            var localMouse = m_DragArea.WorldToLocal(evt.position);
            m_DraggedItem.style.left = localMouse.x - m_PointerOffsetInDragArea.x;
            m_DraggedItem.style.top = localMouse.y - m_PointerOffsetInDragArea.y;
        }

        void OnPointerUp(PointerUpEvent evt) => EndDrag();

        void OnPointerCancel(PointerCancelEvent evt) => EndDrag();

        void OnPointerCaptureOut(PointerCaptureOutEvent evt) => EndDrag();

        void EndDrag()
        {
            if (m_DraggedItem == null) return;

            var item = m_DraggedItem;
            m_DraggedItem = null;

            item.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            item.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            item.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            item.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

            if (item.HasPointerCapture(m_PointerId))
                item.ReleasePointer(m_PointerId);

            item.RemoveFromClassList("dragging");
            item.style.left = StyleKeyword.Null;
            item.style.top = StyleKeyword.Null;
        }
        #endregion
        /// </sample>
    }
}
