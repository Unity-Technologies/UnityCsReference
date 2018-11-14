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
    struct KeyCombination : IEquatable<KeyCombination>
    {
        [SerializeField]
        KeyCode m_KeyCode;
        [SerializeField]
        ShortcutModifiers m_Modifiers;

        public KeyCode keyCode => m_KeyCode;
        public ShortcutModifiers modifiers => m_Modifiers;

        static Dictionary<KeyCode, string> s_KeyCodeToMenuItemKeyCodeString = new Dictionary<KeyCode, string>()
        {
            { KeyCode.LeftArrow, "LEFT" },
            { KeyCode.UpArrow, "UP" },
            { KeyCode.RightArrow, "RIGHT" },
            { KeyCode.DownArrow, "DOWN" },

            { KeyCode.PageDown, "PGDN" },
            { KeyCode.PageUp, "PGUP" },
            { KeyCode.Home, "HOME" },
            { KeyCode.Insert, "INS" },
            { KeyCode.Delete, "DEL" },
            { KeyCode.End, "END" },

            { KeyCode.F1, "F1" },
            { KeyCode.F2, "F2" },
            { KeyCode.F3, "F3" },
            { KeyCode.F4, "F4" },
            { KeyCode.F5, "F5" },
            { KeyCode.F6, "F6" },
            { KeyCode.F7, "F7" },
            { KeyCode.F8, "F8" },
            { KeyCode.F9, "F9" },
            { KeyCode.F10, "F10" },
            { KeyCode.F11, "F11" },
            { KeyCode.F12, "F12" }
        };

        static Dictionary<string, KeyCode> s_MenuItemKeyCodeStringToKeyCode;

        static KeyCombination()
        {
            // Populate s_MenuItemKeyCodeStringToKeyCode by reversing s_KeyCodeToMenuItemKeyCodeString
            s_MenuItemKeyCodeStringToKeyCode = new Dictionary<string, KeyCode>(s_KeyCodeToMenuItemKeyCodeString.Count);
            foreach (var entry in s_KeyCodeToMenuItemKeyCodeString)
            {
                s_MenuItemKeyCodeStringToKeyCode.Add(entry.Value, entry.Key);
            }
        }

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

        internal static KeyCombination FromPrefKeyKeyboardEvent(Event evt)
        {
            return new KeyCombination(evt.keyCode, ConvertEventModifiersToShortcutModifiers(evt.modifiers, true));
        }

        static ShortcutModifiers ConvertEventModifiersToShortcutModifiers(EventModifiers eventModifiers, bool coalesceCommandAndControl)
        {
            ShortcutModifiers modifiers = ShortcutModifiers.None;
            if ((eventModifiers & EventModifiers.Alt) != 0)
                modifiers |= ShortcutModifiers.Alt;
            if ((eventModifiers & EventModifiers.Shift) != 0)
                modifiers |= ShortcutModifiers.Shift;

            if (coalesceCommandAndControl)
            {
                if ((eventModifiers & (EventModifiers.Command | EventModifiers.Control)) != 0)
                    modifiers |= ShortcutModifiers.ControlOrCommand;
            }
            else if (Application.platform == RuntimePlatform.OSXEditor && (eventModifiers & EventModifiers.Command) != 0)
                modifiers |= ShortcutModifiers.ControlOrCommand;
            else if (Application.platform != RuntimePlatform.OSXEditor && (eventModifiers & EventModifiers.Control) != 0)
                modifiers |= ShortcutModifiers.ControlOrCommand;

            return modifiers;
        }

        public bool alt => (modifiers & ShortcutModifiers.Alt) == ShortcutModifiers.Alt;
        public bool controlOrCommand => (modifiers & ShortcutModifiers.ControlOrCommand) == ShortcutModifiers.ControlOrCommand;
        public bool shift => (modifiers & ShortcutModifiers.Shift) == ShortcutModifiers.Shift;

        public Event ToKeyboardEvent()
        {
            Event e = new Event();
            e.type = EventType.KeyDown;
            e.alt = alt;
            e.command = controlOrCommand && Application.platform == RuntimePlatform.OSXEditor;
            e.control = controlOrCommand && Application.platform != RuntimePlatform.OSXEditor;
            e.shift = shift;
            e.keyCode = keyCode;
            return e;
        }

        public static string SequenceToString(IEnumerable<KeyCombination> keyCombinations)
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

        public string ToMenuShortcutString()
        {
            if (keyCode == KeyCode.None)
                return string.Empty;

            var builder = new StringBuilder();
            if ((modifiers & ShortcutModifiers.Alt) != 0)
                builder.Append("&");
            if ((modifiers & ShortcutModifiers.Shift) != 0)
                builder.Append("#");
            if ((modifiers & ShortcutModifiers.ControlOrCommand) != 0)
                builder.Append("%");
            if (modifiers == ShortcutModifiers.None)
                builder.Append("_");

            KeyCodeToMenuItemKeyCodeString(keyCode, builder);

            return builder.ToString();
        }

        static void KeyCodeToMenuItemKeyCodeString(KeyCode keyCode, StringBuilder builder)
        {
            string keyCodeString;
            if (s_KeyCodeToMenuItemKeyCodeString.TryGetValue(keyCode, out keyCodeString))
            {
                builder.Append(keyCodeString);
                return;
            }

            var character = (char)keyCode;
            if (character >= ' ' && character <= '@' ||
                character >= '[' && character <= '~')
                builder.Append(character.ToString());
        }

        internal static bool TryParseMenuItemBindingString(string menuItemBindingString, out KeyCombination keyCombination)
        {
            if (string.IsNullOrEmpty(menuItemBindingString))
            {
                keyCombination = default(KeyCombination);
                return false;
            }

            var modifiers = ShortcutModifiers.None;
            var startIndex = 0;
            var found = false;
            do
            {
                found = true;
                if (startIndex >= menuItemBindingString.Length)
                {
                    found = false;
                    break;
                }

                switch (menuItemBindingString[startIndex])
                {
                    case '&':
                        modifiers |= ShortcutModifiers.Alt;
                        startIndex++;
                        break;

                    case '%':
                        modifiers |= ShortcutModifiers.ControlOrCommand;
                        startIndex++;
                        break;

                    case '#':
                        modifiers |= ShortcutModifiers.Shift;
                        startIndex++;
                        break;

                    case '_':
                        startIndex++;
                        break;

                    default:
                        found = false;
                        break;
                }
            }
            while (found);

            var keyCodeString = menuItemBindingString.Substring(startIndex, menuItemBindingString.Length - startIndex);
            KeyCode keyCode;
            ShortcutModifiers additionalModifiers;
            if (!TryParseMenuItemKeyCodeString(keyCodeString, out keyCode, out additionalModifiers))
            {
                keyCombination = default(KeyCombination);
                return false;
            }

            modifiers |= additionalModifiers;
            keyCombination = new KeyCombination(keyCode, modifiers);
            return true;
        }

        static bool TryParseMenuItemKeyCodeString(string keyCodeString, out KeyCode keyCode, out ShortcutModifiers additionalModifiers)
        {
            keyCode = default(KeyCode);
            additionalModifiers = ShortcutModifiers.None;

            if (string.IsNullOrEmpty(keyCodeString))
                return false;

            if (s_MenuItemKeyCodeStringToKeyCode.TryGetValue(keyCodeString, out keyCode))
                return true;

            if (keyCodeString.Length != 1)
                return false;

            var character = keyCodeString[0];
            if (character >= 'A' && character <= 'Z')
            {
                keyCode = KeyCode.A + (character - 'A');
                additionalModifiers = ShortcutModifiers.Shift;
                return true;
            }

            keyCode = (KeyCode)character;
            return Enum.IsDefined(typeof(KeyCode), keyCode);
        }

        static void VisualizeModifiers(ShortcutModifiers modifiers, StringBuilder builder)
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                if ((modifiers & ShortcutModifiers.Alt) != 0)
                    builder.Append("⌥");
                if ((modifiers & ShortcutModifiers.Shift) != 0)
                    builder.Append("⇧");
                if ((modifiers & ShortcutModifiers.ControlOrCommand) != 0)
                    builder.Append("⌘");
            }
            else
            {
                if ((modifiers & ShortcutModifiers.ControlOrCommand) != 0)
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

        public bool Equals(KeyCombination other)
        {
            return m_KeyCode == other.m_KeyCode && m_Modifiers == other.m_Modifiers;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is KeyCombination && Equals((KeyCombination)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)m_KeyCode * 397) ^ (int)m_Modifiers;
            }
        }
    }
}
