// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The state component of the <see cref="GraphTool"/>.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class ToolStateComponent : PersistedStateComponent<ToolStateComponent.StateUpdater>
    {
        /// <summary>
        /// The updater for the <see cref="ToolStateComponent"/>.
        /// </summary>
        [UnityRestricted]
        internal class StateUpdater : BaseUpdater<ToolStateComponent>
        {
            /// <summary>
            /// Loads a graph.
            /// </summary>
            /// <param name="graph">The graph to load.</param>
            /// <param name="boundObject">The GameObject to which the graph is bound, if any.</param>
            /// <param name="title">The title associated with the graph to load.</param>
            public void LoadGraph(GraphModel graph, GameObject boundObject, string title = "")
            {
                graph?.GraphObject?.RemoveObsoleteSubgraphAssets();

                if (m_State.m_CurrentGraph.GraphReference != default)
                    m_State.m_LastOpenedGraph = m_State.m_CurrentGraph;

                graph?.UpdateExternalVariableDeclarationReferences();
                m_State.m_CurrentGraph = graph == null ? default : new GraphInfos
                {
                    GraphReference = graph.GetGraphReference(),
                    Label = title,
                    BoundObject = boundObject
                };

                m_State.m_CurrentGraph.GraphModel = graph;
                m_State.m_CurrentGraph.GraphObject = graph?.GraphObject;


                m_State.m_LastOpenedGraph = m_State.m_CurrentGraph;
                m_State.SetUpdateType(UpdateType.Complete);
            }

            /// <summary>
            /// Pushes the currently opened graph onto the graph history stack.
            /// </summary>
            public void PushCurrentGraph()
            {
                m_State.m_SubgraphStack?.Add(m_State.m_CurrentGraph);
                m_State.SetUpdateType(UpdateType.Complete);
            }

            /// <summary>
            /// Removes the most recent <paramref name="length"/> elements from the graph history stack.
            /// </summary>
            /// <param name="length">The number of elements to remove.</param>
            public void TruncateHistory(int length)
            {
                m_State.m_SubgraphStack?.RemoveRange(length, m_State.m_SubgraphStack.Count - length);
                m_State.SetUpdateType(UpdateType.Complete);
            }

            /// <summary>
            /// Empties the graph history stack.
            /// </summary>
            public void ClearHistory()
            {
                m_State.m_SubgraphStack?.Clear();
                m_State.SetUpdateType(UpdateType.Complete);
            }
        }

        [Serializable]
        struct GraphInfos : IEquatable<GraphInfos>
        {
            [SerializeField]
            GraphReference m_GraphReference;

            [SerializeField]
            string m_Label;

            [SerializeField]
            GameObject m_BoundObject;

            /// <summary>
            /// The GameObject bound to this graph.
            /// </summary>
            public GameObject BoundObject
            {
                get => m_BoundObject;
                set => m_BoundObject = value;
            }

            /// <summary>
            /// The graph reference.
            /// </summary>
            public GraphReference GraphReference
            {
                get => m_GraphReference;
                set
                {
                    m_GraphReference = value;
                    GraphObject = null;
                    GraphModel = null;
                }
            }

            /// <summary>
            /// The label.
            /// </summary>
            public string Label
            {
                get => m_Label;
                set => m_Label = value;
            }

            public void Resolve(ToolStateComponent toolStateComponent)
            {
                GraphModel = toolStateComponent.ResolveGraphModelFromReference(GraphReference);
                GraphObject = GraphModel?.GraphObject;
            }

            public GraphObject GraphObject { get; set; }

            public GraphModel GraphModel { get; set; }

            /// <inheritdoc />
            public bool Equals(GraphInfos other)
            {
                return m_GraphReference.Equals(other.m_GraphReference) && m_Label == other.m_Label && m_BoundObject == other.m_BoundObject;
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                return obj is GraphInfos other && Equals(other);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                return HashCode.Combine(m_GraphReference, m_Label, m_BoundObject);
            }

            public static bool operator ==(GraphInfos left, GraphInfos right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(GraphInfos left, GraphInfos right)
            {
                return !left.Equals(right);
            }
        }

        class SubgraphList : IReadOnlyList<GraphReference>
        {
            readonly ToolStateComponent m_Component;

            public SubgraphList(ToolStateComponent component)
            {
                m_Component = component;
            }

            public IEnumerator<GraphReference> GetEnumerator()
            {
                foreach (var subGraph in m_Component.m_SubgraphStack)
                {
                    yield return subGraph.GraphReference;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
            public int Count => m_Component.m_SubgraphStack.Count;

            public GraphReference this[int index] => m_Component.m_SubgraphStack[index].GraphReference;
        }

        [SerializeField]
        GraphInfos m_CurrentGraph;
        [SerializeField]
        GraphInfos m_LastOpenedGraph;
        [SerializeField]
        List<GraphInfos> m_SubgraphStack;

        SubgraphList m_SubgraphList;

        /// <summary>
        /// The currently opened <see cref="GraphModel"/>.
        /// </summary>
        public GraphModel GraphModel
        {
            get
            {
                if (CurrentGraph == default)
                    return null;

                if (m_CurrentGraph.GraphObject == null && !ReferenceEquals(m_CurrentGraph.GraphObject, null))
                {
                    // The graph object existed but was has been deleted.
                    return null;
                }

                return m_CurrentGraph.GraphModel;
            }
        }

        public GraphObject GraphObject => m_CurrentGraph.GraphObject;

        /// <summary>
        /// The currently opened graph.
        /// </summary>
        public GraphReference CurrentGraph => m_CurrentGraph.GraphReference;

        /// <summary>
        /// A stack containing the history of opened graph.
        /// </summary>
        public IReadOnlyList<GraphReference> SubgraphStack => m_SubgraphList;

        /// <summary>
        /// The <see cref="Editor.GraphTool"/> associated with this state component.
        /// </summary>
        public GraphTool GraphTool { get; internal set; }

        /// <summary>
        /// The label of the current graph.
        /// </summary>
        public string CurrentGraphLabel => string.IsNullOrEmpty(m_CurrentGraph.Label) ? GraphModel?.Name : m_CurrentGraph.Label;

        /// <summary>
        /// The bound <see cref="GameObject"/> to the current graph.
        /// </summary>
        public GameObject CurrentGraphBoundObject => m_CurrentGraph.BoundObject;

        /// <summary>
        /// Returns the <see cref="Editor.GraphObject"/> at index <paramref name="index"/> in the <see cref="SubgraphStack"/>.
        /// </summary>
        /// <param name="index">The index in the <see cref="SubgraphStack"/>.</param>
        /// <returns>The <see cref="Editor.GraphObject"/> at index <paramref name="index"/> in the <see cref="SubgraphStack"/>.</returns>
        public GraphObject GetSubGraphObject(int index)
        {
            return m_SubgraphStack[index].GraphObject;
        }

        /// <summary>
        /// Returns the <see cref="GraphModel"/> at index <paramref name="index"/> in the <see cref="SubgraphStack"/>.
        /// </summary>
        /// <param name="index">The index in the <see cref="SubgraphStack"/>.</param>
        /// <returns>The <see cref="GraphModel"/> at index <paramref name="index"/> in the <see cref="SubgraphStack"/>.</returns>
        public GraphModel GetSubGraphModel(int index)
        {
            var graphObject = GetSubGraphObject(index);
            if (m_CurrentGraph.GraphObject == null && !ReferenceEquals(m_CurrentGraph.GraphObject, null))
            {
                return null;
            }

            return m_SubgraphStack[index].GraphModel;
        }

        /// <summary>
        /// Resolves the <see cref="CurrentGraph"/> <see cref="GraphReference"/>. Loading the <see cref="GraphObject"/> if necessary and updating <see cref="ToolStateComponent.GraphModel"/> and <see cref="ToolStateComponent.GraphObject"/>.
        /// </summary>
        /// <returns>The current <see cref="GraphModel"/>, or null if it couldn't be loaded.</returns>
        public GraphModel ResolveGraphModel()
        {
            m_CurrentGraph.Resolve(this);

            return m_CurrentGraph.GraphModel;
        }


        /// <summary>
        /// Resolves the <see cref="SubgraphStack"/> <see cref="GraphReference"/>s. Loading the <see cref="GraphObject"/>s if necessary and updating <see cref="GetSubGraphObject"/> and <see cref="GetSubGraphModel"/>.
        /// </summary>
        public void ResolveSubGraphs()
        {
            for (int i = 0; i < m_SubgraphStack.Count; i++)
            {
                var subGraph = m_SubgraphStack[i];
                subGraph.Resolve(this);
                m_SubgraphStack[i] = subGraph;
            }
        }

        /// <summary>
        /// Resolves the <see cref="SubgraphStack"/> <see cref="GraphReference"/> at the given index. Loading the <see cref="GraphObject"/> if necessary and updating <see cref="GetSubGraphObject"/> and <see cref="GetSubGraphModel"/> for this index.
        /// </summary>
        /// <param name="index">The index in <see cref="SubgraphStack"/>.</param>
        /// <returns>The sub graph at the given index, or null if it couldn't be loaded.</returns>
        public GraphModel ResolveSubGraph(int index)
        {
            var subGraph = m_SubgraphStack[index];
            subGraph.Resolve(this);
            m_SubgraphStack[index] = subGraph;

            return subGraph.GraphModel;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolStateComponent" /> class.
        /// </summary>
        public ToolStateComponent()
        {
            m_SubgraphStack = new List<GraphInfos>();
            m_SubgraphList = new SubgraphList(this);
        }

        /// <summary>
        /// Returns the label of the subgraph at the given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The label of the subgraph at the given index.</returns>
        public string GetSubgraphLabel(int index)
        {
            return m_SubgraphStack[index].Label;
        }

        /// <summary>
        /// Returns the bound <see cref="GameObject"/> of the subgraph at the given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The bound <see cref="GameObject"/> of the subgraph at the given index.</returns>
        public GameObject GetSubgraphBoundObject(int index)
        {
            return m_SubgraphStack[index].BoundObject;
        }

        /// <inheritdoc />
        protected override void Move(IStateComponent other, IChangeset changeset)
        {
            base.Move(other, changeset);

            if (other is ToolStateComponent toolStateComponent)
            {
                if (m_CurrentGraph != default && !m_CurrentGraph.Equals(toolStateComponent.m_CurrentGraph) ||
                    m_LastOpenedGraph != default && !m_LastOpenedGraph.Equals(toolStateComponent.m_LastOpenedGraph) ||
                    m_SubgraphStack != null && !m_SubgraphStack.ListEquals(toolStateComponent.m_SubgraphStack))
                {
                    m_CurrentGraph = toolStateComponent.m_CurrentGraph;
                    m_LastOpenedGraph = toolStateComponent.m_LastOpenedGraph;
                    m_SubgraphStack = toolStateComponent.m_SubgraphStack;

                    SetUpdateType(UpdateType.Complete);
                }

                toolStateComponent.m_CurrentGraph = default;
                toolStateComponent.m_LastOpenedGraph = default;
            }
        }

        /// <inheritdoc />
        public override void UndoRedoPerformed(bool isRedo)
        {
            base.UndoRedoPerformed(isRedo);

            // Check that all referenced graphs still exist (assets may have been deleted).
            ResolveGraphModel();
            if (m_CurrentGraph.GraphObject == null)
            {
                m_CurrentGraph = default;
            }

            if (m_LastOpenedGraph.GraphReference != default && ResolveGraphModelFromReference(m_LastOpenedGraph.GraphReference) == null)
            {
                m_LastOpenedGraph = default;
            }

            if (m_SubgraphStack != null)
            {
                ResolveSubGraphs();
                for (var i = m_SubgraphStack.Count - 1; i >= 0; i--)
                {
                    if (m_SubgraphStack[i].GraphModel == null)
                    {
                        m_SubgraphStack.RemoveAt(i);
                    }
                }
            }
        }

        internal LoadGraphCommand GetLoadLastOpenedGraphCommand()
        {
            var graphModel = ResolveGraphModelFromReference(m_LastOpenedGraph.GraphReference);
            return graphModel != null ? new LoadGraphCommand(graphModel, null, LoadGraphCommand.LoadStrategies.KeepHistory, title: m_LastOpenedGraph.Label) : null;
        }

        GraphReference GetGraphModelReference(GraphModel graphModel)
        {
            return GraphTool != null ? GraphTool.GetGraphModelReference(graphModel) : new GraphReference(graphModel);
        }

        GraphModel ResolveGraphModelFromReference(in GraphReference reference)
        {
            return GraphTool != null ? GraphTool.ResolveGraphModelFromReference(reference) : GraphReference.ResolveGraphModel(reference);
        }

        internal bool HasLastOpenedGraph()
        {
            return m_LastOpenedGraph != default;
        }
    }
}
