// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor;
using UnityEditor.Toolbars;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Toolbar button to toggle the display of the minimap.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    [UnityRestricted]
    internal sealed class MiniMapPanelToggle : PanelToggle
    {
        public const string id = "GraphToolkit/Overlay Windows/MiniMap";

        /// <inheritdoc />
        protected override string WindowId => MiniMapOverlay.idValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="MiniMapPanelToggle"/> class.
        /// </summary>
        public MiniMapPanelToggle()
        {
            name = "MiniMap";
            tooltip = L10n.Tr("MiniMap");
            icon = EditorGUIUtilityBridge.LoadIcon($"{GraphElementHelper.k_IconFolder}PanelsToolbar/MiniMap.png");
        }
    }
}
