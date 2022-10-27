// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Toolbars;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Toolbar button to create a new graph.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    class NewGraphButton : MainToolbarButton
    {
        public const string id = "GTF/Main/New Graph";

        /// <summary>
        /// Initializes a new instance of the <see cref="NewGraphButton"/> class.
        /// </summary>
        public NewGraphButton()
        {
            name = "NewGraph";
            tooltip = L10n.Tr("New Graph");
            icon = EditorGUIUtility.FindTexture("GraphToolsFoundation/MainToolbar/CreateNew");
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            GraphTool?.Dispatch(new UnloadGraphCommand());
        }
    }
}
