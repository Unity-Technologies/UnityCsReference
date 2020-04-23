using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [Serializable]
    internal class StyleProperty
    {
        [SerializeField]
        string m_Name;

        public string name
        {
            get
            {
                return m_Name;
            }
            internal set
            {
                m_Name = value;
            }
        }

        [SerializeField]
        int m_Line;

        public int line
        {
            get
            {
                return m_Line;
            }
            internal set
            {
                m_Line = value;
            }
        }

        [SerializeField]
        StyleValueHandle[] m_Values;

        public StyleValueHandle[] values
        {
            get
            {
                return m_Values;
            }
            internal set
            {
                m_Values = value;
            }
        }

        [NonSerialized]
        internal bool isCustomProperty;

        [NonSerialized]
        internal bool requireVariableResolve;
    }
}
