// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: SceneTooling not yet converted
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
            #pragma warning disable UAL0015 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
            GUIContent content = EditorToolUtility.GetToolbarIcon(m_Context);
            #pragma warning restore UAL0015
            tooltip = content.tooltip;
            icon = content.image as Texture2D;

            this.RegisterValueChangedCallback(evt =>
            {
                #pragma warning disable UAL0015 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
                EditorToolManager.activeToolContext = evt.newValue ? m_Context : null;
                #pragma warning restore UAL0015
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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
