// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for overlay toolbars.
    /// </summary>
    class OverlayToolbar : Overlay, ICreateToolbar
    {
        /// <summary>
        /// The graph tool.
        /// </summary>
        protected BaseGraphTool GraphTool => (containerWindow as GraphViewEditorWindow)?.GraphTool;

        /// <inheritdoc />
        public virtual IEnumerable<string> toolbarElements => GraphTool?.GetToolbarProvider(this)?.GetElementIds() ?? Enumerable.Empty<string>();

        /// <inheritdoc />
        public override VisualElement CreatePanelContent()
        {
            return new EditorToolbar(toolbarElements, containerWindow).rootVisualElement;
        }

        /// <summary>
        /// Adds a stylesheet to the toolbar root visual element.
        /// </summary>
        /// <param name="stylesheet"></param>
        protected void AddStylesheet(StyleSheet stylesheet)
        {
            rootVisualElement.styleSheets.Add(stylesheet);
        }
    }
}
