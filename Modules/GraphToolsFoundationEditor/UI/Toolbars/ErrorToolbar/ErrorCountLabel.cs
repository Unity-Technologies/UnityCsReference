// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Toolbar element that displays the number of errors in the graph.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    class ErrorCountLabel : Label, IAccessContainerWindow, IToolbarElement
    {
        public const string id = "GTF/Error/Count";

        ErrorToolbarUpdateObserver m_UpdateObserver;

        /// <inheritdoc />
        public EditorWindow containerWindow { get; set; }

        /// <summary>
        /// The graph tool.
        /// </summary>
        protected BaseGraphTool GraphTool => (containerWindow as GraphViewEditorWindow)?.GraphTool;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorCountLabel"/> class.
        /// </summary>
        protected ErrorCountLabel()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        /// <summary>
        /// Event handler for <see cref="AttachToPanelEvent"/>.
        /// </summary>
        /// <param name="evt">The event to handle.</param>
        protected void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            if (GraphTool != null)
            {
                if (m_UpdateObserver == null)
                {
                    m_UpdateObserver = new ErrorToolbarUpdateObserver(this, GraphTool.ToolState, GraphTool.GraphProcessingState);
                    GraphTool.ObserverManager?.RegisterObserver(m_UpdateObserver);
                }
            }

            Update();
        }

        /// <summary>
        /// Event handler for <see cref="DetachFromPanelEvent"/>.
        /// </summary>
        /// <param name="evt">The event to handle.</param>
        protected void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            GraphTool?.ObserverManager?.UnregisterObserver(m_UpdateObserver);
            m_UpdateObserver = null;
        }

        /// <inheritdoc />
        public virtual void Update()
        {
            var errorCount = GraphTool?.GraphProcessingState.Errors.Count ?? 0;
            text = errorCount == 1 ? $"{errorCount} error" : $"{errorCount} errors";
        }
    }
}
