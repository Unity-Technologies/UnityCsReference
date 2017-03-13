// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class WebTemplate
    {
        public string m_Path, m_Name;
        public Texture2D m_Thumbnail;
        public string[] m_CustomKeys;

        public string[] CustomKeys
        {
            get
            {
                return m_CustomKeys;
            }
        }

        public override bool Equals(System.Object other)
        {
            return other is WebTemplate && other.ToString().Equals(ToString());
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ m_Path.GetHashCode();
        }

        public override string ToString()
        {
            return m_Path;
        }

        public GUIContent ToGUIContent(Texture2D defaultIcon)
        {
            return new GUIContent(m_Name, m_Thumbnail == null ? defaultIcon : m_Thumbnail);
        }
    }
}
