// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.StyleSheets
{
    [Serializable]
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
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
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal int valueIndex;

        internal StyleValueHandle(int valueIndex, StyleValueType valueType)
        {
            this.valueIndex = valueIndex;
            m_ValueType = valueType;
        }
    }
}
