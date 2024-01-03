// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    public enum ShortcutStage
    {
        Begin,
        End
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
        readonly string m_Tag;
        readonly ShortcutType m_Type;
        readonly int m_Priority;

        public Identifier identifier => m_Identifier;
        public string displayName => m_DisplayName;

        public IList<KeyCombination> combinations => activeCombination;

        public bool overridden => m_OverriddenCombinations != null;

        public Action<ShortcutArguments> action => m_Action;
        public Type context => m_Context;
        public string tag => m_Tag;
        public ShortcutType type => m_Type;
        public int priority => m_Priority;

        internal ShortcutModifiers m_ReservedModifier;

        internal ShortcutEntry(Identifier id, IEnumerable<KeyCombination> defaultCombination, Action<ShortcutArguments> action, Type context, ShortcutType type, string displayName = null, int priority = int.MaxValue)
            : this(id, defaultCombination, action, context, null, type, displayName, priority) {}

        internal ShortcutEntry(Identifier id, IEnumerable<KeyCombination> defaultCombination, Action<ShortcutArguments> action, Type context, string tag, ShortcutType type, string displayName = null, int priority = int.MaxValue)
        {
            m_Identifier = id;
            m_DefaultCombinations = defaultCombination.ToList();
            m_Context = context ?? ContextManager.globalContextType;
            m_Tag = tag;
            m_Action = action;
            m_Type = type;
            m_DisplayName = displayName ?? id.path;
            m_Priority = ShortcutAttributeUtility.AssignPriority(context, priority);

            if (typeof(IShortcutContext).IsAssignableFrom(m_Context))
                foreach (var attribute in m_Context.GetCustomAttributes(typeof(ReserveModifiersAttribute), true))
                    m_ReservedModifier |= (attribute as ReserveModifiersAttribute).Modifiers;
        }

        public override string ToString()
        {
            return $"{string.Join(",", combinations.Select(c => c.ToString()).ToArray())} [{context?.Name}] {(!string.IsNullOrWhiteSpace(tag) ? $"[{tag}]" : "")}";
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

        public bool StartsWith(IList<KeyCombination> prefix, IEnumerable<KeyCode> keyCodes = null)
        {
            if (activeCombination.Count < prefix.Count)
                return false;

            if (prefix.Count == 0)
                return true;

            for (int i = 0; i < prefix.Count - 1; i++)
            {
                if (!prefix[i].Equals(activeCombination[i]))
                    return false;
            }

            var lastKeyCombination = prefix.Last();
            var lastKeyCombinationActive = activeCombination[prefix.Count - 1];

            if (m_ReservedModifier != 0)
            {
                lastKeyCombination = new KeyCombination(lastKeyCombination.keyCode,
                    lastKeyCombination.modifiers & ~m_ReservedModifier);
                lastKeyCombinationActive = new KeyCombination(lastKeyCombinationActive.keyCode,
                    lastKeyCombinationActive.modifiers & ~m_ReservedModifier);
            }

            if (lastKeyCombination.Equals(lastKeyCombinationActive))
            {
                if (keyCodes != null)
                {
                    var otherCombinationHasDesiredKeyCode = HasSpecifiedKeyCode(keyCodes, lastKeyCombination.keyCode);
                    var activeCombinationHasDesiredKeyCode = HasSpecifiedKeyCode(keyCodes, lastKeyCombinationActive.keyCode);

                    return otherCombinationHasDesiredKeyCode && activeCombinationHasDesiredKeyCode;
                }

                return true;
            }

            return false;
        }

        static bool HasSpecifiedKeyCode(IEnumerable<KeyCode> keyCodes, KeyCode currentKeyCode)
        {
            if (keyCodes == null)
                return false;

            foreach (var keyCode in keyCodes)
            {
                if (keyCode == currentKeyCode)
                    return true;
            }

            return false;
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
            if (m_Type == ShortcutType.Menu && m_Identifier.path.StartsWith(ShortcutManagerWindowViewController.k_MainMenu))
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
