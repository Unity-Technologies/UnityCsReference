// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    [MovedFrom("Unity.UI.Builder")]
    internal class StyleFieldPopupWindow : EditorWindow
    {

        VisualElement m_Content;
        private float m_LastHeight;
        bool m_Resizable;

        // When true, the popup grows/shrinks to fit its content and forces the OS window's
        // min/max size to that content height. When false, content flex-grows to fill the
        // window and the auto-resize is a no-op — leave sizing to the caller (min/max on
        // ShowAsDropDown), so the user can drag the corner freely.
        public bool resizable
        {
            get => m_Resizable;
            set
            {
                m_Resizable = value;
                if (m_Content != null)
                    m_Content.style.flexGrow = m_Resizable ? 1 : 0;
            }
        }

        public VisualElement content
        {
            get => m_Content;
            set
            {
                if (m_Content == value)
                    return;

                if (m_Content != null)
                {
                    m_Content.RemoveFromHierarchy();
                    m_Content.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                }
                m_Content = value;
                m_LastHeight = 0;
                if (m_Content != null)
                {
                    rootVisualElement.Add(m_Content);
                    m_Content.style.position = Position.Relative;
                    m_Content.style.flexGrow = m_Resizable ? 1 : 0;
                    m_Content.style.flexShrink = 0;
                    m_Content.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                    if (!m_Resizable)
                        ResizeToContent();
                }
            }
        }

        public event Action closed;

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (m_Parent == null || m_Resizable)
                return;
            ResizeToContent();
        }

        public void ResizeToContent()
        {
            if (m_Parent == null || m_Parent.window == null || float.IsNaN(content.layout.width) || float.IsNaN(content.layout.height))
                return;

            if (Mathf.Approximately(m_LastHeight, content.layout.height))
                return;

            m_LastHeight = content.layout.height;

            rootVisualElement.schedule.Execute(() =>
            {
                var pos = m_Parent.window.position;

                pos.height = content.layout.height;

                // ShowAsDropDown pins minSize/maxSize to the initial size; unpin the height
                // so the window can follow the content.
                minSize = new Vector2(minSize.x, pos.height);
                maxSize = new Vector2(maxSize.x, pos.height);
                position = pos;
            });
        }

        private void OnDisable()
        {
            content = null;
            closed?.Invoke();
        }
    }
}
