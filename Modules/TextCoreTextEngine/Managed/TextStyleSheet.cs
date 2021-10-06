// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;


namespace UnityEngine.TextCore.Text
{
    [Serializable][ExcludeFromPresetAttribute][ExcludeFromObjectFactory]
    public class TextStyleSheet : ScriptableObject
    {
        /// <summary>
        ///
        /// </summary>
        internal List<TextStyle> styles
        {
            get { return m_StyleList; }
        }

        [SerializeField]
        private List<TextStyle> m_StyleList = new List<TextStyle>(1);
        private Dictionary<int, TextStyle> m_StyleLookupDictionary;

        private void Reset()
        {
            LoadStyleDictionaryInternal();
        }

        /// <summary>
        /// Get the Style for the given hash code value.
        /// </summary>
        /// <param name="hashCode">Hash code of the style.</param>
        /// <returns>The style matching the hash code.</returns>
        public TextStyle GetStyle(int hashCode)
        {
            if (m_StyleLookupDictionary == null)
                LoadStyleDictionaryInternal();

            TextStyle style;

            if (m_StyleLookupDictionary.TryGetValue(hashCode, out style))
                return style;

            return null;
        }

        /// <summary>
        /// Get the Style for the given name.
        /// </summary>
        /// <param name="name">The name of the style.</param>
        /// <returns>The style if found.</returns>
        public TextStyle GetStyle(string name)
        {
            if (m_StyleLookupDictionary == null)
                LoadStyleDictionaryInternal();

            int hashCode = TextUtilities.GetHashCodeCaseInSensitive(name);
            TextStyle style;

            if (m_StyleLookupDictionary.TryGetValue(hashCode, out style))
                return style;

            return null;
        }

        /// <summary>
        /// Function to refresh the Style Dictionary.
        /// </summary>
        public void RefreshStyles()
        {
            LoadStyleDictionaryInternal();
        }

        /// <summary>
        ///
        /// </summary>
        private void LoadStyleDictionaryInternal()
        {
            if (m_StyleLookupDictionary == null)
                m_StyleLookupDictionary = new Dictionary<int, TextStyle>();
            else
                m_StyleLookupDictionary.Clear();

            // Read Styles from style list and store them into dictionary for faster access.
            for (int i = 0; i < m_StyleList.Count; i++)
            {
                m_StyleList[i].RefreshStyle();

                if (!m_StyleLookupDictionary.ContainsKey(m_StyleList[i].hashCode))
                    m_StyleLookupDictionary.Add(m_StyleList[i].hashCode, m_StyleList[i]);
            }

            // Add Normal Style if it does not already exists
            int normalStyleHashCode = TextUtilities.GetHashCodeCaseInSensitive("Normal");
            if (!m_StyleLookupDictionary.ContainsKey(normalStyleHashCode))
            {
                TextStyle style = new TextStyle("Normal", string.Empty, string.Empty);
                m_StyleList.Add(style);
                m_StyleLookupDictionary.Add(normalStyleHashCode, style);
            }

            //// Event to update objects when styles are changed in the editor.
            TextEventManager.ON_TEXT_STYLE_PROPERTY_CHANGED(true);
        }
    }
}
