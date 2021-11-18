// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Tool Settings/Pivot Mode")]
    sealed class PivotModeDropdown : EditorToolbarDropdown
    {
        readonly GUIContent m_Center;
        readonly GUIContent m_Pivot;

        public PivotModeDropdown()
        {
            name = "Pivot Mode";

            m_Center = EditorGUIUtility.TrTextContentWithIcon("Center",
                "Toggle Tool Handle Position\n\nThe tool handle is placed at the center of the selection.",
                "ToolHandleCenter");
            m_Pivot = EditorGUIUtility.TrTextContentWithIcon("Pivot",
                "Toggle Tool Handle Position\n\nThe tool handle is placed at the active object's pivot point.",
                "ToolHandlePivot");

            RegisterCallback<AttachToPanelEvent>(AttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(DetachedFromPanel);

            clicked += OpenContextMenu;

            PivotModeChanged();
        }

        void OpenContextMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(m_Center, Tools.pivotMode == PivotMode.Center, () => Tools.pivotMode = PivotMode.Center);
            menu.AddItem(m_Pivot, Tools.pivotMode == PivotMode.Pivot, () => Tools.pivotMode = PivotMode.Pivot);
            menu.DropDown(worldBound);
        }

        void PivotModeChanged()
        {
            var content = Tools.pivotMode == PivotMode.Center ? m_Center : m_Pivot;
            text = content.text;
            tooltip = content.tooltip;
            icon = content.image as Texture2D;

            //Ensuring constant size of the text area
            var textElement = this.Q<TextElement>(className: EditorToolbar.elementLabelClassName);
            textElement.style.width = 40;
        }

        void AttachedToPanel(AttachToPanelEvent evt)
        {
            Tools.pivotModeChanged += PivotModeChanged;
        }

        void DetachedFromPanel(DetachFromPanelEvent evt)
        {
            Tools.pivotModeChanged -= PivotModeChanged;
        }
    }

    [EditorToolbarElement("Tool Settings/Pivot Rotation")]
    sealed class PivotRotationDropdown : EditorToolbarDropdown
    {
        readonly GUIContent m_Local;
        readonly GUIContent m_Global;

        public PivotRotationDropdown()
        {
            name = "Pivot Rotation";

            m_Local = EditorGUIUtility.TrTextContent("Local",
                "Toggle Tool Handle Rotation\n\nTool handles are in the active object's rotation.",
                "ToolHandleLocal");
            m_Global = EditorGUIUtility.TrTextContent("Global",
                "Toggle Tool Handle Rotation\n\nTool handles are in global rotation.",
                "ToolHandleGlobal");

            RegisterCallback<AttachToPanelEvent>(AttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(DetachedFromPanel);

            clicked += OpenContextMenu;

            PivotRotationChanged();
        }

        void OpenContextMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(m_Global, Tools.pivotRotation == PivotRotation.Global, () => Tools.pivotRotation = PivotRotation.Global);
            menu.AddItem(m_Local, Tools.pivotRotation == PivotRotation.Local, () => Tools.pivotRotation = PivotRotation.Local);
            menu.DropDown(worldBound);
        }

        void PivotRotationChanged()
        {
            var content = Tools.pivotRotation == PivotRotation.Global ? m_Global : m_Local;
            text = content.text;
            tooltip = content.tooltip;
            icon = content.image as Texture2D;

            //Ensuring constant size of the text area
            var textElement = this.Q<TextElement>(className: EditorToolbar.elementLabelClassName);
            textElement.style.width = 40;
        }

        void AttachedToPanel(AttachToPanelEvent evt)
        {
            Tools.pivotRotationChanged += PivotRotationChanged;
        }

        void DetachedFromPanel(DetachFromPanelEvent evt)
        {
            Tools.pivotRotationChanged -= PivotRotationChanged;
        }
    }
}
