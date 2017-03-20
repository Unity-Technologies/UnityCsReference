// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.StyleSheets
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
    }
}
