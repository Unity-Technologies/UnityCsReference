// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace UnityEditor.ShortcutManagement
{
    public enum ShortcutStage
    {
        Begin,
        End,
    }

    public struct ShortcutArguments
    {
        public object context;
        public ShortcutStage stage;
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
        List<KeyCombination> m_OverriddenCombinations;

        readonly Action<ShortcutArguments> m_Action;
        readonly Type m_Context;
        readonly ShortcutType m_Type;

        public Identifier identifier => m_Identifier;

        public IEnumerable<KeyCombination> combinations => activeCombination;

        public bool overridden => m_OverriddenCombinations != null;

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
                if (m_OverriddenCombinations != null)
                    return m_OverriddenCombinations;
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
                        if ((modifier & ShortcutModifiers.Action) == ShortcutModifiers.Action)
                        {
                            newModifier = newModifier & ~ShortcutModifiers.Action;
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
            m_OverriddenCombinations = null;
        }

        internal void SetOverride(IEnumerable<KeyCombination> newKeyCombinations)
        {
            m_OverriddenCombinations = newKeyCombinations.ToList();
            if (m_Type == ShortcutType.Menu && m_OverriddenCombinations.Any())
                Menu.SetHotkey(m_Identifier.path.Substring(Discovery.k_MainMenuShortcutPrefix.Length), m_OverriddenCombinations[0].ToMenuShortcutString());
        }

        internal void ApplyOverride(SerializableShortcutEntry shortcutOverride)
        {
            SetOverride(shortcutOverride.combinations);
        }
    }

    [Flags]
    public enum ShortcutModifiers
    {
        None = 0,
        Alt = 1,
        Action = 2,
        Shift = 4
    }
}
