// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Overlays;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The toolbar that displays the error count and buttons to navigate the errors.
    /// </summary>
    [Overlay(typeof(GraphViewEditorWindow), toolbarId, "Error Notifications", ussName = "ErrorNotifications",
        defaultDisplay = true, defaultDockZone = DockZone.BottomToolbar, defaultDockPosition = DockPosition.Bottom,
        defaultLayout = Layout.HorizontalToolbar)]
    [Icon("Icons/GraphToolsFoundation/ErrorToolbar/ErrorNotification.png")]
    sealed class ErrorOverlayToolbar : OverlayToolbar
    {
        public const string toolbarId = "gtf-error-notifications";

        /// <inheritdoc />
        protected internal override Layout supportedLayouts => Layout.HorizontalToolbar;

        public ErrorOverlayToolbar()
        {
            rootVisualElement.AddStylesheet_Internal("ErrorOverlayToolbar.uss");
        }
    }
}
