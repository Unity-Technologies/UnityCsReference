// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    class ToolButton : EditorToolbarToggle
    {
        readonly Tool m_TargetTool;

        readonly Texture2D m_PanViewIcon;
        readonly Texture2D m_PanViewOnIcon;
        readonly Texture2D m_OrbitViewIcon;
        readonly Texture2D m_OrbitViewOnIcon;
        readonly Texture2D m_FpsViewIcon;
        readonly Texture2D m_FpsViewOnIcon;
        readonly Texture2D m_ZoomViewIcon;
        readonly Texture2D m_ZoomViewOnIcon;

        public Action displayChanged;

        public ToolButton(Tool targetTool)
        {
            m_TargetTool = targetTool;

            name = targetTool + "Tool";
            tooltip = L10n.Tr(targetTool + " Tool");
            this.RegisterValueChangedCallback((evt) =>
            {
                if (evt.newValue)
                    Tools.current = m_TargetTool;

                // Keep the toggle checked if target is still the current tool
                if (m_TargetTool == Tools.current)
                    SetValueWithoutNotify(true);
            });

            switch (targetTool)
            {
                case Tool.View:
                    m_PanViewIcon = EditorGUIUtility.LoadIconRequired("ViewToolMove");
                    m_PanViewOnIcon = EditorGUIUtility.LoadIconRequired("ViewToolMove On");
                    m_FpsViewIcon = m_OrbitViewIcon = EditorGUIUtility.LoadIconRequired("ViewToolOrbit");
                    m_FpsViewOnIcon = m_OrbitViewOnIcon = EditorGUIUtility.LoadIconRequired("ViewToolOrbit On");
                    m_ZoomViewIcon = EditorGUIUtility.LoadIconRequired("ViewToolZoom");
                    m_ZoomViewOnIcon = EditorGUIUtility.LoadIconRequired("ViewToolZoom On");
                    break;
                case Tool.Move:
                    onIcon = EditorGUIUtility.LoadIconRequired("MoveTool On");
                    offIcon = EditorGUIUtility.LoadIconRequired("MoveTool");
                    break;
                case Tool.Rotate:
                    onIcon = EditorGUIUtility.LoadIconRequired("RotateTool On");
                    offIcon = EditorGUIUtility.LoadIconRequired("RotateTool");
                    break;
                case Tool.Scale:
                    onIcon = EditorGUIUtility.LoadIconRequired("ScaleTool On");
                    offIcon = EditorGUIUtility.LoadIconRequired("ScaleTool");
                    break;
                case Tool.Transform:
                    onIcon = EditorGUIUtility.LoadIconRequired("TransformTool On");
                    offIcon = EditorGUIUtility.LoadIconRequired("TransformTool");
                    break;
                case Tool.Rect:
                    onIcon = EditorGUIUtility.FindTexture("RectTool On");
                    offIcon = EditorGUIUtility.FindTexture("RectTool");
                    break;
            }

            UpdateState();
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            ToolManager.activeToolChanged += UpdateState;
            ToolManager.activeContextChanged += UpdateState;
            SceneViewMotion.viewToolActiveChanged += UpdateState;

            if (m_TargetTool == Tool.View)
            {
                Tools.viewToolChanged += UpdateViewToolContent;
                UpdateViewToolContent();
            }
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            ToolManager.activeContextChanged -= UpdateState;
            ToolManager.activeToolChanged -= UpdateState;
            SceneViewMotion.viewToolActiveChanged -= UpdateState;

            if (m_TargetTool == Tool.View)
                Tools.viewToolChanged -= UpdateViewToolContent;
        }

        void UpdateViewToolContent()
        {
            switch (Tools.viewTool)
            {
                case ViewTool.Orbit:
                    onIcon = m_OrbitViewOnIcon;
                    offIcon = m_OrbitViewIcon;
                    break;
                case ViewTool.Pan:
                    onIcon = m_PanViewOnIcon;
                    offIcon = m_PanViewIcon;
                    break;
                case ViewTool.Zoom:
                    onIcon = m_ZoomViewOnIcon;
                    offIcon = m_ZoomViewIcon;
                    break;
                case ViewTool.FPS:
                    onIcon = m_FpsViewOnIcon;
                    offIcon = m_FpsViewIcon;
                    break;
            }
        }

        void UpdateState()
        {
            SetValueWithoutNotify(IsActiveTool());

            var missing = EditorToolUtility.GetEditorToolWithEnum(m_TargetTool) is NoneTool;
            var display = missing ? DisplayStyle.None : DisplayStyle.Flex;

            if (style.display != display)
            {
                style.display = display;
                displayChanged?.Invoke();
            }
        }

        bool IsActiveTool()
        {
            if (Tools.viewToolActive)
                return m_TargetTool == Tool.View;
            return Tools.current == m_TargetTool;
        }
    }
}
