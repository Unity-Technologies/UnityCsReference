// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    sealed class LastCustomToolButton : EditorToolbarDropdownToggle
    {
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
            ToolManager.activeToolChanged += UpdateContents;
            EditorToolManager.availableComponentToolsChanged += UpdateContents;
            SceneViewMotion.viewToolActiveChanged += UpdateState;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            SceneViewMotion.viewToolActiveChanged -= UpdateState;
            ToolManager.activeToolChanged -= UpdateContents;
            EditorToolManager.availableComponentToolsChanged -= UpdateContents;
        }

        void ToggleEnabled(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                var last = EditorToolManager.lastCustomTool;

                if (last == null || last is NoneTool)
                    DropDownMenu();
                else
                    ToolManager.SetActiveTool(last);
            }
            else
            {
                ToolManager.RestorePreviousTool();
            }
        }

        void DropDownMenu()
        {
            EditorToolGUI.ShowCustomGlobalToolsContextMenu(worldBound);
        }

        void UpdateState()
        {
            var last = EditorToolManager.lastCustomTool;
            SetValueWithoutNotify(ToolManager.IsActiveTool(last));
        }

        void UpdateContents()
        {
            var last = EditorToolManager.lastCustomTool;
            var content = EditorToolUtility.GetToolbarIcon(last);
            icon = content.image as Texture2D;
            tooltip = content.tooltip;
            SetEnabled(EditorToolUtility.GetNonBuiltinToolCount() > 0);
            UpdateState();
        }
    }
}
