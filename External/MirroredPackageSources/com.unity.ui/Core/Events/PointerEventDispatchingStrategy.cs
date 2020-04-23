namespace UnityEngine.UIElements
{
    class PointerEventDispatchingStrategy : IEventDispatchingStrategy
    {
        public bool CanDispatchEvent(EventBase evt)
        {
            return evt is IPointerEvent;
        }

        public virtual void DispatchEvent(EventBase evt, IPanel panel)
        {
            SetBestTargetForEvent(evt, panel);
            SendEventToTarget(evt);
            evt.stopDispatch = true;
        }

        static void SendEventToTarget(EventBase evt)
        {
            if (evt.target != null)
            {
                EventDispatchUtilities.PropagateEvent(evt);
            }
        }

        static void SetBestTargetForEvent(EventBase evt, IPanel panel)
        {
            UpdateElementUnderPointer(evt, panel, out VisualElement elementUnderPointer);

            if (evt.target == null && elementUnderPointer != null)
            {
                evt.propagateToIMGUI = false;
                evt.target = elementUnderPointer;
            }
            else if (evt.target == null && elementUnderPointer == null)
            {
                // Event occured outside the window.
                // Send event to visual tree root and
                // don't modify evt.propagateToIMGUI.
                evt.target = panel?.visualTree;
            }
            else if (evt.target != null)
            {
                evt.propagateToIMGUI = false;
            }
        }

        static void UpdateElementUnderPointer(EventBase evt, IPanel panel, out VisualElement elementUnderPointer)
        {
            IPointerEvent pointerEvent = evt as IPointerEvent;

            BaseVisualElementPanel basePanel = panel as BaseVisualElementPanel;

            bool shouldRecomputeTopElementUnderPointer = (evt as IPointerEventInternal)?.recomputeTopElementUnderPointer ?? true;

            elementUnderPointer = shouldRecomputeTopElementUnderPointer
                ? basePanel?.RecomputeTopElementUnderPointer(pointerEvent.position, evt)
                : basePanel?.GetTopElementUnderPointer(pointerEvent.pointerId);
        }
    }
}
