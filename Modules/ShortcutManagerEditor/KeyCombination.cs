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
    public struct KeyCombination : IEquatable<KeyCombination>
    {
        [SerializeField]
        internal KeyCode m_KeyCode;
        [SerializeField]
        ShortcutModifiers m_Modifiers;

        // Linux may send key masks and they will be greater than the shortcut index length.
        // Check implemented at getter and not at assignment because binding validator
        // should be able to work out what the original key code was by accessing internal field.
        public KeyCode keyCode => (int)m_KeyCode < Directory.MaxIndexedEntries ? m_KeyCode : KeyCode.None;
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
            { KeyCode.Tab, "TAB" },
            { KeyCode.Space, "SPACE"},
            { KeyCode.Backspace, "BACKSPACE"},

            // UUM-40161
            // Even though ShortcutManager doesn't allow enter key bindings,
            // we must have this conversion to be able to handle menu shortcut
            // string which can have enter key bindings.
            { KeyCode.Return, "ENTER" },

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
            { KeyCode.F12, "F12" },

            { KeyCode.Mouse0, "M0" },
            { KeyCode.Mouse1, "M1" },
            { KeyCode.Mouse2, "M2" },
            { KeyCode.Mouse3, "M3" },
            { KeyCode.Mouse4, "M4" },
            { KeyCode.Mouse5, "M5" },
            { KeyCode.Mouse6, "M6" },
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

        internal static KeyCombination FromInput(Event evt)
        {
            return FromKeyboardInput(evt.keyCode, evt.modifiers);
        }

        internal static KeyCombination FromKeyboardInput(Event evt)
        {
            return FromKeyboardInput(evt.keyCode, evt.modifiers);
        }

        internal static KeyCombination FromKeyboardInput(KeyCode keyCode, EventModifiers modifiers)
        {
            return new KeyCombination(keyCode, ConvertEventModifiersToShortcutModifiers(modifiers, false));
        }

        internal static KeyCombination FromKeyboardInputAndAddEventModifiers(KeyCombination combination, EventModifiers modifiers)
        {
            return new KeyCombination(combination.keyCode, combination.modifiers | ConvertEventModifiersToShortcutModifiers(modifiers, false));
        }

        internal static KeyCombination FromKeyboardInputAndRemoveEventModifiers(KeyCombination combination, EventModifiers modifiers)
        {
            var convertedEventModifier = ConvertEventModifiersToShortcutModifiers(modifiers, false);
            var newModifiers = combination.modifiers & ~convertedEventModifier;

            return new KeyCombination(combination.keyCode, newModifiers);
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
                    modifiers |= ShortcutModifiers.Action;
            }
            else
            {
                if (Application.platform == RuntimePlatform.OSXEditor)
                {
                    if ((eventModifiers & EventModifiers.Command) != 0)
                        modifiers |= ShortcutModifiers.Action;
                    if ((eventModifiers & EventModifiers.Control) != 0)
                        modifiers |= ShortcutModifiers.Control;
                }
                else if ((eventModifiers & EventModifiers.Control) != 0)
                    modifiers |= ShortcutModifiers.Action;
            }

            return modifiers;
        }

        public bool alt => (modifiers & ShortcutModifiers.Alt) == ShortcutModifiers.Alt;
        public bool action => (modifiers & ShortcutModifiers.Action) == ShortcutModifiers.Action;
        public bool shift => (modifiers & ShortcutModifiers.Shift) == ShortcutModifiers.Shift;
        public bool control => (modifiers & ShortcutModifiers.Control) == ShortcutModifiers.Control;

        internal Event ToKeyboardEvent()
        {
            Event e = new Event();
            e.type = EventType.KeyDown;
            e.alt = alt;
            e.command = action && Application.platform == RuntimePlatform.OSXEditor;
            e.control = action && Application.platform != RuntimePlatform.OSXEditor || control;
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
            if ((modifiers & ShortcutModifiers.Control) != 0)
                builder.Append("^");
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
            var shortcutStarted = false;

            bool found;
            do
            {
                found = true;
                if (startIndex >= menuItemBindingString.Length)
                    break;

                switch (menuItemBindingString[startIndex])
                {
                    case '&':
                        modifiers |= ShortcutModifiers.Alt;
                        startIndex++;
                        break;

                    case '%':
                        modifiers |= ShortcutModifiers.Action;
                        startIndex++;
                        break;

                    case '#':
                        modifiers |= ShortcutModifiers.Shift;
                        startIndex++;
                        break;

                    case '^':
                        modifiers |= ShortcutModifiers.Control;
                        startIndex++;
                        break;

                    case '_':
                        startIndex++;
                        break;

                    default:
                        found = false;
                        break;
                }

                shortcutStarted |= found;
            }
            while (found);

            var keyCodeString = menuItemBindingString.Substring(startIndex, menuItemBindingString.Length - startIndex);
            KeyCode keyCode;
            ShortcutModifiers additionalModifiers;
            if (!shortcutStarted || !TryParseMenuItemKeyCodeString(keyCodeString, out keyCode, out additionalModifiers))
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

            if (s_MenuItemKeyCodeStringToKeyCode.TryGetValue(keyCodeString.ToUpperInvariant(), out keyCode))
                return true;

            if (keyCodeString.Length != 1)
                return false;

            var character = char.ToLowerInvariant(keyCodeString[0]);

            keyCode = (KeyCode)character;
            return Enum.IsDefined(typeof(KeyCode), keyCode);
        }

        internal static string SequenceToMenuString(IEnumerable<KeyCombination> keyCombinations)
        {
            if (!keyCombinations.Any())
                return "";

            //TODO: once we start supporting chords we need to figure out how to represent that for menus.
            return keyCombinations.Single().ToMenuShortcutString();
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
                if ((modifiers & ShortcutModifiers.Control) != 0)
                    builder.Append("^");
            }
            else
            {
                if ((modifiers & ShortcutModifiers.Action | modifiers & ShortcutModifiers.Control) != 0)
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

        static Dictionary<KeyCode, string> s_KeyCodeNamesMacOS = new Dictionary<KeyCode, string>
        {
            { KeyCode.Backspace, "⌫" },
            { KeyCode.Tab, "⇥" },
            { KeyCode.Return, "↩" },
            { KeyCode.Escape, "⎋" },
            { KeyCode.Delete, "⌦" },
            { KeyCode.UpArrow, "↑" },
            { KeyCode.DownArrow, "↓" },
            { KeyCode.RightArrow, "→" },
            { KeyCode.LeftArrow, "←" },
            { KeyCode.Home, "↖" },
            { KeyCode.End, "↘" },
            { KeyCode.PageUp, "⇞" },
            { KeyCode.PageDown, "⇟" },
        };

        static Dictionary<KeyCode, string> s_KeyCodeNamesNotMacOS = new Dictionary<KeyCode, string>
        {
            { KeyCode.Return, "Enter" },
            { KeyCode.Escape, "Esc" },
            { KeyCode.Delete, "Del" },
            { KeyCode.UpArrow, "Up Arrow" },
            { KeyCode.DownArrow, "Down Arrow" },
            { KeyCode.RightArrow, "Right Arrow" },
            { KeyCode.LeftArrow, "Left Arrow" },
            { KeyCode.PageUp, "Page Up" },
            { KeyCode.PageDown, "Page Down" },
        };

        static Dictionary<KeyCode, string> s_KeyCodeNamesCommon = new Dictionary<KeyCode, string>
        {
            { KeyCode.Exclaim, "!" },
            { KeyCode.DoubleQuote, "\"" },
            { KeyCode.Hash, "#" },
            { KeyCode.Dollar, "$" },
            { KeyCode.Percent, "%" },
            { KeyCode.Ampersand, "&" },
            { KeyCode.Quote, "'" },
            { KeyCode.LeftParen, "(" },
            { KeyCode.RightParen, ")" },
            { KeyCode.Asterisk, "*" },
            { KeyCode.Plus, "+" },
            { KeyCode.Comma, "," },
            { KeyCode.Minus, "-" },
            { KeyCode.Period, "." },
            { KeyCode.Slash, "/" },
            { KeyCode.Alpha0, "0" },
            { KeyCode.Alpha1, "1" },
            { KeyCode.Alpha2, "2" },
            { KeyCode.Alpha3, "3" },
            { KeyCode.Alpha4, "4" },
            { KeyCode.Alpha5, "5" },
            { KeyCode.Alpha6, "6" },
            { KeyCode.Alpha7, "7" },
            { KeyCode.Alpha8, "8" },
            { KeyCode.Alpha9, "9" },
            { KeyCode.Colon, ":" },
            { KeyCode.Semicolon, ";" },
            { KeyCode.Less, "<" },
            { KeyCode.Equals, "=" },
            { KeyCode.Greater, ">" },
            { KeyCode.Question, "?" },
            { KeyCode.At, "@" },
            { KeyCode.LeftBracket, "[" },
            { KeyCode.Backslash, "\\" },
            { KeyCode.RightBracket, "]" },
            { KeyCode.Caret, "^" },
            { KeyCode.Underscore, "_" },
            { KeyCode.BackQuote, "`" },
            { KeyCode.LeftCurlyBracket, "{" },
            { KeyCode.Pipe, "|" },
            { KeyCode.RightCurlyBracket, "}" },
            { KeyCode.Tilde, "~" },
            { KeyCode.Keypad0, "Num 0" },
            { KeyCode.Keypad1, "Num 1" },
            { KeyCode.Keypad2, "Num 2" },
            { KeyCode.Keypad3, "Num 3" },
            { KeyCode.Keypad4, "Num 4" },
            { KeyCode.Keypad5, "Num 5" },
            { KeyCode.Keypad6, "Num 6" },
            { KeyCode.Keypad7, "Num 7" },
            { KeyCode.Keypad8, "Num 8" },
            { KeyCode.Keypad9, "Num 9" },
            { KeyCode.KeypadPeriod, "Num ." },
            { KeyCode.KeypadDivide, "Num /" },
            { KeyCode.KeypadMultiply, "Num *" },
            { KeyCode.KeypadMinus, "Num -" },
            { KeyCode.KeypadPlus, "Num +" },
            { KeyCode.KeypadEnter, "Num Enter" },
            { KeyCode.KeypadEquals, "Num =" },
            { KeyCode.WheelUp, "Wheel Up"},
            { KeyCode.WheelDown, "Wheel Down"},
            { KeyCode.Mouse0, "Mouse 0"},
            { KeyCode.Mouse1, "Mouse 1"},
            { KeyCode.Mouse2, "Mouse 2"},
            { KeyCode.Mouse3, "Mouse 3"},
            { KeyCode.Mouse4, "Mouse 4"},
            { KeyCode.Mouse5, "Mouse 5"},
            { KeyCode.Mouse6, "Mouse 6"},
        };

        internal static readonly KeyCode[] k_MouseKeyCodes = new KeyCode[7]
        {
            KeyCode.Mouse0,
            KeyCode.Mouse1,
            KeyCode.Mouse2,
            KeyCode.Mouse3,
            KeyCode.Mouse4,
            KeyCode.Mouse5,
            KeyCode.Mouse6
        };

        internal static readonly Dictionary<KeyCode, EventModifiers> k_KeyCodeToEventModifiers = new Dictionary<KeyCode, EventModifiers>()
        {
            { KeyCode.None, EventModifiers.None},
            { KeyCode.LeftAlt, EventModifiers.Alt},
            { KeyCode.RightAlt, EventModifiers.Alt},
            { KeyCode.LeftControl, EventModifiers.Control},
            { KeyCode.RightControl, EventModifiers.Control},
            { KeyCode.LeftCommand, EventModifiers.Command},
            { KeyCode.RightCommand, EventModifiers.Command},
            { KeyCode.LeftShift, EventModifiers.Shift},
            { KeyCode.RightShift, EventModifiers.Shift}
        };

        static bool TryFormatKeycode(KeyCode code, StringBuilder builder)
        {
            string name;
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                if (s_KeyCodeNamesMacOS.TryGetValue(code, out name))
                {
                    builder.Append(name);
                    return true;
                }
            }
            else
            {
                if (s_KeyCodeNamesNotMacOS.TryGetValue(code, out name))
                {
                    builder.Append(name);
                    return true;
                }
            }

            if (s_KeyCodeNamesCommon.TryGetValue(code, out name))
            {
                builder.Append(name);
                return true;
            }

            return false;
        }

        public bool Equals(KeyCombination other)
        {
            bool modifiersMatch;
            if (Application.platform != RuntimePlatform.OSXEditor)
            {
                const ShortcutModifiers controlMergeMask = ShortcutModifiers.Action | ShortcutModifiers.Control;
                bool thisHasControl = (modifiers & controlMergeMask) != 0;
                bool otherHasControl = (other.modifiers & controlMergeMask) != 0;
                ShortcutModifiers thisModifiers = m_Modifiers & ~controlMergeMask;
                ShortcutModifiers otherModifiers = other.m_Modifiers & ~controlMergeMask;

                modifiersMatch = thisModifiers == otherModifiers && thisHasControl == otherHasControl;
            }
            else
            {
                modifiersMatch = m_Modifiers == other.m_Modifiers;
            }

            return m_KeyCode == other.m_KeyCode && modifiersMatch;
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
