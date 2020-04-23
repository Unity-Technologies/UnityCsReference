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

            m_Target.RegisterCallback<PointerDownEvent>(OnPointerDownEvent);
            m_Target.RegisterCallback<PointerUpEvent>(OnPointerUpEvent);
            m_Target.RegisterCallback<PointerLeaveEvent>(OnPointerLeaveEvent);
            m_Target.RegisterCallback<PointerMoveEvent>(OnPointerMoveEvent);

            m_Target.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
            m_Target.RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            m_Target.RegisterCallback<DragExitedEvent>(OnDragExitedEvent);

            m_Target.RegisterCallback<DetachFromPanelEvent>(UnregisterCallbacksFromTarget);
        }

        private void UnregisterCallbacksFromTarget(DetachFromPanelEvent evt)
        {
            m_Target.UnregisterCallback<PointerDownEvent>(OnPointerDownEvent);
            m_Target.UnregisterCallback<PointerUpEvent>(OnPointerUpEvent);
            m_Target.UnregisterCallback<PointerLeaveEvent>(OnPointerLeaveEvent);

            m_Target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdate);
            m_Target.UnregisterCallback<DragPerformEvent>(OnDragPerformEvent);
            m_Target.UnregisterCallback<DragExitedEvent>(OnDragExitedEvent);
            m_Target.UnregisterCallback<PointerMoveEvent>(OnPointerMoveEvent);

            m_Target.UnregisterCallback<DetachFromPanelEvent>(UnregisterCallbacksFromTarget);
        }

        protected abstract bool CanStartDrag(Vector3 pointerPosition);
        protected abstract StartDragArgs StartDrag(Vector3 pointerPosition);
        protected abstract DragVisualMode UpdateDrag(Vector3 pointerPosition);

        protected abstract void OnDrop(Vector3 pointerPosition);
        protected abstract void ClearDragAndDropUI();

        private void OnPointerDownEvent(PointerDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
            {
                m_CanStartDrag = false;
                return;
            }

            if (CanStartDrag(evt.position))
            {
                m_CanStartDrag = true;
                m_Start = evt.position;
            }
        }

        private void OnPointerUpEvent(PointerUpEvent evt)
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

        private void OnPointerMoveEvent(PointerMoveEvent evt)
        {
            if (!m_CanStartDrag)
                return;

            if (Mathf.Abs(m_Start.x - evt.position.x) > k_DistanceToActivation ||
                Mathf.Abs(m_Start.y - evt.position.y) > k_DistanceToActivation)
            {
                var args = StartDrag(evt.position);
                DragAndDropUtility.dragAndDrop.StartDrag(args);
                m_CanStartDrag = false;
            }
        }
    }
}
