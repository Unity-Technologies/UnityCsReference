// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.TextCore
{
    [Serializable]
    class TextStyleSheet : ScriptableObject
    {
        static TextStyleSheet s_Instance;

        [SerializeField]
        List<TextStyle> m_StyleList = new List<TextStyle>(1);
        Dictionary<int, TextStyle> m_StyleDictionary = new Dictionary<int, TextStyle>();

        /// <summary>
        /// Get a singleton instance of the TextStyleSheet
        /// </summary>
        public static TextStyleSheet instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = TextSettings.defaultStyleSheet;

                    if (s_Instance == null) return null;

                    // Load the style dictionary.
                    s_Instance.LoadStyleDictionaryInternal();
                }

                return s_Instance;
            }
        }

        /// <summary>
        /// Static Function to load the Default Style Sheet.
        /// </summary>
        /// <returns></returns>
        public static TextStyleSheet LoadDefaultStyleSheet()
        {
            s_Instance = null;

            return instance;
        }

        /// <summary>
        /// Function to retrieve the Style matching the HashCode.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <returns></returns>
        public static TextStyle GetStyle(int hashCode)
        {
            if (instance == null)
            {
                return null;
            }

            return instance.GetStyleInternal(hashCode);
        }

        /// <summary>
        /// Internal method to retrieve the Style matching the Hashcode.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <returns></returns>
        TextStyle GetStyleInternal(int hashCode)
        {
            TextStyle style;
            if (m_StyleDictionary.TryGetValue(hashCode, out style))
            {
                return style;
            }

            return null;
        }

        public void UpdateStyleDictionaryKey(int old_key, int new_key)
        {
            if (m_StyleDictionary.ContainsKey(old_key))
            {
                TextStyle style = m_StyleDictionary[old_key];
                m_StyleDictionary.Add(new_key, style);
                m_StyleDictionary.Remove(old_key);
            }
        }

        /// <summary>
        /// Function to refresh the Style Dictionary.
        /// </summary>
        public static void RefreshStyles()
        {
            instance.LoadStyleDictionaryInternal();
        }

        /// <summary>
        ///
        /// </summary>
        void LoadStyleDictionaryInternal()
        {
            m_StyleDictionary.Clear();

            // Read Styles from style list and store them into dictionary for faster access.
            for (int i = 0; i < m_StyleList.Count; i++)
            {
                m_StyleList[i].RefreshStyle();

                if (!m_StyleDictionary.ContainsKey(m_StyleList[i].hashCode))
                    m_StyleDictionary.Add(m_StyleList[i].hashCode, m_StyleList[i]);
            }
        }
    }
}
