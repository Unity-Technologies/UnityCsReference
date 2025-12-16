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
    /// Toolbar button to toggle the display of the blackboard.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    [UnityRestricted]
    internal sealed class BlackboardPanelToggle : PanelToggle
    {
        public const string id = "GraphToolkit/Overlay Windows/Blackboard";

        static readonly string k_CachedTooltipText =  L10n.Tr("Blackboard");

        /// <inheritdoc />
        protected override string WindowId => BlackboardOverlay.idValue;

        /// <inheritdoc />
        protected override string TooltipText => k_CachedTooltipText;

        /// <inheritdoc />
        protected override string ShortcutString => ShortcutToggleBlackboardEvent.GetShortcutString((containerWindow as GraphViewEditorWindow)?.GraphTool);

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardPanelToggle"/> class.
        /// </summary>
        public BlackboardPanelToggle()
        {
            name = "Blackboard";
            icon = EditorGUIUtilityBridge.LoadIcon($"{GraphElementHelper.k_IconFolder}PanelsToolbar/Blackboard.png");
            UpdateInspectorTooltip();
        }
    }
}
