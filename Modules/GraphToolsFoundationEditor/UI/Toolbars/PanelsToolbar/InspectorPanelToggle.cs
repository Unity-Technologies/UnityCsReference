// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Toolbars;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Toolbar button to toggle the display of the inspector.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    sealed class InspectorPanelToggle : PanelToggle
    {
        public const string id = "GTF/Overlay Windows/Inspector";

        /// <inheritdoc />
        protected override string WindowId => ModelInspectorOverlay_Internal.idValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="InspectorPanelToggle"/> class.
        /// </summary>
        public InspectorPanelToggle()
        {
            name = "Inspector";
            tooltip = L10n.Tr("Inspector");
            icon = EditorGUIUtility.FindTexture("GraphToolsFoundation/PanelsToolbar/Inspector");
        }
    }
}
