// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Toolbars;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Toolbar element to display the option menu built by <see cref="GraphView.BuildOptionMenu"/>.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    sealed class OptionDropDownMenu : EditorToolbarDropdown, IAccessContainerWindow
    {
        public const string id = "GTF/Main/Options";

        /// <inheritdoc />
        public EditorWindow containerWindow { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionDropDownMenu"/> class.
        /// </summary>
        public OptionDropDownMenu()
        {
            name = "Options";
            tooltip = L10n.Tr("Options");
            clicked += OnClick;
            icon = EditorGUIUtility.FindTexture("GraphToolsFoundation/OptionsToolbar/Options");
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
