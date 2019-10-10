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

            if (pointerEvent == null)
                return;

            BaseVisualElementPanel basePanel = panel as BaseVisualElementPanel;

            bool shouldRecomputeTopElementUnderPointer = true;
            if (evt is IPointerEventInternal)
            {
                shouldRecomputeTopElementUnderPointer =
                    ((IPointerEventInternal)pointerEvent).recomputeTopElementUnderPointer;
            }

            VisualElement elementUnderPointer = shouldRecomputeTopElementUnderPointer
                ? basePanel?.Pick(pointerEvent.position)
                : basePanel?.GetTopElementUnderPointer(pointerEvent.pointerId);

            if (evt.target == null && elementUnderPointer != null)
            {
                evt.propagateToIMGUI = false;
                evt.target = elementUnderPointer;
            }
            else if (evt.target == null && elementUnderPointer == null)
            {
                // Don't modify evt.propagateToIMGUI.
                evt.target = panel?.visualTree;
            }
            else if (evt.target != null)
            {
                evt.propagateToIMGUI = false;
            }

            if (basePanel != null && shouldRecomputeTopElementUnderPointer)
            {
                basePanel.SetElementUnderPointer(elementUnderPointer, evt);
            }

            if (evt.target != null)
            {
                EventDispatchUtilities.PropagateEvent(evt);
            }

            evt.stopDispatch = true;
        }
    }
}
