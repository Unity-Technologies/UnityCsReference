// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class PreviewRootElement : VisualElement
    {
        internal class Styles
        {
            public static readonly string ussClassName = "unity-preview-pane";
            public static readonly string uxmlPath = "UXML/InspectorWindow/InspectorPreview.uxml";
            public static readonly string ussPath = "StyleSheets/InspectorWindow/InspectorPreview.uss";
            public static readonly string controlsUssPath = "StyleSheets/InspectorWindow/InspectorPreviewControls.uss";
            public static readonly string titleName = "title";
            public static readonly string windowResizeName = "sub-title";
            public static readonly string previewName = "inspector-preview";
            public static readonly string contentContainerName = "content-container";
            public static readonly string toolbarName = "toolbar";
            public static readonly string ellipsisMenuName = "ellipsis-menu";
            public static readonly string headerName = "header";
            public static readonly string dropdownName = "preview-selector-dropdown";
            public static readonly string dropdownButton = "unity-dropdown-button";
            public static readonly string basePopupFieldArrow = "unity-base-popup-field__arrow";
            public static readonly string previewPopupFieldArrow = "unity-dropdown-arrow";
        }

        VisualElement m_Container;
        VisualElement m_Toolbar;
        VisualElement m_Header;
        Label m_Title;
        VisualElement m_Resizer;
        DropdownField m_Dropdown;
        ToolbarMenu m_EllipsisMenu;

        public VisualElement GetHeader() { return m_Header; }
        public Label GetTitle() { return m_Title; }
        public VisualElement GetResizer() { return m_Resizer; }
        public VisualElement GetButtonPane() { return m_Toolbar; }
        public VisualElement GetPreviewPane() { return m_Container; }
        public DropdownField GetDropdown() { return m_Dropdown; }
        public VisualElement GetEllipsisMenu() { return m_EllipsisMenu; }

        public PreviewRootElement()
        {
            AddToClassList(Styles.ussClassName);

            var visualAsset = EditorGUIUtility.Load(Styles.uxmlPath) as VisualTreeAsset;
            visualAsset.CloneTree(this);

            AddStyleSheetPath(Styles.ussPath);
            AddStyleSheetPath(Styles.controlsUssPath);

            name = Styles.previewName;
            m_Container = this.Q(Styles.contentContainerName);
            m_Toolbar = this.Q(Styles.toolbarName);
            m_Header = this.Q(Styles.headerName);
            m_Title = m_Header?.Q<Label>(Styles.titleName);
            m_Resizer = m_Header?.Q(Styles.windowResizeName);
            m_Dropdown = m_Header?.Q<DropdownField>(Styles.dropdownName);

            focusable = true;

            m_EllipsisMenu = this.Q<ToolbarMenu>(Styles.ellipsisMenuName);
            m_EllipsisMenu.style.display = DisplayStyle.Flex;
        }

        public void AppendActionToEllipsisMenu(string actionName,
            Action<DropdownMenuAction> action,
            Func<DropdownMenuAction, DropdownMenuAction.Status> actionStatusCallback,
            object userData = null)
        {
            m_EllipsisMenu.menu.AppendAction(actionName, action, actionStatusCallback, userData);
        }

        public void ClearEllipsisMenu()
        {
            m_EllipsisMenu.menu.ClearItems();
        }

        public void AddButton(string propertyName, Texture2D image, Action clickEvent)
        {
            if (m_Toolbar.Q(propertyName) != null)
                return;

            var button = new Button(clickEvent);
            button.name = propertyName;
            button.style.backgroundImage = image;
            m_Toolbar.Add(button);
        }

        public void UpdateButtonIcon(string propertyName, Texture2D image)
        {
            var button = m_Toolbar.Q(propertyName);

            if (button == null)
                return;

            button.style.backgroundImage = image;
        }

        public void AddDropdownWithIcon(string propertyName, Texture2D image, Action clickEvent)
        {
            if (m_Toolbar.Q(propertyName) != null)
                return;

            var dropdown = new Button(clickEvent);
            dropdown.name = propertyName;
            dropdown.AddToClassList(Styles.dropdownButton);

            var icon = new Image();
            icon.image = image;

            var arrow = new VisualElement();
            arrow.AddToClassList(Styles.basePopupFieldArrow);
            arrow.AddToClassList(Styles.previewPopupFieldArrow);

            dropdown.style.flexDirection = FlexDirection.Row;
            dropdown.Add(icon);
            dropdown.Add(arrow);

            m_Toolbar.Add(dropdown);
        }

        public override VisualElement contentContainer => m_Container == null ? this : m_Container;
    }
}
