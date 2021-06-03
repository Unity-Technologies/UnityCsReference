// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
        readonly string m_DisplayName;

        readonly List<KeyCombination> m_DefaultCombinations = new List<KeyCombination>();
        List<KeyCombination> m_OverriddenCombinations;

        readonly Action<ShortcutArguments> m_Action;
        readonly Type m_Context;
        readonly ShortcutType m_Type;

        public Identifier identifier => m_Identifier;
        public string displayName => m_DisplayName;

        public IList<KeyCombination> combinations => activeCombination;

        public bool overridden => m_OverriddenCombinations != null;

        public Action<ShortcutArguments> action => m_Action;
        public Type context => m_Context;
        public ShortcutType type => m_Type;

        ShortcutModifiers m_ReservedModifier;

        internal ShortcutEntry(Identifier id, IEnumerable<KeyCombination> defaultCombination, Action<ShortcutArguments> action, Type context, ShortcutType type, string displayName = null)
        {
            m_Identifier = id;
            m_DefaultCombinations = defaultCombination.ToList();
            m_Context = context ?? ContextManager.globalContextType;
            m_Action = action;
            m_Type = type;
            m_DisplayName = displayName ?? id.path;

            if (typeof(IShortcutToolContext).IsAssignableFrom(m_Context))
                foreach (var attribute in m_Context.GetCustomAttributes(typeof(ReserveModifiersAttribute), true))
                    m_ReservedModifier |= (attribute as ReserveModifiersAttribute).modifier;
        }

        public override string ToString()
        {
            return $"{string.Join(",", combinations.Select(c => c.ToString()).ToArray())} [{context?.Name}]";
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

            if (prefix.Count != 0 && typeof(IShortcutToolContext).IsAssignableFrom(m_Context))
            {
                var lastKeyCombination = prefix.Last();
                lastKeyCombination = new KeyCombination(lastKeyCombination.keyCode, lastKeyCombination.modifiers & ~m_ReservedModifier);

                var lastKeyCombinationActive = activeCombination[prefix.Count - 1];
                lastKeyCombinationActive = new KeyCombination(lastKeyCombinationActive.keyCode, lastKeyCombinationActive.modifiers & ~m_ReservedModifier);

                for (int i = 0; i < prefix.Count - 1; i++)
                {
                    if (!prefix[i].Equals(activeCombination[i]))
                        return false;
                }

                return lastKeyCombination.Equals(lastKeyCombinationActive);
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
            if (m_Type == ShortcutType.Menu)
            {
                var newMenuKey = m_OverriddenCombinations.Any() ? m_OverriddenCombinations[0].ToMenuShortcutString() : "";
                Menu.SetHotkey(m_Identifier.path.Substring(Discovery.k_MainMenuShortcutPrefix.Length), newMenuKey);
            }
        }

        internal List<KeyCombination> GetDefaultCombinations()
        {
            return m_DefaultCombinations;
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
        Shift = 4,
        Control = 8
    }
}
