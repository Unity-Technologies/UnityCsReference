// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

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

        public IList<KeyCombination> combinations => activeCombination;

        public bool overridden => m_OverridenCombinations != null;

        public Action<ShortcutArguments> action => m_Action;
        public Type context => m_Context;
        public ShortcutType type => m_Type;

        ShortcutModifiers m_ReservedModifier;

        internal ShortcutEntry(Identifier id, IEnumerable<KeyCombination> defaultCombination, Action<ShortcutArguments> action, Type context, ShortcutType type)
        {
            m_Identifier = id;
            m_DefaultCombinations = defaultCombination.ToList();
            m_Context = context ?? ContextManager.globalContextType;
            m_Action = action;
            m_Type = type;

            if (typeof(IShortcutToolContext).IsAssignableFrom(m_Context))
                foreach (var attribute in m_Context.GetCustomAttributes(typeof(ReserveModifiersAttribute), true))
                    m_ReservedModifier |= (attribute as ReserveModifiersAttribute).modifier;
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

        public bool StartsWith(List<KeyCombination> prefix)
        {
            if (activeCombination.Count < prefix.Count)
                return false;

            if (prefix.Count != 0 && typeof(IShortcutToolContext).IsAssignableFrom(m_Context))
            {
                var lastKeyCombination = prefix.Last();
                lastKeyCombination = new KeyCombination(lastKeyCombination.keyCode, lastKeyCombination.modifiers & ~m_ReservedModifier);

                for (int i = 0; i < prefix.Count - 1; i++)
                {
                    if (!prefix[i].Equals(activeCombination[i]))
                        return false;
                }

                return lastKeyCombination.Equals(activeCombination[prefix.Count - 1]);
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

        public void ResetToDefault()
        {
            m_OverridenCombinations = null;
        }

        public void SetOverride(List<KeyCombination> newKeyCombinations)
        {
            m_OverridenCombinations = new List<KeyCombination>(newKeyCombinations);
            if (m_Type == ShortcutType.Menu && m_OverridenCombinations.Any())
                Menu.SetHotkey(m_Identifier.path, m_OverridenCombinations[0].ToMenuShortcutString());
        }

        public void ApplyOverride(SerializableShortcutEntry shortcutOverride)
        {
            SetOverride(shortcutOverride.keyCombination);
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
}
