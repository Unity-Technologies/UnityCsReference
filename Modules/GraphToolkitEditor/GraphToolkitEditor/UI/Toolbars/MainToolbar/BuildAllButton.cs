// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Toolbar button to build the graph.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    [UnityRestricted]
    internal class BuildAllButton : MainToolbarButton
    {
        public const string id = "GraphToolkit/Main/Build All";

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildAllButton"/> class.
        /// </summary>
        public BuildAllButton()
        {
            name = "BuildAll";
            tooltip = L10n.Tr("Build All");
            icon = EditorGUIUtilityBridge.LoadIcon($"{GraphElementHelper.k_IconFolder}MainToolbar_Overlay/BuildAll.png");
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
