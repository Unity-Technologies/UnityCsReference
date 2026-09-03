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

        readonly string m_MenuPath;

        public MainToolbarMenuItemButton(string menuPath)
        {
            m_MenuPath = menuPath;
            var segments = menuPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            content = new MainToolbarContent(segments[^1], k_TooltipStart + menuPath);
        }

        internal override VisualElement CreateElement()
        {
            var button = new EditorToolbarButton(content.text, content.image, () => EditorApplication.ExecuteMenuItem(m_MenuPath));
            button.AddToClassList(EditorToolbar.elementClassName);
            button.text = content.text;
            button.icon = content.image;
            button.tooltip = content.tooltip;
            return button;
        }

        [InitializeOnLoadMethod]
        static void RegisterMenuItemButtonFactory()
        {
            MainToolbar.menuItemButtonFactory = path => new MainToolbarMenuItemButton(path);
        }
    }
}
