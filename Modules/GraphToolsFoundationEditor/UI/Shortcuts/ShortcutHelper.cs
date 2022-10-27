// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Helper class for registering shortcuts.
    /// </summary>
    static class ShortcutHelper
    {
        static bool BlackboardShortcutFilter(string shortcutId)
        {
            return shortcutId == ShortcutShowItemLibraryEvent.id;
        }

        /// <summary>
        /// Registers all GTF provided shortcuts for tool <paramref name="toolName"/>.
        /// </summary>
        /// <param name="toolName">The name of the tool that wants to use the shortcuts.</param>
        /// <param name="shortcutFilter">A function used to filter shortcuts. It receives a shortcut
        /// id and it should return false if the shortcut is not wanted by the tool. If null, no filtering occurs.</param>
        /// <param name="registerNow">Set to true to force the immediate discovery of the shortcuts by the
        /// <see cref="ShortcutManager"/>. You need to set this to true if you register shortcuts after the
        /// ShortcutManager has been initialized.</param>
        /// <typeparam name="T">The window type of the tool.</typeparam>
        public static void RegisterDefaultShortcuts<T>(string toolName, Func<string, bool> shortcutFilter = null, bool registerNow = false)
            where T : EditorWindow
        {
            ShortcutProviderProxy_Internal.GetInstance().AddTool(toolName, typeof(T), shortcutFilter, registerNow);
        }

        internal static void RegisterDefaultShortcutsForBlackboard_Internal<T>(string toolName)
        {
            ShortcutProviderProxy_Internal.GetInstance().AddTool(toolName, typeof(T), BlackboardShortcutFilter);
        }

        /// <summary>
        /// Appends a hotkey suffix to a menu item that has the same effect as a shortcut.
        /// </summary>
        /// <param name="menuItemText">The menu item name.</param>
        /// <param name="toolName">The name of the tool doing this request.</param>
        /// <param name="shortcutId">The shortcut id (without the tool name prefix).</param>
        /// <returns>Returns <paramref name="menuItemText"/> appended with a string representing the
        /// key bound to the shortcut <paramref name="shortcutId"/></returns>
        public static string CreateShortcutMenuItemEntry(string menuItemText, string toolName, string shortcutId)
        {
            ShortcutBinding binding;
            try
            {
                binding = ShortcutManager.instance.GetShortcutBinding(toolName + "/" + shortcutId);
            }
            catch (ArgumentException)
            {
                Debug.LogWarning(
                    "Shortcuts bindings do not appear to be registered for your tool.\n" +
                    "To register the default bindings in your tool add the following\n" +
                    "(where MyGraphWindow and MyStencil are respectively your tool's window type and stencil type):\n\n" +
                    "[InitializeOnLoadMethod]\n" +
                    "static void RegisterTool()\n" +
                    "{\n" +
                    "    ShortcutHelper.RegisterDefaultShortcuts<MyGraphWindow>(MyStencil.toolName);\n" +
                    "}\n\n");
                return menuItemText;
            }

            var hotKey = "";
            if (binding.keyCombinationSequence.Count() == 1)
            {
                var kc = binding.keyCombinationSequence.First();

                if (kc.action)
                    hotKey += "%";
                if (kc.alt)
                    hotKey += "&";
                if (kc.shift)
                    hotKey += "#";

                if (hotKey == "")
                    hotKey = "_";

                hotKey = " " + hotKey + kc.keyCode;
            }

            return menuItemText + hotKey;
        }
    }
}
