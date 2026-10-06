// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// This class represents a popup window. It is designed to host a single VisualElement as its content, and it will
/// automatically resize to fit the content's height. The content can be set through the Content property, and the
/// window will adjust its size whenever the content's geometry changes. The window also provides a Closed event that
/// is invoked when the window is closed.
/// </summary>
class PopupWindow : EditorWindow
{
    VisualElement m_Content;
    private float m_LastHeight;

    /// <summary>
    /// The content of the popup window. The window will resize to fit the content.
    /// </summary>
    public VisualElement Content
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

    /// <summary>
    /// This event is invoked when the popup window is closed. It can be used to perform any necessary cleanup or
    /// actions when the window is closed.
    /// </summary>
    public event Action Closed;

    void OnDisable()
    {
        Content = null;
        Closed?.Invoke();
    }

    void OnGeometryChanged(GeometryChangedEvent evt)
    {
        if (m_Parent == null)
            return;
        ResizeToContent();
    }

    void ResizeToContent()
    {
        if (m_Parent == null || m_Parent.window == null || float.IsNaN(Content.layout.width) ||
            float.IsNaN(Content.layout.height))
            return;

        if (Mathf.Approximately(m_LastHeight, Content.layout.height))
            return;

        m_LastHeight = Content.layout.height;

        rootVisualElement.schedule.Execute(() =>
        {
            var pos = m_Parent.window.position;

            pos.height = Content.layout.height;
            position = pos;
        });
    }
}
