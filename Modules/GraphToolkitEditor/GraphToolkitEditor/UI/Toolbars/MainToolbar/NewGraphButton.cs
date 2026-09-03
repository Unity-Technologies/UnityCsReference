// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: GraphToolkit not yet converted
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
            tooltip = L10n.Tr("New Graph", null);
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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
