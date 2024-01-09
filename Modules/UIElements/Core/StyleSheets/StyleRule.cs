// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class StyleRule
    {
        [SerializeField]
        StyleProperty[] m_Properties;

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [SerializeField]
        internal int line;

        public StyleProperty[] properties
        {
            get
            {
                return m_Properties;
            }
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            internal set
            {
                m_Properties = value;
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [NonSerialized]
        internal int customPropertiesCount;
    }
}
