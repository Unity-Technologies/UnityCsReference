// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Toolbar element that displays the breadcrumbs and makes it possible to interact with it.
    /// </summary>
    [EditorToolbarElement(id, typeof(GraphViewEditorWindow))]
    [UnityRestricted]
    internal class GraphBreadcrumbs : ToolbarBreadcrumbs, IAccessContainerWindow, IToolbarElement
    {
        public const string id = "GraphToolkit/Breadcrumbs/Breadcrumbs";

        ToolbarUpdateObserver m_UpdateObserver;

        /// <inheritdoc />
        public EditorWindow containerWindow { get; set; }

        /// <summary>
        /// The graph tool.
        /// </summary>
        public GraphTool GraphTool => (containerWindow as GraphViewEditorWindow)?.GraphTool;

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

            GraphTool.ToolState.ResolveGraphModel();
            GraphTool.ToolState.ResolveSubGraphs();
            var i = 0;
            var graphModels = GraphTool.ToolState.SubgraphStack;
            if (graphModels != null)
            {
                for (; i < graphModels.Count; i++)
                {
                    var label = GetBreadcrumbLabel(i);
                    this.CreateOrUpdateItem(i, label, OnBreadcrumbClick);
                }
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
        /// Gets the label for the breadcrumb at <propref name="index"/>.
        /// </summary>
        /// <param name="index">The index of the breadcrumb.</param>
        /// <returns>The label of the breadcrumb.</returns>
        protected virtual string GetBreadcrumbLabel(int index)
        {
            string graphName = null;
            if (index == -1)
            {
                graphName = GraphTool.ToolState.CurrentGraphLabel;

                if (string.IsNullOrEmpty(graphName))
                    graphName = GraphTool.ToolState.GraphModel?.Name;
            }
            else if (index < GraphTool.ToolState.SubgraphStack.Count)
            {
                graphName = GraphTool.ToolState.GetSubgraphLabel(index);
                if (string.IsNullOrEmpty(graphName))
                {
                    graphName = GraphTool.ToolState.GetSubGraphModel(index)?.Name;
                }
            }

            return string.IsNullOrEmpty(graphName) ? "<Unknown>" : graphName;
        }

        /// <summary>
        /// Handles a click on breadcrumb at <propref name="index"/>.
        /// </summary>
        /// <param name="index">The index of the breadcrumb.</param>
        protected virtual void OnBreadcrumbClick(int index)
        {
            if (GraphTool.ToolState.SubgraphStack.Count <= index)
                return;

            var graphModel = GraphTool.ToolState.ResolveSubGraph(index);
            if (graphModel != null)
                GraphTool?.Dispatch(new LoadGraphCommand(graphModel,
                    LoadGraphCommand.LoadStrategies.KeepHistory, index, GraphTool.ToolState.GetSubgraphLabel(index)));
        }
    }
}
