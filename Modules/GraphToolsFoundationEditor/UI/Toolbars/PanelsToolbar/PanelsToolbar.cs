// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Overlays;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The panel toggle toolbar.
    /// </summary>
    [Overlay(typeof(GraphViewEditorWindow), toolbarId, "Panel Toggles", ussName = "PanelToggles",
        defaultDisplay = true, defaultDockZone = DockZone.TopToolbar, defaultDockPosition = DockPosition.Bottom,
        defaultDockIndex = 1, defaultLayout = Layout.HorizontalToolbar)]
    [Icon("Icons/GraphToolsFoundation/PanelsToolbar/Panels.png")]
    sealed class PanelsToolbar : OverlayToolbar
    {
        public const string toolbarId = "gtf-panel-toggles-toolbar";
    }
}
