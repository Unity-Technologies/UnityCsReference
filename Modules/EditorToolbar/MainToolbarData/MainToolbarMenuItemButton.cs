// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    sealed class MainToolbarMenuItemButton : MainToolbarElement
    {
        static readonly string k_TooltipStart = L10n.Tr("Triggers menu item ", null);

        const string k_MenuItemButtonClassName = "unity-editor-toolbar-menu-item-button";

        readonly string m_MenuPath;

        public MainToolbarMenuItemButton(string menuPath)
        {
            m_MenuPath = menuPath;
            var segments = menuPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            content = new MainToolbarContent(segments[^1], k_TooltipStart + menuPath);
        }

        bool isMenuItemEnabled => Menu.MenuItemExists(m_MenuPath) && Menu.GetEnabled(m_MenuPath);

        internal override VisualElement CreateElement()
        {
            enabled = isMenuItemEnabled;

            var button = new EditorToolbarButton(content.text, content.image, () => EditorApplication.ExecuteMenuItem(m_MenuPath));
            button.AddToClassList(EditorToolbar.elementClassName);
            button.AddToClassList(k_MenuItemButtonClassName);
            button.text = content.text;
            button.icon = content.image;
            button.tooltip = content.tooltip;

            // Schedule only updates if attached to a panel so it will stop updating when the element is recreated during a refresh
            button.schedule.Execute(() =>
            {
                bool isEnabled = isMenuItemEnabled;
                if (button.enabledSelf != isEnabled)
                    button.SetEnabled(isEnabled);
            }).Every(200);

            return button;
        }

        [InitializeOnLoadMethod]
        static void RegisterMenuItemButtonFactory()
        {
            MainToolbar.menuItemButtonFactory = path => new MainToolbarMenuItemButton(path);
        }
    }
}
