// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// UIToolkit event sent as a result of a shortcut being triggered.
    /// </summary>
    interface IShortcutEvent
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

    /// <summary>
    /// UIToolkit event sent as a result of a shortcut being triggered.
    /// </summary>
    /// <typeparam name="T">Type of event.</typeparam>
    class ShortcutEventBase<T> : EventBase<T>, IShortcutEvent where T : ShortcutEventBase<T>, new()
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
        /// Initializes the event.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            MousePosition = PointerDeviceState.GetPointerPosition(PointerId.mousePointerId, ContextType.Editor);
            propagation = EventPropagation.TricklesDown | EventPropagation.Bubbles;
        }

        internal static void SendTestEvent_Internal(GraphViewEditorWindow window, ShortcutStage stage)
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
        }
    }
}
