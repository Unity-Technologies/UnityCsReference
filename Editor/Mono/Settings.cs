// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.Linq;

namespace UnityEditor
{
    internal interface IPrefType
    {
        string ToUniqueString();
        void FromUniqueString(string sstr);
        void Load();
    }

    internal class PrefColor : IPrefType
    {
        string m_Name;
        Color m_Color;
        Color m_DefaultColor;

        bool m_SeparateColors;
        Color m_OptionalDarkColor;
        Color m_OptionalDarkDefaultColor;

        bool m_Loaded;

        public PrefColor()
        {
            m_Loaded = true;
        }

        public PrefColor(string name, float defaultRed, float defaultGreen, float defaultBlue, float defaultAlpha)
        {
            this.m_Name = name;
            this.m_Color = this.m_DefaultColor = new Color(defaultRed, defaultGreen, defaultBlue, defaultAlpha);
            this.m_SeparateColors = false;
            this.m_OptionalDarkColor = this.m_OptionalDarkDefaultColor = Color.clear;
            Settings.Add(this);
            m_Loaded = false;
        }

        public PrefColor(string name, float defaultRed, float defaultGreen, float defaultBlue, float defaultAlpha, float defaultRed2, float defaultGreen2, float defaultBlue2, float defaultAlpha2)
        {
            this.m_Name = name;
            this.m_Color = this.m_DefaultColor = new Color(defaultRed, defaultGreen, defaultBlue, defaultAlpha);
            this.m_SeparateColors = true;
            this.m_OptionalDarkColor = this.m_OptionalDarkDefaultColor = new Color(defaultRed2, defaultGreen2, defaultBlue2, defaultAlpha2);
            Settings.Add(this);
            m_Loaded = false;
        }

        public void Load()
        {
            if (m_Loaded)
                return;

            m_Loaded = true;

            PrefColor pk = Settings.Get(m_Name, this);
            this.m_Name = pk.m_Name;
            this.m_Color = pk.m_Color;
            this.m_SeparateColors = pk.m_SeparateColors;
            this.m_OptionalDarkColor = pk.m_OptionalDarkColor;
        }

        public Color Color
        {
            get
            {
                Load();

                if (m_SeparateColors && EditorGUIUtility.isProSkin)
                    return m_OptionalDarkColor;

                return m_Color;
            }
            set
            {
                Load();

                if (m_SeparateColors && EditorGUIUtility.isProSkin)
                    m_OptionalDarkColor = value;
                else
                    m_Color = value;
            }
        }
        public string Name { get { Load(); return m_Name; } }

        public static implicit operator Color(PrefColor pcolor) { return pcolor.Color; }

        public string ToUniqueString()
        {
            Load();

            if (m_SeparateColors)
                return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0};{1};{2};{3};{4};{5};{6};{7};{8}", m_Name, m_Color.r, m_Color.g, m_Color.b, m_Color.a, m_OptionalDarkColor.r, m_OptionalDarkColor.g, m_OptionalDarkColor.b, m_OptionalDarkColor.a);

            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0};{1};{2};{3};{4}", m_Name, m_Color.r, m_Color.g, m_Color.b, m_Color.a);
        }

        public void FromUniqueString(string s)
        {
            Load();

            string[] split = s.Split(';');

            // PrefColor with a single color should have 5 substrings.
            // PrefColor with separate colors should have 9 substrings.
            if (split.Length != 5 && split.Length != 9)
            {
                Debug.LogError("Parsing PrefColor failed");
                return;
            }

            m_Name = split[0];
            split[1] = split[1].Replace(',', '.');
            split[2] = split[2].Replace(',', '.');
            split[3] = split[3].Replace(',', '.');
            split[4] = split[4].Replace(',', '.');
            float r, g, b, a;
            bool success = float.TryParse(split[1], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out r);
            success &= float.TryParse(split[2], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out g);
            success &= float.TryParse(split[3], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out b);
            success &= float.TryParse(split[4], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out a);

            if (success)
            {
                m_Color = new Color(r, g, b, a);
            }
            else
            {
                Debug.LogError("Parsing PrefColor failed");
            }

            if (split.Length == 9)
            {
                m_SeparateColors = true;

                split[5] = split[5].Replace(',', '.');
                split[6] = split[6].Replace(',', '.');
                split[7] = split[7].Replace(',', '.');
                split[8] = split[8].Replace(',', '.');
                success = float.TryParse(split[5], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out r);
                success &= float.TryParse(split[6], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out g);
                success &= float.TryParse(split[7], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out b);
                success &= float.TryParse(split[8], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out a);

                if (success)
                {
                    m_OptionalDarkColor = new Color(r, g, b, a);
                }
                else
                {
                    Debug.LogError("Parsing PrefColor failed");
                }
            }
            else
            {
                m_SeparateColors = false;
                m_OptionalDarkColor = Color.clear;
            }
        }

        internal void ResetToDefault()
        {
            Load();
            m_Color = m_DefaultColor;
            m_OptionalDarkColor = m_OptionalDarkDefaultColor;
        }
    }

    internal class PrefKey : IPrefType
    {
        bool m_Loaded;

        public PrefKey() { m_Loaded = true; }

        public PrefKey(string name, string shortcut)
        {
            this.m_name = name;
            this.m_Shortcut = shortcut;
            this.m_DefaultShortcut = shortcut;
            Settings.Add(this);
            m_Loaded = false;
        }

        public void Load()
        {
            if (m_Loaded)
                return;

            m_Loaded = true;

            this.m_event = Event.KeyboardEvent(m_Shortcut);
            PrefKey pk = Settings.Get(m_name, this);
            this.m_name = pk.Name;
            this.m_event = pk.KeyboardEvent;
        }

        public static implicit operator Event(PrefKey pkey) { pkey.Load(); return pkey.m_event; }

        public string Name { get { Load(); return m_name; } }

        public Event  KeyboardEvent
        {
            get { Load(); return m_event; }
            set { Load(); m_event = value; }
        }

        private string m_name;
        private Event  m_event;
        private string m_Shortcut;
        private string m_DefaultShortcut;

        public string ToUniqueString()
        {
            Load();
            string s = m_name + ";" + (m_event.alt ? "&" : "") + (m_event.command ? "%" : "") + (m_event.shift ? "#" : "") + (m_event.control ? "^" : "") + m_event.keyCode;
            return s;
        }

        public bool activated
        {
            get
            {
                Load();
                return Event.current.Equals((Event)this) && !GUIUtility.textFieldInput;
            }
        }


        public void FromUniqueString(string s)
        {
            Load();
            int i = s.IndexOf(";");
            if (i < 0)
            {
                Debug.LogError("Malformed string in Keyboard preferences");
                return;
            }
            m_name = s.Substring(0, i);
            m_event = Event.KeyboardEvent(s.Substring(i + 1));
        }

        internal void ResetToDefault()
        {
            Load();
            m_event = Event.KeyboardEvent(m_DefaultShortcut);
        }
    }


    internal class Settings
    {
        static List<IPrefType> m_AddedPrefs = new List<IPrefType>();
        static SortedList<string, object> m_Prefs = new SortedList<string, object>();

        static internal void Add(IPrefType value)
        {
            m_AddedPrefs.Add(value);
        }

        static internal T Get<T>(string name, T defaultValue)
            where T : IPrefType, new()
        {
            Load();

            if (defaultValue == null)
                throw new System.ArgumentException("default can not be null", "defaultValue");
            if (m_Prefs.ContainsKey(name))
                return (T)m_Prefs[name];
            else
            {
                string sstr = EditorPrefs.GetString(name, "");
                if (sstr == "")
                {
                    Set(name, defaultValue);
                    return defaultValue;
                }
                else
                {
                    defaultValue.FromUniqueString(sstr);
                    Set(name, defaultValue);
                    return defaultValue;
                }
            }
        }

        static internal void Set<T>(string name, T value)
            where T : IPrefType
        {
            Load();

            EditorPrefs.SetString(name, value.ToUniqueString());
            m_Prefs[name] = value;
        }

        static internal IEnumerable<KeyValuePair<string, T>> Prefs<T>()
            where T : IPrefType
        {
            Load();

            foreach (KeyValuePair<string, object> kvp in m_Prefs)
            {
                if (kvp.Value is T)
                    yield return new KeyValuePair<string, T>(kvp.Key, (T)kvp.Value);
            }
        }

        static void Load()
        {
            if (!m_AddedPrefs.Any())
                return;

            List<IPrefType> loadPrefs = new List<IPrefType>(m_AddedPrefs);
            m_AddedPrefs.Clear();

            foreach (IPrefType pref in loadPrefs)
                pref.Load();
        }
    }

    internal class SavedInt
    {
        int m_Value;
        string m_Name;
        bool m_Loaded;

        public SavedInt(string name, int value)
        {
            m_Name = name;
            m_Loaded = false;
            m_Value = value;
        }

        private void Load()
        {
            if (m_Loaded)
                return;

            m_Loaded = true;
            m_Value = EditorPrefs.GetInt(m_Name, m_Value);
        }

        public int value
        {
            get { Load(); return m_Value; }
            set
            {
                Load();
                if (m_Value == value)
                    return;
                m_Value = value;
                EditorPrefs.SetInt(m_Name, value);
            }
        }

        public static implicit operator int(SavedInt s)
        {
            return s.value;
        }
    }

    internal class SavedFloat
    {
        float m_Value;
        string m_Name;
        bool m_Loaded;

        public SavedFloat(string name, float value)
        {
            m_Name = name;
            m_Loaded = false;
            m_Value = value;
        }

        private void Load()
        {
            if (m_Loaded)
                return;

            m_Loaded = true;
            m_Value = EditorPrefs.GetFloat(m_Name, m_Value);
        }

        public float value
        {
            get { Load(); return m_Value; }
            set
            {
                Load();
                if (m_Value == value)
                    return;
                m_Value = value;
                EditorPrefs.SetFloat(m_Name, value);
            }
        }

        public static implicit operator float(SavedFloat s)
        {
            return s.value;
        }
    }

    internal class SavedBool
    {
        bool m_Value;
        string m_Name;
        bool m_Loaded;

        public SavedBool(string name, bool value)
        {
            m_Name = name;
            m_Loaded = false;
            m_Value = value;
        }

        private void Load()
        {
            if (m_Loaded)
                return;

            m_Loaded = true;
            m_Value = EditorPrefs.GetBool(m_Name, m_Value);
        }

        public bool value
        {
            get { Load(); return m_Value; }
            set
            {
                Load();
                if (m_Value == value)
                    return;
                m_Value = value;
                EditorPrefs.SetBool(m_Name, value);
            }
        }

        public static implicit operator bool(SavedBool s)
        {
            return s.value;
        }
    }
}
