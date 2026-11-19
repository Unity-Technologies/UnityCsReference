// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.ShortcutManagement;
using UnityEngine.Bindings;

namespace Unity.GraphToolsAuthoringFramework.InternalEditorBridge
{
    /// <summary>
    /// Public facing shortcut definition.
    /// </summary>
    struct ShortcutDefinition
    {
        public string ToolName;
        public string ShortcutId;
        public Type Context;
        public ShortcutBinding DefaultBinding;
        public string DisplayName;
        public bool IsClutch;
        public MethodInfo MethodInfo;
    }

    /// <summary>
    /// A proxy for shortcut discovery.
    /// </summary>
    interface IDiscoveryShortcutProviderProxy
    {
        /// <summary>
        /// Gets the list of shortcuts.
        /// </summary>
        /// <returns>A list of shortcut definitions.</returns>
        IEnumerable<ShortcutDefinition> GetDefinedShortcuts();
    }
}
