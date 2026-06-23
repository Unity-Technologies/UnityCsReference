// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    // Describes which payload fields a captured UIToolkitPanelEventInfo carries, so the reader knows
    // how to interpret them without re-deriving the concrete event type. The event's concrete type
    // name always travels out-of-band in the PANEL_EVENT_TYPE_NAMES chunk (indexed by the event's
    // eventNameIndex), so this enum only needs to encode the payload shape — not the type identity.
    // Exactly one kind per event: the shapes are mutually exclusive because the payload slots are
    // overloaded (buttonOrKeyCode is button OR keyCode OR direction, positionX/Y is pointer position
    // OR move vector), so a single event can only ever carry one shape. Stable wire format: values
    // are append-only, never reorder or repurpose existing values.
    [VisibleToOtherModules("UnityEditor.UIElementsModule")]
    internal enum UIToolkitProfilerEventKind : byte
    {
        // No interpretable payload (any event implementing none of the probed interfaces). Only
        // the type name is shown.
        None = 0,
        // button in buttonOrKeyCode + position in positionX/positionY. Set for IPointerEvent and
        // IMouseEvent (incl. WheelEvent) — both display identically as button + coordinates.
        Pointer = 1,
        // KeyCode in buttonOrKeyCode + character/modifiers packed into keyCharAndModifiers.
        Keyboard = 2,
        // EventModifiers in the high 16 bits of keyCharAndModifiers; no other payload. Set for an
        // INavigationEvent that isn't a NavigationMoveEvent (NavigationSubmitEvent,
        // NavigationCancelEvent).
        Navigation = 3,
        // NavigationMoveEvent: direction in buttonOrKeyCode + move vector in positionX/positionY +
        // EventModifiers in the high 16 bits of keyCharAndModifiers.
        NavigationMove = 4,
    }

    [VisibleToOtherModules("UnityEditor.UIElementsModule")]
    internal static class UIToolkitProfilerEventTypeMap
    {
        // Fills UIToolkitPanelEventInfo's payload slots from evt and returns the kind describing what
        // was written, so the reader can format it without knowing the concrete type:
        //   - IPointerEvent  → button + position (Vector3 panel coords; Z is the pointer depth) +
        //                      modifiers packed into keyCharAndModifiers (character bits stay 0)
        //   - IMouseEvent    → button + mousePosition (Vector2, Z stays 0) + modifiers; covers WheelEvent
        //   - IKeyboardEvent → keyCode in buttonOrKeyCode + character/modifiers packed into
        //                      keyCharAndModifiers
        //   - NavigationMoveEvent → direction in buttonOrKeyCode + move vector in positionX/positionY
        //                      + modifiers (high 16 bits of keyCharAndModifiers)
        //   - INavigationEvent → modifiers only (covers NavigationSubmitEvent / NavigationCancelEvent)
        //   - anything else  → fields untouched (caller passes a zeroed-out info), returns None
        // Probed via interfaces rather than an eventTypeId switch so a single branch covers every
        // pointer / mouse / keyboard / navigation event (e.g. KeyDownEvent and KeyUpEvent, or
        // MouseDownEvent and PointerEnterEvent) — no per-type whitelist to maintain. Order matters:
        // IPointerEvent before IMouseEvent because pointer events implement both IPointerOrMouseEvent
        // ancestors but carry richer (Vector3) position data; NavigationMoveEvent before the generic
        // INavigationEvent (which it also implements) so its direction + move payload is captured.
        public static UIToolkitProfilerEventKind PopulateEventInfo(EventBase evt, ref UIToolkitPanelEventInfo info)
        {
            if (evt is IPointerEvent pe)
            {
                info.buttonOrKeyCode = (uint)pe.button;
                info.positionX = pe.position.x;
                info.positionY = pe.position.y;
                info.positionZ = pe.position.z;
                info.keyCharAndModifiers = (uint)pe.modifiers << 16; // modifiers only (no character)
                return UIToolkitProfilerEventKind.Pointer;
            }
            if (evt is IMouseEvent me)
            {
                info.buttonOrKeyCode = (uint)me.button;
                info.positionX = me.mousePosition.x;
                info.positionY = me.mousePosition.y;
                info.keyCharAndModifiers = (uint)me.modifiers << 16; // modifiers only (no character)
                return UIToolkitProfilerEventKind.Pointer;
            }
            if (evt is IKeyboardEvent ke)
            {
                info.buttonOrKeyCode = (uint)ke.keyCode;
                // Pack character (UTF-16, low 16 bits) + modifiers (EventModifiers flags, high 16
                // bits) into one slot. Unpacked by PanelComponentsPaneController.FormatEventPayload.
                info.keyCharAndModifiers = (uint)ke.character | ((uint)ke.modifiers << 16);
                return UIToolkitProfilerEventKind.Keyboard;
            }
            if (evt is NavigationMoveEvent nme)
            {
                info.buttonOrKeyCode = (uint)nme.direction;
                info.positionX = nme.move.x;
                info.positionY = nme.move.y;
                info.keyCharAndModifiers = (uint)nme.modifiers << 16; // modifiers only (no character)
                return UIToolkitProfilerEventKind.NavigationMove;
            }
            if (evt is INavigationEvent ne)
            {
                info.keyCharAndModifiers = (uint)ne.modifiers << 16; // modifiers only (no character)
                return UIToolkitProfilerEventKind.Navigation;
            }
            return UIToolkitProfilerEventKind.None;
        }
    }
}
