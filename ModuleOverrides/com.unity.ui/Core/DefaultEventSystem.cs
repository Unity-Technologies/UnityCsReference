// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    internal class DefaultEventSystem
    {
        private bool isAppFocused => Application.isFocused;

        internal static Func<bool> IsEditorRemoteConnected = () => false;

        private IInput m_Input;
        internal IInput input
        {
            get => m_Input ?? (m_Input = GetDefaultInput());
            set => m_Input = value;
        }

        private IInput GetDefaultInput()
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
                Debug.LogWarning(
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

        private bool ShouldIgnoreEventsOnAppNotFocused()
        {
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.Windows:
                case OperatingSystemFamily.Linux:
                case OperatingSystemFamily.MacOSX:
                    if (IsEditorRemoteConnected())
                        return false;
                    return true;
                default:
                    return false;
            }
        }

        private readonly string m_HorizontalAxis = "Horizontal";
        private readonly string m_VerticalAxis = "Vertical";
        private readonly string m_SubmitButton = "Submit";
        private readonly string m_CancelButton = "Cancel";
        private readonly float m_InputActionsPerSecond = 10;
        private readonly float m_RepeatDelay = 0.5f;

        private bool m_SendingTouchEvents;
        private bool m_SendingPenEvent;

        private Event m_Event = new Event();
        private BaseRuntimePanel m_FocusedPanel;
        private BaseRuntimePanel m_PreviousFocusedPanel;
        private Focusable m_PreviousFocusedElement;
        private EventModifiers m_CurrentModifiers;

        public BaseRuntimePanel focusedPanel
        {
            get => m_FocusedPanel;
            set
            {
                if (m_FocusedPanel != value)
                {
                    m_FocusedPanel?.Blur();
                    m_FocusedPanel = value;
                    m_FocusedPanel?.Focus();
                }
            }
        }

        public void Reset()
        {
            m_LastMousePressButton = -1;
            m_NextMousePressTime = 0f;
            m_LastMouseClickCount = 0;
            m_LastMousePosition = Vector2.zero;
            m_MouseProcessedAtLeastOnce = false;
        }

        public enum UpdateMode
        {
            Always,
            IgnoreIfAppNotFocused
        }

        public void Update(UpdateMode updateMode = UpdateMode.Always)
        {
            if (!isAppFocused && ShouldIgnoreEventsOnAppNotFocused() && updateMode == UpdateMode.IgnoreIfAppNotFocused)
                return;

            m_SendingPenEvent = ProcessPenEvents();

            // touch needs to take precedence because of the mouse emulation layer
            if (!m_SendingPenEvent)
                m_SendingTouchEvents = ProcessTouchEvents();

            if (!m_SendingPenEvent && !m_SendingTouchEvents)
                ProcessMouseEvents();
            else
                m_MouseProcessedAtLeastOnce = false; // don't send mouse move after pen/touch until mouse truly moves

            using (FocusBasedEventSequence())
            {
                SendIMGUIEvents();
                SendInputEvents();
            }
        }

        internal struct FocusBasedEventSequenceContext : IDisposable
        {
            private DefaultEventSystem es;

            public FocusBasedEventSequenceContext(DefaultEventSystem es)
            {
                this.es = es;
                es.m_PreviousFocusedPanel = es.focusedPanel;
                es.m_PreviousFocusedElement = es.focusedPanel?.focusController.GetLeafFocusedElement();
            }
            public void Dispose()
            {
                es.m_PreviousFocusedPanel = null;
                es.m_PreviousFocusedElement = null;
            }
        }

        internal FocusBasedEventSequenceContext FocusBasedEventSequence()
        {
            return new FocusBasedEventSequenceContext(this);
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
                    SendFocusBasedEvent(self => UIElementsRuntimeUtility.CreateEvent(self.m_Event), this);
                    ProcessTabEvent(m_Event, m_CurrentModifiers);
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
                    SendPositionBasedEvent(screenPosition, screenDelta, pointerType, targetDisplay, (panelPosition, panelDelta, evt) =>
                    {
                        evt.mousePosition = panelPosition;
                        evt.delta = panelDelta;
                        return UIElementsRuntimeUtility.CreateEvent(evt);
                    }, m_Event, deselectIfNoTarget: m_Event.type == EventType.MouseDown || m_Event.type == EventType.TouchDown);
                }
            }
        }

        private int m_LastMousePressButton = -1;
        private float m_NextMousePressTime = 0f;
        private int m_LastMouseClickCount = 0;
        private Vector2 m_LastMousePosition = Vector2.zero;
        private bool m_MouseProcessedAtLeastOnce;

        private void ProcessMouseEvents()
        {
            if (!input.mousePresent)
                return;

            var position = UIElementsRuntimeUtility.MultiDisplayBottomLeftToPanelPosition(input.mousePosition, out var targetDisplay);
            var delta = position - m_LastMousePosition;
            var scrollDelta = input.mouseScrollDelta;

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

                    SendPositionBasedEvent(position, delta, PointerId.mousePointerId, targetDisplay,
                        (panelPosition, panelDelta, self) => PointerMoveEvent.GetPooled(EventType.MouseMove,
                            panelPosition, panelDelta, -1, 0, self.m_CurrentModifiers), this);
                }

                if (!Mathf.Approximately(scrollDelta.x, 0f) || !Mathf.Approximately(scrollDelta.y, 0f))
                {
                    SendPositionBasedEvent(position, delta, PointerId.mousePointerId, targetDisplay,
                        (panelPosition, _, t) => WheelEvent.GetPooled(panelPosition, -1, 0, t.scrollDelta, t.modifiers),
                        (modifiers: m_CurrentModifiers, scrollDelta));
                }
            }

            int mouseButtonCount = input.mouseButtonCount;
            for (var button = 0; button < mouseButtonCount; button++)
            {
                if (input.GetMouseButtonDown(button))
                {
                    if (m_LastMousePressButton != button || Time.unscaledTime >= m_NextMousePressTime)
                    {
                        m_LastMousePressButton = button;
                        m_LastMouseClickCount = 0;
                        m_NextMousePressTime = Time.unscaledTime + Event.GetDoubleClickTime();
                    }

                    var clickCount = ++m_LastMouseClickCount;

                    SendPositionBasedEvent(position, delta, PointerId.mousePointerId, targetDisplay,
                        (panelPosition, panelDelta, t) => PointerEventHelper.GetPooled(EventType.MouseDown,
                            panelPosition, panelDelta, t.button, t.clickCount, t.modifiers),
                        (button, clickCount, modifiers: m_CurrentModifiers), deselectIfNoTarget:true);
                }

                if (input.GetMouseButtonUp(button))
                {
                    var clickCount = m_LastMouseClickCount;

                    SendPositionBasedEvent(position, delta, PointerId.mousePointerId, targetDisplay,
                        (panelPosition, panelDelta, t) => PointerEventHelper.GetPooled(EventType.MouseUp,
                            panelPosition, panelDelta, t.button, t.clickCount, t.modifiers),
                        (button, clickCount, modifiers: m_CurrentModifiers));
                }
            }
        }

        void SendInputEvents()
        {
            bool sendNavigationMove = ShouldSendMoveFromInput();

            if (sendNavigationMove)
            {
                SendFocusBasedEvent(
                    self => NavigationMoveEvent.GetPooled(self.GetRawMoveVector(),
                        self.input.anyKey ? NavigationDeviceType.Keyboard : NavigationDeviceType.NonKeyboard,
                        self.m_CurrentModifiers), this);
            }

            if (input.GetButtonDown(m_SubmitButton))
            {
                SendFocusBasedEvent(
                    self => NavigationSubmitEvent.GetPooled(
                        self.input.anyKey ? NavigationDeviceType.Keyboard : NavigationDeviceType.NonKeyboard,
                        self.m_CurrentModifiers), this);
            }

            if (input.GetButtonDown(m_CancelButton))
            {
                SendFocusBasedEvent(
                    self => NavigationCancelEvent.GetPooled(
                        self.input.anyKey ? NavigationDeviceType.Keyboard : NavigationDeviceType.NonKeyboard,
                        self.m_CurrentModifiers), this);
            }
        }

        internal void SendFocusBasedEvent<TArg>(Func<TArg, EventBase> evtFactory, TArg arg)
        {
            // Send all focus-based events to the same previously focused panel if there's one
            // This allows Navigation events to use the same target as related KeyDown (and eventually Gamepad) events
            if (m_PreviousFocusedPanel != null)
            {
                using (EventBase evt = evtFactory(arg))
                {
                    evt.target = m_PreviousFocusedElement ?? m_PreviousFocusedPanel.visualTree;
                    m_PreviousFocusedPanel.visualTree.SendEvent(evt);
                    UpdateFocusedPanel(m_PreviousFocusedPanel);
                    return;
                }
            }

            // Send Keyboard events to all panels if none is focused.
            // This is so that navigation with Tab can be started without clicking on an element.

            // Try all the panels, from closest to deepest
            var panels = UIElementsRuntimeUtility.GetSortedPlayerPanels();
            for (var i = panels.Count - 1; i >= 0; i--)
            {
                var panel = panels[i];
                if (panel is BaseRuntimePanel runtimePanel)
                {
                    using (EventBase evt = evtFactory(arg))
                    {
                        // Since there was no focused element, send event to the visualTree to avoid reacting to a
                        // focus change in between events.
                        evt.target = runtimePanel.visualTree;
                        runtimePanel.visualTree.SendEvent(evt);

                        if (runtimePanel.focusController.focusedElement != null)
                        {
                            focusedPanel = runtimePanel;
                            break;
                        }

                        if (evt.isPropagationStopped)
                        {
                            return;
                        }
                    }
                }
            }
        }

        // For Unit Tests
        internal void SendPositionBasedEvent<TArg>(Vector3 mousePosition, Vector3 delta, int pointerId,
            Func<Vector3, Vector3, TArg, EventBase> evtFactory, TArg arg, bool deselectIfNoTarget = false) =>
            SendPositionBasedEvent(mousePosition, delta, pointerId, null, evtFactory, arg, deselectIfNoTarget);

        void SendPositionBasedEvent<TArg>(Vector3 mousePosition, Vector3 delta, int pointerId, int? targetDisplay, Func<Vector3, Vector3, TArg, EventBase> evtFactory, TArg arg, bool deselectIfNoTarget = false)
        {
            // Allow focus to be lost before processing the event
            if (focusedPanel != null)
            {
                UpdateFocusedPanel(focusedPanel);
            }

            var capturingPanel = PointerDeviceState.GetPlayerPanelWithSoftPointerCapture(pointerId);

            // Allow element with pointer capture to update panel soft capture
            var capturing = RuntimePanel.s_EventDispatcher.pointerState.GetCapturingElement(pointerId);
            if (capturing is VisualElement capturingVE)
            {
                capturingPanel = capturingVE.panel;
            }

            BaseRuntimePanel targetPanel = null;
            Vector2 targetPanelPosition = Vector2.zero;
            Vector2 targetPanelDelta = Vector2.zero;

            if (capturingPanel is BaseRuntimePanel capturingRuntimePanel)
            {
                // Panel with soft capture has priority, that is it will receive pointer events until pointer up
                targetPanel = capturingRuntimePanel;
                targetPanel.ScreenToPanel(mousePosition, delta, out targetPanelPosition, out targetPanelDelta);
            }
            else
            {
                // Find a candidate panel for the event
                // Try all the panels, from closest to deepest
                var panels = UIElementsRuntimeUtility.GetSortedPlayerPanels();
                for (var i = panels.Count - 1; i >= 0; i--)
                {
                    if (panels[i] is BaseRuntimePanel runtimePanel && (targetDisplay == null || runtimePanel.targetDisplay == targetDisplay))
                    {
                        if (runtimePanel.ScreenToPanel(mousePosition, delta, out targetPanelPosition, out targetPanelDelta) &&
                            runtimePanel.Pick(targetPanelPosition) != null)
                        {
                            targetPanel = runtimePanel;
                            break;
                        }
                    }
                }
            }

            BaseRuntimePanel lastActivePanel = PointerDeviceState.GetPanel(pointerId, ContextType.Player) as BaseRuntimePanel;

            if (lastActivePanel != targetPanel)
            {
                // Allow last panel the pointer was in to dispatch [Mouse|Pointer][Out|Leave] events if needed.
                lastActivePanel?.PointerLeavesPanel(pointerId, lastActivePanel.ScreenToPanel(mousePosition));
                targetPanel?.PointerEntersPanel(pointerId, targetPanelPosition);
            }

            if (targetPanel != null)
            {
                using (EventBase evt = evtFactory(targetPanelPosition, targetPanelDelta, arg))
                {
                    targetPanel.visualTree.SendEvent(evt);

                    if (evt.processedByFocusController)
                    {
                        UpdateFocusedPanel(targetPanel);
                    }

                    if (evt.eventTypeId == PointerDownEvent.TypeId())
                        PointerDeviceState.SetPlayerPanelWithSoftPointerCapture(pointerId, targetPanel);
                    else if (evt.eventTypeId == PointerUpEvent.TypeId() && ((PointerUpEvent)evt).pressedButtons == 0)
                        PointerDeviceState.SetPlayerPanelWithSoftPointerCapture(pointerId, null);
                }
            }
            else
            {
                if (deselectIfNoTarget)
                {
                    focusedPanel = null;
                }
            }
        }

        private void UpdateFocusedPanel(BaseRuntimePanel runtimePanel)
        {
            if (runtimePanel.focusController.focusedElement != null)
            {
                focusedPanel = runtimePanel;
            }
            else if (focusedPanel == runtimePanel)
            {
                focusedPanel = null;
            }
        }

        private static EventBase MakeTouchEvent(Touch touch, EventModifiers modifiers)
        {
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    return PointerDownEvent.GetPooled(touch, modifiers);
                case TouchPhase.Moved:
                    return PointerMoveEvent.GetPooled(touch, modifiers);
                case TouchPhase.Stationary:
                    return PointerStationaryEvent.GetPooled(touch, modifiers);
                case TouchPhase.Ended:
                    return PointerUpEvent.GetPooled(touch, modifiers);
                case TouchPhase.Canceled:
                    return PointerCancelEvent.GetPooled(touch, modifiers);
                default:
                    return null;
            }
        }

        private static EventBase MakePenEvent(PenData pen, EventModifiers modifiers)
        {
            switch (pen.contactType)
            {
                case PenEventType.PenDown:
                    return PointerDownEvent.GetPooled(pen, modifiers);
                case PenEventType.PenUp:
                    return PointerUpEvent.GetPooled(pen, modifiers);
                case PenEventType.NoContact:
                default:
                    return null;
            }
        }

        private bool ProcessTouchEvents()
        {
            for (int i = 0; i < input.touchCount; ++i)
            {
                Touch touch = input.GetTouch(i);

                if (touch.type == TouchType.Indirect)
                    continue;

                // Flip Y Coordinates.
                touch.position = UIElementsRuntimeUtility.MultiDisplayBottomLeftToPanelPosition(touch.position, out var targetDisplay);
                touch.rawPosition = UIElementsRuntimeUtility.MultiDisplayBottomLeftToPanelPosition(touch.rawPosition, out _);
                touch.deltaPosition = UIElementsRuntimeUtility.ScreenBottomLeftToPanelDelta(touch.deltaPosition);

                SendPositionBasedEvent(touch.position, touch.deltaPosition, PointerId.touchPointerIdBase + touch.fingerId, targetDisplay, (panelPosition, panelDelta, _touch) =>
                {
                    _touch.position = panelPosition;
                    _touch.deltaPosition = panelDelta;
                    return MakeTouchEvent(_touch, EventModifiers.None);
                }, touch);
            }

            return input.touchCount > 0;
        }
        private bool ProcessPenEvents()
        {
            PenData p  = input.GetLastPenContactEvent();
            if (p.contactType == PenEventType.NoContact)
                return false;

            SendPositionBasedEvent(p.position, p.deltaPos, PointerId.penPointerIdBase, null, (panelPosition, panelDelta, _pen) =>
            {
                _pen.position = panelPosition;
                _pen.deltaPos = panelDelta;
                return MakePenEvent(_pen, EventModifiers.None);
            }, p);
            input.ClearLastPenContactEvent();
            return true;
        }


        private Vector2 GetRawMoveVector()
        {
            Vector2 move = Vector2.zero;
            move.x = input.GetAxisRaw(m_HorizontalAxis);
            move.y = input.GetAxisRaw(m_VerticalAxis);

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

        private bool ShouldSendMoveFromInput()
        {
            float time = Time.unscaledTime;

            Vector2 movement = GetRawMoveVector();
            if (Mathf.Approximately(movement.x, 0f) && Mathf.Approximately(movement.y, 0f))
            {
                m_ConsecutiveMoveCount = 0;
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

            // Debug.Log(m_ProcessingEvent.rawType + " axis:" + m_AllowAxisEvents + " value:" + "(" + x + "," + y + ")");
            var moveDirection = NavigationMoveEvent.DetermineMoveDirection(movement.x, movement.y);

            if (moveDirection != NavigationMoveEvent.Direction.None)
            {
                if (!similarDir)
                    m_ConsecutiveMoveCount = 0;
                m_ConsecutiveMoveCount++;
                m_PrevActionTime = time;
                m_LastMoveVector = movement;
            }
            else
            {
                m_ConsecutiveMoveCount = 0;
            }

            return moveDirection != NavigationMoveEvent.Direction.None;
        }

        void ProcessTabEvent(Event e, EventModifiers modifiers)
        {
            if (e.type == EventType.KeyDown && e.character == '\t')
            {
                var direction = e.shift ? NavigationMoveEvent.Direction.Previous : NavigationMoveEvent.Direction.Next;
                SendFocusBasedEvent(
                    t => NavigationMoveEvent.GetPooled(t.direction,
                        t.input.anyKey ? NavigationDeviceType.Keyboard : NavigationDeviceType.NonKeyboard, t.modifiers),
                    (direction, modifiers, input));
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
        }
    }
}
