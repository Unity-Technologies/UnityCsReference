// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    // TODO: Find better name
    enum ShortcutState
    {
        Begin = 1,
        End
    }

    struct ShortcutArguments
    {
        public object context;
        public ShortcutState state;
    }

    enum ShortcutType
    {
        Action,
        Clutch,
        Menu,
    }

    [Serializable]
    struct Identifier
    {
        public string path;
        public const string kPathSeparator = "/";

        public Identifier(MethodInfo methodInfo, ShortcutAttribute attribute)
        {
            path = attribute.identifier;
        }

        public Identifier(string path)
        {
            this.path = path;
        }

        public override string ToString()
        {
            return path;
        }
    }

    class ShortcutEntry
    {
        readonly Identifier m_Identifier;

        readonly List<KeyCombination> m_DefaultCombinations = new List<KeyCombination>();
        List<KeyCombination> m_OverridenCombinations;

        readonly Action<ShortcutArguments> m_Action;
        readonly Type m_Context;
        readonly ShortcutType m_Type;

        public Identifier identifier => m_Identifier;

        public IEnumerable<KeyCombination> combinations => activeCombination;

        public bool overridden => m_OverridenCombinations != null;

        public Action<ShortcutArguments> action => m_Action;
        public Type context => m_Context;
        public ShortcutType type => m_Type;

        internal ShortcutEntry(Identifier id, IEnumerable<KeyCombination> defaultCombination, Action<ShortcutArguments> action, Type context, ShortcutType type)
        {
            m_Identifier = id;
            m_DefaultCombinations = defaultCombination.ToList();
            m_Context = context ?? ContextManager.globalContextType;
            m_Action = action;
            m_Type = type;
        }

        public override string ToString()
        {
            return $"{string.Join(",", combinations.Select(c=>c.ToString()).ToArray())} [{context?.Name}]";
        }

        List<KeyCombination> activeCombination
        {
            get
            {
                if (m_OverridenCombinations != null)
                    return m_OverridenCombinations;
                return m_DefaultCombinations;
            }
        }

        public bool StartsWith(IList<KeyCombination> prefix)
        {
            if (activeCombination.Count < prefix.Count)
                return false;

            if (prefix.Count != 0)
            {
                var contextType = context;
                if (typeof(IShortcutToolContext).IsAssignableFrom(contextType))
                {
                    var attributes = contextType.GetCustomAttributes(typeof(ReserveModifiersAttribute), true);

                    var lastKeyCombination = prefix.Last();
                    var newModifier = lastKeyCombination.modifiers;

                    foreach (var attribute in attributes)
                    {
                        var modifier = (attribute as ReserveModifiersAttribute).modifier;
                        if ((modifier & ShortcutModifiers.Shift) == ShortcutModifiers.Shift)
                        {
                            newModifier = newModifier & ~ShortcutModifiers.Shift;
                        }
                        if ((modifier & ShortcutModifiers.Alt) == ShortcutModifiers.Alt)
                        {
                            newModifier = newModifier & ~ShortcutModifiers.Alt;
                        }
                        if ((modifier & ShortcutModifiers.ControlOrCommand) == ShortcutModifiers.ControlOrCommand)
                        {
                            newModifier = newModifier & ~ShortcutModifiers.ControlOrCommand;
                        }
                    }

                    lastKeyCombination = new KeyCombination(lastKeyCombination.keyCode, newModifier);

                    for (int i = 0; i < prefix.Count - 1; i++)
                    {
                        if (!prefix[i].Equals(activeCombination[i]))
                            return false;
                    }

                    return lastKeyCombination.Equals(activeCombination[prefix.Count - 1]);
                }
            }

            for (int i = 0; i < prefix.Count; i++)
            {
                if (!prefix[i].Equals(activeCombination[i]))
                    return false;
            }

            return true;
        }

        public bool FullyMatches(List<KeyCombination> other)
        {
            if (activeCombination.Count != other.Count)
                return false;

            return StartsWith(other);
        }

        internal void ResetToDefault()
        {
            m_OverridenCombinations = null;
        }

        internal void SetOverride(List<KeyCombination> newKeyCombinations)
        {
            m_OverridenCombinations = new List<KeyCombination>(newKeyCombinations);
            if (m_Type == ShortcutType.Menu && m_OverridenCombinations.Any())
                Menu.SetHotkey(m_Identifier.path.Substring(Discovery.k_MainMenuShortcutPrefix.Length), m_OverridenCombinations[0].ToMenuShortcutString());
        }

        internal void ApplyOverride(SerializableShortcutEntry shortcutOverride)
        {
            SetOverride(shortcutOverride.combinations);
        }
    }

    [Flags]
    enum ShortcutModifiers
    {
        None = 0,
        Alt = 1,
        ControlOrCommand = 2,
        Shift = 4
    }

    [Serializable]
    struct KeyCombination
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

        public KeyCombination(KeyCode keyCode, EventModifiers eventModifiers)
        {
            m_KeyCode = keyCode;
            m_Modifiers = ShortcutModifiers.None;

            if ((eventModifiers & EventModifiers.Alt) == EventModifiers.Alt)
                m_Modifiers |= ShortcutModifiers.Alt;
            if ((eventModifiers & EventModifiers.Control) == EventModifiers.Control)
                m_Modifiers |= ShortcutModifiers.ControlOrCommand;
            if ((eventModifiers & EventModifiers.Command) == EventModifiers.Command)
                m_Modifiers |= ShortcutModifiers.ControlOrCommand;
            if ((eventModifiers & EventModifiers.Shift) == EventModifiers.Shift)
                m_Modifiers |= ShortcutModifiers.Shift;
        }

        internal KeyCombination(Event evt)
        {
            m_KeyCode = evt.keyCode;
            m_Modifiers = ShortcutModifiers.None;

            if (evt.alt)
                m_Modifiers |= ShortcutModifiers.Alt;
            if (evt.control || evt.command)
                m_Modifiers |= ShortcutModifiers.ControlOrCommand;
            if (evt.shift)
                m_Modifiers |= ShortcutModifiers.Shift;
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
    }
}
