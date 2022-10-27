// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Toolbars;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Toolbar element to navigate to the previous error in the graph.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    class PreviousErrorButton : ErrorToolbarButton
    {
        public const string id = "GTF/Error/Previous";

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviousErrorButton"/> class.
        /// </summary>
        public PreviousErrorButton()
        {
            name = "PreviousError";
            tooltip = L10n.Tr("Previous Error");
            icon = EditorGUIUtility.FindTexture("GraphToolsFoundation/ErrorToolbar/PreviousError");
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            var state = GraphView?.GraphViewModel.GraphViewState;
            if (state != null)
                FrameAndSelectError(state.ErrorIndex - 1);
        }
    }
}
