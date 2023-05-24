// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections.Generic;
using Unity.IntegerTime;
using UnityEngine.InputForUI;

namespace UnityEngine.UIElements
{
    internal partial class DefaultEventSystem
    {
        private class InputForUIProcessor
        {
            private readonly DefaultEventSystem m_EventSystem;

            private DiscreteTime m_LastPointerTimestamp = DiscreteTime.Zero;
            private DiscreteTime m_NextPointerTimestamp = DiscreteTime.Zero;
            private readonly Queue<UnityEngine.InputForUI.Event> m_EventList = new Queue<UnityEngine.InputForUI.Event>();

            public InputForUIProcessor(DefaultEventSystem eventSystem)
            {
                m_EventSystem = eventSystem;
            }

            public void Reset()
            {
                m_LastPointerTimestamp = DiscreteTime.Zero;
                m_NextPointerTimestamp = DiscreteTime.Zero;
                m_EventList.Clear();
            }

            public bool OnEvent(in UnityEngine.InputForUI.Event ev)
            {
                m_EventList.Enqueue(ev);
                return true;
            }

            public void ProcessInputForUIEvents()
            {
                if (m_EventList.Count == 0)
                    return;

                // Keep track of what elements were focused before the start of the event sequence. If focus is changed
                // by some Key event, we need to still send Navigation events to the previously focused element.
                FocusBasedEventSequenceContext? focusContext = null;

                while (m_EventList.Count > 0)
                {
                    InputForUI.Event newEvent = m_EventList.Dequeue();

                    switch (newEvent.type)
                    {
                        case InputForUI.Event.Type.PointerEvent:
                            ProcessPointerEvent(newEvent.asPointerEvent);
                            break;
                        case InputForUI.Event.Type.KeyEvent:
                            focusContext ??= m_EventSystem.FocusBasedEventSequence();
                            ProcessKeyEvent(newEvent.asKeyEvent);
                            break;
                        case InputForUI.Event.Type.TextInputEvent:
                            focusContext ??= m_EventSystem.FocusBasedEventSequence();
                            ProcessTextInputEvent(newEvent.asTextInputEvent);
                            break;
                        case InputForUI.Event.Type.IMECompositionEvent:
                            focusContext ??= m_EventSystem.FocusBasedEventSequence();
                            ProcessIMECompositionEvent(newEvent.asIMECompositionEvent);
                            break;
                        case InputForUI.Event.Type.CommandEvent:
                            focusContext ??= m_EventSystem.FocusBasedEventSequence();
                            ProcessCommandEvent(newEvent.asCommandEvent);
                            break;
                        case InputForUI.Event.Type.NavigationEvent:
                            focusContext ??= m_EventSystem.FocusBasedEventSequence();
                            ProcessNavigationEvent(newEvent.asNavigationEvent);
                            break;
                        default:
                            if (m_EventSystem.verbose)
                                m_EventSystem.Log("Unsupported event (" + (int)newEvent.type + "): " + newEvent);
                            break;
                    }
                }

                focusContext?.Dispose();

                m_LastPointerTimestamp = m_NextPointerTimestamp;
            }

            EventModifiers GetModifiers(InputForUI.EventModifiers eventModifiers)
            {
                EventModifiers mod = EventModifiers.None;

                if (eventModifiers.isShiftPressed)
                {
                    mod |= EventModifiers.Shift;
                }
                if (eventModifiers.isCtrlPressed)
                {
                    mod |= EventModifiers.Control;
                }
                if (eventModifiers.isAltPressed)
                {
                    mod |= EventModifiers.Alt;
                }
                if (eventModifiers.isMetaPressed)
                {
                    mod |= EventModifiers.Command;
                }
                if (eventModifiers.isCapsLockEnabled)
                {
                    mod |= EventModifiers.CapsLock;
                }
                if (eventModifiers.isNumericPressed)
                {
                    mod |= EventModifiers.Numeric;
                }
                if (eventModifiers.isFunctionKeyPressed)
                {
                    mod |= EventModifiers.FunctionKey;
                }

                return mod;
            }

            void ProcessPointerEvent(PointerEvent pointerEvent)
            {
                var position = pointerEvent.position;
                var targetDisplay = pointerEvent.displayIndex;
                var deltaPosition = pointerEvent.deltaPosition;

                var pointerIdBase = pointerEvent.eventSource == EventSource.Touch ? PointerId.touchPointerIdBase :
                    pointerEvent.eventSource == EventSource.Pen ? PointerId.penPointerIdBase : PointerId.mousePointerId;
                var pointerId = pointerIdBase + pointerEvent.pointerIndex;

                var deltaTime = m_LastPointerTimestamp != DiscreteTime.Zero ? (float)(pointerEvent.timestamp - m_LastPointerTimestamp) : 0;
                m_NextPointerTimestamp = pointerEvent.timestamp;

                if (pointerEvent.type == PointerEvent.Type.PointerMoved)
                {
                    if (!Mathf.Approximately(deltaPosition.x, 0f) || !Mathf.Approximately(deltaPosition.y, 0f))
                    {
                        m_EventSystem.SendPositionBasedEvent(position, deltaPosition, pointerId, targetDisplay,
                            (panelPosition, panelDelta, t) => PointerMoveEvent.GetPooled(t.pointerEvent, panelPosition,
                                panelDelta, t.pointerId, t.deltaTime), (pointerEvent, pointerId, deltaTime));
                    }
                }
                else if (pointerEvent.type == PointerEvent.Type.ButtonPressed)
                {
                    m_EventSystem.SendPositionBasedEvent(position, deltaPosition, pointerId, targetDisplay,
                        (panelPosition, panelDelta, t) => PointerDownEvent.GetPooled(t.pointerEvent, panelPosition,
                            panelDelta, t.pointerId, t.deltaTime), (pointerEvent, pointerId, deltaTime));
                }
                else if (pointerEvent.type == PointerEvent.Type.ButtonReleased)
                {
                    m_EventSystem.SendPositionBasedEvent(position, deltaPosition, pointerId, targetDisplay,
                        (panelPosition, panelDelta, t) => PointerUpEvent.GetPooled(t.pointerEvent, panelPosition,
                            panelDelta, t.pointerId, t.deltaTime), (pointerEvent, pointerId, deltaTime), true);
                }
                else if (pointerEvent.type == PointerEvent.Type.TouchCanceled)
                {
                    m_EventSystem.SendPositionBasedEvent(position, deltaPosition, pointerId, targetDisplay,
                        (panelPosition, panelDelta, t) => PointerCancelEvent.GetPooled(t.pointerEvent, panelPosition,
                            panelDelta, t.pointerId, t.deltaTime), (pointerEvent, pointerId, deltaTime));
                }
                else if (pointerEvent.type == PointerEvent.Type.Scroll)
                {
                    var scrollDelta = pointerEvent.scroll;

                    if (m_EventSystem.verbose)
                        m_EventSystem.Log("ScrollDelta: " + scrollDelta);

                    m_EventSystem.SendPositionBasedEvent(pointerEvent.position, pointerEvent.deltaPosition,
                        PointerId.mousePointerId, targetDisplay,
                        (panelPosition, _, t) => WheelEvent.GetPooled(t.scrollDelta, panelPosition, t.modifiers),
                        (modifiers: GetModifiers(pointerEvent.eventModifiers), scrollDelta));
                }
                else
                {
                    if (m_EventSystem.verbose)
                        m_EventSystem.Log("Unsupported event " + pointerEvent);
                }
            }

            void ProcessNavigationEvent(NavigationEvent navigationEvent)
            {
                if (m_EventSystem.verbose)
                    m_EventSystem.Log(navigationEvent);

                var mod = GetModifiers(navigationEvent.eventModifiers);

                var deviceType = navigationEvent.eventSource == EventSource.Keyboard ? NavigationDeviceType.Keyboard :
                    navigationEvent.eventSource == EventSource.Unspecified ? NavigationDeviceType.Unknown :
                    NavigationDeviceType.NonKeyboard;

                if (navigationEvent.type == NavigationEvent.Type.Move)
                {
                    Vector2 move = Vector2.zero;

                    if (navigationEvent.direction == NavigationEvent.Direction.Left) move.x = -1;
                    else if (navigationEvent.direction == NavigationEvent.Direction.Right) move.x = 1;
                    else if (navigationEvent.direction == NavigationEvent.Direction.Up) move.y = 1;
                    else if (navigationEvent.direction == NavigationEvent.Direction.Down) move.y = -1;

                    if (move != Vector2.zero)
                    {
                        m_EventSystem.SendFocusBasedEvent(t => NavigationMoveEvent.GetPooled(t.move, t.deviceType, t.mod),
                            (move, deviceType, mod));
                    }
                    else
                    {
                        var direction = navigationEvent.direction == NavigationEvent.Direction.Previous
                            ? NavigationMoveEvent.Direction.Previous
                            : NavigationMoveEvent.Direction.Next;

                        m_EventSystem.SendFocusBasedEvent(t => NavigationMoveEvent.GetPooled(t.direction, t.deviceType, t.mod),
                            (direction, deviceType, mod));
                    }
                }
                else if (navigationEvent.type == NavigationEvent.Type.Submit)
                {
                    m_EventSystem.SendFocusBasedEvent(t => NavigationSubmitEvent.GetPooled(t.deviceType, t.mod), (deviceType, mod));
                }
                else if (navigationEvent.type == NavigationEvent.Type.Cancel)
                {
                    m_EventSystem.SendFocusBasedEvent(t => NavigationCancelEvent.GetPooled(t.deviceType, t.mod), (deviceType, mod));
                }
            }

            void ProcessKeyEvent(KeyEvent keyEvent)
            {
                if (m_EventSystem.verbose)
                    m_EventSystem.Log(keyEvent);

                if (keyEvent.type == KeyEvent.Type.KeyPressed || keyEvent.type == KeyEvent.Type.KeyRepeated)
                {
                    m_EventSystem.SendFocusBasedEvent(t => KeyDownEvent.GetPooled('\0', t.keyCode, t.modifiers),
                        (modifiers:GetModifiers(keyEvent.eventModifiers), keyEvent.keyCode));

                    // Don't process Tab event here. We trust InputForUI to send us a NavigationEvent when appropriate.
                }
                else if (keyEvent.type == KeyEvent.Type.KeyReleased)
                {
                    m_EventSystem.SendFocusBasedEvent(t => KeyUpEvent.GetPooled('\0', t.keyCode, t.modifiers),
                        (modifiers:GetModifiers(keyEvent.eventModifiers), keyEvent.keyCode));
                }
            }

            void ProcessTextInputEvent(TextInputEvent textInputEvent)
            {
                if (m_EventSystem.verbose)
                    m_EventSystem.Log(textInputEvent);

                m_EventSystem.SendFocusBasedEvent(t => KeyDownEvent.GetPooled(t.character, KeyCode.None, t.modifiers),
                    (modifiers:GetModifiers(textInputEvent.eventModifiers), textInputEvent.character));
            }

            void ProcessCommandEvent(CommandEvent commandEvent)
            {
                if (m_EventSystem.verbose)
                    m_EventSystem.Log(commandEvent);

                // Command events aren't part of UITK runtime input at the moment.
                // They are sent to Editor panels, in the Editor only,
                // or sent from UITK controls ContextualMenu actions.
                // See for example TextEditingUtilities.HandleKeyEvent(Event e) ending up calling PerformOperation.
            }

            void ProcessIMECompositionEvent(IMECompositionEvent compositionEvent)
            {
                if (m_EventSystem.verbose)
                    m_EventSystem.Log(compositionEvent);

                // IME Composition events aren't part of UITK runtime input at the moment.
                // They are sent while the user is entering a sequence of text that can be completed from a list of
                // suggestions, each time they type a new letter and upon confirmation of their choice of word.
                // Once the choice of word is confirmed, a sequence of corresponding TextInputEvents are sent and
                // match all the letters that need to be entered in the TextField.
                // The IME Composition events don't affect the state of any of the UITK controls and aren't displayed
                // by UITK visual elements at the moment, so for compatibility with non-InputForUI events, we don't
                // support them for the time being.
            }
        }
    }
}
