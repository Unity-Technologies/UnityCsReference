// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Overlays
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class OverlayAttribute : Attribute
    {
        Type m_EditorWindowType;
        string m_Id;
        string m_UssName;
        bool m_DefaultDisplay;
        string m_DisplayName;
        DockZone m_DefaultDockZone;
        DockPosition m_DefaultDockPosition;
        int m_DefaultDockIndex;
        Layout m_DefaultLayout;
        float m_DefaultWidth;
        float m_DefaultHeight;

        public Type editorWindowType
        {
            get => m_EditorWindowType;
            set => m_EditorWindowType = value;
        }

        public string id
        {
            get => m_Id;
            set => m_Id = value;
        }

        public string displayName
        {
            get => m_DisplayName;
            set => m_DisplayName = value;
        }

        public string ussName
        {
            get => m_UssName;
            set => m_UssName = value;
        }

        public bool defaultDisplay
        {
            get => m_DefaultDisplay;
            set => m_DefaultDisplay = value;
        }

        public DockZone defaultDockZone
        {
            get => m_DefaultDockZone;
            set => m_DefaultDockZone = value;
        }

        public DockPosition defaultDockPosition
        {
            get => m_DefaultDockPosition;
            set => m_DefaultDockPosition = value;
        }

        public int defaultDockIndex
        {
            get => m_DefaultDockIndex;
            set => m_DefaultDockIndex = value;
        }

        public Layout defaultLayout
        {
            get => m_DefaultLayout;
            set => m_DefaultLayout = value;
        }

        public float defaultWidth
        {
            get => m_DefaultWidth;
            set => m_DefaultWidth = value;
        }

        public float defaultHeight
        {
            get => m_DefaultHeight;
            set => m_DefaultHeight = value;
        }

        public OverlayAttribute()
        {
            m_EditorWindowType = null;
            m_DefaultDisplay = true;
            m_Id = null;
            m_DisplayName = null;
            m_UssName = null;
            m_DefaultDockZone = DockZone.RightColumn;
            m_DefaultDockPosition = DockPosition.Bottom;
            m_DefaultDockIndex = int.MaxValue;
            m_DefaultLayout = Layout.Panel;
            m_DefaultWidth = float.NegativeInfinity;
            m_DefaultHeight = float.NegativeInfinity;
            if (string.IsNullOrEmpty(m_UssName)) m_UssName = m_Id;
        }

        public OverlayAttribute(Type editorWindowType, string id, string displayName, string ussName, bool defaultDisplay = false):this()
        {
            this.editorWindowType = editorWindowType;
            this.displayName = displayName;
            this.id = id;
            this.defaultDisplay = defaultDisplay;
            this.ussName = ussName;
        }

        public OverlayAttribute(Type editorWindowType, string id, string displayName, bool defaultDisplay = false)
            : this(editorWindowType, id, displayName, displayName, defaultDisplay)
        {
        }

        public OverlayAttribute(Type editorWindowType, string displayName, bool defaultDisplay = false)
            : this(editorWindowType, displayName, displayName, displayName, defaultDisplay)
        {
        }
    }
}
