// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    internal abstract class DragEventsProcessor
    {
        private bool m_CanStartDrag;
        private Vector3 m_Start;
        internal readonly VisualElement m_Target;

        private const int k_DistanceToActivation = 5;

        internal DragEventsProcessor(VisualElement target)
        {
            m_Target = target;

            m_Target.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            m_Target.RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
            m_Target.RegisterCallback<PointerLeaveEvent>(OnPointerLeaveEvent);

            m_Target.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
            m_Target.RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            m_Target.RegisterCallback<DragExitedEvent>(OnDragExitedEvent);
            m_Target.RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);

            m_Target.RegisterCallback<DetachFromPanelEvent>(UnregisterCallbacksFromTarget);
        }

        private void UnregisterCallbacksFromTarget(DetachFromPanelEvent evt)
        {
            m_Target.UnregisterCallback<MouseDownEvent>(OnMouseDownEvent);
            m_Target.UnregisterCallback<MouseUpEvent>(OnMouseUpEvent);
            m_Target.UnregisterCallback<PointerLeaveEvent>(OnPointerLeaveEvent);

            m_Target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdate);
            m_Target.UnregisterCallback<DragPerformEvent>(OnDragPerformEvent);
            m_Target.UnregisterCallback<DragExitedEvent>(OnDragExitedEvent);
            m_Target.UnregisterCallback<MouseMoveEvent>(OnMouseMoveEvent);

            m_Target.UnregisterCallback<DetachFromPanelEvent>(UnregisterCallbacksFromTarget);
        }

        protected abstract bool CanStartDrag(Vector3 pointerPosition);
        protected abstract StartDragArgs StartDrag(Vector3 pointerPosition);
        protected abstract DragVisualMode UpdateDrag(Vector3 pointerPosition);

        protected abstract void OnDrop(Vector3 pointerPosition);
        protected abstract void ClearDragAndDropUI();

        private void OnMouseDownEvent(MouseDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
            {
                m_CanStartDrag = false;
                return;
            }

            if (CanStartDrag(evt.mousePosition))
            {
                m_CanStartDrag = true;
                m_Start = evt.mousePosition;
            }
        }

        private void OnMouseUpEvent(MouseUpEvent evt)
        {
            m_CanStartDrag = false;
        }

        private void OnPointerLeaveEvent(PointerLeaveEvent evt)
        {
            if (evt.target == m_Target)
                ClearDragAndDropUI();
        }

        private void OnDragExitedEvent(DragExitedEvent evt)
        {
            ClearDragAndDropUI();
        }

        private void OnDragPerformEvent(DragPerformEvent evt)
        {
            m_CanStartDrag = false;
            OnDrop(evt.mousePosition);

            ClearDragAndDropUI();
            DragAndDropUtility.dragAndDrop.AcceptDrag();
        }

        private void OnDragUpdate(DragUpdatedEvent evt)
        {
            var visualMode = UpdateDrag(evt.mousePosition);
            DragAndDropUtility.dragAndDrop.SetVisualMode(visualMode);
        }

        private void OnMouseMoveEvent(MouseMoveEvent evt)
        {
            if (!m_CanStartDrag)
                return;

            if (Mathf.Abs(m_Start.x - evt.mousePosition.x) > k_DistanceToActivation ||
                Mathf.Abs(m_Start.y - evt.mousePosition.y) > k_DistanceToActivation)
            {
                var args = StartDrag(evt.mousePosition);
                DragAndDropUtility.dragAndDrop.StartDrag(args);
                m_CanStartDrag = false;
            }
        }
    }
}
