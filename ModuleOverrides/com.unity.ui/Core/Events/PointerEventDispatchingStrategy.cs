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
                // Event occurred outside the window.
                // Fix for case 1306631 - a MouseUp event received outside of the GameView
                // is re-directed to the DockArea IMGUIContainer. Otherwise send the event
                // to the visual tree root and don't modify evt.propagateToIMGUI, allowing
                // MouseLeaveWindow events may be received via trickle down traversal.
                if (panel?.contextType == ContextType.Editor && evt.eventTypeId == PointerUpEvent.TypeId())
                    evt.target = (panel as Panel)?.rootIMGUIContainer;
                else
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
                ? basePanel?.RecomputeTopElementUnderPointer(pointerEvent.pointerId, pointerEvent.position, evt)
                : basePanel?.GetTopElementUnderPointer(pointerEvent.pointerId);
        }
    }
}
