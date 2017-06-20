// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CustomGridBrushAttribute : Attribute
    {
        private bool m_HideAssetInstances;
        private bool m_HideDefaultInstance;
        private bool m_DefaultBrush;
        private string m_DefaultName;

        public bool hideAssetInstances
        {
            get { return m_HideAssetInstances; }
        }

        public bool hideDefaultInstance
        {
            get { return m_HideDefaultInstance; }
        }

        public bool defaultBrush
        {
            get { return m_DefaultBrush; }
        }

        public string defaultName
        {
            get { return m_DefaultName; }
        }

        public CustomGridBrushAttribute()
        {
            m_HideAssetInstances = false;
            m_HideDefaultInstance = false;
            m_DefaultBrush = false;
            m_DefaultName = "";
        }

        public CustomGridBrushAttribute(bool hideAssetInstances, bool hideDefaultInstance, bool defaultBrush, string defaultName)
        {
            this.m_HideAssetInstances = hideAssetInstances;
            this.m_HideDefaultInstance = hideDefaultInstance;
            this.m_DefaultBrush = defaultBrush;
            this.m_DefaultName = defaultName;
        }
    }
}
