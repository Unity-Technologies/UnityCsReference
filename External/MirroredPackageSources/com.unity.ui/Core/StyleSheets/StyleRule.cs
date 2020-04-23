using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [Serializable]
    internal class StyleRule
    {
        [SerializeField]
        StyleProperty[] m_Properties;

        [SerializeField]
        internal int line;

        public StyleProperty[] properties
        {
            get
            {
                return m_Properties;
            }
            internal set
            {
                m_Properties = value;
            }
        }

        [NonSerialized]
        internal int customPropertiesCount;
    }
}
