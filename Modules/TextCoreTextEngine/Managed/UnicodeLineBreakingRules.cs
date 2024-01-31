// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;


namespace UnityEngine.TextCore.Text
{
    [Serializable]
    public class UnicodeLineBreakingRules
    {
        /// <summary>
        /// Text file that contains the Unicode line breaking rules defined here https://www.unicode.org/reports/tr14/tr14-22.html
        /// </summary>
        public UnityEngine.TextAsset lineBreakingRules
        {
            get => m_UnicodeLineBreakingRules;
        }
        [SerializeField]
        #pragma warning disable 0649
        UnityEngine.TextAsset m_UnicodeLineBreakingRules;

        /// <summary>
        /// Text file that contains the leading characters
        /// </summary>
        public UnityEngine.TextAsset leadingCharacters
        {
            get => m_LeadingCharacters;
        }
        [SerializeField]
        #pragma warning disable 0649
        UnityEngine.TextAsset m_LeadingCharacters;

        /// <summary>
        /// Text file that contains the following characters
        /// </summary>
        public UnityEngine.TextAsset followingCharacters
        {
            get => m_FollowingCharacters;
        }
        [SerializeField]
        #pragma warning disable 0649
        UnityEngine.TextAsset m_FollowingCharacters;

        /// <summary>
        ///
        /// </summary>
        internal HashSet<uint> leadingCharactersLookup
        {
            get
            {
                if (m_LeadingCharactersLookup == null)
                    LoadLineBreakingRules();

                return m_LeadingCharactersLookup;
            }
            set => m_LeadingCharactersLookup = value;
        }

        /// <summary>
        ///
        /// </summary>
        internal HashSet<uint> followingCharactersLookup
        {
            get
            {
                if (m_LeadingCharactersLookup == null)
                    LoadLineBreakingRules();

                return m_FollowingCharactersLookup;
            }
            set => m_FollowingCharactersLookup = value;
        }

        /// <summary>
        /// Determines if Modern or Traditional line breaking rules should be used for Korean text.
        /// </summary>
        public bool useModernHangulLineBreakingRules
        {
            get => m_UseModernHangulLineBreakingRules;
            set => m_UseModernHangulLineBreakingRules = value;
        }
        [SerializeField]
        private bool m_UseModernHangulLineBreakingRules;

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        HashSet<uint> m_LeadingCharactersLookup;
        HashSet<uint> m_FollowingCharactersLookup;

        /// <summary>
        ///
        /// </summary>
        internal void LoadLineBreakingRules()
        {
            if (m_LeadingCharactersLookup == null)
            {
                if (m_LeadingCharacters == null)
                    m_LeadingCharacters = Resources.Load<UnityEngine.TextAsset>("LineBreaking Leading Characters");

                m_LeadingCharactersLookup = m_LeadingCharacters != null ? GetCharacters(m_LeadingCharacters) : new HashSet<uint>();

                if (m_FollowingCharacters == null)
                    m_FollowingCharacters = Resources.Load<UnityEngine.TextAsset>("LineBreaking Following Characters");

                m_FollowingCharactersLookup = m_FollowingCharacters != null ? GetCharacters(m_FollowingCharacters) : new HashSet<uint>();
            }
        }

        internal void LoadLineBreakingRules(UnityEngine.TextAsset leadingRules, UnityEngine.TextAsset followingRules)
        {
            if (m_LeadingCharactersLookup == null)
            {
                if (leadingRules == null)
                    leadingRules = Resources.Load<UnityEngine.TextAsset>("LineBreaking Leading Characters");

                m_LeadingCharactersLookup = leadingRules != null ? GetCharacters(leadingRules) : new HashSet<uint>();

                if (followingRules == null)
                    followingRules = Resources.Load<UnityEngine.TextAsset>("LineBreaking Following Characters");

                m_FollowingCharactersLookup = followingRules != null ? GetCharacters(followingRules) : new HashSet<uint>();
            }
        }

        private static HashSet<uint> GetCharacters(UnityEngine.TextAsset file)
        {
            HashSet<uint> ruleSet = new HashSet<uint>();
            string text = file.text;

            for (int i = 0; i < text.Length; i++)
                ruleSet.Add(text[i]);

            return ruleSet;
        }
    }
}
