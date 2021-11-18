// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    sealed class LastCustomToolButton : EditorToolbarDropdownToggle
    {
        EditorTool m_LastGlobalTool;

        public LastCustomToolButton()
        {
            dropdownClicked += DropDownMenu;
            this.RegisterValueChangedCallback(ToggleEnabled);
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            UpdateContents();
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            EditorToolManager.availableComponentToolsChanged += UpdateContents;
            ToolManager.activeToolChanged += UpdateContents;
            SceneViewMotion.viewToolActiveChanged += UpdateState;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            ToolManager.activeToolChanged -= UpdateContents;
            SceneViewMotion.viewToolActiveChanged -= UpdateState;
            EditorToolManager.availableComponentToolsChanged -= UpdateContents;
        }

        void ToggleEnabled(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                if(m_LastGlobalTool == null || m_LastGlobalTool is NoneTool)
                {
                    DropDownMenu();
                    SetValueWithoutNotify(false);
                }
                else
                    ToolManager.SetActiveTool(m_LastGlobalTool);
            }
            else
            {
                ToolManager.RestorePreviousPersistentTool();
            }
        }

        void DropDownMenu()
        {
            EditorToolGUI.ShowCustomGlobalToolsContextMenu(worldBound);
        }

        void UpdateState()
        {
            SetValueWithoutNotify(EditorToolUtility.IsGlobalTool(EditorToolManager.activeTool));
        }

        void UpdateContents()
        {
            var tool = EditorToolManager.activeTool;
            if(EditorToolUtility.IsGlobalTool(tool))
                m_LastGlobalTool = tool;

            var content = EditorToolUtility.GetToolbarIcon(m_LastGlobalTool);
            icon = content.image as Texture2D;
            tooltip = content.tooltip;
            style.display = EditorToolUtility.GetNonBuiltinToolCount() > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            UpdateState();
        }
    }
}
