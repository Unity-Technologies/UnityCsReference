// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Tool Settings/Pivot Mode")]
    sealed class PivotModeDropdown : ToolbarButton
    {
        const string k_DropdownIconClass = "unity-toolbar-dropdown-label-icon";
        const string k_ToolSettingsClass = "unity-tool-settings";
        readonly TextElement m_Label;
        readonly VisualElement m_Icon;
        readonly GUIContent m_Center;
        readonly GUIContent m_Pivot;

        public PivotModeDropdown()
        {
            name = "Pivot Mode";
            AddToClassList(k_ToolSettingsClass);
            m_Icon = EditorToolbarUtility.AddIconElement(this);
            m_Icon.AddToClassList(k_DropdownIconClass);
            m_Label = EditorToolbarUtility.AddTextElement(this);
            m_Label.style.flexGrow = 1;
            EditorToolbarUtility.AddArrowElement(this);

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
            m_Label.text = content.text;
            tooltip = content.tooltip;
            m_Icon.name = Tools.pivotMode == PivotMode.Center ? "CenterIcon" : "PivotIcon";
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
    sealed class PivotRotationDropdown : ToolbarButton
    {
        const string k_DropdownIconClass = "unity-toolbar-dropdown-label-icon";
        const string k_ToolSettingsClass = "unity-tool-settings";

        readonly TextElement m_Label;
        readonly VisualElement m_Icon;
        readonly GUIContent m_Local;
        readonly GUIContent m_Global;

        public PivotRotationDropdown()
        {
            name = "Pivot Rotation";
            AddToClassList(k_ToolSettingsClass);
            m_Icon = EditorToolbarUtility.AddIconElement(this);
            m_Icon.AddToClassList(k_DropdownIconClass);
            m_Label = EditorToolbarUtility.AddTextElement(this);
            m_Label.style.flexGrow = 1;
            EditorToolbarUtility.AddArrowElement(this);

            m_Local = EditorGUIUtility.TrTextContent("Local",
                "Toggle Tool Handle Rotation\n\nTool handles are in the active object's rotation.");
            m_Global = EditorGUIUtility.TrTextContent("Global",
                "Toggle Tool Handle Rotation\n\nTool handles are in global rotation.");

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
            m_Label.text = content.text;
            tooltip = content.tooltip;
            m_Icon.name = Tools.pivotRotation == PivotRotation.Global ? "GlobalIcon" : "LocalIcon";
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
