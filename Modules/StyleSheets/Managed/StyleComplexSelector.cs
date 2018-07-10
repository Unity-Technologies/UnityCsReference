// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using System.Linq;

namespace UnityEngine.StyleSheets
{
    [Serializable]
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
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
            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
            internal set
            {
                m_Selectors = value;
            }
        }

        [SerializeField]
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal int ruleIndex;

        public override string ToString()
        {
            return string.Format("[{0}]", string.Join(", ", m_Selectors.Select(x => x.ToString()).ToArray()));
        }
    }
}
