// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor;
using UnityEditor.Toolbars;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Toolbar button to create a new graph.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    [UnityRestricted]
    internal class NewGraphButton : MainToolbarButton
    {
        public const string id = "GraphToolkit/Main/New Graph";

        /// <summary>
        /// Initializes a new instance of the <see cref="NewGraphButton"/> class.
        /// </summary>
        public NewGraphButton()
        {
            name = "NewGraph";
            tooltip = L10n.Tr("New Graph");
            icon = EditorGUIUtilityBridge.LoadIcon("CreateAddNew");
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            var window = containerWindow as GraphViewEditorWindow;
            window?.ShowOnboardingWindow();
        }
    }
}
