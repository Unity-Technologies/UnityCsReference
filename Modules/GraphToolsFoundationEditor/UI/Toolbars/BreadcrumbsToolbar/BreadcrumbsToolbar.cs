// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Overlays;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The toolbar that displays the breadcrumbs.
    /// </summary>
    [Overlay(typeof(GraphViewEditorWindow), toolbarId, "Breadcrumbs", true,
        defaultDockZone = DockZone.TopToolbar, defaultDockPosition = DockPosition.Top,
        defaultDockIndex = 1000, defaultLayout = Layout.HorizontalToolbar)]
    [Icon("Icons/GraphToolsFoundation/BreadcrumbsToolbar/Breadcrumb.png")]
    sealed class BreadcrumbsToolbar : OverlayToolbar
    {
        public const string toolbarId = "gtf-breadcrumbs";

        /// <inheritdoc />
        protected internal override Layout supportedLayouts => Layout.HorizontalToolbar;
    }
}
