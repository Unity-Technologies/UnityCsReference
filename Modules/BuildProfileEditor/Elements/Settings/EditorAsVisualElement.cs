// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements
{
    /// <summary>
    /// Creates a custom inspector for a ScriptableObject, handles
    /// destroying and recreating the editor on panel attach and detach.
    /// </summary>
    class EditorAsVisualElement : VisualElement
    {
        ScriptableObject m_TargetObject;
        Editor m_Editor = null;

        public EditorAsVisualElement(ScriptableObject target)
        {
            m_TargetObject = target;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_Editor = Editor.CreateEditor(m_TargetObject);
            var inspectorGUI = m_Editor.CreateInspectorGUI();
            this.Add(inspectorGUI);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (m_Editor == null)
            {
                return;
            }

            this.Clear();
            UnityEngine.Object.DestroyImmediate(m_Editor);
            m_Editor = null;
        }
    }
}
