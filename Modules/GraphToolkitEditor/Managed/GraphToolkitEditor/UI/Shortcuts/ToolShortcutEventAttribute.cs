// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Attribute used to associate a shortcut to a class derived from <see cref="ShortcutEventBase{T}"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    [UnityRestricted]
    internal sealed class ToolShortcutEventAttribute : Attribute
    {
        internal string ToolName { get; }
        internal string Identifier { get; }
        internal string DisplayName { get; }
        internal ShortcutBinding DefaultBinding { get; }
        internal bool IsClutch { get; }
        internal RuntimePlatform[] OnlyOnPlatforms { get; }
        internal RuntimePlatform[] ExcludedPlatforms { get; }

        /// <summary>
        /// Initializes a new instance of the attribute.
        /// </summary>
        /// <param name="toolName">The tool that defines this shortcut.</param>
        /// <param name="id">The shortcut unique ID.</param>
        /// <param name="defaultBinding">Default keyboard mapping.</param>
        /// <param name="isClutch">True if the shortcut is a clutch.</param>
        /// <param name="onlyOnPlatforms">An array of platforms on which the shortcut is defined. If null, this parameter has no effect.</param>
        /// <param name="excludedPlatforms">An array of platforms on which the shortcut is not defined. If null, this parameter has no effect.</param>
        public ToolShortcutEventAttribute(
            string toolName,
            string id,
            ShortcutBinding defaultBinding,
            bool isClutch = false,
            RuntimePlatform[] onlyOnPlatforms = null,
            RuntimePlatform[] excludedPlatforms = null)
        {
            ToolName = toolName;
            Identifier = id;
            DefaultBinding = defaultBinding;
            DisplayName = Identifier;
            IsClutch = isClutch;
            OnlyOnPlatforms = onlyOnPlatforms;
            ExcludedPlatforms = excludedPlatforms;
        }

        /// <summary>
        /// Initializes a new instance of the attribute.
        /// </summary>
        /// <param name="toolName">The tool that defines this shortcut.</param>
        /// <param name="id">The shortcut unique ID.</param>
        /// <param name="isClutch">True if the shortcut is a clutch.</param>
        public ToolShortcutEventAttribute(string toolName, string id, bool isClutch = false)
            : this(toolName, id, ShortcutBinding.empty, isClutch)
        {
        }

        /// <summary>
        /// Initializes a new instance of the attribute.
        /// </summary>
        /// <param name="toolName">The tool that defines this shortcut.</param>
        /// <param name="id">The shortcut unique ID.</param>
        /// <param name="defaultKeyCode">The default key for the shortcut.</param>
        /// <param name="defaultShortcutModifiers">The default modifiers for the shortcut.</param>
        /// <param name="isClutch">True if the shortcut is a clutch.</param>
        /// <param name="onlyOnPlatforms">An array of platforms on which the shortcut is defined. If null, this parameter has no effect.</param>
        /// <param name="excludedPlatforms">An array of platforms on which the shortcut is not defined. If null, this parameter has no effect.</param>
        public ToolShortcutEventAttribute(
            string toolName,
            string id,
            KeyCode defaultKeyCode,
            ShortcutModifiers defaultShortcutModifiers = ShortcutModifiers.None,
            bool isClutch = false,
            RuntimePlatform[] onlyOnPlatforms = null,
            RuntimePlatform[] excludedPlatforms = null)
            : this(toolName, id, new ShortcutBinding(new KeyCombination(defaultKeyCode, defaultShortcutModifiers)), isClutch, onlyOnPlatforms, excludedPlatforms)
        {
        }
    }
}
