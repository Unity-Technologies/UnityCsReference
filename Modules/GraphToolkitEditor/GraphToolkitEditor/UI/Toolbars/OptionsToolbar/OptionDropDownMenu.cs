// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: GraphToolkit not yet converted
using System;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor;
using UnityEditor.Toolbars;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Toolbar element to display the option menu built by <see cref="GraphView.BuildOptionMenu"/>.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    [UnityRestricted]
    internal sealed class OptionDropDownMenu : EditorToolbarDropdown, IAccessContainerWindow
    {
        public const string id = "GraphToolkit/Main/Options";

        /// <inheritdoc />
        public EditorWindow containerWindow { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionDropDownMenu"/> class.
        /// </summary>
        public OptionDropDownMenu()
        {
            name = "Options";
            tooltip = L10n.Tr("Options", null);
            clicked += OnClick;
            icon = EditorGUIUtilityBridge.LoadIcon("_Menu");
        }

        void OnClick()
        {
            var graphViewWindow = containerWindow as GraphViewEditorWindow;

            if (graphViewWindow == null)
                return;

            GenericMenu menu = new GenericMenu();
            graphViewWindow.GraphView?.BuildOptionMenu(menu);
            menu.ShowAsContext();
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
