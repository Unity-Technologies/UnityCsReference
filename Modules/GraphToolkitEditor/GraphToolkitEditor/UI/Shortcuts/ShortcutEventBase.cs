// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using Unity.GraphToolkit.InternalBridge;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// UIToolkit event sent as a result of a shortcut being triggered.
    /// </summary>
    [UnityRestricted]
    internal interface IShortcutEvent
    {
        /// <summary>
        /// The mouse position when the shortcut was triggered.
        /// </summary>
        Vector2 MousePosition { get; }
        /// <summary>
        /// The stage of the shortcut.
        /// </summary>
        ShortcutStage Stage { get; }
    }

    internal interface IShortcutContextHandler
    {
        void HandleShortcut(EventBase e);
    }

    /// <summary>
    /// UIToolkit event sent as a result of a shortcut being triggered.
    /// </summary>
    /// <typeparam name="T">Type of event.</typeparam>
    [UnityRestricted]
    internal class ShortcutEventBase<T> : EventBase<T>, IShortcutEvent where T : ShortcutEventBase<T>, new()
    {
        /// <inheritdoc />
        public Vector2 MousePosition { get; private set; }

        /// <inheritdoc />
        public ShortcutStage Stage { get; private set; }

        /// <summary>
        /// Gets a ShortcutEvent from the pool of events and initializes it.
        /// </summary>
        /// <param name="stage">The stage of the shortcut.</param>
        /// <returns>A freshly initialized shortcut event.</returns>
        public static ShortcutEventBase<T> GetPooled(ShortcutStage stage)
        {
            var e = GetPooled();
            e.Stage = stage;
            return e;
        }

        /// <summary>
        /// Returns the current binding for a shortcut, for a given <see cref="GraphTool"/>.
        /// </summary>
        /// <param name="tool">The <see cref="GraphTool"/> for which the shortcut should be fetched.</param>
        /// <returns>The current binding for a shortcut, for a given <see cref="GraphTool"/>.</returns>
        public static ShortcutBinding GetCurrentBinding(GraphTool tool)
        {
            var attribute = typeof(T).GetCustomAttribute<ToolShortcutEventAttribute>();
            if (attribute != null && (attribute.ToolName == null || attribute.ToolName == tool.Name))
            {
                return ShortcutManager.instance.GetShortcutBinding(tool.Name + "/" + attribute.Identifier);
            }

            return default;
        }

        internal static string GetShortcutString(GraphTool tool)
        {
            var attribute = typeof(T).GetCustomAttribute<ToolShortcutEventAttribute>();
            if (attribute != null && (attribute.ToolName == null || attribute.ToolName == tool.Name))
            {
                string bindingString = null;
                try
                {
                    bindingString = ShortcutManager.instance.GetShortcutBinding(tool.Name + "/" + attribute.Identifier).ToString();
                }
                catch (ArgumentException) // ShortcutManager throws if it can't find the shortcut
                {
                }
                return bindingString;
            }

            return null;
        }

        internal static string GetMenuItemShortcutString(GraphTool tool)
        {
            var attribute = typeof(T).GetCustomAttribute<ToolShortcutEventAttribute>();
            if (attribute != null && (attribute.ToolName == null || attribute.ToolName == tool.Name))
            {
                string bindingString = null;
                try
                {
                    bindingString = ShortcutManager.instance.GetShortcutBinding(tool.Name + "/" + attribute.Identifier).GetShortcutMenuString();
                }
                catch (ArgumentException) // ShortcutManager throws if it can't find the shortcut
                {
                }
                return bindingString;
            }

            return null;
        }

        internal static string GetMenuItemName(GraphTool tool)
        {
            var attribute = typeof(T).GetCustomAttribute<ToolShortcutEventAttribute>();
            if (attribute != null && (attribute.ToolName == null || attribute.ToolName == tool.Name))
            {
                string bindingString = null;
                try
                {
                    bindingString = ShortcutManager.instance.GetShortcutBinding(tool.Name + "/" + attribute.Identifier).GetShortcutMenuString();
                }
                catch (ArgumentException) // ShortcutManager throws if it can't find the shortcut
                {
                }
                return bindingString != null ? attribute.DisplayName + " " + bindingString : attribute.DisplayName;
            }

            return null;
        }

        /// <summary>
        /// Initializes the event.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            MousePosition = EngineBridge.GetMousePosition();
            this.SetPropagationBothWay();
        }

        internal static void SendTestEvent(GraphViewEditorWindow window, ShortcutStage stage)
        {
            SendEvent(new ShortcutArguments() { context = window, stage = stage });
        }

        // ReSharper disable once UnusedMember.Global
        // Used by reflection by ShortcutProviderProxy.GetDefinedShortcuts().
        /// <summary>
        /// Sends a shortcut event as a response to a shortcut being triggered.
        /// </summary>
        /// <param name="args">The shortcut arguments</param>
        /// <remarks>This method is used as the callback for every shortcut that should trigger a UIToolkit event.</remarks>
        protected static void SendEvent(ShortcutArguments args)
        {
            var window = args.context as EditorWindow;
            if (window != null && EditorWindow.focusedWindow == window)
            {
                using (var e = GetPooled(args.stage))
                {
                    e.target = window.rootVisualElement.panel.focusController.focusedElement ?? window.rootVisualElement;
                    window.rootVisualElement.SendEvent(e);
                }
            }
            else if (args.context is IShortcutContextHandler shortcutContextHandler)
            {
                using (var e = GetPooled(args.stage))
                {
                    shortcutContextHandler.HandleShortcut(e);
                }
            }
        }
    }
}
