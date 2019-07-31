// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
            IPointerEvent pointerEvent = evt as IPointerEvent;

            BaseVisualElementPanel basePanel = panel as BaseVisualElementPanel;

            if (basePanel != null && pointerEvent != null)
            {
                bool shouldRecomputeTopElementUnderPointer =
                    ((IPointerEventInternal)pointerEvent).recomputeTopElementUnderPointer;

                VisualElement elementUnderPointer = shouldRecomputeTopElementUnderPointer ?
                    basePanel.Pick(pointerEvent.position) :
                    basePanel.GetTopElementUnderPointer(pointerEvent.pointerId);

                if (evt.target == null)
                {
                    evt.target = elementUnderPointer;
                }

                if (shouldRecomputeTopElementUnderPointer)
                {
                    basePanel.SetElementUnderPointer(elementUnderPointer, evt);
                }
            }

            if (evt.target != null)
            {
                evt.propagateToIMGUI = false;
                EventDispatchUtilities.PropagateEvent(evt);
            }

            evt.stopDispatch = true;
        }
    }
}
