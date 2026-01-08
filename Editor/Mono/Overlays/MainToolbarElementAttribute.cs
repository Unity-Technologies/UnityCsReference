// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Toolbars
{
    [AttributeUsage(AttributeTargets.Method)]
    sealed class UnityOnlyMainToolbarPresetAttribute : Attribute { }

    public enum MainToolbarDockPosition
    {
        Left,
        Right,
        Middle,
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MainToolbarElementAttribute : Attribute
    {
        // id path
        // display name
        // default left, middle, right dock zone
        // default dock index/order
        // display by default
        public const int defaultMenuPriority = 1000;
        
        string m_Path;
        int m_DefaultDockIndex = 100;
        MainToolbarDockPosition m_DefaultDropZone;
        int m_MenuPriority = defaultMenuPriority;

        public string path
        {
            get => m_Path;
            set => m_Path = value;
        }

        public int defaultDockIndex
        {
            get => m_DefaultDockIndex;
            set => m_DefaultDockIndex = value;
        }

        public MainToolbarDockPosition defaultDockPosition
        {
            get => m_DefaultDropZone;
            set => m_DefaultDropZone = value;
        }
        
        public int menuPriority
        {
            get => m_MenuPriority;
            set => m_MenuPriority = value;
        }

        internal string displayName
        {
            get => m_Path.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];
        }

        string m_UssNameOverride;
        public string ussName
        {
            get => m_UssNameOverride == null ? m_Path.Replace(" ", "") : m_UssNameOverride;
            set => m_UssNameOverride = value;
        }

        public MainToolbarElementAttribute(string path)
        {
            this.path = path;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    sealed class MainToolbarElementAvailabilityAttribute : Attribute
    {
        string m_Path;

        public string path
        {
            get => m_Path;
            set => m_Path = value;
        }

        public MainToolbarElementAvailabilityAttribute(string path)
        {
            this.path = path;
        }
    }
}
