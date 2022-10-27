// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Toolbar button to build the graph.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    class BuildAllButton : MainToolbarButton
    {
        public const string id = "GTF/Main/Build All";

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildAllButton"/> class.
        /// </summary>
        public BuildAllButton()
        {
            name = "BuildAll";
            tooltip = L10n.Tr("Build All");
            icon = EditorGUIUtility.FindTexture("GraphToolsFoundation/MainToolbar_Overlay/BuildAll");
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            try
            {
                GraphTool?.Dispatch(new BuildAllEditorCommand());
            }
            catch (Exception e) // so the button doesn't get stuck
            {
                Debug.LogException(e);
            }
        }
    }
}
