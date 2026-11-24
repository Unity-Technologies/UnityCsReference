// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    class EditorToolbarHeader : VisualElement
    {
        const string m_IconClassName = "unity-editor-toolbar-header";
        const string m_GroupArrowClassName = m_IconClassName + "__arrow-icon";
        const string m_GroupArrowCollapsedClassName = m_GroupArrowClassName + "-collapsed";
        const string m_TextIconContainerClassName = m_IconClassName + "__text-icon-container";
        
        Image m_GroupArrowElement;
        VisualElement m_ToolbarContentContainer;
        
        
        bool m_Collapsed;
        public bool collapsed
        {
            set
            {
                m_Collapsed = value;
                if (m_Collapsed)
                    m_GroupArrowElement.AddToClassList(m_GroupArrowCollapsedClassName);
                else
                    m_GroupArrowElement.RemoveFromClassList(m_GroupArrowCollapsedClassName);
            }
            get => m_Collapsed;
        }
        
        
        public event Action<VisualElement> clicked;
        
        public EditorToolbarHeader(EditorToolbarIcon icon)
        {
            AddToClassList(m_IconClassName);
            
            m_GroupArrowElement = new Image();
            m_GroupArrowElement.AddToClassList(m_GroupArrowClassName);
            Add(m_GroupArrowElement);
            
            m_ToolbarContentContainer = new VisualElement();
            m_ToolbarContentContainer.AddToClassList(m_TextIconContainerClassName);
            Add(m_ToolbarContentContainer);
            
            _ = new EditorToolbarContent(m_ToolbarContentContainer, icon);
            
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
        }
        
        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            RegisterCallback<ClickEvent>(OnClicked);
        }

        void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<ClickEvent>(OnClicked);
        }

        void OnClicked(ClickEvent evt)
        {
            clicked?.Invoke(this);
        }
    }
}
