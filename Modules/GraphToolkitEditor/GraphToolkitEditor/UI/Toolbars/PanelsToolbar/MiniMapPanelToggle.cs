// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: GraphToolkit not yet converted
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

        static readonly string k_CachedTooltipText =  L10n.Tr("MiniMap", null);

        /// <inheritdoc />
        protected override string WindowId => MiniMapOverlay.idValue;

        /// <inheritdoc />
        protected override string TooltipText => k_CachedTooltipText;

        /// <inheritdoc />
        protected override string ShortcutString => ShortcutToggleMinimapEvent.GetShortcutString((containerWindow as GraphViewEditorWindow)?.GraphTool);

        /// <summary>
        /// Initializes a new instance of the <see cref="MiniMapPanelToggle"/> class.
        /// </summary>
        public MiniMapPanelToggle()
        {
            name = "MiniMap";
            icon = EditorGUIUtilityBridge.LoadIcon($"{GraphElementHelper.k_IconFolder}PanelsToolbar/MiniMap.png");
            UpdateInspectorTooltip();
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
