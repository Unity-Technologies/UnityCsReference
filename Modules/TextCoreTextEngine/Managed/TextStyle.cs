// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable 0649 // Disabled warnings.

namespace UnityEngine.TextCore.Text
{
    [System.Serializable]
    public class TextStyle
    {
        public static TextStyle NormalStyle
        {
            get
            {
                if (k_NormalStyle == null)
                    k_NormalStyle = new TextStyle("Normal", string.Empty, string.Empty);

                return k_NormalStyle;
            }
        }
        internal static TextStyle k_NormalStyle;

        // PUBLIC PROPERTIES

        /// <summary>
        /// The name identifying this style. ex. <style="name">.
        /// </summary>
        public string name
        { get { return m_Name; } set { if (value != m_Name) m_Name = value; } }

        /// <summary>
        /// The hash code corresponding to the name of this style.
        /// </summary>
        public int hashCode
        { get { return m_HashCode; } set { if (value != m_HashCode) m_HashCode = value; } }

        /// <summary>
        /// The initial definition of the style. ex. <b> <u>.
        /// </summary>
        public string styleOpeningDefinition
        { get { return m_OpeningDefinition; } }

        /// <summary>
        /// The closing definition of the style. ex. </b> </u>.
        /// </summary>
        public string styleClosingDefinition
        { get { return m_ClosingDefinition; } }


        public uint[] styleOpeningTagArray
        { get { return m_OpeningTagArray; } }


        public uint[] styleClosingTagArray
        { get { return m_ClosingTagArray; } }


        // PRIVATE FIELDS
        [SerializeField]
        private string m_Name;

        [SerializeField]
        private int m_HashCode;

        [SerializeField]
        private string m_OpeningDefinition;

        [SerializeField]
        private string m_ClosingDefinition;

        [SerializeField]
        private uint[] m_OpeningTagArray;

        [SerializeField]
        private uint[] m_ClosingTagArray;

        [SerializeField]
        internal uint[] m_OpeningTagUnicodeArray;

        [SerializeField]
        internal uint[] m_ClosingTagUnicodeArray;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="styleName">Name of the style.</param>
        /// <param name="styleOpeningDefinition">Style opening definition.</param>
        /// <param name="styleClosingDefinition">Style closing definition.</param>
        internal TextStyle(string styleName, string styleOpeningDefinition, string styleClosingDefinition)
        {
            m_Name = styleName;
            m_HashCode = TextUtilities.GetHashCodeCaseInSensitive(styleName);
            m_OpeningDefinition = styleOpeningDefinition;
            m_ClosingDefinition = styleClosingDefinition;

            RefreshStyle();
        }

        /// <summary>
        /// Function to update the content of the int[] resulting from changes to OpeningDefinition & ClosingDefinition.
        /// </summary>
        public void RefreshStyle()
        {
            m_HashCode = TextUtilities.GetHashCodeCaseInSensitive(m_Name);

            int s1 = m_OpeningDefinition.Length;
            m_OpeningTagArray = new uint[s1];
            m_OpeningTagUnicodeArray = new uint[s1];

            for (int i = 0; i < s1; i++)
            {
                m_OpeningTagArray[i] = m_OpeningDefinition[i];
                m_OpeningTagUnicodeArray[i] = m_OpeningDefinition[i];
            }

            int s2 = m_ClosingDefinition.Length;
            m_ClosingTagArray = new uint[s2];
            m_ClosingTagUnicodeArray = new uint[s2];

            for (int i = 0; i < s2; i++)
            {
                m_ClosingTagArray[i] = m_ClosingDefinition[i];
                m_ClosingTagUnicodeArray[i] = m_ClosingDefinition[i];
            }
        }
    }
}
