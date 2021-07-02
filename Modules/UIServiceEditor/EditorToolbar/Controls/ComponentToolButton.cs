// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    sealed class ComponentToolButton<T> : EditorToolbarToggle where T : EditorTool
    {
        T m_Tool;

        public ComponentToolButton(T tool)
        {
            m_Tool = tool;
            GUIContent content = EditorToolUtility.GetToolbarIcon(m_Tool);
            tooltip = content.tooltip;
            icon = content.image as Texture2D;

            this.RegisterValueChangedCallback(evt => { SetToolActive(evt.newValue); });
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            UpdateState();
        }

        void UpdateState()
        {
            SetValueWithoutNotify(!Tools.viewToolActive && ToolManager.IsActiveTool(m_Tool));
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            ToolManager.activeToolChanged += UpdateState;
            SceneViewMotion.viewToolActiveChanged += UpdateState;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            ToolManager.activeToolChanged -= UpdateState;
            SceneViewMotion.viewToolActiveChanged -= UpdateState;
        }

        void SetToolActive(bool active)
        {
            if (active)
                ToolManager.SetActiveTool(m_Tool);
            else
                ToolManager.RestorePreviousTool();
        }
    }
}
