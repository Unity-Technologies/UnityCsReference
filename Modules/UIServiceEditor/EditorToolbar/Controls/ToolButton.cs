// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    class ToolButton : ToolbarButton
    {
        readonly Tool m_TargetTool;
        protected readonly VisualElement iconElement;
        const string k_OnClassName = EditorToolbar.elementIconClassName + "-on";
        const string k_OffClassName = EditorToolbar.elementIconClassName + "-off";

        public ToolButton(Tool targetTool)
        {
            iconElement = EditorToolbarUtility.AddIconElement(this);
            m_TargetTool = targetTool;

            name = targetTool + "Tool";
            tooltip = L10n.Tr(targetTool + " Tool");
            clicked += SetToolActive;


            UpdateState();
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        protected virtual void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            ToolManager.activeToolChanged += OnToolChanged;
            SceneViewMotion.viewToolActiveChanged += OnViewToolActiveChanged;

            if (m_TargetTool == Tool.View)
            {
                Tools.viewToolChanged += UpdateViewToolContent;
                UpdateViewToolContent();
            }
        }

        protected virtual void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            ToolManager.activeToolChanged -= OnToolChanged;
            SceneViewMotion.viewToolActiveChanged -= OnViewToolActiveChanged;

            if (m_TargetTool == Tool.View)
                Tools.viewToolChanged -= UpdateViewToolContent;
        }

        void OnViewToolActiveChanged()
        {
            UpdateState();
        }

        void UpdateViewToolContent()
        {
            name = $"ViewTool_{Tools.viewTool}";
        }

        protected virtual void OnToolChanged()
        {
            UpdateState();
        }

        protected void UpdateState()
        {
            bool activeTool = Tools.viewToolActive
                ? m_TargetTool == Tool.View
                : IsActiveTool();

            if (activeTool)
                pseudoStates |= PseudoStates.Checked;
            else
                pseudoStates &= ~PseudoStates.Checked;

            iconElement.EnableInClassList(k_OffClassName, !activeTool);
            iconElement.EnableInClassList(k_OnClassName, activeTool);
        }

        protected virtual bool IsActiveTool()
        {
            return Tools.current == m_TargetTool;
        }

        protected virtual void SetToolActive()
        {
            Tools.current = m_TargetTool;
        }
    }
}
