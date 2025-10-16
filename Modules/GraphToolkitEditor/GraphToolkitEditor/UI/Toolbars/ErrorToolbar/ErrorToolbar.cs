// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor.Overlays;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The toolbar that displays the error count and buttons to navigate the errors.
    /// </summary>
    [Overlay(typeof(GraphViewEditorWindow), toolbarId, "Error Notifications", ussName = "ErrorNotifications",
        defaultDisplay = true, defaultDockZone = DockZone.BottomToolbar, defaultDockPosition = DockPosition.Top,
        defaultLayout = Layout.HorizontalToolbar)]
    [Icon("UnityEditor.ConsoleWindow.png")]
    [UnityRestricted]
    internal sealed class ErrorToolbar : Toolbar
    {
        public const string toolbarId = "gtf-error-notifications";

        /// <inheritdoc />
        protected internal override Layout supportedLayouts => Layout.HorizontalToolbar;

        public ErrorToolbar()
        {
            rootVisualElement.AddPackageStylesheet("ErrorOverlayToolbar.uss");
        }
    }
}
