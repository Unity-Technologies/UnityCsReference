// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.TextCore
{
    [System.Serializable]
    class TextStyle
    {
        /// <summary>
        /// The name identifying this style.
        /// </summary>
        public string name
        {
            get { return m_Name; }
            set
            {
                if (value != m_Name)
                    m_Name = value;
            }
        }

        /// <summary>
        /// The hash code corresponding to the name of this style.
        /// </summary>
        public int hashCode
        {
            get { return m_HashCode; }
            set
            {
                if (value != m_HashCode)
                    m_HashCode = value;
            }
        }

        /// <summary>
        /// The initial definition of the style.
        /// </summary>
        public string styleOpeningDefinition { get { return m_OpeningDefinition; } }

        /// <summary>
        /// The closing definition of the style.
        /// </summary>
        public string styleClosingDefinition { get { return m_ClosingDefinition; } }


        public int[] styleOpeningTagArray { get { return m_OpeningTagArray; } }

        public int[] styleClosingTagArray { get { return m_ClosingTagArray; } }


        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        string m_Name;

        [SerializeField]
        int m_HashCode;

        [SerializeField]
        string m_OpeningDefinition = string.Empty;

        [SerializeField]
        string m_ClosingDefinition = string.Empty;

        [SerializeField]
        int[] m_OpeningTagArray;

        [SerializeField]
        int[] m_ClosingTagArray;

        /// <summary>
        /// Function to update the content of the int[] resulting from changes to OpeningDefinition & ClosingDefinition.
        /// </summary>
        public void RefreshStyle()
        {
            m_HashCode = TextUtilities.GetHashCodeCaseInSensitive(m_Name);

            m_OpeningTagArray = new int[m_OpeningDefinition.Length];
            for (int i = 0; i < m_OpeningDefinition.Length; i++)
                m_OpeningTagArray[i] = m_OpeningDefinition[i];

            m_ClosingTagArray = new int[m_ClosingDefinition.Length];
            for (int i = 0; i < m_ClosingDefinition.Length; i++)
                m_ClosingTagArray[i] = m_ClosingDefinition[i];
        }
    }
}
