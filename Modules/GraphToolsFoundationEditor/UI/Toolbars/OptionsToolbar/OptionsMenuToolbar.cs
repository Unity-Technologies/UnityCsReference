// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Overlays;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The toolbar for the option menu.
    /// </summary>
    [Overlay(typeof(GraphViewEditorWindow), toolbarId, "Options", true,
        defaultDockZone = DockZone.TopToolbar, defaultDockPosition = DockPosition.Bottom,
        defaultDockIndex = 1000, defaultLayout = Layout.HorizontalToolbar)]
    [Icon("Icons/GraphToolsFoundation/OptionsToolbar/Options.png")]
    sealed class OptionsMenuToolbar : OverlayToolbar
    {
        public const string toolbarId = "gtf-options";
    }
}
