// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal partial class DefaultEventSystem
    {
        internal class LegacyInputProcessor
        {
            const string m_HorizontalAxis = "Horizontal";
            const string m_VerticalAxis = "Vertical";
            const string m_SubmitButton = "Submit";
            const string m_CancelButton = "Cancel";
            const float m_InputActionsPerSecond = 10;
            const float m_RepeatDelay = 0.5f;

            private bool m_SendingTouchEvents;
            private bool m_SendingPenEvent;
            private EventModifiers m_CurrentModifiers;
            private EventModifiers m_CurrentPointerModifiers => m_CurrentModifiers & (EventModifiers.Control | EventModifiers.Shift | EventModifiers.Alt | EventModifiers.Command);

            private int m_LastMousePressButton = -1;
            private float m_NextMousePressTime = 0f;
            private int m_LastMouseClickCount = 0;
            private Vector2 m_LastMousePosition = Vector2.zero;
            private bool m_MouseProcessedAtLeastOnce;

            private Dictionary<int, int> m_TouchFingerIdToFingerIndex = new();
            private int m_TouchNextFingerIndex = 0;

            private IInput m_Input;
            public IInput input
            {
                get => m_Input ??= GetDefaultInput();
                set => m_Input = value;
            }

            private readonly Event m_Event = new Event();
            private readonly DefaultEventSystem m_EventSystem;

            public LegacyInputProcessor(DefaultEventSystem eventSystem)
            {
                m_EventSystem = eventSystem;
            }

            public IInput GetDefaultInput()
            {
                IInput input = new Input();
                try
                {
                    // When legacy input manager is disabled, any query to Input will throw an InvalidOperationException
                    input.GetAxisRaw(m_HorizontalAxis);
                }
                catch (InvalidOperationException)
                {
                    input = new NoInput();
                    m_EventSystem.LogWarning(
                        "UI Toolkit is currently relying on the legacy Input Manager for its active input source, " +
                        "but the legacy Input Manager is not available using your current Project Settings. " +
                        "Some UI Toolkit functionality might be missing or not working properly as a result. " +
                        "To fix this problem, you can enable \"Input Manager (old)\" or \"Both\" in the " +
                        "Active Input Source setting of the Player section. " +
                        "UI Toolkit is using its internal default event system to process input. " +
                        "Alternatively, you may activate new Input System support with UI Toolkit by " +
                        "adding an EventSystem component to your active scene.");
                }
                return input;
            }

            public void Reset()
            {
                m_SendingTouchEvents = false;
                m_SendingPenEvent = false;
                m_CurrentModifiers = default;
                m_LastMousePressButton = -1;
                m_NextMousePressTime = 0f;
                m_LastMouseClickCount = 0;
                m_LastMousePosition = Vector2.zero;
                m_MouseProcessedAtLeastOnce = false;
                m_ConsecutiveMoveCount = 0;
                m_IsMoveFromKeyboard = false;
                m_TouchFingerIdToFingerIndex.Clear();
                m_TouchNextFingerIndex = 0;
            }

            public void ProcessLegacyInputEvents()
            {
                m_SendingPenEvent = ProcessPenEvents();

                // touch needs to take precedence because of the mouse emulation layer
                if (!m_SendingPenEvent)
                    m_SendingTouchEvents = ProcessTouchEvents();

                if (!m_SendingPenEvent && !m_SendingTouchEvents)
                    ProcessMouseEvents();
                else
                    m_MouseProcessedAtLeastOnce = false; // don't send mouse move after pen/touch until mouse truly moves

                using (m_EventSystem.FocusBasedEventSequence())
                {
                    SendIMGUIEvents();
                    SendInputEvents();
                }
            }

            void SendIMGUIEvents()
            {
                bool first = true;

                while (Event.PopEvent(m_Event))
                {
                    if (m_Event.type == EventType.Ignore || m_Event.type == EventType.Repaint ||
                        m_Event.type == EventType.Layout)
                        continue;

                    m_CurrentModifiers = first ? m_Event.modifiers : (m_CurrentModifiers | m_Event.modifiers);
                    first = false;

                    if (m_Event.type == EventType.KeyUp || m_Event.type == EventType.KeyDown)
                    {
                        m_EventSystem.SendFocusBasedEvent(e => UIElementsRuntimeUtility.CreateEvent(e), m_Event);
                        ProcessTabEvent(m_Event, m_CurrentModifiers);
                    }
                    else if (m_Event.type == EventType.ScrollWheel)
                    {
                        // There's different scrollDelta rates between Input Manager, New Input and IMGUI. UITK events use
                        // IMGUI conventions. Factors can vary between platforms (they come from PlatformDependent code).
                        // For example, InputEvent::InputEvent (Windows PlatformInputEvent.cpp) and NewInput::OnMessage (NewInput.cpp)
                        // read data differently from the WM_MOUSEWHEEL message.
                        // Since we want to rely as little as possible on IMGUI event position for multiple display support,
                        // we use the mouse position from input and combine it with the scroll delta from IMGUI.
                        var position = UIElementsRuntimeUtility.MultiDisplayBottomLeftToPanelPosition(input.mousePosition, out var targetDisplay);
                        var delta = position - m_LastMousePosition;
                        var scrollDelta = m_Event.delta;

                        m_EventSystem.SendPositionBasedEvent(position, delta, PointerId.mousePointerId, targetDisplay,
                            (panelPosition, _, t) => WheelEvent.GetPooled(t.scrollDelta, panelPosition, t.modifiers),
                            (modifiers: m_CurrentPointerModifiers, scrollDelta));
                    }
                    else if (!m_SendingTouchEvents && !m_SendingPenEvent && m_Event.pointerType != UnityEngine.PointerType.Mouse ||
                             m_Event.type == EventType.MouseEnterWindow || m_Event.type == EventType.MouseLeaveWindow)
                    {
                        int pointerType =
                            m_Event.pointerType == UnityEngine.PointerType.Mouse ? PointerId.mousePointerId :
                            m_Event.pointerType == UnityEngine.PointerType.Touch ? PointerId.touchPointerIdBase :
                            PointerId.penPointerIdBase;
                        Vector3 screenPosition = UIElementsRuntimeUtility.MultiDisplayToLocalScreenPosition(m_Event.mousePosition, out var targetDisplay);
                        Vector2 screenDelta = m_Event.delta;

                        m_EventSystem.SendPositionBasedEvent(screenPosition, screenDelta, pointerType, targetDisplay, (panelPosition, panelDelta, evt) =>
                            {
                                evt.mousePosition = panelPosition;
                                evt.delta = panelDelta;
                                return UIElementsRuntimeUtility.CreateEvent(evt);
                            }, m_Event, deselectIfNoTarget: m_Event.type == EventType.MouseDown || m_Event.type == EventType.TouchDown);
                    }
                }
            }

            private void ProcessMouseEvents()
            {
                if (!input.mousePresent)
                    return;

                var position = UIElementsRuntimeUtility.MultiDisplayBottomLeftToPanelPosition(input.mousePosition, out var targetDisplay);
                var delta = position - m_LastMousePosition;

                if (!m_MouseProcessedAtLeastOnce)
                {
                    delta = Vector2.zero;
                    m_LastMousePosition = position;
                    m_MouseProcessedAtLeastOnce = true;
                }
                else
                {
                    if (!Mathf.Approximately(delta.x, 0f) || !Mathf.Approximately(delta.y, 0f))
                    {
                        // Only adjust lastMousePosition if we send a PointerMoveEvent, otherwise let the delta accumulate.
                        m_LastMousePosition = position;

                        m_EventSystem.SendPositionBasedEvent(position, delta, PointerId.mousePointerId, targetDisplay,
                            (panelPosition, panelDelta, t) => PointerMoveEvent.GetPooled(EventType.MouseMove,
                                panelPosition, panelDelta, -1, 0, t.modifiers, t.targetDisplay ?? 0),
                            (modifiers: m_CurrentPointerModifiers, targetDisplay));
                    }
                }

                int mouseButtonCount = input.mouseButtonCount;
                for (var button = 0; button < mouseButtonCount; button++)
                {
                    if (input.GetMouseButtonDown(button))
                    {
                        if (m_LastMousePressButton != button || input.unscaledTime >= m_NextMousePressTime)
                        {
                            m_LastMousePressButton = button;
                            m_LastMouseClickCount = 0;
                        }

                        var clickCount = ++m_LastMouseClickCount;

                        m_NextMousePressTime = input.unscaledTime + input.doubleClickTime;

                        m_EventSystem.SendPositionBasedEvent(position, delta, PointerId.mousePointerId, targetDisplay,
                            (panelPosition, panelDelta, t) => PointerEventHelper.GetPooled(EventType.MouseDown,
                                panelPosition, panelDelta, t.button, t.clickCount, t.modifiers, t.targetDisplay ?? 0),
                            (button, clickCount, modifiers: m_CurrentPointerModifiers, targetDisplay), deselectIfNoTarget:true);
                    }

                    if (input.GetMouseButtonUp(button))
                    {
                        var clickCount = m_LastMouseClickCount;

                        m_EventSystem.SendPositionBasedEvent(position, delta, PointerId.mousePointerId, targetDisplay,
                            (panelPosition, panelDelta, t) => PointerEventHelper.GetPooled(EventType.MouseUp,
                                panelPosition, panelDelta, t.button, t.clickCount, t.modifiers, t.targetDisplay ?? 0),
                            (button, clickCount, modifiers: m_CurrentPointerModifiers, targetDisplay));
                    }
                }
            }

            void SendInputEvents()
            {
                bool sendNavigationMove = ShouldSendMoveFromInput();

                if (sendNavigationMove)
                {
                    m_EventSystem.SendFocusBasedEvent(
                        self => NavigationMoveEvent.GetPooled(self.GetRawMoveVector(),
                            self.m_IsMoveFromKeyboard ? NavigationDeviceType.Keyboard : NavigationDeviceType.NonKeyboard,
                            self.m_CurrentModifiers), this);
                }

                if (input.GetButtonDown(m_SubmitButton))
                {
                    m_EventSystem.SendFocusBasedEvent(
                        self => NavigationSubmitEvent.GetPooled(
                            self.input.anyKey ? NavigationDeviceType.Keyboard : NavigationDeviceType.NonKeyboard,
                            self.m_CurrentModifiers), this);
                }

                if (input.GetButtonDown(m_CancelButton))
                {
                    m_EventSystem.SendFocusBasedEvent(
                        self => NavigationCancelEvent.GetPooled(
                            self.input.anyKey ? NavigationDeviceType.Keyboard : NavigationDeviceType.NonKeyboard,
                            self.m_CurrentModifiers), this);
                }
            }

            private bool ProcessTouchEvents()
            {
                bool allAreReleased = true;
                for (int i = 0; i < input.touchCount; ++i)
                {
                    Touch touch = input.GetTouch(i);

                    // PointerStationaryEvent is now obsolete. We support it but we don't send it anymore.
                    if (touch.type == TouchType.Indirect || touch.phase == TouchPhase.Stationary)
                        continue;

                    if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved)
                        allAreReleased = false;

                    // Flip Y Coordinates.
                    touch.position = UIElementsRuntimeUtility.MultiDisplayBottomLeftToPanelPosition(touch.position, out var targetDisplay);
                    touch.rawPosition = UIElementsRuntimeUtility.MultiDisplayBottomLeftToPanelPosition(touch.rawPosition, out _);
                    touch.deltaPosition = UIElementsRuntimeUtility.ScreenBottomLeftToPanelDelta(touch.deltaPosition);

                    if (!m_TouchFingerIdToFingerIndex.TryGetValue(touch.fingerId, out var fingerIndex))
                    {
                        fingerIndex = m_TouchNextFingerIndex++;
                        m_TouchFingerIdToFingerIndex.Add(touch.fingerId, fingerIndex);
                    }
                    int pointerId = PointerId.touchPointerIdBase + fingerIndex;

                    m_EventSystem.SendPositionBasedEvent(touch.position, touch.deltaPosition, pointerId, targetDisplay, (panelPosition, panelDelta, t) =>
                    {
                        t.touch.position = panelPosition;
                        t.touch.deltaPosition = panelDelta;
                        return MakeTouchEvent(t.touch, t.pointerId, EventModifiers.None, t.targetDisplay ?? 0);
                    }, (touch, pointerId, targetDisplay));
                }

                if (allAreReleased)
                {
                    m_TouchNextFingerIndex = 0;
                    m_TouchFingerIdToFingerIndex.Clear();
                }

                return input.touchCount > 0;
            }
            private bool ProcessPenEvents()
            {
                PenData p  = input.GetLastPenContactEvent();
                if (p.contactType == PenEventType.NoContact)
                    return false;

                m_EventSystem.SendPositionBasedEvent(p.position, p.deltaPos, PointerId.penPointerIdBase, null, (panelPosition, panelDelta, _pen) =>
                {
                    _pen.position = panelPosition;
                    _pen.deltaPos = panelDelta;
                    return MakePenEvent(_pen, EventModifiers.None, 0);
                }, p);
                input.ClearLastPenContactEvent();
                return true;
            }

            private Vector2 GetRawMoveVector()
            {
                Vector2 move = Vector2.zero;
                move.x = input.GetAxisRaw(m_HorizontalAxis);
                move.y = input.GetAxisRaw(m_VerticalAxis);

                // On button down we want exactly -1 or +1 values so we override what was read from GetAxisRaw. We need
                // to read the axis value to get the sign but we can't be sure that it'll have values -1 or +1 exactly.
                if (input.GetButtonDown(m_HorizontalAxis))
                {
                    if (move.x < 0)
                        move.x = -1f;
                    if (move.x > 0)
                        move.x = 1f;
                }

                if (input.GetButtonDown(m_VerticalAxis))
                {
                    if (move.y < 0)
                        move.y = -1f;
                    if (move.y > 0)
                        move.y = 1f;
                }

                return move;
            }

            private int m_ConsecutiveMoveCount;
            private Vector2 m_LastMoveVector;
            private float m_PrevActionTime;
            private bool m_IsMoveFromKeyboard;

            private bool ShouldSendMoveFromInput()
            {
                float time = input.unscaledTime;

                Vector2 movement = GetRawMoveVector();
                if (Mathf.Approximately(movement.x, 0f) && Mathf.Approximately(movement.y, 0f))
                {
                    m_ConsecutiveMoveCount = 0;
                    m_IsMoveFromKeyboard = false;
                    return false;
                }

                // If user pressed key again, always allow event
                bool allow = input.GetButtonDown(m_HorizontalAxis) || input.GetButtonDown(m_VerticalAxis);
                bool similarDir = (Vector2.Dot(movement, m_LastMoveVector) > 0);
                if (!allow)
                {
                    // Otherwise, user held down key or axis.
                    // If direction didn't change at least 90 degrees, wait for delay before allowing consecutive event.
                    if (similarDir && m_ConsecutiveMoveCount == 1)
                        allow = (time > m_PrevActionTime + m_RepeatDelay);
                    // If direction changed at least 90 degree, or we already had the delay, repeat at repeat rate.
                    else
                        allow = (time > m_PrevActionTime + 1f / m_InputActionsPerSecond);
                }

                if (!allow)
                    return false;

                var moveDirection = NavigationMoveEvent.DetermineMoveDirection(movement.x, movement.y);

                if (moveDirection != NavigationMoveEvent.Direction.None)
                {
                    if (!similarDir)
                        m_ConsecutiveMoveCount = 0;
                    m_ConsecutiveMoveCount++;
                    m_PrevActionTime = time;
                    m_LastMoveVector = movement;
                    m_IsMoveFromKeyboard |= input.anyKey;
                }
                else
                {
                    m_ConsecutiveMoveCount = 0;
                    m_IsMoveFromKeyboard = false;
                }

                return moveDirection != NavigationMoveEvent.Direction.None;
            }

            void ProcessTabEvent(Event e, EventModifiers modifiers)
            {
                if (e.ShouldSendNavigationMoveEventRuntime())
                {
                    var direction = e.shift ? NavigationMoveEvent.Direction.Previous : NavigationMoveEvent.Direction.Next;
                    m_EventSystem.SendFocusBasedEvent(
                        t => NavigationMoveEvent.GetPooled(t.direction,
                            t.input.anyKey ? NavigationDeviceType.Keyboard : NavigationDeviceType.NonKeyboard, t.modifiers),
                        (direction, modifiers, input));
                }
            }

            static EventBase MakeTouchEvent(Touch touch, int pointerId, EventModifiers modifiers, int targetDisplay)
            {
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        return PointerDownEvent.GetPooled(touch, pointerId, modifiers, targetDisplay);
                    case TouchPhase.Moved:
                        return PointerMoveEvent.GetPooled(touch, pointerId, modifiers, targetDisplay);
                    case TouchPhase.Ended:
                        return PointerUpEvent.GetPooled(touch, pointerId, modifiers, targetDisplay);
                    case TouchPhase.Canceled:
                        return PointerCancelEvent.GetPooled(touch, pointerId, modifiers, targetDisplay);
                    default:
                        return null;
                }
            }

            static EventBase MakePenEvent(PenData pen, EventModifiers modifiers, int targetDisplay)
            {
                switch (pen.contactType)
                {
                    case PenEventType.PenDown:
                        return PointerDownEvent.GetPooled(pen, modifiers, targetDisplay);
                    case PenEventType.PenUp:
                        return PointerUpEvent.GetPooled(pen, modifiers, targetDisplay);
                    case PenEventType.NoContact:
                        return PointerMoveEvent.GetPooled(pen, modifiers, targetDisplay);
                    default:
                        return null;
                }
            }

            internal interface IInput
            {
                bool GetButtonDown(string button);
                float GetAxisRaw(string axis);
                void ResetPenEvents();
                void ClearLastPenContactEvent();
                int penEventCount { get; }
                PenData GetPenEvent(int index);
                PenData GetLastPenContactEvent();
                int touchCount { get; }
                Touch GetTouch(int index);
                bool mousePresent { get; }
                bool GetMouseButtonDown(int button);
                bool GetMouseButtonUp(int button);
                Vector3 mousePosition { get; }
                Vector2 mouseScrollDelta { get; }
                int mouseButtonCount { get; }
                bool anyKey { get; }
                float unscaledTime { get; } // overriden in unit tests
                float doubleClickTime { get; }
            }

            private class Input : IInput
            {
                public bool GetButtonDown(string button) => UnityEngine.Input.GetButtonDown(button);
                public float GetAxisRaw(string axis) => UnityEngine.Input.GetAxis(axis);
                public void ResetPenEvents() => UnityEngine.Input.ResetPenEvents();
                public void ClearLastPenContactEvent() => UnityEngine.Input.ClearLastPenContactEvent();
                public int penEventCount => UnityEngine.Input.penEventCount;
                public PenData GetPenEvent(int index) => UnityEngine.Input.GetPenEvent(index);
                public PenData GetLastPenContactEvent() => UnityEngine.Input.GetLastPenContactEvent();
                public int touchCount => UnityEngine.Input.touchCount;
                public Touch GetTouch(int index) => UnityEngine.Input.GetTouch(index);
                public bool mousePresent => UnityEngine.Input.mousePresent;
                public bool GetMouseButtonDown(int button) => UnityEngine.Input.GetMouseButtonDown(button);
                public bool GetMouseButtonUp(int button) => UnityEngine.Input.GetMouseButtonUp(button);
                public Vector3 mousePosition => UnityEngine.Input.mousePosition;
                public Vector2 mouseScrollDelta => UnityEngine.Input.mouseScrollDelta;
                public int mouseButtonCount => 3;
                public bool anyKey => UnityEngine.Input.anyKey;
                public float unscaledTime => Time.unscaledTime;
                public float doubleClickTime => Event.GetDoubleClickTime() * 0.001f;
            }

            private class NoInput : IInput
            {
                public bool GetButtonDown(string button) => false;
                public float GetAxisRaw(string axis) => 0f;
                public int touchCount => 0;
                public Touch GetTouch(int index) => default;
                public void ResetPenEvents() { }
                public void ClearLastPenContactEvent() { }
                public int penEventCount => 0;
                public PenData GetPenEvent(int index) => default;
                public PenData GetLastPenContactEvent() => default;
                public bool mousePresent => false;
                public bool GetMouseButtonDown(int button) => false;
                public bool GetMouseButtonUp(int button) => false;
                public Vector3 mousePosition => default;
                public Vector2 mouseScrollDelta => default;
                public int mouseButtonCount => 0;
                public bool anyKey => false;
                public float unscaledTime => 0;
                public float doubleClickTime => Mathf.Infinity;
            }
        }
    }
}
