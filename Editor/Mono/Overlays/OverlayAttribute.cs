// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Overlays
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class OverlayAttribute : Attribute
    {
        readonly Type m_EditorWindowType;
        readonly string m_Id;
        readonly string m_UssName;
        readonly bool m_DefaultDisplay;
        readonly string m_DisplayName;

        public Type editorWindowType => m_EditorWindowType;
        public string id => m_Id;
        public string displayName => m_DisplayName;
        public string ussName => m_UssName;
        public bool defaultDisplay => m_DefaultDisplay;

        public OverlayAttribute(Type editorWindowType, string id, string displayName, string ussName, bool defaultDisplay = false)
        {
            m_EditorWindowType = editorWindowType;
            m_DefaultDisplay = defaultDisplay;
            m_Id = id;
            m_DisplayName = displayName;
            m_UssName = ussName;
            if (string.IsNullOrEmpty(m_UssName)) m_UssName = m_Id;
        }

        public OverlayAttribute(Type editorWindowType, string id, string displayName, bool defaultLayout = false)
            : this(editorWindowType, id, displayName, displayName, defaultLayout)
        {
        }

        public OverlayAttribute(Type editorWindowType, string displayName, bool defaultLayout = false)
            : this(editorWindowType, displayName, displayName, displayName, defaultLayout)
        {
        }
    }
}
