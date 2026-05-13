// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Overlays;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The main toolbar.
    /// </summary>
    [Overlay(typeof(GraphViewEditorWindow), toolbarId, "Asset Management", ussName = "AssetManagement",
        defaultDisplay = true, defaultDockZone = DockZone.TopToolbar, defaultDockPosition = DockPosition.Top,
        defaultDockIndex = 0, defaultLayout = Layout.HorizontalToolbar)]
    [Icon("Icons/Overlays/ToolsToggle.png")]
    [UnityRestricted]
    internal sealed class MainToolbar : Toolbar
    {
        public const string k_MainToolbarOverlayClassName = "MainToolbar_Overlay";
        /// <summary>
        /// The identification of the <see cref="MainToolbar"/>.
        /// </summary>
        public const string toolbarId = "gtf-asset-management";
    }
}
