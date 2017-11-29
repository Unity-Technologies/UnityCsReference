// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using System.Security.Cryptography;

namespace UnityEditor
{
    // internal class to manage localized editor fonts
    internal class LocalizedEditorFontManager
    {
        private class FontSetting
        {
            private string[] m_fontNames;
            public FontSetting(string[] fontNames)
            {
                m_fontNames = fontNames;
            }

            public string[] fontNames { get { return m_fontNames; } }
        }

        private class FontDictionary
        {
            private Dictionary<string, FontSetting> m_dictionary;
            public FontDictionary()
            {
                m_dictionary = new Dictionary<string, FontSetting>();
            }

            public void Add(string key, FontSetting value)
            {
                m_dictionary.Add(key, value);
            }

            public bool ContainsKey(string key)
            {
                return m_dictionary.ContainsKey(key);
            }

            public FontSetting this[string key]
            {
                get { return m_dictionary[key]; }
            }
        }

        private static Dictionary<SystemLanguage, FontDictionary> m_fontDictionaries;

        private static FontDictionary GetFontDictionary(SystemLanguage language)
        {
            if (!m_fontDictionaries.ContainsKey(language))
            {
                // Debug.LogErrorFormat("no settings for the language [{0}].", language);
                return null;
            }
            return m_fontDictionaries[language];
        }

        private static void ReadFontSettings()
        {
            // init and reload
            if (m_fontDictionaries == null)
            {
                m_fontDictionaries = new Dictionary<SystemLanguage, FontDictionary>();
            }
            m_fontDictionaries.Clear();

            string filepath = null;
            if (filepath == null || !System.IO.File.Exists(filepath))
            {
                filepath = EditorApplication.applicationContentsPath + "/Resources/fontsettings.txt";
            }
            if (!System.IO.File.Exists(filepath))
            {
                // Debug.LogError("no [" + filepath + "] found.");
                return;
            }

            // load setting from file and initialize
            byte[] data = System.IO.File.ReadAllBytes(filepath);

            string dataStr = System.Text.Encoding.UTF8.GetString(data);
            string[] lines = dataStr.Split('\n');
            foreach (var line0 in lines)
            {
                var line = line0;
                line = line.Split('#')[0];
                line = line.Trim();
                if (line.Length <= 0)
                {
                    continue;
                }
                string[] cols = line.Split('|');
                if (cols.Length != 2)
                {
                    Debug.LogError("wrong format for the fontsettings.txt.");
                    continue;
                }
                var lang = (SystemLanguage)System.Enum.Parse(typeof(SystemLanguage), cols[0].Trim());
                string[] pair = cols[1].Split('=');
                if (pair.Length != 2)
                {
                    Debug.LogError("wrong format for the fontsettings.txt.");
                    continue;
                }

                string baseFontName = pair[0].Trim();
                string[] fontNames = pair[1].Split(',');

                for (int i = 0; i < fontNames.Length; ++i)
                {
                    fontNames[i] = fontNames[i].Trim();
                }
                if (!m_fontDictionaries.ContainsKey(lang))
                {
                    m_fontDictionaries.Add(lang, new FontDictionary());
                }
                m_fontDictionaries[lang].Add(baseFontName, new FontSetting(fontNames));
            }
        }

        public static void LocalizeEditorFonts()
        {
            ReadFontSettings();

            var dict = GetFontDictionary(LocalizationDatabase.currentEditorLanguage);
            if (dict == null)
                return;

            /*
            We have the Lucida Grande ttf asset which was always the font we used in the editor.
            But on windows we call ReplaceFontForWindows to override the font used for that asset.
            It would probably make sense to rename the asset to something generic, such as "EditorFont" instead,
            though that might possibly break some things.
            */
            ReplaceFontForLocalization(dict, (Font)EditorGUIUtility.LoadRequired(EditorResourcesUtility.fontsPath + "Lucida Grande.ttf"));
            ReplaceFontForLocalization(dict, (Font)EditorGUIUtility.LoadRequired(EditorResourcesUtility.fontsPath + "Lucida Grande Bold.ttf"));
            ReplaceFontForLocalization(dict, (Font)EditorGUIUtility.LoadRequired(EditorResourcesUtility.fontsPath + "Lucida Grande Small.ttf"));
            ReplaceFontForLocalization(dict, (Font)EditorGUIUtility.LoadRequired(EditorResourcesUtility.fontsPath + "Lucida Grande Small Bold.ttf"));
            ReplaceFontForLocalization(dict, (Font)EditorGUIUtility.LoadRequired(EditorResourcesUtility.fontsPath + "Lucida Grande Big.ttf"));
        }

        static void ReplaceFontForLocalization(FontDictionary dict, Font font)
        {
            if (dict.ContainsKey(font.name))
            {
                font.fontNames = dict[font.name].fontNames;
                // Avoid the font to be unloaded in EditorUtility.UnloadUnusedAssetsIgnoreManagedReferences and lose the font name information.
                font.hideFlags = HideFlags.HideAndDontSave;
            }
        }
    }
}
