// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Object = UnityEngine.Object;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Rendering;

namespace UnityEditor
{
    public abstract class LightingWindowTab : LightingWindow.WindowTab
    {
        internal LightingWindow m_Parent;
        GUIContent m_TitleContent;
        int m_Priority = -1;

        public GUIContent titleContent
        {
            get => m_TitleContent ?? (m_TitleContent = new GUIContent());
            set {
                m_TitleContent = value;
                if (m_Parent != null)
                    m_Parent.SetToolbarDirty();
            }
        }

        public int priority
        {
            get => m_Priority;
            set {
                m_Priority = value;
                if (m_Parent != null)
                    m_Parent.SetToolbarDirty();
            }
        }

        public virtual void OnEnable() { }
        public virtual void OnDisable() { }
        public virtual void OnGUI() { }
        public virtual void OnHeaderSettingsGUI() { }
        public virtual void OnBakeButtonGUI() { }
        public virtual void OnSelectionChange() { }
        public virtual bool HasHelpGUI() { return false; }

        public virtual void OnSummaryGUI()
        {
            LightingWindow.Summary();
        }

        public void FocusTab()
        {
            LightingWindow.FocusTab(this);
        }
    }
}
