// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Toolbars;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Toolbar element to navigate to the next error in the graph.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    class NextErrorButton : ErrorToolbarButton
    {
        public const string id = "GTF/Error/Next";

        /// <summary>
        /// Initializes a new instance of the <see cref="NextErrorButton"/> class.
        /// </summary>
        public NextErrorButton()
        {
            name = "NextError";
            tooltip = L10n.Tr("Next Error");
            icon = EditorGUIUtility.FindTexture("GraphToolsFoundation/ErrorToolbar/NextError");
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            var state = GraphView?.GraphViewModel.GraphViewState;
            if (state != null)
                FrameAndSelectError(state.ErrorIndex + 1);
        }
    }
}
