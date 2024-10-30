// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            internal set
            {
                m_Values = value;
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [NonSerialized]
        internal bool isCustomProperty;

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [NonSerialized]
        internal bool requireVariableResolve;
    }
}
