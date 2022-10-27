// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Toolbars;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Toolbar button to toggle the display of the blackboard.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    sealed class BlackboardPanelToggle : PanelToggle
    {
        public const string id = "GTF/Overlay Windows/Blackboard";

        /// <inheritdoc />
        protected override string WindowId => BlackboardOverlay_Internal.idValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardPanelToggle"/> class.
        /// </summary>
        public BlackboardPanelToggle()
        {
            name = "Blackboard";
            tooltip = L10n.Tr("Blackboard");
            icon = EditorGUIUtility.FindTexture("GraphToolsFoundation/PanelsToolbar/Blackboard");
        }
    }
}
