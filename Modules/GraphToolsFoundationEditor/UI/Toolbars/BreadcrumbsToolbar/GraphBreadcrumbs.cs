// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Toolbar element that displays the breadcrumbs and makes it possible to interact with it.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    class GraphBreadcrumbs : ToolbarBreadcrumbs, IAccessContainerWindow, IToolbarElement
    {
        public const string id = "GTF/Breadcrumbs/Breadcrumbs";

        ToolbarUpdateObserver m_UpdateObserver;

        /// <inheritdoc />
        public EditorWindow containerWindow { get; set; }

        /// <summary>
        /// The graph tool.
        /// </summary>
        public BaseGraphTool GraphTool => (containerWindow as GraphViewEditorWindow)?.GraphTool;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphBreadcrumbs"/> class.
        /// </summary>
        protected GraphBreadcrumbs()
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
                    m_UpdateObserver = new ToolbarUpdateObserver(this, GraphTool.ToolState);
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
            bool isEnabled = GraphTool?.ToolState.GraphModel != null;
            if (!isEnabled)
            {
                style.display = DisplayStyle.None;
                return;
            }
            style.display = StyleKeyword.Null;

            var i = 0;
            var graphModels = GraphTool.ToolState.SubGraphStack;
            for (; i < graphModels.Count; i++)
            {
                var label = GetBreadcrumbLabel(i);
                this.CreateOrUpdateItem(i, label, OnBreadcrumbClick);
            }

            var newCurrentGraph = GetBreadcrumbLabel(-1);
            if (newCurrentGraph != null)
            {
                this.CreateOrUpdateItem(i, newCurrentGraph, OnBreadcrumbClick);
                i++;
            }

            this.TrimItems(i);
        }

        /// <summary>
        /// Gets the label for the breadcrumb at <param name="index"></param>.
        /// </summary>
        /// <param name="index">The index of the breadcrumb.</param>
        /// <returns>The label of the breadcrumb.</returns>
        protected virtual string GetBreadcrumbLabel(int index)
        {
            var graphModels = GraphTool.ToolState.SubGraphStack;
            string graphName = null;
            if (index == -1)
            {
                graphName = GraphTool.ToolState.GraphModel.GetFriendlyScriptName();
            }
            else if (index >= 0 && index < graphModels.Count)
            {
                graphName = graphModels[index].GetGraphModel().GetFriendlyScriptName();
            }

            return string.IsNullOrEmpty(graphName) ? "<Unknown>" : graphName;
        }

        /// <summary>
        /// Handles a click on breadcrumb at <see cref="index"/>.
        /// </summary>
        /// <param name="index">The index of the breadcrumb.</param>
        protected virtual void OnBreadcrumbClick(int index)
        {
            OpenedGraph graphToLoad = default;
            var graphModels = GraphTool.ToolState.SubGraphStack;
            if (index < graphModels.Count)
                graphToLoad = graphModels[index];

            if (graphToLoad.GetGraphModel() != null)
                GraphTool?.Dispatch(new LoadGraphCommand(graphToLoad.GetGraphModel(),
                    graphToLoad.BoundObject, LoadGraphCommand.LoadStrategies.KeepHistory, index));
        }
    }
}
