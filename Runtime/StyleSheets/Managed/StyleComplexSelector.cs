// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.StyleSheets
{
    [Serializable]
    internal class StyleComplexSelector
    {
        [SerializeField]
        int m_Specificity;

        // This "score" is calculated according to the enclosing complex selector specificity
        public int specificity
        {
            get
            {
                return m_Specificity;
            }
            internal set
            {
                m_Specificity = value;
            }
        }

        // This reference is set at runtime as convenience, but is not serialzed
        public StyleRule rule { get; internal set; }

        // A complex selector can be considered simple if it's made of only one selector
        public bool isSimple
        {
            get
            {
                return selectors.Length == 1;
            }
        }

        [SerializeField]
        StyleSelector[] m_Selectors;

        public StyleSelector[] selectors
        {
            get
            {
                return m_Selectors;
            }
            internal set
            {
                m_Selectors = value;
            }
        }

        [SerializeField]
        internal int ruleIndex;
    }
}
