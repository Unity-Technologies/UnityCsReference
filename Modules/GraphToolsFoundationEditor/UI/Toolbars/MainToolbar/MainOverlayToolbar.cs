// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Overlays;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The main toolbar.
    /// </summary>
    [Overlay(typeof(GraphViewEditorWindow), toolbarId, "Asset Management", ussName = "AssetManagement",
        defaultDisplay = true, defaultDockZone = DockZone.TopToolbar, defaultDockPosition = DockPosition.Top,
        defaultDockIndex = 0, defaultLayout = Layout.HorizontalToolbar)]
    [Icon("Icons/Overlays/ToolsToggle.png")]
    sealed class MainOverlayToolbar : OverlayToolbar
    {
        public const string toolbarId = "gtf-asset-management";
    }
}
