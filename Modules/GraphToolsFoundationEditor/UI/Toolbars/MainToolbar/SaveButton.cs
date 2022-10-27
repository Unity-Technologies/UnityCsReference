// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Toolbars;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Toolbar button to save all graphs.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    class SaveButton : MainToolbarButton
    {
        public const string id = "GTF/Main/Save";

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveButton"/> class.
        /// </summary>
        public SaveButton()
        {
            name = "Save";
            tooltip = L10n.Tr("Save");
            icon = EditorGUIUtility.FindTexture("GraphToolsFoundation/MainToolbar_Overlay/Save");
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            var graphEditorWindow = containerWindow as GraphViewEditorWindow;
            if (graphEditorWindow != null)
            {
                var graphAsset = graphEditorWindow.GraphView.GraphViewModel.GraphModelState.GraphModel.Asset;
                if (graphAsset != null)
                {
                    graphAsset.Save();
                }
                else
                {
                    AssetDatabase.SaveAssets();
                }
            }
        }
    }
}
