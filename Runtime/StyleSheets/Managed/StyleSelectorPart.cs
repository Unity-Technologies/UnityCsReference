// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.StyleSheets
{
    [Serializable]
    internal struct StyleSelectorPart
    {
        [SerializeField]
        string m_Value;

        public string value
        {
            get
            {
                return m_Value;
            }
            internal set
            {
                m_Value = value;
            }
        }

        [SerializeField]
        StyleSelectorType m_Type;

        public StyleSelectorType type
        {
            get
            {
                return m_Type;
            }
            internal set
            {
                m_Type = value;
            }
        }

        public override string ToString()
        {
            return string.Format("[StyleSelectorPart: value={0}, type={1}]", value, type);
        }
    }
}
