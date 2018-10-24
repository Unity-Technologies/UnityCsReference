// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    [Serializable]
    public struct KeyCombination
    {
        [SerializeField]
        KeyCode m_KeyCode;
        [SerializeField]
        ShortcutModifiers m_Modifiers;

        public KeyCode keyCode => m_KeyCode;
        public ShortcutModifiers modifiers => m_Modifiers;

        public KeyCombination(KeyCode keyCode, ShortcutModifiers shortcutModifiers = ShortcutModifiers.None)
        {
            m_KeyCode = keyCode;
            m_Modifiers = shortcutModifiers;
        }

        internal static KeyCombination FromKeyboardInput(Event evt)
        {
            return FromKeyboardInput(evt.keyCode, evt.modifiers);
        }

        internal static KeyCombination FromKeyboardInput(KeyCode keyCode, EventModifiers modifiers)
        {
            return new KeyCombination(keyCode, ConvertEventModifiersToShortcutModifiers(modifiers, false));
        }

        internal static KeyCombination ParseLegacyBindingString(string binding)
        {
            var keyEvent = Event.KeyboardEvent(binding);
            return new KeyCombination(keyEvent.keyCode, ConvertEventModifiersToShortcutModifiers(keyEvent.modifiers, true));
        }

        private static ShortcutModifiers ConvertEventModifiersToShortcutModifiers(EventModifiers eventModifiers, bool coalesceCommandAndControl)
        {
            ShortcutModifiers modifiers = ShortcutModifiers.None;
            if ((eventModifiers & EventModifiers.Alt) != 0)
                modifiers |= ShortcutModifiers.Alt;
            if ((eventModifiers & EventModifiers.Shift) != 0)
                modifiers |= ShortcutModifiers.Shift;

            if (coalesceCommandAndControl)
            {
                if ((eventModifiers & (EventModifiers.Command | EventModifiers.Control)) != 0)
                    modifiers |= ShortcutModifiers.Action;
            }
            else if (Application.platform == RuntimePlatform.OSXEditor && (eventModifiers & EventModifiers.Command) != 0)
                modifiers |= ShortcutModifiers.Action;
            else if (Application.platform != RuntimePlatform.OSXEditor && (eventModifiers & EventModifiers.Control) != 0)
                modifiers |= ShortcutModifiers.Action;

            return modifiers;
        }

        public bool alt => (modifiers & ShortcutModifiers.Alt) == ShortcutModifiers.Alt;
        public bool action => (modifiers & ShortcutModifiers.Action) == ShortcutModifiers.Action;
        public bool shift => (modifiers & ShortcutModifiers.Shift) == ShortcutModifiers.Shift;

        internal Event ToKeyboardEvent()
        {
            Event e = new Event();
            e.type = EventType.KeyDown;
            e.alt = alt;
            e.command = action && Application.platform == RuntimePlatform.OSXEditor;
            e.control = action && Application.platform != RuntimePlatform.OSXEditor;
            e.shift = shift;
            e.keyCode = keyCode;
            return e;
        }

        internal static string SequenceToString(IEnumerable<KeyCombination> keyCombinations)
        {
            if (!keyCombinations.Any())
                return "";

            var builder = new StringBuilder();

            builder.Append(keyCombinations.First());

            foreach (var keyCombination in keyCombinations.Skip(1))
            {
                builder.Append(", ");
                builder.Append(keyCombination);
            }

            return builder.ToString();
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            VisualizeModifiers(modifiers, builder);
            VisualizeKeyCode(keyCode, builder);
            return builder.ToString();
        }

        internal string ToMenuShortcutString()
        {
            if (keyCode == KeyCode.None)
                return string.Empty;

            var builder = new StringBuilder();
            if ((modifiers & ShortcutModifiers.Alt) != 0)
                builder.Append("&");
            if ((modifiers & ShortcutModifiers.Shift) != 0)
                builder.Append("#");
            if ((modifiers & ShortcutModifiers.Action) != 0)
                builder.Append("%");
            if (modifiers == ShortcutModifiers.None)
                builder.Append("_");

            VisualizeKeyCode(keyCode, builder);

            return builder.ToString();
        }

        static void VisualizeModifiers(ShortcutModifiers modifiers, StringBuilder builder)
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                if ((modifiers & ShortcutModifiers.Alt) != 0)
                    builder.Append("⌥");
                if ((modifiers & ShortcutModifiers.Shift) != 0)
                    builder.Append("⇧");
                if ((modifiers & ShortcutModifiers.Action) != 0)
                    builder.Append("⌘");
            }
            else
            {
                if ((modifiers & ShortcutModifiers.Action) != 0)
                    builder.Append("Ctrl+");
                if ((modifiers & ShortcutModifiers.Alt) != 0)
                    builder.Append("Alt+");
                if ((modifiers & ShortcutModifiers.Shift) != 0)
                    builder.Append("Shift+");
            }
        }

        static void VisualizeKeyCode(KeyCode keyCode, StringBuilder builder)
        {
            if (!TryFormatKeycode(keyCode, builder))
                builder.Append(keyCode.ToString());
        }

        static bool TryFormatKeycode(KeyCode code, StringBuilder builder)
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                switch (code)
                {
                    case KeyCode.Return:
                        builder.Append("↩");
                        break;
                    case KeyCode.Backspace:
                        builder.Append("⌫");
                        break;
                    case KeyCode.Delete:
                        builder.Append("⌦");
                        break;
                    case KeyCode.Escape:
                        builder.Append("⎋");
                        break;
                    case KeyCode.RightArrow:
                        builder.Append("→");
                        break;
                    case KeyCode.LeftArrow:
                        builder.Append("←");
                        break;
                    case KeyCode.UpArrow:
                        builder.Append("↑");
                        break;
                    case KeyCode.DownArrow:
                        builder.Append("↓");
                        break;
                    case KeyCode.PageUp:
                        builder.Append("⇞");
                        break;
                    case KeyCode.PageDown:
                        builder.Append("⇟");
                        break;
                    case KeyCode.Home:
                        builder.Append("↖");
                        break;
                    case KeyCode.End:
                        builder.Append("↘");
                        break;
                    case KeyCode.Tab:
                        builder.Append("⇥");
                        break;
                    default:
                        return false;
                }
            }
            else
            {
                switch (code)
                {
                    case KeyCode.Delete:
                        builder.Append("DEL");
                        break;
                    case KeyCode.Backspace:
                        builder.Append("BACKSPACE");
                        break;
                    case KeyCode.LeftArrow:
                        builder.Append("LEFT");
                        break;
                    case KeyCode.RightArrow:
                        builder.Append("RIGHT");
                        break;
                    case KeyCode.UpArrow:
                        builder.Append("UP");
                        break;
                    case KeyCode.DownArrow:
                        builder.Append("DOWN");
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }
    }
}
