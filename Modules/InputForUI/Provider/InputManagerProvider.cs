// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.IntegerTime;
using UnityEngine;

namespace UnityEngine.InputForUI
{
    /// <summary>
    /// Event provider based on InputManager.
    /// Relies on partial InputEvent provider for some event types.
    /// </summary>
    internal class InputManagerProvider : IEventProviderImpl
    {
        private InputEventPartialProvider _inputEventPartialProvider;

        // This provider doesn't support multiplayer
        private const int kDefaultPlayerId = 0;

        private string _compositionString = string.Empty;

        private Configuration _configuration = Configuration.GetDefaultConfiguration();
        private IInput _input = new Input();
        private ITime _time = new Time();

        private EventModifiers _eventModifiers => _inputEventPartialProvider._eventModifiers;

        private NavigationEventRepeatHelper _navigationEventRepeatHelper = new();

        private const int kMaxMouseButtons = 5;
        private PointerState _mouseState;

        private bool _isPenPresent;
        private bool _seenAtLeastOnePenPosition;
        private Vector2 _lastSeenPenPositionForDetection;
        private PointerState _penState;
        private PenData _lastPenData;

        private Dictionary<int, int> _touchFingerIdToFingerIndex = new();
        private int _touchNextFingerIndex;
        private PointerState _touchState;

        private const float kSmallestReportedMovementSqrDist = 0.01f;
        const float kScrollUGUIScaleFactor = 3.0f;

        public InputManagerProvider() { }

        // For unit testing
        internal InputManagerProvider(IInput inputOverride, ITime timeOverride)
        {
            _input = inputOverride;
            _time = timeOverride;
        }

        public void Initialize()
        {
            _inputEventPartialProvider ??= new InputEventPartialProvider();
            _inputEventPartialProvider.Initialize();
            _inputEventPartialProvider._sendNavigationEventOnTabKey = true;

            _mouseState.Reset();

            _isPenPresent = false;
            _seenAtLeastOnePenPosition = false;
            _lastSeenPenPositionForDetection = default;
            _penState.Reset();
            _lastPenData = default;

            _touchFingerIdToFingerIndex.Clear();
            _touchNextFingerIndex = 0;
            _touchState.Reset();
        }

        public void Shutdown()
        {
        }

        public void Update()
        {
            _inputEventPartialProvider.Update();

            var currentTime = (DiscreteTime)_time.timeAsRational;

            DetectPen();

            // Report touch events
            var touchWasReported = false;
            if (_input.touchSupported)
                touchWasReported = CheckTouchEvents(currentTime);

            // GetPenEvent doesn't work because on Windows PenStatus is always Contact,
            // hence we can't detect if pen is actually in contact with the screen,
            // so we need to use contactType which is only present via GetLastPenContactEvent.
            var penWasReported = false;
            if (!touchWasReported && _isPenPresent)
                penWasReported = CheckPenEvent(currentTime, _input.GetLastPenContactEvent());
            else
                _penState.Reset();

            // Don't report mouse at all if pen was reported this frame,
            // this filters out any events based on simulated mouse movements.
            if (!penWasReported && !touchWasReported && _input.mousePresent)
            {
                CheckMouseEvents(currentTime);
            }
            else
            {
                // Silently update the internal mouse state to avoid sending mouse events on the next frame.
                CheckMouseEvents(currentTime, muted:true);
                _mouseState.LastPositionValid = false;
            }

            // Always process scroll
            if (_input.mousePresent)
                CheckMouseScroll(currentTime);

            CheckIfIMEChanged(currentTime);

            DirectionNavigation(currentTime);

            SubmitCancelNavigation(currentTime);

            NextPreviousNavigation(currentTime);
        }

        private bool CheckTouchEvents(DiscreteTime currentTime)
        {
            var allAreReleased = true;
            var touchWasReported = false;
            for (var i = 0; i < _input.touchCount; ++i)
            {
                var touch = _input.GetTouch(i);

                if (touch.type == TouchType.Indirect || touch.phase == TouchPhase.Stationary)
                    continue;

                if (!_touchFingerIdToFingerIndex.TryGetValue(touch.fingerId, out var fingerIndex))
                {
                    fingerIndex = _touchNextFingerIndex++;
                    _touchFingerIdToFingerIndex.Add(touch.fingerId, fingerIndex);
                }

                // Flip Y Coordinates.
                var position = MultiDisplayBottomLeftToPanelPosition(touch.position, out var targetDisplay);
                var deltaPosition = ScreenBottomLeftToPanelDelta(touch.deltaPosition);

                var type = PointerEvent.Type.PointerMoved;
                var button = PointerEvent.Button.None;

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                    {
                        type = PointerEvent.Type.ButtonPressed;
                        button = PointerEvent.Button.FingerInTouch;
                        allAreReleased = false;
                        _touchState.OnButtonDown(currentTime, button);
                        break;
                    }
                    case TouchPhase.Ended:
                    {
                        type = PointerEvent.Type.ButtonReleased;
                        button = PointerEvent.Button.FingerInTouch;
                        _touchState.OnButtonUp(currentTime, button);
                        break;
                    }
                    case TouchPhase.Canceled:
                    {
                        type = PointerEvent.Type.TouchCanceled;
                        button = PointerEvent.Button.FingerInTouch;
                        _touchState.OnButtonUp(currentTime, button);
                        break;
                    }
                    case TouchPhase.Moved:
                    {
                        allAreReleased = false;
                        break;
                    }
                }

                EventProvider.Dispatch(Event.From(new PointerEvent()
                {
                    type = type,
                    pointerIndex = fingerIndex,
                    position = position,
                    deltaPosition = deltaPosition,
                    scroll = Vector2.zero,
                    displayIndex = targetDisplay,
                    tilt = AzimuthAndAlitutudeToTilt(touch.altitudeAngle, touch.azimuthAngle),
                    twist = 0.0f,
                    // TODO Should we clamp the pressure between 0 and 1 ?
                    // TODO Can it be possible for touch.pressure to be above the touch.maximumPossiblePressure ?
                    pressure = Mathf.Abs(touch.maximumPossiblePressure) > Mathf.Epsilon
                        ? touch.pressure / touch.maximumPossiblePressure
                        : 1.0f,
                    isInverted = false,
                    button = button,
                    buttonsState = _touchState.ButtonsState,
                    clickCount = _touchState.ClickCount,
                    timestamp = currentTime,
                    eventSource = EventSource.Touch,
                    playerId = kDefaultPlayerId,
                    eventModifiers = _eventModifiers
                }));
                touchWasReported = true;
            }

            if (allAreReleased)
            {
                _touchNextFingerIndex = 0;
                _touchFingerIdToFingerIndex.Clear();
            }

            return touchWasReported;
        }

        private void DetectPen()
        {
            if (_isPenPresent)
                return;

            // Detect pen by checking for not-insignificant movement of the pen.
            var position = _input.GetLastPenContactEvent().position;
            if (_seenAtLeastOnePenPosition)
            {
                var sqrDist = (position - _lastSeenPenPositionForDetection).sqrMagnitude;
                _isPenPresent = sqrDist >= kSmallestReportedMovementSqrDist;
            }
            else
            {
                _lastSeenPenPositionForDetection = position;
                _seenAtLeastOnePenPosition = true;
            }
        }

        private static PointerEvent.Button PenStatusToButton(PenStatus status)
        {
            // PenStatus is unreliable,
            // in Windows editor it has Contact set all the time.
            // So we use just Eraser/Barrel flags to figure out potential buttons.
            if ((status & PenStatus.Eraser) != 0)
                return PointerEvent.Button.PenEraserInTouch;
            if ((status & PenStatus.Barrel) != 0)
                return PointerEvent.Button.PenBarrelButton;
            return PointerEvent.Button.PenTipInTouch;
        }

        private bool CheckPenEvent(DiscreteTime currentTime, in PenData currentPenData)
        {
            // TODO currentPenData.position is in panel coordinates, but we don't know anything about display index
            var position = currentPenData.position;
            var targetDisplay = 0;

            // For some reason deltaPos is always (0, 0) on Windows, so reverting to the manual computation.
            var delta = _penState.LastPositionValid ? position - _penState.LastPosition : Vector2.zero;

            PointerEvent.Type type;
            var button = PointerEvent.Button.None;
            if (currentPenData.contactType != _lastPenData.contactType)
            {
                switch (currentPenData.contactType)
                {
                    case PenEventType.PenDown:
                    {
                        type = PointerEvent.Type.ButtonPressed;
                        button = PenStatusToButton(currentPenData.penStatus);
                        _penState.OnButtonDown(currentTime, button);
                        break;
                    }
                    case PenEventType.PenUp:
                        type = PointerEvent.Type.ButtonReleased;
                        // Use the last penStatus to figure out which button was _released_,
                        // as current penStatus doesn't contain the released button.
                        button = PenStatusToButton(_lastPenData.penStatus);
                        _penState.OnButtonUp(currentTime, button);
                        break;
                    default:
                        type = PointerEvent.Type.PointerMoved; // ignore the contact event
                        break;
                }
            }
            else
                type = PointerEvent.Type.PointerMoved;

            _lastPenData = currentPenData;

            var penWasReported = false;
            // don't report empty move events unless it's a first event
            if (type != PointerEvent.Type.PointerMoved || _penState.LastPositionValid == false ||
                delta.sqrMagnitude >= kSmallestReportedMovementSqrDist)
            {
                EventProvider.Dispatch(Event.From(new PointerEvent()
                {
                    type = type,
                    pointerIndex = 0,
                    position = position,
                    deltaPosition = delta,
                    scroll = Vector2.zero,
                    displayIndex = targetDisplay,
                    tilt = currentPenData.tilt,
                    twist = currentPenData.twist,
                    pressure = currentPenData.pressure,
                    isInverted = (currentPenData.penStatus & PenStatus.Inverted) != 0,
                    button = button,
                    buttonsState = _penState.ButtonsState,
                    clickCount = _penState.ClickCount,
                    timestamp = currentTime,
                    eventSource = EventSource.Pen,
                    playerId = kDefaultPlayerId,
                    eventModifiers = _eventModifiers
                }));
                penWasReported = true;
            }

            _penState.OnMove(currentTime, position, targetDisplay);
            return penWasReported;
        }

        private void CheckMouseEvents(DiscreteTime currentTime, bool muted = false)
        {
            var position = MultiDisplayBottomLeftToPanelPosition(_input.mousePosition, out var targetDisplay);

            if (_mouseState.LastPositionValid)
            {
                var delta = position - _mouseState.LastPosition;
                if (delta.sqrMagnitude >= kSmallestReportedMovementSqrDist)
                {
                    if (!muted)
                        EventProvider.Dispatch(Event.From(new PointerEvent()
                        {
                            type = PointerEvent.Type.PointerMoved,
                            pointerIndex = 0,
                            position = position,
                            deltaPosition = delta,
                            scroll = Vector2.zero,
                            displayIndex = targetDisplay,
                            tilt = Vector2.zero,
                            twist = 0.0f,
                            pressure = 0.0f,
                            isInverted = false,
                            button = 0,
                            buttonsState = _mouseState.ButtonsState,
                            clickCount = 0,
                            timestamp = currentTime,
                            eventSource = EventSource.Mouse,
                            playerId = kDefaultPlayerId,
                            eventModifiers = _eventModifiers
                        }));

                    // Only adjust lastMousePosition if we send a PointerMoveEvent, otherwise let the delta accumulate.
                    _mouseState.OnMove(currentTime, position, targetDisplay);
                }
            }
            else
                _mouseState.OnMove(currentTime, position, targetDisplay);

            for (var buttonIndex = 0; buttonIndex < kMaxMouseButtons; ++buttonIndex)
            {
                var button = PointerEvent.ButtonFromButtonIndex(buttonIndex);
                var previousState = _mouseState.ButtonsState.Get(button);
                var isDown = _input.GetMouseButtonDown(buttonIndex);
                var isUp = _input.GetMouseButtonUp(buttonIndex);
                var currentState = _input.GetMouseButton(buttonIndex);

                var it = ButtonEventsIterator.FromState(previousState, isDown, isUp, currentState);
                var previousStateInIterator = previousState;
                while (it.MoveNext())
                {
                    _mouseState.OnButtonChange(currentTime, button, previousStateInIterator, it.Current);
                    previousStateInIterator = it.Current;

                    if (!muted)
                        EventProvider.Dispatch(Event.From(new PointerEvent()
                        {
                            type = it.Current ? PointerEvent.Type.ButtonPressed : PointerEvent.Type.ButtonReleased,
                            pointerIndex = 0,
                            position = _mouseState.LastPosition, // TODO Keep old mouse position so not to allow movement during mouse button events.
                            deltaPosition = Vector2.zero,
                            scroll = Vector2.zero,
                            displayIndex = _mouseState.LastDisplayIndex,
                            tilt = Vector2.zero,
                            twist = 0.0f,
                            pressure = 0.0f,
                            isInverted = false,
                            button = button,
                            buttonsState = _mouseState.ButtonsState,
                            clickCount = _mouseState.ClickCount,
                            timestamp = currentTime,
                            eventSource = EventSource.Mouse,
                            playerId = kDefaultPlayerId,
                            eventModifiers = _eventModifiers
                        }));
                }
            }
        }

        private void CheckMouseScroll(DiscreteTime currentTime)
        {
            var scrollDelta = _input.mouseScrollDelta;
            if (scrollDelta.sqrMagnitude < kSmallestReportedMovementSqrDist)
                return;

            // Retrieve position and target display
            Vector2 position;
            var targetDisplay = 0;
            if (_mouseState.LastPositionValid)
            {
                position = _mouseState.LastPosition;
                targetDisplay = _mouseState.LastDisplayIndex;
            }
            else // we have no other way but to poll the position
                position = MultiDisplayBottomLeftToPanelPosition(_input.mousePosition, out targetDisplay);

            // Make it look similar to IMGUI event scroll values.
            scrollDelta.x *= kScrollUGUIScaleFactor;
            scrollDelta.y *= -kScrollUGUIScaleFactor;

            EventProvider.Dispatch(Event.From(new PointerEvent()
            {
                type = PointerEvent.Type.Scroll,
                pointerIndex = 0,
                position = position,
                deltaPosition = Vector2.zero,
                scroll = scrollDelta,
                displayIndex = targetDisplay,
                tilt = Vector2.zero,
                twist = 0.0f,
                pressure = 0.0f,
                isInverted = false,
                button = 0,
                buttonsState = _mouseState.ButtonsState,
                clickCount = 0,
                timestamp = currentTime,
                eventSource = EventSource.Mouse,
                playerId = kDefaultPlayerId,
                eventModifiers = _eventModifiers
            }));
        }

        private PointerEvent ToPointerStateEvent(DiscreteTime currentTime, in PointerState state, EventSource eventSource)
        {
            return new PointerEvent
            {
                type = PointerEvent.Type.State,
                pointerIndex = 0,
                position = state.LastPosition,
                deltaPosition = Vector2.zero,
                scroll = Vector2.zero,
                displayIndex = state.LastDisplayIndex,
                tilt = eventSource == EventSource.Pen ? _lastPenData.tilt : Vector2.zero,
                twist = eventSource == EventSource.Pen ? _lastPenData.twist : 0.0f,
                pressure = eventSource == EventSource.Pen ? _lastPenData.pressure : 0.0f,
                isInverted = eventSource == EventSource.Pen && ((_lastPenData.penStatus & PenStatus.Inverted) != 0),
                button = 0,
                buttonsState = state.ButtonsState,
                clickCount = 0,
                timestamp = currentTime,
                eventSource = eventSource,
                playerId = kDefaultPlayerId,
                eventModifiers = _eventModifiers
            };
        }

        private void NextPreviousNavigation(DiscreteTime currentTime)
        {
            var navigateNext = (InputManagerGetButtonDownOrDefault(_configuration.NavigateNextButton) ? 1 : 0) +
                               (InputManagerGetButtonDownOrDefault(_configuration.NavigatePreviousButton) ? -1 : 0);

            if (navigateNext != 0)
            {
                // same as https://github.cds.internal.unity3d.com/unity/unity/blob/trunk/Modules/UIElements/Core/DefaultEventSystem.cs#L596
                if (_eventModifiers.isShiftPressed)
                    navigateNext = -navigateNext;

                EventProvider.Dispatch(Event.From(new NavigationEvent
                {
                    type = NavigationEvent.Type.Move,
                    direction = navigateNext >= 0 ? NavigationEvent.Direction.Next : NavigationEvent.Direction.Previous,
                    timestamp = currentTime,
                    eventSource = GetEventSourceFromPressedKey(),
                    playerId = kDefaultPlayerId,
                    eventModifiers = _eventModifiers
                }));
            }
        }

        private void SubmitCancelNavigation(DiscreteTime currentTime)
        {
            if (InputManagerGetButtonDownOrDefault(_configuration.SubmitButton))
            {
                EventProvider.Dispatch(Event.From(new NavigationEvent
                {
                    type = NavigationEvent.Type.Submit,
                    direction = NavigationEvent.Direction.None,
                    timestamp = currentTime,
                    eventSource = GetEventSourceFromPressedKey(),
                    playerId = kDefaultPlayerId,
                    eventModifiers = _eventModifiers
                }));
            }

            if (InputManagerGetButtonDownOrDefault(_configuration.CancelButton))
            {
                EventProvider.Dispatch(Event.From(new NavigationEvent
                {
                    type = NavigationEvent.Type.Cancel,
                    direction = NavigationEvent.Direction.None,
                    timestamp = currentTime,
                    eventSource = GetEventSourceFromPressedKey(),
                    playerId = kDefaultPlayerId,
                    eventModifiers = _eventModifiers
                }));
            }
        }

        private void DirectionNavigation(DiscreteTime currentTime)
        {
            var (move, axesButtonWerePressed) = ReadCurrentNavigationMoveVector();
            var direction = NavigationEvent.DetermineMoveDirection(move);
            if (direction == NavigationEvent.Direction.None)
                _navigationEventRepeatHelper.Reset();
            else if (_navigationEventRepeatHelper.ShouldSendMoveEvent(currentTime, direction, axesButtonWerePressed))
            {
                EventSource eventSource = GetEventSourceFromPressedKey();

                // GetEventSourceFromPressedKey() doesn't check for axes events so we need to use axesButtonWerePressed
                // for now.
                // If there's a move position and nothing is detected by GetEventSourceFromPressedKey(), then
                // we assume the event comes from a Gamepad.
                if (eventSource == EventSource.Unspecified && !axesButtonWerePressed)
                    eventSource = EventSource.Gamepad;

                EventProvider.Dispatch(Event.From(new NavigationEvent
                {
                    type = NavigationEvent.Type.Move,
                    direction = direction,
                    timestamp = currentTime,
                    eventSource = eventSource,
                    playerId = kDefaultPlayerId,
                    eventModifiers = _eventModifiers
                }));
            }
        }

        private void CheckIfIMEChanged(DiscreteTime currentTime)
        {
            var currentCompositionString = _input.compositionString;
            if (string.IsNullOrEmpty(_compositionString) != string.IsNullOrEmpty(currentCompositionString) &&
                _compositionString != currentCompositionString)
            {
                _compositionString = currentCompositionString;
                EventProvider.Dispatch(Event.From(ToIMECompositionEvent(currentTime, _compositionString)));
            }
        }

        public void OnFocusChanged(bool focus)
        {
            _inputEventPartialProvider.OnFocusChanged(focus);
        }

        public bool RequestCurrentState(Event.Type type)
        {
            if (_inputEventPartialProvider.RequestCurrentState(type))
                return true;

            var currentTime = (DiscreteTime)_time.timeAsRational;

            switch (type)
            {
                case Event.Type.PointerEvent:
                {
                    if (_touchState.LastPositionValid)
                        EventProvider.Dispatch(Event.From(ToPointerStateEvent(currentTime, _touchState, EventSource.Touch)));
                    if (_penState.LastPositionValid)
                        EventProvider.Dispatch(Event.From(ToPointerStateEvent(currentTime, _penState, EventSource.Pen)));
                    if (_mouseState.LastPositionValid)
                        EventProvider.Dispatch(Event.From(ToPointerStateEvent(currentTime, _mouseState, EventSource.Mouse)));
                    else
                    {
                        // TODO maybe it's reasonable to poll and dispatch mouse state here anyway?
                    }

                    return _touchState.LastPositionValid ||
                           _penState.LastPositionValid ||
                           _mouseState.LastPositionValid;
                }
                case Event.Type.IMECompositionEvent:
                    EventProvider.Dispatch(Event.From(ToIMECompositionEvent(currentTime, _compositionString)));
                    return true;
                default:
                    return false;
            }
        }

        public uint playerCount => 1; // not supported

        /// <summary>
        /// Returns the source of the navigation event
        /// </summary>
        /// <remarks> Checks if keyboard or joystick keys were pressed and returns the appropriate
        /// <see cref="EventSource"/> </remarks>
        private EventSource GetEventSourceFromPressedKey()
        {
            if (InputManagerKeyboardWasPressed())
                return EventSource.Keyboard;

            if (InputManagerJoystickWasPressed())
                return EventSource.Gamepad;

            return EventSource.Unspecified;
        }

        /// <summary>
        /// Checks if the button pressed was from a Keyboard
        /// </summary>
        /// TODO should this be checked with from joystick button state instead? We don't have joystick button state
        private bool InputManagerJoystickWasPressed()
        {
            const KeyCode keyStart = KeyCode.Joystick1Button0;
            const KeyCode keyEnd = KeyCode.Joystick8Button19;
            // Check all joystick keycodes only
            for (var key = keyStart; key <= keyEnd; key++)
            {
                if (_input.GetKey(key))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if the button pressed was from a Keyboard
        /// </summary>
        /// TODO should this be checked with from keyboard button state instead? We don't have keyboard button state
        private bool InputManagerKeyboardWasPressed()
        {
            const KeyCode keyStart = KeyCode.None;
            const KeyCode keyEnd = KeyCode.Menu;
            // Check all keyboard keycodes only
            for (var key = keyStart; key <= keyEnd; key++)
            {
                if (_input.GetKey(key))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Tries to read input manager axis, returns default value if axis doesn't exist.
        /// </summary>
        private float InputManagerGetAxisRawOrDefault(string axisName)
        {
            try
            {
                return !string.IsNullOrEmpty(axisName) ? _input.GetAxisRaw(axisName) : default;
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Tries to read input manager axis button down state, returns default value if axis doesn't exist.
        /// </summary>
        private bool InputManagerGetButtonDownOrDefault(string axisName)
        {
            try
            {
                var validAxis = !string.IsNullOrEmpty(axisName);
                return validAxis ? _input.GetButtonDown(axisName) : default;
            }
            catch
            {
                return default;
            }
        }

        private (Vector2, bool) ReadCurrentNavigationMoveVector()
        {
            var move = new Vector2(
                InputManagerGetAxisRawOrDefault(_configuration.HorizontalAxis),
                InputManagerGetAxisRawOrDefault(_configuration.VerticalAxis));

            var btnPressed = false;
            if (InputManagerGetButtonDownOrDefault(_configuration.HorizontalAxis))
            {
                if (move.x < 0)
                    move.x = -1f;
                else if (move.x > 0)
                    move.x = 1f;

                btnPressed = true;
            }

            if (InputManagerGetButtonDownOrDefault(_configuration.VerticalAxis))
            {
                if (move.y < 0)
                    move.y = -1f;
                else if (move.y > 0)
                    move.y = 1f;
                btnPressed = true;
            }

            return (move, btnPressed);
        }

        private IMECompositionEvent ToIMECompositionEvent(DiscreteTime currentTime, string compositionString)
        {
            return new IMECompositionEvent()
            {
                compositionString = compositionString,
                timestamp = currentTime,
                eventSource = EventSource.Unspecified,
                playerId = kDefaultPlayerId,
                eventModifiers = _eventModifiers
            };
        }

        // copied from PointerEvents.cs
        /// <summary>
        /// Converts touch or stylus tilt to azimuth angle.
        /// </summary>
        /// <param name="tilt">Angle relative to the X and Y axis, in radians. abs(tilt.y) must be < pi/2</param>
        /// <returns>Azimuth angle as determined by tilt along x and y axese.</returns>
        internal static float TiltToAzimuth(Vector2 tilt)
        {
            float azimuth = 0f;
            if (tilt.x != 0)
            {
                azimuth = Mathf.PI / 2 - Mathf.Atan2(-Mathf.Cos(tilt.x) * Mathf.Sin(tilt.y), Mathf.Cos(tilt.y) * Mathf.Sin(tilt.x));
                if (azimuth < 0) // fix range to [0, 2*pi)
                    azimuth += 2 * Mathf.PI;
                // follow UIKit conventions where azimuth is 0 when the cap end of the stylus points along the positive x axis of the device's screen
                if (azimuth >= (Mathf.PI / 2))
                    azimuth -= Mathf.PI / 2;
                else
                    azimuth += (3 * Mathf.PI / 2);
            }

            return azimuth;
        }

        // copied from PointerEvents.cs
        /// <summary>
        /// Converts touch or stylus azimuth and altitude to tilt
        /// </summary>
        /// <param name="tilt">Angle relative to the X and Y axis, in radians. abs(tilt.y) must be < pi/2</param>
        /// <returns>Azimuth angle as determined by tilt along x and y axese.</returns>
        internal static Vector2 AzimuthAndAlitutudeToTilt(float altitude, float azimuth)
        {
            Vector2 t = new Vector2(0, 0);

            t.x = Mathf.Atan(Mathf.Cos(azimuth) * Mathf.Cos(altitude) / Mathf.Sin(azimuth));
            t.y = Mathf.Atan(Mathf.Cos(azimuth) * Mathf.Sin(altitude) / Mathf.Sin(azimuth));

            return t;
        }

        // copied from PointerEvents.cs
        /// <summary>
        /// Converts touch or stylus tilt to altitude angle.
        /// </summary>
        /// <param name="tilt">Angle relative to the X and Y axis, in radians. abs(tilt.y) must be < pi/2</param>
        /// <returns>Altitude angle as determined by tilt along x and y axese.</returns>
        internal static float TiltToAltitude(Vector2 tilt)
        {
            return Mathf.PI / 2 - Mathf.Acos(Mathf.Cos(tilt.x) * Mathf.Cos(tilt.y));
        }

        // copied from UIElementsRuntimeUtility.cs
        private static Vector2 MultiDisplayBottomLeftToPanelPosition(Vector2 position, out int targetDisplay)
        {
            var screenPosition = MultiDisplayToLocalScreenPosition(position, out var targetDisplayMaybe);
            targetDisplay = targetDisplayMaybe.GetValueOrDefault();
            return ScreenBottomLeftToPanelPosition(screenPosition, targetDisplay);
        }

        // copied from UIElementsRuntimeUtility.cs
        private static Vector2 MultiDisplayToLocalScreenPosition(Vector2 position, out int? targetDisplay)
        {
            var relativePosition = Display.RelativeMouseAt(position);
            if (relativePosition != Vector3.zero)
            {
                targetDisplay = (int)relativePosition.z;
                return relativePosition;
            }
            targetDisplay = Display.activeEditorGameViewTarget;
            return position;
        }

        // copied from UIElementsRuntimeUtility.cs
        private static Vector2 ScreenBottomLeftToPanelPosition(Vector2 position, int targetDisplay)
        {
            // Flip positions Y axis between input and UITK
            var screenHeight = Screen.height;
            if (targetDisplay > 0 && targetDisplay < Display.displays.Length)
                screenHeight = Display.displays[targetDisplay].systemHeight;
            position.y = screenHeight - position.y;
            return position;
        }

        // copied from UIElementsRuntimeUtility.cs
        private static Vector2 ScreenBottomLeftToPanelDelta(Vector2 delta)
        {
            // Flip deltas Y axis between input and UITK
            delta.y = -delta.y;
            return delta;
        }

        private struct ButtonEventsIterator : IEnumerator
        {
            // true means button was pressed, false means button was released
            public bool Current => _bit % 2 == 0;

            // TODO add comments to explain what is going on in here
            public bool MoveNext()
            {
                do
                {
                    _bit++;
                    if ((_mask & (1u << _bit)) != 0)
                        return true;
                } while (_bit < kMaxBits);
                return false;
            }

            public void Reset()
            {
                // TODO could we start from 0 instead?
                _bit = -1;
            }

            object IEnumerator.Current => Current;

            // represents sequence of pressed->released->pressed->released->... as bits, first bit being pressed, second bit being released, etc
            private uint _mask;
            private int _bit;
            private const uint kWasPressed = 0x01;
            private const uint kWasReleased = 0x02;
            private const int kMaxBits = 4;

            /// <summary>
            /// This method decodes 4 states from InputManager into a sequence of one or more events.
            /// Given:
            /// P - previous state of the button
            /// D - Input.GetMouseButtonDown value
            /// U - Input.GetMouseButtonUp value
            /// C - Input.GetMouseButton value
            ///
            /// Then:
            /// P D U C   Example       Explanation
            /// 0 0 0 0   0->0          Nothing happened
            /// 0 0 0 1                 Not possible
            /// 0 0 1 0   0->0          Already released button released again?
            /// 0 0 1 1                 Not possible
            /// 0 1 0 0                 Not possible
            /// 0 1 0 1   0->1          Button got pressed
            /// 0 1 1 0   0->(1->0)+    Button was pressed and then released 1 or more times
            /// 0 1 1 1   0->(1->0)+->1 Button was pressed and then released 1 or more times and then pressed
            /// 1 0 0 0                 Not possible
            /// 1 0 0 1   1->1          Nothing happened
            /// 1 0 1 0   1->0          Button got released
            /// 1 0 1 1                 Not possible, Up should result in current being 0
            /// 1 1 0 0                 Not possible, Down should result in current being 1
            /// 1 1 0 1   1->1          Already pressed button pressed again?
            /// 1 1 1 0   1->(0->1)+->0 Button was released and then pressed 1 or more times and then released
            /// 1 1 1 1   1->(0->1)+    Button was released and then pressed 1 or more times
            /// </summary>
            /// <returns>Mask of possible events</returns>
            public static ButtonEventsIterator FromState(bool previous, bool down, bool up, bool current)
            {
                // TODO it's all nice and dandy, but apparently input manager down up are unreliable
                // When using Wacom on Windows and using Wacom Touch mode, tapping on tablet generates:
                // - Mouse Down (previous = false, down = true, up = true, current = true)
                // - Mouse Up
                // which in reality should been (previous = false, down = true, up = true, current = false).
                // Reverting to less precise table until that is fixed
                var mask = (previous == false && current == true) ? kWasPressed :
                    (previous == true && current == false) ? kWasReleased :
                    0;
                return new ButtonEventsIterator
                {
                    _mask = mask,
                    _bit = -1
                };
            }
        }

        public struct Configuration
        {
            public string HorizontalAxis;

            public string VerticalAxis;

            public string SubmitButton;

            public string CancelButton;

            public string NavigateNextButton;

            public string NavigatePreviousButton;

            public float InputActionsPerSecond;

            public float RepeatDelay;

            public static Configuration GetDefaultConfiguration()
            {
                return new Configuration
                {
                    HorizontalAxis = "Horizontal",
                    VerticalAxis = "Vertical",
                    SubmitButton = "Submit",
                    CancelButton = "Cancel",
                    NavigateNextButton = "Next",
                    NavigatePreviousButton = "Previous",
                    InputActionsPerSecond = 10,
                    RepeatDelay = 0.5f,
                };
            }
        }

        internal interface IInput
        {
            string compositionString { get; }
            bool GetKey(KeyCode keyCode);
            bool GetKeyDown(KeyCode keyCode);
            bool GetButtonDown(string button);
            float GetAxisRaw(string axis);
            PenData GetPenEvent(int index);
            PenData GetLastPenContactEvent();
            bool touchSupported { get; }
            int touchCount { get; }
            Touch GetTouch(int index);
            bool mousePresent { get; }
            bool GetMouseButton(int button);
            bool GetMouseButtonDown(int button);
            bool GetMouseButtonUp(int button);
            Vector3 mousePosition { get; }
            Vector2 mouseScrollDelta { get; }
        }

        private class Input : IInput
        {
            public string compositionString => UnityEngine.Input.compositionString;
            public bool GetKey(KeyCode key) => UnityEngine.Input.GetKey(key);
            public bool GetKeyDown(KeyCode key) => UnityEngine.Input.GetKeyDown(key);
            public bool GetButtonDown(string button) => UnityEngine.Input.GetButtonDown(button);
            public float GetAxisRaw(string axis) => UnityEngine.Input.GetAxisRaw(axis);
            public PenData GetPenEvent(int index) => UnityEngine.Input.GetPenEvent(index);
            public PenData GetLastPenContactEvent() => UnityEngine.Input.GetLastPenContactEvent();
            public bool touchSupported => UnityEngine.Input.touchSupported;
            public int touchCount => UnityEngine.Input.touchCount;
            public Touch GetTouch(int index) => UnityEngine.Input.GetTouch(index);
            public bool mousePresent => UnityEngine.Input.mousePresent;
            public bool GetMouseButton(int button) => UnityEngine.Input.GetMouseButton(button);
            public bool GetMouseButtonDown(int button) => UnityEngine.Input.GetMouseButtonDown(button);
            public bool GetMouseButtonUp(int button) => UnityEngine.Input.GetMouseButtonUp(button);
            public Vector3 mousePosition => UnityEngine.Input.mousePosition;
            public Vector2 mouseScrollDelta => UnityEngine.Input.mouseScrollDelta;
        }

        internal interface ITime
        {
            Unity.IntegerTime.RationalTime timeAsRational { get; }
        }

        private class Time : ITime
        {
            public Unity.IntegerTime.RationalTime timeAsRational => UnityEngine.Time.timeAsRational;
        }
    }
}
