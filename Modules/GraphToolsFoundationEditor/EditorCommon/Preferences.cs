// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.CommandStateObserver;
using UnityEditor;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Class defining preference flags (of type bool).
    /// </summary>
    class BoolPref : Enumeration
    {
        // 0 was FullUIRebuildOnChange, now unused
        // 1 was WarnOnUIFullRebuild, now unused

        /// <summary>
        /// Toggles logging of UI build time.
        /// </summary>
        public static readonly BoolPref LogUIBuildTime = new BoolPref(2, nameof(LogUIBuildTime));

        // 3 was BoundObjectLogging, now unused

        /// <summary>
        /// Only process the graph model when user stops moving the mouse for a while. Otherwise, graph is processed after each change.
        /// </summary>
        public static readonly BoolPref OnlyProcessWhenIdle = new BoolPref(4, nameof(OnlyProcessWhenIdle), new[] { "AutoRecompile", "AutoProcess" });

        /// <summary>
        /// Toggles automatic alignment of nodes created from a port.
        /// </summary>
        public static readonly BoolPref AutoAlignDraggedWires = new BoolPref(5, nameof(AutoAlignDraggedWires));

        /// <summary>
        /// Toggles logging of dependencies between models by the <see cref="PositionDependenciesManager_Internal"/>.
        /// </summary>
        public static readonly BoolPref DependenciesLogging = new BoolPref(6, nameof(DependenciesLogging));

        /// <summary>
        /// Toggles displaying error message when a <see cref="ICommand"/> is dispatched while another <see cref="ICommand"/> is processed.
        /// </summary>
        public static readonly BoolPref ErrorOnRecursiveDispatch = new BoolPref(7, nameof(ErrorOnRecursiveDispatch));

        // 8 was ErrorOnMultipleDispatchesPerFrame, now unused

        /// <summary>
        /// Toggles logging of all dispatched <see cref="ICommand"/>.
        /// </summary>
        public static readonly BoolPref LogAllDispatchedCommands = new BoolPref(9, nameof(LogAllDispatchedCommands), new[] { "LogAllDispatchedActions" });

        /// <summary>
        /// Toggles displaying unused nodes in a different style.
        /// </summary>
        public static readonly BoolPref ShowUnusedNodes = new BoolPref(10, nameof(ShowUnusedNodes));

        /// <summary>
        /// Toggles logging of UI update information.
        /// </summary>
        public static readonly BoolPref LogUIUpdate = new BoolPref(12, nameof(LogUIUpdate));
        public static readonly BoolPref AutoItemizeVariables = new BoolPref(13, nameof(AutoItemizeVariables));
        public static readonly BoolPref AutoItemizeConstants = new BoolPref(14, nameof(AutoItemizeConstants));

        /// <summary>
        /// Prevents the Item Library to close after losing focus.
        /// </summary>
        /// <remarks>For debugging purposes</remarks>
        public static readonly BoolPref ItemLibraryStaysOpenOnBlur = new BoolPref(15, nameof(ItemLibraryStaysOpenOnBlur));

        /// <summary>
        /// The base preference id for tools. <see cref="BoolPref"/> values defined by tools should have an id
        /// larger than this value.
        /// </summary>
        protected static readonly int k_ToolBasePrefId = 10000;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoolPref" /> class.
        /// </summary>
        protected BoolPref(int id, string name, string[] obsoleteNames = null)
            : base(id, name, obsoleteNames)
        {
        }
    }

    /// <summary>
    /// Class defining preference of type int.
    /// </summary>
    class IntPref : Enumeration
    {
        /// <summary>
        /// The base preference id for tools. <see cref="IntPref"/> values defined by tools should have an id
        /// larger than this value.
        /// </summary>
        protected static readonly int k_ToolBasePrefId = 10000;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntPref" /> class.
        /// </summary>
        protected IntPref(int id, string name, string[] obsoleteNames = null)
            : base(id, name, obsoleteNames)
        {
        }
    }

    /// <summary>
    /// Class defining preference of type string.
    /// </summary>
    class StringPref : Enumeration
    {
        public static readonly StringPref ItemLibrarySize = new StringPref(0, nameof(ItemLibrarySize));

        /// <summary>
        /// The base preference id for tools. <see cref="StringPref"/> values defined by tools should have an id
        /// larger than this value.
        /// </summary>
        protected static readonly int k_ToolBasePrefId = 10000;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringPref" /> class.
        /// </summary>
        protected StringPref(int id, string name, string[] obsoleteNames = null)
            : base(id, name, obsoleteNames)
        {
        }
    }

    /// <summary>
    /// Holds the configuration options for the tool.
    /// The preferences are backed by <see cref="EditorPrefs"/>, which means they are
    /// persisted in the editor preferences file.
    /// </summary>
    class Preferences
    {
        /// <summary>
        /// Creates and Initializes a new instance of <see cref="Preferences"/>.
        /// </summary>
        /// <param name="editorPreferencesPrefix">A unique prefix for storing preferences in the editor.</param>
        /// <returns>A new Preferences instance.</returns>
        public static Preferences CreatePreferences(string editorPreferencesPrefix)
        {
            var preferences = new Preferences(editorPreferencesPrefix);
            preferences.Initialize<BoolPref, IntPref, StringPref>();
            return preferences;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Preferences"/> without reading values stored in user preferences.
        /// <remarks>Useful when creating tests that shouldn't be affected by previous user preferences.</remarks>
        /// </summary>
        /// <param name="editorPreferencesPrefix">A unique prefix for storing preferences in the editor.</param>
        /// <returns>A new Preferences instance.</returns>
        internal static Preferences CreateTransient_Internal(string editorPreferencesPrefix)
        {
            var preferences = new Preferences(editorPreferencesPrefix);
            preferences.Transient = true;
            preferences.Initialize<BoolPref, IntPref, StringPref>();
            return preferences;
        }

        Dictionary<BoolPref, bool> m_BoolPrefs;
        Dictionary<IntPref, int> m_IntPrefs;
        Dictionary<StringPref, string> m_StringPrefs;
        string m_EditorPreferencesPrefix;

        /// <summary>
        /// If true, preferences aren't actually read or saved to User Preferences.
        /// <remarks>Useful for unit tests that shouldn't have different behaviours depending on user preferences.</remarks>
        /// </summary>
        bool Transient { get; set; }

        Preferences()
        {
            m_BoolPrefs = new Dictionary<BoolPref, bool>();
            m_IntPrefs = new Dictionary<IntPref, int>();
            m_StringPrefs = new Dictionary<StringPref, string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Preferences" /> class.
        /// </summary>
        /// <param name="editorPreferencesPrefix">The prefix for the preference names.</param>
        protected Preferences(string editorPreferencesPrefix) : this()
        {
            m_EditorPreferencesPrefix = editorPreferencesPrefix;
            if (!m_EditorPreferencesPrefix.EndsWith("."))
            {
                m_EditorPreferencesPrefix += ".";
            }
        }

        /// <summary>
        /// Initializes this <see cref="Preferences"/> object using the instances of
        /// the <see cref="Enumeration"/> types as keys. The values for the keys is
        /// the value from <see cref="EditorPrefs"/> for the key or, if there is no such key, the value
        /// set by <see cref="SetDefaultValues"/> or, if no value is set, the default value for the type.
        /// </summary>
        /// <typeparam name="TBool">The type defining boolean preference keys.</typeparam>
        /// <typeparam name="TInt">The type defining integer preference keys.</typeparam>
        /// <typeparam name="TString">The type defining string preference keys.</typeparam>
        protected void Initialize<TBool, TInt, TString>()
            where TBool : BoolPref
            where TInt : IntPref
            where TString : StringPref
        {
            InitPrefType<TBool, BoolPref, bool>(m_BoolPrefs);
            InitPrefType<TInt, IntPref, int>(m_IntPrefs);
            InitPrefType<TString, StringPref, string>(m_StringPrefs);

            SetDefaultValues();
            if (!Transient)
                ReadAllFromEditorPrefs<TBool, TInt, TString>();
        }

        /// <summary>
        /// Sets the default values for the preferences, when `default(type)` is not the right default value.
        /// </summary>
        protected virtual void SetDefaultValues()
        {
            if (Unsupported.IsDeveloperBuild())
            {
                SetBoolNoEditorUpdate(BoolPref.ErrorOnRecursiveDispatch, true);
            }
        }

        /// <summary>
        /// Sets the value of a boolean preference without updating <see cref="EditorPrefs"/>.
        /// Use this function to set a value that should not be persisted, like a default value.
        /// </summary>
        /// <param name="k">The preference key.</param>
        /// <param name="value">The preference value.</param>
        public void SetBoolNoEditorUpdate(BoolPref k, bool value)
        {
            m_BoolPrefs[k] = value;
        }

        /// <summary>
        /// Sets the value of an integer preference without updating <see cref="EditorPrefs"/>.
        /// Use this function to set a value that should not be persisted, like a default value.
        /// </summary>
        /// <param name="k">The preference key.</param>
        /// <param name="value">The preference value.</param>
        public void SetIntNoEditorUpdate(IntPref k, int value)
        {
            m_IntPrefs[k] = value;
        }

        /// <summary>
        /// Sets the value of a string preference without updating <see cref="EditorPrefs"/>.
        /// Use this function to set a value that should not be persisted, like a default value.
        /// </summary>
        /// <param name="k">The preference key.</param>
        /// <param name="value">The preference value.</param>
        public void SetStringNoEditorUpdate(StringPref k, string value)
        {
            m_StringPrefs[k] = value;
        }

        /// <summary>
        /// Returns the value of a boolean preference.
        /// </summary>
        /// <param name="k">The preference key.</param>
        /// <returns>The value of the preference, or false if the preference was never set.</returns>
        public bool GetBool(BoolPref k)
        {
            m_BoolPrefs.TryGetValue(k, out var result);
            return result;
        }

        /// <summary>
        /// Returns the value of an integer preference.
        /// </summary>
        /// <param name="k">The preference key.</param>
        /// <returns>The value of the preference, or 0 if the preference was never set.</returns>
        public int GetInt(IntPref k)
        {
            m_IntPrefs.TryGetValue(k, out var result);
            return result;
        }

        /// <summary>
        /// Returns the value of a string preference.
        /// </summary>
        /// <param name="k">The preference key.</param>
        /// <returns>The value of the preference, or 0 if the preference was never set.</returns>
        public string GetString(StringPref k)
        {
            m_StringPrefs.TryGetValue(k, out var result);
            return result;
        }

        /// <summary>
        /// Sets the value of a boolean preference.
        /// </summary>
        /// <param name="k">The preference key.</param>
        /// <param name="value">The preference value.</param>
        public void SetBool(BoolPref k, bool value)
        {
            SetBoolNoEditorUpdate(k, value);

            if (Transient)
                return;

            EditorPrefs.SetBool(GetKeyName(k), value);

            if (k.ObsoleteNames != null)
            {
                foreach (var obsoleteName in k.ObsoleteNames)
                {
                    EditorPrefs.DeleteKey(m_EditorPreferencesPrefix + obsoleteName);
                }
            }
        }

        /// <summary>
        /// Sets the value of an integer preference.
        /// </summary>
        /// <param name="k">The preference key.</param>
        /// <param name="value">The preference value.</param>
        public void SetInt(IntPref k, int value)
        {
            SetIntNoEditorUpdate(k, value);

            if (Transient)
                return;

            EditorPrefs.SetInt(GetKeyName(k), value);

            if (k.ObsoleteNames != null)
            {
                foreach (var obsoleteName in k.ObsoleteNames)
                {
                    EditorPrefs.DeleteKey(m_EditorPreferencesPrefix + obsoleteName);
                }
            }
        }

        /// <summary>
        /// Sets the value of a string preference.
        /// </summary>
        /// <param name="k">The preference key.</param>
        /// <param name="value">The preference value.</param>
        public void SetString(StringPref k, string value)
        {
            SetStringNoEditorUpdate(k, value);

            if (Transient)
                return;

            EditorPrefs.SetString(GetKeyName(k), value);

            if (k.ObsoleteNames != null)
            {
                foreach (var obsoleteName in k.ObsoleteNames)
                {
                    EditorPrefs.DeleteKey(m_EditorPreferencesPrefix + obsoleteName);
                }
            }
        }

        /// <summary>
        /// Toggles the value of a boolean preference.
        /// </summary>
        /// <param name="k">The preference key.</param>
        public void ToggleBool(BoolPref k)
        {
            SetBool(k, !GetBool(k));
        }

        static void InitPrefType<TKeySource, TKey, TValue>(Dictionary<TKey, TValue> prefs)
            where TKeySource : TKey
            where TKey : Enumeration
        {
            var keys = Enumeration.GetAll<TKeySource, TKey>();
            foreach (var key in keys)
                prefs[key] = default;
        }

        void ReadAllFromEditorPrefs<TBool, TInt, TString>()
            where TBool : BoolPref
            where TInt : IntPref
            where TString : StringPref
        {
            if (Transient)
                return;

            foreach (var pref in Enumeration.GetAll<TBool, BoolPref>())
            {
                ReadBoolFromEditorPref(pref);
            }

            foreach (var pref in Enumeration.GetAll<TInt, IntPref>())
            {
                ReadIntFromEditorPref(pref);
            }

            foreach (var pref in Enumeration.GetAll<TString, StringPref>())
            {
                ReadStringFromEditorPref(pref);
            }
        }

        void ReadBoolFromEditorPref(BoolPref k)
        {
            if (Transient)
                return;

            string keyName = GetKeyNameInEditorPrefs(k);
            if (keyName != null)
            {
                bool value = EditorPrefs.GetBool(keyName);
                SetBoolNoEditorUpdate(k, value);
            }
        }

        void ReadIntFromEditorPref(IntPref k)
        {
            if (Transient)
                return;

            string keyName = GetKeyNameInEditorPrefs(k);
            if (keyName != null)
            {
                int value = EditorPrefs.GetInt(keyName);
                SetIntNoEditorUpdate(k, value);
            }
        }

        void ReadStringFromEditorPref(StringPref k)
        {
            if (Transient)
                return;

            string keyName = GetKeyNameInEditorPrefs(k);
            if (keyName != null)
            {
                string value = EditorPrefs.GetString(keyName);
                SetStringNoEditorUpdate(k, value);
            }
        }

        string GetKeyName<T>(T key) where T : Enumeration
        {
            return m_EditorPreferencesPrefix + key;
        }

        IEnumerable<string> GetObsoleteKeyNames<T>(T key) where T : Enumeration
        {
            foreach (var obsoleteName in key.ObsoleteNames)
            {
                yield return m_EditorPreferencesPrefix + obsoleteName;
            }
        }

        string GetKeyNameInEditorPrefs<T>(T key) where T : Enumeration
        {
            var keyName = GetKeyName(key);

            if (!EditorPrefs.HasKey(keyName) && key.ObsoleteNames != null)
            {
                keyName = null;

                foreach (var obsoleteKeyName in GetObsoleteKeyNames(key))
                {
                    if (EditorPrefs.HasKey(obsoleteKeyName))
                    {
                        keyName = obsoleteKeyName;
                        break;
                    }
                }
            }

            return keyName;
        }
    }
}
