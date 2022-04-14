// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    sealed class EditorToolContextButton<T> : EditorToolbarToggle where T : EditorToolContext
    {
        T m_Context;

        public EditorToolContextButton(T ctx)
        {
            m_Context = ctx;
            GUIContent content = EditorToolUtility.GetToolbarIcon(m_Context);
            tooltip = content.tooltip;
            icon = content.image as Texture2D;

            this.RegisterValueChangedCallback(evt =>
            {
                EditorToolManager.activeToolContext = evt.newValue ? m_Context : null;
            });

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            UpdateState();
        }

        void UpdateState()
        {
            SetValueWithoutNotify(ToolManager.IsActiveContext(m_Context));
        }

        void OnAttachedToPanel(AttachToPanelEvent evt) => ToolManager.activeContextChanged += UpdateState;

        void OnDetachFromPanel(DetachFromPanelEvent evt) => ToolManager.activeContextChanged -= UpdateState;
    }
}
