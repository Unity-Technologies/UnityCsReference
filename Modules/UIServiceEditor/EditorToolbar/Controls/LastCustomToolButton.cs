// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    sealed class LastCustomToolButton : ToolButton
    {
        public LastCustomToolButton() : base(Tool.Custom)
        {
            UpdateContent();
            var rightClickable = new Clickable(OpenToolSelector);
            rightClickable.activators.Clear();
            rightClickable.activators.Add(new ManipulatorActivationFilter {button = MouseButton.RightMouse});
            this.AddManipulator(rightClickable);
        }

        protected override void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            base.OnAttachedToPanel(evt);
            Selection.selectionChanged += OnSelectionChanged;
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            base.OnDetachFromPanel(evt);
            Selection.selectionChanged -= OnSelectionChanged;
        }

        void OpenToolSelector()
        {
            EditorToolGUI.DoEditorToolMenu();
        }

        void UpdateContent()
        {
            var tool = EditorToolManager.GetLastCustomTool();
            GUIContent content;

            if (tool == null || (content = tool.toolbarIcon) == null)
                content = EditorToolUtility.GetIcon(ToolManager.activeToolType);

            iconElement.style.backgroundImage = new StyleBackground(content.image as Texture2D);
            tooltip = tool != null ? content.tooltip : "Editor Tools";

            SetEnabled(EditorToolUtility.GetNonBuiltinToolCount() > 0);
        }

        protected override void SetToolActive()
        {
            //If no last custom exists, the value will be none
            if (EditorToolUtility.GetEditorToolWithEnum(Tool.Custom) == null)
            {
                OpenToolSelector();
            }
            else
            {
                base.SetToolActive();
            }
        }

        protected override void OnToolChanged()
        {
            base.OnToolChanged();

            UpdateContent();
        }

        void OnSelectionChanged()
        {
            UpdateContent();
        }
    }
}
