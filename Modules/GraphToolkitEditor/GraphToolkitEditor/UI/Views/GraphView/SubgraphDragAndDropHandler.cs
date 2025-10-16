// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Handles drag and drop of graph objects into the graph. Creates subgraph nodes that reference the dropped graph assets.
    /// </summary>
    [UnityRestricted]
    internal class SubgraphDragAndDropHandler : IDragAndDropHandler
    {
        readonly List<GraphObject> m_DraggedGraphAssets = new();

        /// <summary>
        /// The view receiving the dragged graph assets.
        /// </summary>
        protected GraphView GraphView { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubgraphDragAndDropHandler"/> class.
        /// </summary>
        /// <param name="graphView">The view receiving the dragged graph assets.</param>
        public SubgraphDragAndDropHandler(GraphView graphView)
        {
            GraphView = graphView;
        }

        /// <inheritdoc />
        public virtual bool CanHandleDrop()
        {
            var objectReferences = DragAndDrop.objectReferences;
            if (objectReferences == null || objectReferences.Length == 0)
                return false;

            for (var i = 0; i < objectReferences.Length; i++)
            {
                if (!TryGetDraggedGraphObject(objectReferences[i], out _))
                    return false;
            }

            return true;
        }

        /// <inheritdoc />
        public virtual void OnDragEnter(DragEnterEvent evt)
        {
            m_DraggedGraphAssets.Clear();
            var objectReferences = DragAndDrop.objectReferences;
            for (var i = 0; i < objectReferences.Length; i++)
            {
                if (TryGetDraggedGraphObject(objectReferences[i], out var graphObject))
                    m_DraggedGraphAssets.Add(graphObject);
            }
        }

        /// <inheritdoc />
        public virtual void OnDragLeave(DragLeaveEvent evt)
        {
        }

        /// <inheritdoc />
        public virtual void OnDragUpdated(DragUpdatedEvent evt)
        {
            DragAndDrop.visualMode = m_DraggedGraphAssets.Count > 0 ? DragAndDropVisualMode.Move : DragAndDropVisualMode.Rejected;
            evt.StopPropagation();
        }

        /// <inheritdoc />
        public virtual void OnDragPerform(DragPerformEvent evt)
        {
            if (m_DraggedGraphAssets.Count > 0)
            {
                for (var i = 0; i < m_DraggedGraphAssets.Count; i++)
                    GraphView.Dispatch(new CreateSubgraphNodeFromExistingGraphCommand(m_DraggedGraphAssets[i],
                        GraphView.ContentViewContainer.WorldToLocal(evt.mousePosition)));
            }
            m_DraggedGraphAssets.Clear();
            evt.StopPropagation();
        }

        /// <inheritdoc />
        public virtual void OnDragExited(DragExitedEvent evt)
        {
            m_DraggedGraphAssets.Clear();
        }

        bool TryGetDraggedGraphObject(Object objectRef, out GraphObject graphObject)
        {
            graphObject = objectRef as GraphObject;

            if (graphObject == null)
                graphObject = GraphObject.LoadGraphObjectCopyAtPathAndForget(AssetDatabase.GetAssetPath(objectRef), typeof(GraphObject));

            return graphObject != null && graphObject.GraphModel.CanBeDroppedInOtherGraph(GraphView.GraphModel);
        }
    }
}
