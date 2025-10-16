// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.ShortcutManagement;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Helper class for registering shortcuts.
    /// </summary>
    [UnityRestricted]
    internal static class ShortcutHelper
    {
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
            ShortcutProviderProxy.GetInstance().AddTool(toolName, typeof(T), shortcutFilter, registerNow);
        }

        internal static void UnregisterDefaultShortcuts<T>(string toolName, Func<string, bool> shortcutFilter = null,
            bool registerNow = false)
        {
            ShortcutProviderProxy.GetInstance().RemoveTool(toolName, typeof(T), shortcutFilter, registerNow);
        }
    }
}
