// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class StyleFieldPopupWindow : EditorWindow
    {
        VisualElement m_Content;
        private float m_LastHeight;

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
                    m_Content.style.flexGrow = 0;
                    m_Content.style.flexShrink = 0;
                    m_Content.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                    ResizeToContent();
                }
            }
        }

        public event Action closed;

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (m_Parent == null)
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
