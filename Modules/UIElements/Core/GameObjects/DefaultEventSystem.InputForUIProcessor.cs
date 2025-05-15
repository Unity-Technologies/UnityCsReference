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

                var (pointerIdBase, pointerIdCount) = pointerEvent.eventSource switch
                {
                    EventSource.Mouse => (PointerId.mousePointerId, 1),
                    EventSource.Touch => (PointerId.touchPointerIdBase, PointerId.touchPointerCount),
                    EventSource.Pen => (PointerId.penPointerIdBase, PointerId.penPointerCount),
                    EventSource.TrackedDevice => (PointerId.trackedPointerIdBase, PointerId.trackedPointerCount),
                    _ => (PointerId.invalidPointerId, 1)
                };

                if (pointerIdBase == PointerId.invalidPointerId)
                {
                    if (m_EventSystem.verbose)
                        m_EventSystem.Log("Pointer event source not supported: " + pointerEvent + " (source=" + pointerEvent.eventSource + ")");
                    return;
                }

                if (pointerEvent.pointerIndex < 0 || pointerEvent.pointerIndex >= pointerIdCount)
                {
                    if (m_EventSystem.verbose)
                        m_EventSystem.Log("Pointer index out of range: " + pointerEvent + " (index=" + pointerEvent.pointerIndex + ", should have 0 <= index < " + pointerIdCount + ")");
                    return;
                }

                var pointerId = pointerIdBase + pointerEvent.pointerIndex;
                if (pointerId < 0 || pointerId >= PointerId.maxPointers)
                {
                    if (m_EventSystem.verbose)
                        m_EventSystem.Log("Pointer id out of range: " + pointerEvent + " (id=" + pointerId + ", should have 0 <= id < " + PointerId.maxPointers + ")");
                    return;
                }

                var deltaTime = m_LastPointerTimestamp != DiscreteTime.Zero ? (float)(pointerEvent.timestamp - m_LastPointerTimestamp) : 0;
                m_NextPointerTimestamp = pointerEvent.timestamp;
                Func<Vector3, (PointerEvent pointerEvent, int pointerId, float deltaTime), EventBase> evtFactory;
                bool deselectIfNoTarget = false;

                if (pointerEvent.type == PointerEvent.Type.PointerMoved)
                {
                    if (pointerEvent.eventSource != EventSource.TrackedDevice &&
                        Mathf.Approximately(deltaPosition.x, 0f) && Mathf.Approximately(deltaPosition.y, 0f))
                        return;

                    evtFactory = (panelPosition, t) =>
                        PointerMoveEvent.GetPooled(t.pointerEvent, panelPosition, t.pointerId, t.deltaTime);
                }
                else if (pointerEvent.type == PointerEvent.Type.ButtonPressed)
                {
                    evtFactory = (panelPosition, t) =>
                        PointerDownEvent.GetPooled(t.pointerEvent, panelPosition, t.pointerId, t.deltaTime);
                }
                else if (pointerEvent.type == PointerEvent.Type.ButtonReleased)
                {
                    evtFactory = (panelPosition, t) =>
                        PointerUpEvent.GetPooled(t.pointerEvent, panelPosition, t.pointerId, t.deltaTime);
                    deselectIfNoTarget = true;
                }
                else if (pointerEvent.type == PointerEvent.Type.TouchCanceled ||
                         pointerEvent.type == PointerEvent.Type.TrackedCanceled)
                {
                    evtFactory = (panelPosition, t) =>
                        PointerCancelEvent.GetPooled(t.pointerEvent, panelPosition, t.pointerId, t.deltaTime);
                }
                else if (pointerEvent.type == PointerEvent.Type.Scroll)
                {
                    evtFactory = (panelPosition, t) =>
                        WheelEvent.GetPooled(t.pointerEvent.scroll, panelPosition,
                            GetModifiers(t.pointerEvent.eventModifiers));
                }
                else
                {
                    if (m_EventSystem.verbose)
                        m_EventSystem.Log("Unsupported event " + pointerEvent);
                    return;
                }

                if (pointerEvent.eventSource == EventSource.TrackedDevice)
                {
                    var maxDistance = pointerEvent.maxDistance > 0 ? pointerEvent.maxDistance : Mathf.Infinity;
                    m_EventSystem.SendRayBasedEvent(pointerEvent.worldRay, maxDistance, pointerId, evtFactory,
                        (pointerEvent, pointerId, deltaTime), deselectIfNoTarget);
                }
                else
                {
                    m_EventSystem.SendPositionBasedEvent(position, deltaPosition, pointerId, targetDisplay,
                        evtFactory, (pointerEvent, pointerId, deltaTime), deselectIfNoTarget);
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

                // IME Events are sent while the user is entering a sequence of text that can be completed from a list of
                // suggestions, each time they type a new letter and upon confirmation of their choice of word.
                // Once the choice of word is confirmed, a sequence of corresponding TextInputEvents are sent and
                // match all the letters that need to be entered in the TextField.
                m_EventSystem.SendFocusBasedEvent(_ => IMEEvent.GetPooled(compositionEvent.compositionString), 0);
            }
        }
    }
}
