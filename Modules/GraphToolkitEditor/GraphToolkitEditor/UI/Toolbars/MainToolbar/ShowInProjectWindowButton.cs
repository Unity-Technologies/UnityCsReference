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
    /// Toolbar button to focus the current graph's asset in the Project Window.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    [UnityRestricted]
    internal class ShowInProjectWindowButton : MainToolbarButton
    {
        const string k_FileAccessIconFilename = "FileAccess@4x.png";
        public const string id = "GraphToolkit/Main/ShowInProjectWindow";

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowInProjectWindowButton"/> class.
        /// </summary>
        public ShowInProjectWindowButton()
        {
            name = "ShowInProjectWindow";
            tooltip = L10n.Tr("Show in Project Window");
            clicked += OnClick;
            icon = EditorGUIUtilityBridge.LoadIcon($"{GraphElementHelper.k_IconFolder}{MainToolbar.k_MainToolbarOverlayClassName}/{k_FileAccessIconFilename}");
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            var graphEditorWindow = containerWindow as GraphViewEditorWindow;
            if (graphEditorWindow is null)
                return;

            var graphObject = graphEditorWindow.GraphView?.GraphViewModel?.GraphModelState?.GraphModel?.GraphObject;
            graphObject?.ShowGraphObjectInProjectWindow();
        }
    }
}
