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
                var last = EditorToolManager.GetLastCustomTool();

                if (last == null || last is NoneTool)
                    DropDownMenu();
                else
                    ToolManager.SetActiveTool(last);
            }
            else
            {
                ToolManager.RestorePreviousPersistentTool();
            }
        }

        void DropDownMenu()
        {
            EditorToolGUI.DropDownComponentToolsContextMenu(worldBound);
        }

        void UpdateState()
        {
            SetValueWithoutNotify(Tools.current == Tool.Custom);
        }

        void UpdateContents()
        {
            var last = EditorToolManager.GetLastCustomTool();
            var content = EditorToolUtility.GetToolbarIcon(last);
            icon = content.image as Texture2D;
            tooltip = content.tooltip;
            SetEnabled(EditorToolUtility.GetNonBuiltinToolCount() > 0);

            UpdateState();
        }
    }
}
