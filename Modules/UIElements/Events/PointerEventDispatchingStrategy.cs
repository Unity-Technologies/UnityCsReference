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

            bool shouldRecomputeTopElementUnderPointer = true;
            if (evt is IPointerEventInternal)
            {
                shouldRecomputeTopElementUnderPointer =
                    ((IPointerEventInternal)pointerEvent).recomputeTopElementUnderPointer;
            }

            elementUnderPointer = shouldRecomputeTopElementUnderPointer
                ? basePanel?.Pick(pointerEvent.position)
                : basePanel?.GetTopElementUnderPointer(pointerEvent.pointerId);


            if (basePanel != null && shouldRecomputeTopElementUnderPointer)
            {
                basePanel.SetElementUnderPointer(elementUnderPointer, evt);
            }
        }
    }
}
