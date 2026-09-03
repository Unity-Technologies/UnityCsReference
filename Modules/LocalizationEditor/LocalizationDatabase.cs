// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor
{
    /// <summary>
    /// This provides Localization function.
    /// </summary>
    public static partial class L10n
    {
        [NoAutoStaticsCleanup] // plain lock object; holds no state
        static readonly object lockObject = new object();
        [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.Clear)]
        [IgnoreForUAL0015("This is a cache that is cleared on code reload and can be rebuilt on demand")]
        static readonly Dictionary<Assembly, string> s_GroupNames = new Dictionary<Assembly, string>(128);

        private readonly struct LocKey : IEquatable<LocKey>
        {
            [NotNull] public readonly string defaultString;
            [NotNull] public readonly string groupName;

            public LocKey(string _defaultString, string _groupName)
            {
                defaultString = _defaultString ?? string.Empty;
                groupName = _groupName ?? string.Empty;
            }

            public bool Equals(LocKey other)
            {
                return defaultString == other.defaultString && groupName == other.groupName;
            }

            public override bool Equals(object obj)
            {
                return obj is LocKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (defaultString.GetHashCode() * 397) ^ groupName.GetHashCode();
                }
            }
        }

        [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.Clear)]
        [IgnoreForUAL0015("This is a cache that is cleared on code reload and can be rebuilt on demand")]
        static readonly Dictionary<LocKey, string> s_LocalizedStringCache = new Dictionary<LocKey, string>(10 << 10);

        internal static void ClearCache()
        {
            lock (lockObject)
                s_LocalizedStringCache.Clear();
        }

        internal static string GetGroupName(System.Reflection.Assembly assembly)
        {
            if (assembly == null)
                return null;

            lock (lockObject)
            {
                if (!s_GroupNames.TryGetValue(assembly, out var name))
                {
                    var attrobjs = assembly.GetCustomAttributes(typeof(LocalizationAttribute), true /* inherit */);
                    if (attrobjs.Length > 0 && attrobjs[0] != null)
                    {
                        var locAttr = (LocalizationAttribute)attrobjs[0];
                        string locGroupName = locAttr.locGroupName;
                        if (locGroupName == null)
                            locGroupName = assembly.GetName().Name;
                        name = locGroupName;
                        s_GroupNames[assembly] = name;
                    }
                    else
                    {
                        s_GroupNames[assembly] = null;
                    }
                }
                return name;
            }
        }

        /// <summary>
        /// Get the translation for the given argument.
        /// </summary>
        /// <param name="str">The original string to be translated.</param>
        public static string Tr(string str)
        {
            return TrForAssembly(str, Assembly.GetCallingAssembly());
        }

        internal static string TrForContext(string str, object context)
        {
            return TrForAssembly(str, context?.GetType().Assembly);
        }

        internal static string TrForAssembly(string str, Assembly groupAssembly)
        {
            return TrWithGroup(str, GetGroupName(groupAssembly));
        }

        static string TrWithGroup(string str, string groupName)
        {
            if (!LocalizationDatabase.enableEditorLocalization)
                return str;

            if (string.IsNullOrEmpty(str))
                return str;

            lock (lockObject)
            {
                var key = new LocKey(str, groupName);

                if (s_LocalizedStringCache.TryGetValue(key, out var localized))
                    return localized;

                localized = (groupName != null)
                    ? LocalizationDatabase.GetLocalizedStringWithGroupName(str, groupName)
                    : LocalizationDatabase.GetLocalizedString(str);

                s_LocalizedStringCache[key] = localized;
                return localized;
            }
        }

        /// <summary>
        /// Get the translation array for the given argument array.
        /// </summary>
        /// <param name="str_list">The original strings to be translated.</param>
        public static string[] Tr(string[] str_list)
        {
            var groupAssembly = Assembly.GetCallingAssembly();
            var res = new string[str_list.Length];
            for (var i = 0; i < res.Length; ++i)
                res[i] = TrForAssembly(str_list[i], groupAssembly);
            return res;
        }

        /// <summary>
        /// Get the translation for the given argument.
        /// </summary>
        /// <param name="str">The original string to be translated.</param>
        /// <param name="groupName">The specified group name for the translation.</param>
        public static string Tr(string str, string groupName)
        {
            return TrWithGroup(str, groupName);
        }

        /// <summary>
        /// Get the translation array for the given argument array.
        /// </summary>
        /// <param name="str_list">The original strings to be translated.</param>
        /// <param name="groupName">The specified group name for the translation.</param>
        public static string[] Tr(string[] str_list, string groupName)
        {
            var res = new string[str_list.Length];
            for (var i = 0; i < res.Length; ++i)
                res[i] = TrWithGroup(str_list[i], groupName);
            return res;
        }

        [ExcludeFromDocs]
        public static string TrPath(string path)
        {
            return TrPathCore(null, path);
        }

        [ExcludeFromDocs]
        public static string TrPath(string path, string groupName)
        {
            return TrPathCore(groupName, path);
        }

        static string TrPathCore(string groupName, string path)
        {
            string[] separatingChars = { "/" };
            var result = new System.Text.StringBuilder(256);
            var items = path.Split(separatingChars, System.StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < items.Length; ++i)
            {
                result.Append(TrWithGroup(items[i], groupName));
                if (i < items.Length - 1)
                    result.Append("/");
            }
            return result.ToString();
        }

        [ExcludeFromDocs]
        public static GUIContent TextContent(string text, string tooltip = null, Texture icon = null)
        {
            return TextContentCore(GetGroupName(Assembly.GetCallingAssembly()), text, tooltip, icon);
        }

        static GUIContent TextContentCore(string groupName, string text, string tooltip, Texture icon)
        {
            if (!LocalizationDatabase.enableEditorLocalization || groupName == null)
                return EditorGUIUtility.TrTextContent(text, tooltip, icon);

            var gc = new GUIContent(TrWithGroup(text, groupName));
            gc.tooltip = TrWithGroup(tooltip, groupName);
            gc.image = icon;
            return gc;
        }

        [ExcludeFromDocs]
        public static GUIContent TextContent(string text, string tooltip, string iconName)
        {
            return TextContentWithIconNameCore(GetGroupName(Assembly.GetCallingAssembly()), text, tooltip, iconName);
        }

        [ExcludeFromDocs]
        public static GUIContent TextContent(string text, string tooltip, string iconName, string groupName)
        {
            return TextContentWithIconNameCore(groupName, text, tooltip, iconName);
        }

        static GUIContent TextContentWithIconNameCore(string groupName, string text, string tooltip, string iconName)
        {
            // No icon name means no icon. Without this, LoadIconRequired logs an error for the empty name,
            // and this is the only shape a caller wanting a group, a tooltip and no icon can spell.
            if (string.IsNullOrEmpty(iconName))
                return TextContentCore(groupName, text, tooltip, null);

            if (!LocalizationDatabase.enableEditorLocalization || groupName == null)
                return EditorGUIUtility.TrTextContent(text, tooltip, iconName);

            var gc = new GUIContent(TrWithGroup(text, groupName));
            gc.tooltip = TrWithGroup(tooltip, groupName);
            gc.image = EditorGUIUtility.LoadIconRequired(iconName);
            return gc;
        }

        [ExcludeFromDocs]
        public static GUIContent TextContent(string text, Texture icon)
        {
            return TextContentWithTextureCore(GetGroupName(Assembly.GetCallingAssembly()), text, icon);
        }

        [ExcludeFromDocs]
        public static GUIContent TextContent(string text, Texture icon, string groupName)
        {
            return TextContentWithTextureCore(groupName, text, icon);
        }

        static GUIContent TextContentWithTextureCore(string groupName, string text, Texture icon)
        {
            if (!LocalizationDatabase.enableEditorLocalization || groupName == null)
                return EditorGUIUtility.TrTextContentWithIcon(text, icon);

            var gc = new GUIContent(TrWithGroup(text, groupName));
            gc.image = icon;
            return gc;
        }

        [ExcludeFromDocs]
        public static GUIContent TextContentWithIcon(string text, Texture icon)
        {
            return TextContentWithIconTextureCore(GetGroupName(Assembly.GetCallingAssembly()), text, icon);
        }

        [ExcludeFromDocs]
        public static GUIContent TextContentWithIcon(string text, Texture icon, string groupName)
        {
            return TextContentWithIconTextureCore(groupName, text, icon);
        }

        static GUIContent TextContentWithIconTextureCore(string groupName, string text, Texture icon)
        {
            if (!LocalizationDatabase.enableEditorLocalization || groupName == null)
                return EditorGUIUtility.TrTextContentWithIcon(text, icon);

            var gc = new GUIContent(TrWithGroup(text, groupName));
            gc.image = icon;
            return gc;
        }

        [ExcludeFromDocs]
        public static GUIContent TextContentWithIcon(string text, string iconName)
        {
            return TextContentWithIconNamedCore(GetGroupName(Assembly.GetCallingAssembly()), text, iconName);
        }


        static GUIContent TextContentWithIconNamedCore(string groupName, string text, string iconName)
        {
            if (string.IsNullOrEmpty(iconName))
                return TextContentWithIconTextureCore(groupName, text, null);

            if (!LocalizationDatabase.enableEditorLocalization || groupName == null)
                return EditorGUIUtility.TextContentWithIcon(text, iconName);

            var gc = new GUIContent(TrWithGroup(text, groupName));
            gc.image = EditorGUIUtility.LoadIconRequired(iconName);
            return gc;
        }

        [ExcludeFromDocs]
        public static GUIContent TextContentWithIcon(string text, string tooltip, string iconName)
        {
            return TextContentWithIconTooltipNamedCore(GetGroupName(Assembly.GetCallingAssembly()), text, tooltip, iconName);
        }

        [ExcludeFromDocs]
        public static GUIContent TextContentWithIcon(string text, string tooltip, string iconName, string groupName)
        {
            return TextContentWithIconTooltipNamedCore(groupName, text, tooltip, iconName);
        }

        static GUIContent TextContentWithIconTooltipNamedCore(string groupName, string text, string tooltip, string iconName)
        {
            if (string.IsNullOrEmpty(iconName))
                return TextContentWithIconTooltipTextureCore(groupName, text, tooltip, null);

            if (!LocalizationDatabase.enableEditorLocalization || groupName == null)
                return EditorGUIUtility.TrTextContentWithIcon(text, tooltip, iconName);

            var gc = new GUIContent(TrWithGroup(text, groupName));
            gc.tooltip = TrWithGroup(tooltip, groupName);
            gc.image = EditorGUIUtility.LoadIconRequired(iconName);
            return gc;
        }

        [ExcludeFromDocs]
        public static GUIContent TextContentWithIcon(string text, string tooltip, Texture icon)
        {
            return TextContentWithIconTooltipTextureCore(GetGroupName(Assembly.GetCallingAssembly()), text, tooltip, icon);
        }

        [ExcludeFromDocs]
        public static GUIContent TextContentWithIcon(string text, string tooltip, Texture icon, string groupName)
        {
            return TextContentWithIconTooltipTextureCore(groupName, text, tooltip, icon);
        }

        static GUIContent TextContentWithIconTooltipTextureCore(string groupName, string text, string tooltip, Texture icon)
        {
            if (!LocalizationDatabase.enableEditorLocalization || groupName == null)
                return EditorGUIUtility.TrTextContentWithIcon(text, tooltip, icon);

            var gc = new GUIContent(TrWithGroup(text, groupName));
            gc.tooltip = TrWithGroup(tooltip, groupName);
            gc.image = icon;
            return gc;
        }

        [ExcludeFromDocs]
        public static GUIContent TextContentWithIcon(string text, string tooltip, MessageType messageType)
        {
            return TextContentWithIconTooltipMessageCore(GetGroupName(Assembly.GetCallingAssembly()), text, tooltip, messageType);
        }

        [ExcludeFromDocs]
        public static GUIContent TextContentWithIcon(string text, string tooltip, MessageType messageType, string groupName)
        {
            return TextContentWithIconTooltipMessageCore(groupName, text, tooltip, messageType);
        }

        static GUIContent TextContentWithIconTooltipMessageCore(string groupName, string text, string tooltip, MessageType messageType)
        {
            if (!LocalizationDatabase.enableEditorLocalization || groupName == null)
                return EditorGUIUtility.TrTextContentWithIcon(text, tooltip, messageType);

            var gc = new GUIContent(TrWithGroup(text, groupName));
            gc.tooltip = TrWithGroup(tooltip, groupName);
            gc.image = EditorGUIUtility.GetHelpIcon(messageType);
            return gc;
        }

        [ExcludeFromDocs]
        public static GUIContent TextContentWithIcon(string text, MessageType messageType)
        {
            return TextContentWithIconMessageCore(GetGroupName(Assembly.GetCallingAssembly()), text, messageType);
        }

        [ExcludeFromDocs]
        public static GUIContent TextContentWithIcon(string text, MessageType messageType, string groupName)
        {
            return TextContentWithIconMessageCore(groupName, text, messageType);
        }

        static GUIContent TextContentWithIconMessageCore(string groupName, string text, MessageType messageType)
        {
            if (!LocalizationDatabase.enableEditorLocalization || groupName == null)
                return EditorGUIUtility.TrTextContentWithIcon(text, messageType);

            var gc = new GUIContent(TrWithGroup(text, groupName));
            gc.image = EditorGUIUtility.GetHelpIcon(messageType);
            return gc;
        }

        [ExcludeFromDocs]
        public static GUIContent IconContent(string iconName, string tooltip = null)
        {
            return IconContentNamedCore(GetGroupName(Assembly.GetCallingAssembly()), iconName, tooltip);
        }

        [ExcludeFromDocs]
        public static GUIContent IconContent(string iconName, string tooltip, string groupName)
        {
            return IconContentNamedCore(groupName, iconName, tooltip);
        }

        static GUIContent IconContentNamedCore(string groupName, string iconName, string tooltip)
        {
            if (!LocalizationDatabase.enableEditorLocalization || groupName == null)
                return EditorGUIUtility.TrIconContent(iconName, tooltip);

            var gc = new GUIContent();
            gc.tooltip = TrWithGroup(tooltip, groupName);
            gc.image = EditorGUIUtility.LoadIconRequired(iconName);
            return gc;
        }

        [ExcludeFromDocs]
        public static GUIContent IconContent(Texture icon, string tooltip = null)
        {
            return IconContentTextureCore(GetGroupName(Assembly.GetCallingAssembly()), icon, tooltip);
        }

        [ExcludeFromDocs]
        public static GUIContent IconContent(Texture icon, string tooltip, string groupName)
        {
            return IconContentTextureCore(groupName, icon, tooltip);
        }

        static GUIContent IconContentTextureCore(string groupName, Texture icon, string tooltip)
        {
            if (!LocalizationDatabase.enableEditorLocalization || groupName == null)
                return EditorGUIUtility.TrIconContent(icon, tooltip);

            var gc = new GUIContent();
            gc.tooltip = TrWithGroup(tooltip, groupName);
            gc.image = icon;
            return gc;
        }

        [ExcludeFromDocs]
        public static GUIContent TempContent(string t)
        {
            return TempContentCore(GetGroupName(Assembly.GetCallingAssembly()), t);
        }

        [ExcludeFromDocs]
        public static GUIContent TempContent(string t, string groupName)
        {
            return TempContentCore(groupName, t);
        }

        static GUIContent TempContentCore(string groupName, string t)
        {
            if (!LocalizationDatabase.enableEditorLocalization || groupName == null)
                return EditorGUIUtility.TempContent(t);

            return EditorGUIUtility.TempContent(TrWithGroup(t, groupName));
        }

        [ExcludeFromDocs]
        public static GUIContent[] TempContent(string[] texts)
        {
            return TempContentArrayCore(GetGroupName(Assembly.GetCallingAssembly()), texts);
        }

        [ExcludeFromDocs]
        public static GUIContent[] TempContent(string[] texts, string groupName)
        {
            return TempContentArrayCore(groupName, texts);
        }

        static GUIContent[] TempContentArrayCore(string groupName, string[] texts)
        {
            if (!LocalizationDatabase.enableEditorLocalization || groupName == null)
                return EditorGUIUtility.TempContent(texts);

            var retval = new GUIContent[texts.Length];
            for (var i = 0; i < texts.Length; i++)
                retval[i] = new GUIContent(TrWithGroup(texts[i], groupName));
            return retval;
        }

        [ExcludeFromDocs]
        public static GUIContent[] TempContent(string[] texts, string[] tooltips)
        {
            return TempContentTooltipArrayCore(GetGroupName(Assembly.GetCallingAssembly()), texts, tooltips);
        }

        [ExcludeFromDocs]
        public static GUIContent[] TempContent(string[] texts, string[] tooltips, string groupName)
        {
            return TempContentTooltipArrayCore(groupName, texts, tooltips);
        }

        static GUIContent[] TempContentTooltipArrayCore(string groupName, string[] texts, string[] tooltips)
        {
            if (!LocalizationDatabase.enableEditorLocalization || groupName == null)
                return EditorGUIUtility.TempContent(texts, tooltips);

            var retval = new GUIContent[texts.Length];
            for (var i = 0; i < texts.Length; i++)
                retval[i] = new GUIContent(TrWithGroup(texts[i], groupName), TrWithGroup(tooltips[i], groupName));
            return retval;
        }

    }

    internal static class LocalizationGroupStack
    {
        [NoAutoStaticsCleanup] // stack of group-name strings, balanced by Push/Pop within a LocalizationGroup scope; lazily recreated
        static Stack<string> s_GroupNameStack;
        public static void Push(string groupName)
        {
            if (s_GroupNameStack == null)
                s_GroupNameStack = new Stack<string>();
            if (s_GroupNameStack.Count >= 16)
                Assert.IsTrue(false); // check the leak.
            s_GroupNameStack.Push(groupName);
            LocalizationDatabase.SetContextGroupName(groupName);
        }

        public static void Pop()
        {
            if (s_GroupNameStack == null || s_GroupNameStack.Count <= 0)
                Assert.IsTrue(false);
            s_GroupNameStack.Pop();
            if (s_GroupNameStack.Count > 0)
            {
                string top = s_GroupNameStack.Peek();
                LocalizationDatabase.SetContextGroupName(top);
            }
            else
                LocalizationDatabase.SetContextGroupName(null);
        }
    }

    /// <summary>
    /// This provides an auto dispose Localization system.
    /// This can be called recursively.
    /// </summary>
    public class LocalizationGroup : IDisposable
    {
        string m_LocGroupName;
        bool m_Pushed = false;

        /// <summary>
        /// a current group name for the localization.
        /// </summary>
        public string locGroupName { get { return m_LocGroupName; } }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public LocalizationGroup()
        {
            initialize(Assembly.GetCallingAssembly());
        }

        /// <summary>
        /// constructor.
        /// <param name="behaviour">group name will become the name of Assembly the behaviour belongs to.</param>
        /// </summary>
        public LocalizationGroup(Behaviour behaviour)
        {
            if (behaviour != null)
            {
                System.Type type = behaviour.GetType();
                initialize(type.Assembly);
            }
        }

        /// <summary>
        /// constructor.
        /// <param name="type">group name will become the name of Assembly the type belongs to.</param>
        /// </summary>
        public LocalizationGroup(System.Type type)
        {
            initialize(type.Assembly);
        }

        /// <summary>
        /// constructor.
        /// <param name="obj">group name will become the name of Assembly the obj belongs to.</param>
        /// </summary>
        public LocalizationGroup(System.Object obj)
        {
            if (obj == null)
                return;
            initialize(obj.GetType().Assembly);
        }

        void initialize(System.Reflection.Assembly assembly)
        {
            string groupName = L10n.GetGroupName(assembly);
            LocalizationGroupStack.Push(groupName);
            m_Pushed = true;
            m_LocGroupName = groupName;
        }

        /// <summary>
        /// dispose current state.
        /// </summary>
        public void Dispose()
        {
            if (m_Pushed)
                LocalizationGroupStack.Pop();
        }
    }
}

namespace UnityEditor.Localization.Editor
{
    /// <summary>
    /// This provides Localization function for Packages.
    /// </summary>
    [System.Obsolete("Localization has been deprecated. Please use UnityEditor.L10n instead", true)]
    public static class Localization
    {
        /// <summary>
        /// get proper translation for the given argument.
        /// </summary>
        [System.Obsolete("Obsolete msg (UnityUpgradable) -> UnityEditor.L10n.Tr(*)", true)]
        public static string Tr(string str)
        {
            if (!LocalizationDatabase.enableEditorLocalization)
                return str;
            var assembly = Assembly.GetCallingAssembly();
            object[] attrobjs = assembly.GetCustomAttributes(typeof(LocalizationAttribute), true /* inherit */);
            if (attrobjs != null && attrobjs.Length > 0 && attrobjs[0] != null)
            {
                LocalizationAttribute locAttr = (LocalizationAttribute)attrobjs[0];
                string locGroupName = locAttr.locGroupName;
                if (locGroupName == null)
                    locGroupName = assembly.GetName().Name;
                var new_str = LocalizationDatabase.GetLocalizedStringWithGroupName(str, locGroupName);
                return new_str;
            }
            return str;
        }
    }

    /// <summary>
    /// This provides an auto dispose Localization system.
    /// This can be called recursively.
    /// </summary>
    [System.Obsolete("LocalizationGroup has been deprecated. Please use UnityEditor.LocalizationGroup instead", true)]
    public class LocalizationGroup : IDisposable
    {
        string m_LocGroupName;
        bool m_Pushed = false;

        /// <summary>
        /// a current group name for the localization.
        /// </summary>
        public string locGroupName { get { return m_LocGroupName; } }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public LocalizationGroup()
        {
            initialize(Assembly.GetCallingAssembly());
        }

        /// <summary>
        /// constructor.
        /// <param name="behaviour">group name will become the name of Assembly the behaviour belongs to.</param>
        /// </summary>
        public LocalizationGroup(Behaviour behaviour)
        {
            if (behaviour != null)
            {
                System.Type type = behaviour.GetType();
                initialize(type.Assembly);
            }
        }

        /// <summary>
        /// constructor.
        /// <param name="type">group name will become the name of Assembly the type belongs to.</param>
        /// </summary>
        public LocalizationGroup(System.Type type)
        {
            initialize(type.Assembly);
        }

        /// <summary>
        /// constructor.
        /// <param name="obj">group name will become the name of Assembly the obj belongs to.</param>
        /// </summary>
        public LocalizationGroup(System.Object obj)
        {
            if (obj == null)
                return;
            initialize(obj.GetType().Assembly);
        }

        void initialize(System.Reflection.Assembly assembly)
        {
            string groupName = null;
            object[] attrobjs = assembly.GetCustomAttributes(typeof(LocalizationAttribute), true /* inherit */);
            if (attrobjs != null && attrobjs.Length > 0 && attrobjs[0] != null) // focus on only the first.
            {
                LocalizationAttribute locAttr = (LocalizationAttribute)attrobjs[0];
                groupName = locAttr.locGroupName;
                if (groupName == null)
                    groupName = assembly.GetName().Name;
            }
            LocalizationGroupStack.Push(groupName);
            m_Pushed = true;
            m_LocGroupName = groupName;
        }

        /// <summary>
        /// dispose current state.
        /// </summary>
        public void Dispose()
        {
            if (m_Pushed)
                LocalizationGroupStack.Pop();
        }
    }
}
