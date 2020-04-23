using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [Serializable]
    internal struct StyleValueHandle
    {
        [SerializeField]
        StyleValueType m_ValueType;

        public StyleValueType valueType
        {
            get
            {
                return m_ValueType;
            }
            internal set
            {
                m_ValueType = value;
            }
        }

        // Which index to read from in the value array for the corresponding type
        [SerializeField]
        internal int valueIndex;

        internal StyleValueHandle(int valueIndex, StyleValueType valueType)
        {
            this.valueIndex = valueIndex;
            m_ValueType = valueType;
        }
    }
}
