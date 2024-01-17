// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace UnityEditor
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions.Custom, "MonoMenuItemData", Header = "Editor/Src/MenuController.h")]
    [NativeAsStruct]
    internal class MenuItemData
    {
        public string name;
        public string shortcut;
        public bool @checked;
        public bool enabled;
        public string disabledTooltip;
        public string icon;
    }

    [NativeHeader("Editor/Src/MenuController.h")]
    internal static class MenuController
    {
        // Keep in sync with AssetsMenu.cpp
        private const string k_RevealInFinderLabel = "Open Containing Folder";

        static Rect m_PositionOverride = EditorMenuExtensions.k_InvalidRect;

        internal static Rect positionOverride
        {
            get => m_PositionOverride;
            set => m_PositionOverride = value;
        }

        [RequiredByNativeCode]
        static void ShowDropdownFromNative(Vector2 position, MenuItemData[] items, string menuRoot)
        {
            if (EditorMenuExtensions.isEditorContextMenuActive)
                CloseAllContextMenus();

            var menu = new DropdownMenu();
            var rectPosition = positionOverride == EditorMenuExtensions.k_InvalidRect ?
                new Rect(GUIUtility.ScreenToGUIPoint(position), Vector2.zero) : GUIUtility.ScreenToGUIRect(positionOverride);

            foreach (var item in items)
            {
                var isSeparator = string.IsNullOrWhiteSpace(item.name) || item.name.EndsWith('/');
                var itemName = isSeparator ? item.name : $"{item.name} {item.shortcut}";

                if (isSeparator)
                {
                    menu.AppendSeparator(itemName);
                    continue;
                }

                // Capture these variables in managed memory as native data is going to be cleared very soon and will cause issues in action status callback
                var itemPath = item.name;
                var isEnabled = item.enabled;
                var isChecked = item.@checked;
                var disabledTooltipText = item.disabledTooltip;

                menu.AppendAction(itemName, a =>
                {
                    // UUM-35952
                    // ProjectBrowser is an IMGUI window thus it doesn't like when we interrupt GUI repaint
                    // with actions that create new windows. Therefore we delay 'Reveal in Finder' action to
                    // happen after ProjectBrowser is done repainting.
                    if (itemName.Trim() == k_RevealInFinderLabel)
                    {
                        EditorApplication.delayCall += () => EditorApplication.ExecuteMenuItem(menuRoot + "/" + itemPath);
                    }
                    else
                    {
                        EditorApplication.ExecuteMenuItem(menuRoot + "/" + itemPath);
                    }
                }, a => {
                    var status = DropdownMenuAction.Status.None;

                    status |= isEnabled ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                    status |= isChecked ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.None;

                    if (!isEnabled)
                        a.tooltip = disabledTooltipText;

                    return status;
                }, null, EditorGUIUtility.LoadIcon(item.icon));
            }

            var parentMenu = EditorWindow.focusedWindow as EditorMenuExtensions.ContextMenu;
            var parentIsMenu = parentMenu != null;

            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX
                && !parentIsMenu && GUIView.mouseOverView is HostView hostView)
                hostView.Focus();

            EditorMenuExtensions.DisplayEditorMenu(menu, rectPosition);
            positionOverride = EditorMenuExtensions.k_InvalidRect;
        }

        [RequiredByNativeCode]
        static void CloseAllContextMenus()
        {
            EditorMenuExtensions.CloseAllContextMenus();
        }
    }
}
