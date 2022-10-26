// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class EditorGenericDropdownMenu : IGenericMenu
    {
        private GenericDropdownMenu m_GenericDropdownMenu;
        internal GenericDropdownMenu genericDropdownMenu => m_GenericDropdownMenu;

        private EditorGenericDropdownMenuWindowContent m_WindowContent;
        internal EditorGenericDropdownMenuWindowContent windowContent => m_WindowContent;

        private Rect m_DropdownPosition;

        public EditorGenericDropdownMenu()
        {
            m_GenericDropdownMenu = new GenericDropdownMenu()
            {
                isSingleSelectionDropdown = false,
                closeOnParentResize = false
            };

            m_GenericDropdownMenu.menuContainer.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            m_GenericDropdownMenu.menuContainer.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_GenericDropdownMenu.scrollView.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_GenericDropdownMenu.scrollView.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            if (m_WindowContent != null && m_WindowContent.editorWindow != null)
            {
                m_WindowContent.editorWindow.Close();
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (m_WindowContent != null && !float.IsNaN(m_GenericDropdownMenu.scrollView.layout.size.x))
            {
                var scrollHeight = m_GenericDropdownMenu.scrollView.layout.height
                                   + m_GenericDropdownMenu.scrollView.resolvedStyle.borderBottomWidth
                                   + m_GenericDropdownMenu.outerContainer.resolvedStyle.borderTopWidth;
                var dropdownRect = new Rect(0f, 0f, m_DropdownPosition.width, scrollHeight);
                var adjustedDropdownRect = ContainerWindow.FitRectToScreen(dropdownRect, true, true);

                if (dropdownRect.height > adjustedDropdownRect.height)
                {
                    // Dropdown didn't fit on screen. Add some padding
                    adjustedDropdownRect.height -= 10f;
                }

                m_GenericDropdownMenu.outerContainer.style.width = adjustedDropdownRect.width;
                m_GenericDropdownMenu.outerContainer.style.height = adjustedDropdownRect.height;
                m_WindowContent.windowSize = adjustedDropdownRect.size;
            }
        }

        public void AddItem(string itemName, bool isChecked, Action action)
        {
            m_GenericDropdownMenu.AddItem(itemName, isChecked, action);
        }

        public void AddItem(string itemName, bool isChecked, Action<object> action, object data)
        {
            m_GenericDropdownMenu.AddItem(itemName, isChecked, action, data);
        }

        public void AddDisabledItem(string itemName, bool isChecked)
        {
            m_GenericDropdownMenu.AddDisabledItem(itemName, isChecked);
        }

        public void AddSeparator(string path)
        {
            m_GenericDropdownMenu.AddSeparator(path);
        }

        public void DropDown(Rect position, VisualElement targetElement = null, bool anchored = false)
        {
            m_DropdownPosition = position;
            m_WindowContent = new EditorGenericDropdownMenuWindowContent(m_GenericDropdownMenu);
            PopupWindow.Show(position, m_WindowContent);
        }

        internal void UpdateItem(string itemName, bool isChecked)
        {
            m_GenericDropdownMenu.UpdateItem(itemName, isChecked);
        }
    }
}
