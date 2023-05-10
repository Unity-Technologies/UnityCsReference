// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.IntegerTime;

namespace UnityEngine.InputForUI
{
    /// <summary>
    /// Event provider based on InputEvent.
    /// Provides some but not all event types.
    /// Has to be used in conjunction with InputManager or InputSystem provider.
    /// </summary>
    internal class InputEventPartialProvider : IEventProviderImpl
    {
        // This provider doesn't support multiplayer
        private const int kDefaultPlayerId = 0;

        private UnityEngine.Event _ev = new UnityEngine.Event();
        private OperatingSystemFamily _operatingSystemFamily;
        private KeyEvent.ButtonsState _keyboardButtonsState;
        internal EventModifiers _eventModifiers;

        public void Initialize()
        {
            _operatingSystemFamily = SystemInfo.operatingSystemFamily;
            _keyboardButtonsState.Reset();
            _eventModifiers.Reset();
        }

        public void Shutdown()
        {
        }

        public void Update()
        {
            var count = UnityEngine.Event.GetEventCount();

            for (var i = 0; i < count; ++i)
            {
                UnityEngine.Event.GetEventAtIndex(i, _ev);
                UpdateEventModifiers(_ev);

                switch (_ev.type)
                {
                    case EventType.KeyDown or EventType.KeyUp:
                        if (_ev.keyCode != KeyCode.None)
                        {
                            // We can assume it's an event from a keyboard at this point.
                            EventProvider.Dispatch(Event.From(ToKeyEventEvent(_ev)));
                        }
                        else if (TextInputEvent.ShouldBeProcessed(_ev.character))
                            EventProvider.Dispatch(Event.From(ToTextInputEvent(_ev)));
                        break;
                    case EventType.ValidateCommand or EventType.ExecuteCommand:
                        EventProvider.Dispatch(Event.From(ToCommandEvent(_ev)));
                        break;
                }
            }
        }

        public void OnFocusChanged(bool focus)
        {
            if (!focus)
            {
                // Reset all key states on focus lost
                _eventModifiers.Reset();
                _keyboardButtonsState.Reset();
            }
        }

        public bool RequestCurrentState(Event.Type type)
        {
            switch (type)
            {
                case Event.Type.KeyEvent:
                    EventProvider.Dispatch(Event.From(new KeyEvent
                    {
                        type = KeyEvent.Type.State,
                        keyCode = KeyCode.None,
                        buttonsState = _keyboardButtonsState,
                        timestamp = (DiscreteTime)Time.timeAsRational,
                        eventSource = EventSource.Keyboard,
                        playerId = kDefaultPlayerId,
                        eventModifiers = _eventModifiers
                    }));
                    return true;
                default:
                    return false;
            }
        }

        // Not used
        public uint playerCount => 0;

        private DiscreteTime GetTimestamp(in UnityEngine.Event ev)
        {
            return (DiscreteTime)Time.timeAsRational;
        }

        private void UpdateEventModifiers(in UnityEngine.Event ev)
        {
            // Some event modifiers are mapped directly from IMGUI event modifiers.
            // But IMGUI doesn't have separate Left/Right key modifiers, so we use key codes to restore them.
            _eventModifiers.SetPressed(EventModifiers.Modifiers.CapsLock, ev.capsLock);
            _eventModifiers.SetPressed(EventModifiers.Modifiers.FunctionKey, ev.functionKey);
            // TODO no numlock in EventModifiers?

            // We're not guaranteed to get KeyUp event during application losing focus, like try pressing Ctrl+K in editor game view, Ctrl key up will not arrive,
            // or some other key sequence that leads to editor loosing focus, on focus regain keys will not be reset.
            // But ev.modifiers are polled at a time of creation of the event and they do work correctly ...
            // But they are lossy and can't separate between left/right shift.
            // So we use some of them to _unset_ our modifiers until a better way is found.
            if (!ev.shift)
                _eventModifiers.SetPressed(EventModifiers.Modifiers.Shift, false);
            if (!ev.control)
                _eventModifiers.SetPressed(EventModifiers.Modifiers.Ctrl, false);
            if (!ev.alt)
                _eventModifiers.SetPressed(EventModifiers.Modifiers.Alt, false);
            if (!ev.command)
                _eventModifiers.SetPressed(EventModifiers.Modifiers.Meta, false);

            // Not every event we get here will be a key event.
            // Use key events to actually set modifiers as they have more fidelity (we can separate between LeftCtrl/RightCtrl).
            if (ev.isKey && ev.keyCode != KeyCode.None)
            {
                var pressed = ev.type == EventType.KeyDown;
                switch (ev.keyCode)
                {
                    case KeyCode.LeftShift:
                        _eventModifiers.SetPressed(EventModifiers.Modifiers.LeftShift, pressed);
                        break;
                    case KeyCode.RightShift:
                        _eventModifiers.SetPressed(EventModifiers.Modifiers.RightShift, pressed);
                        break;
                    case KeyCode.LeftControl:
                        _eventModifiers.SetPressed(EventModifiers.Modifiers.LeftCtrl, pressed);
                        break;
                    case KeyCode.RightControl:
                        _eventModifiers.SetPressed(EventModifiers.Modifiers.RightCtrl, pressed);
                        break;
                    case KeyCode.LeftAlt:
                        _eventModifiers.SetPressed(EventModifiers.Modifiers.LeftAlt, pressed);
                        break;
                    case KeyCode.RightAlt:
                        _eventModifiers.SetPressed(EventModifiers.Modifiers.RightAlt, pressed);
                        break;
                    case KeyCode.LeftMeta:
                        _eventModifiers.SetPressed(EventModifiers.Modifiers.LeftMeta, pressed);
                        break;
                    case KeyCode.RightMeta:
                        _eventModifiers.SetPressed(EventModifiers.Modifiers.RightMeta, pressed);
                        break;
                    case KeyCode.Numlock:
                        _eventModifiers.SetPressed(EventModifiers.Modifiers.Numlock, pressed);
                        break;
                    default:
                        break;
                }
            }
        }

        private KeyEvent ToKeyEventEvent(in UnityEngine.Event ev)
        {
            // handle keyboard
            var oldState = _keyboardButtonsState.IsPressed(ev.keyCode);
            var newState = ev.type == EventType.KeyDown;
            _keyboardButtonsState.SetPressed(ev.keyCode, newState);

            return new KeyEvent
            {
                type = newState
                    ? (oldState ? KeyEvent.Type.KeyRepeated : KeyEvent.Type.KeyPressed)
                    : KeyEvent.Type.KeyReleased,
                keyCode = ev.keyCode, // TODO this needs to be layout independent, expose physical keys in InputEvent
                buttonsState = _keyboardButtonsState,
                timestamp = GetTimestamp(ev),
                eventSource = EventSource.Keyboard,
                playerId = kDefaultPlayerId,
                eventModifiers = _eventModifiers
            };
        }

        private TextInputEvent ToTextInputEvent(in UnityEngine.Event ev)
        {
            return new TextInputEvent()
            {
                character = ev.character,
                timestamp = GetTimestamp(ev),
                eventSource = EventSource.Keyboard,
                playerId = kDefaultPlayerId,
                eventModifiers = _eventModifiers
            };
        }

        private IDictionary<string, CommandEvent.Command> _IMGUICommandToInputForUICommandType =
            new Dictionary<string, CommandEvent.Command>
            {
                {"Cut", CommandEvent.Command.Cut},
                {"Copy", CommandEvent.Command.Copy},
                {"Paste", CommandEvent.Command.Paste},
                {"SelectAll", CommandEvent.Command.SelectAll},
                {"DeselectAll", CommandEvent.Command.DeselectAll},
                {"InvertSelection", CommandEvent.Command.InvertSelection},
                {"Duplicate", CommandEvent.Command.Duplicate},
                {"Rename", CommandEvent.Command.Rename},
                {"Delete", CommandEvent.Command.Delete},
                {"SoftDelete", CommandEvent.Command.SoftDelete},
                {"Find", CommandEvent.Command.Find},
                {"SelectChildren", CommandEvent.Command.SelectChildren},
                {"SelectPrefabRoot", CommandEvent.Command.SelectPrefabRoot},
                {"UndoRedoPerformed", CommandEvent.Command.UndoRedoPerformed},
                {"OnLostFocus", CommandEvent.Command.OnLostFocus},
                {"NewKeyboardFocus", CommandEvent.Command.NewKeyboardFocus},
                {"ModifierKeysChanged", CommandEvent.Command.ModifierKeysChanged},
                {"EyeDropperUpdate", CommandEvent.Command.EyeDropperUpdate},
                {"EyeDropperClicked", CommandEvent.Command.EyeDropperClicked},
                {"EyeDropperCancelled", CommandEvent.Command.EyeDropperCancelled},
                {"ColorPickerChanged", CommandEvent.Command.ColorPickerChanged},
                {"FrameSelected", CommandEvent.Command.FrameSelected},
                {"FrameSelectedWithLock", CommandEvent.Command.FrameSelectedWithLock}
            };

        private CommandEvent ToCommandEvent(in UnityEngine.Event ev)
        {
            if (!_IMGUICommandToInputForUICommandType.TryGetValue(ev.commandName, out var cmd))
                Debug.LogWarning($"Unsupported command name '{ev.commandName}'");

            return new CommandEvent()
            {
                type = ev.type == EventType.ValidateCommand ? CommandEvent.Type.Validate : CommandEvent.Type.Execute,
                command = cmd,
                timestamp = GetTimestamp(ev),
                eventSource = EventSource.Unspecified,
                playerId = kDefaultPlayerId,
                eventModifiers = _eventModifiers
            };
        }
    }
}
