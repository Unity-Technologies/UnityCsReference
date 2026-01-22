// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Overlays;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for overlay toolbars.
    /// </summary>
    [UnityRestricted]
    internal class Toolbar : ToolbarOverlay
    {
        /// <summary>
        /// Whether the overlay toolbar should be enabled or not.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// The graph tool.
        /// </summary>
        protected GraphTool GraphTool => (containerWindow as GraphViewEditorWindow)?.GraphTool;

        GraphViewEditorWindow GraphEditorWindow => containerWindow as GraphViewEditorWindow;

        /// <inheritdoc />
        public override IEnumerable<string> toolbarElements
        {
            get
            {
                var window = GraphEditorWindow;
                if (window != null)
                {
                    return window.GetToolbarDefinition(this)?.ElementIds;
                }

                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Adds a stylesheet to the toolbar root visual element.
        /// </summary>
        /// <param name="stylesheet">The stylesheet to add.</param>
        /// <remarks>
        /// 'AddStylesheet' adds a stylesheet to the toolbar's root visual element, which enables custom styling of the toolbar.
        /// By adding a stylesheet, you can apply Unity Style Sheet (USS) styles to the visual elements, so the
        /// toolbar adheres to the desired design and layout specifications.
        /// </remarks>
        protected void AddStylesheet(StyleSheet stylesheet)
        {
            rootVisualElement.styleSheets.Add(stylesheet);
        }
    }
}
