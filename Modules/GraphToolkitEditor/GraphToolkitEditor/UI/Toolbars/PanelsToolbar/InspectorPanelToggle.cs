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
    /// Toolbar button to toggle the display of the inspector.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    [UnityRestricted]
    internal sealed class InspectorPanelToggle : PanelToggle
    {
        /// <summary>
        /// The identifier of the inspector panel.
        /// </summary>
        public const string id = "GraphToolkit/Overlay Windows/Inspector";

        static readonly string k_CachedTooltipText = L10n.Tr("Graph Inspector");

        /// <inheritdoc />
        protected override string WindowId => ModelInspectorOverlay.idValue;

        /// <inheritdoc />
        protected override string TooltipText => k_CachedTooltipText;

        /// <inheritdoc />
        protected override string ShortcutString => ShortcutToggleInspectorEvent.GetShortcutString((containerWindow as GraphViewEditorWindow)?.GraphTool);

        /// <summary>
        /// Initializes a new instance of the <see cref="InspectorPanelToggle"/> class.
        /// </summary>
        public InspectorPanelToggle()
        {
            name = "Inspector";
            icon = EditorGUIUtilityBridge.LoadIcon($"{GraphElementHelper.k_IconFolder}PanelsToolbar/Inspector.png");
            UpdateInspectorTooltip();
        }
    }
}
