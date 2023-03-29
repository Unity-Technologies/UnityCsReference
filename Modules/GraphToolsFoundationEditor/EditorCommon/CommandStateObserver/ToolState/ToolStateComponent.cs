// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.CommandStateObserver;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The state component of the <see cref="BaseGraphTool"/>.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Editor")]
    class ToolStateComponent : PersistedStateComponent<ToolStateComponent.StateUpdater>
    {
        /// <summary>
        /// The updater for the <see cref="ToolStateComponent"/>.
        /// </summary>
        public class StateUpdater : BaseUpdater<ToolStateComponent>
        {
            /// <summary>
            /// Loads a graph.
            /// </summary>
            /// <param name="graph">The graph to load.</param>
            /// <param name="boundObject">The GameObject to which the graph is bound, if any.</param>
            public void LoadGraph(GraphModel graph, GameObject boundObject)
            {
                if (!string.IsNullOrEmpty(m_State.m_CurrentGraph.GetGraphAssetPath()))
                    m_State.m_LastOpenedGraph = m_State.m_CurrentGraph;

                m_State.m_CurrentGraph = new OpenedGraph(graph, boundObject);
                m_State.m_LastOpenedGraph = m_State.m_CurrentGraph;
                m_State.m_BlackboardGraphModel = null;
                m_State.SetUpdateType(UpdateType.Complete);
            }

            /// <summary>
            /// Pushes the currently opened graph onto the graph history stack.
            /// </summary>
            public void PushCurrentGraph()
            {
                m_State.m_SubGraphStack.Add(m_State.m_CurrentGraph);
                m_State.SetUpdateType(UpdateType.Complete);
            }

            /// <summary>
            /// Removes the most recent <paramref name="length"/> elements from the graph history stack..
            /// </summary>
            /// <param name="length">The number of elements to remove.</param>
            public void TruncateHistory(int length)
            {
                m_State.m_SubGraphStack.RemoveRange(length, m_State.m_SubGraphStack.Count - length);
                m_State.SetUpdateType(UpdateType.Complete);
            }

            /// <summary>
            /// Empties the graph history stack.
            /// </summary>
            public void ClearHistory()
            {
                m_State.m_SubGraphStack.Clear();
                m_State.SetUpdateType(UpdateType.Complete);
            }
        }

        [SerializeField]
        OpenedGraph m_CurrentGraph;

        [SerializeField]
        OpenedGraph m_LastOpenedGraph;

        [SerializeField]
        List<OpenedGraph> m_SubGraphStack;

        [NonSerialized]
        BlackboardGraphModel m_BlackboardGraphModel;

        /// <summary>
        /// The currently opened <see cref="GraphModel"/>.
        /// </summary>
        public GraphModel GraphModel => CurrentGraph.GetGraphModel();

        /// <summary>
        /// The <see cref="BlackboardGraphModel"/> for the <see cref="GraphModel"/>.
        /// </summary>
        public BlackboardGraphModel BlackboardGraphModel
        {
            get
            {
                // m_BlackboardGraphModel will be null after unserialize (open, undo) and LoadGraph.
                if (m_BlackboardGraphModel == null)
                {
                    m_BlackboardGraphModel = GraphModel?.Stencil?.CreateBlackboardGraphModel(GraphModel);
                }
                return m_BlackboardGraphModel;
            }
        }

        /// <summary>
        /// The currently opened graph.
        /// </summary>
        public OpenedGraph CurrentGraph => m_CurrentGraph;

        /// <summary>
        /// The previously opened graph.
        /// </summary>
        public OpenedGraph LastOpenedGraph => m_LastOpenedGraph;

        /// <summary>
        /// A stack containing the history of opened graph.
        /// </summary>
        public IReadOnlyList<OpenedGraph> SubGraphStack => m_SubGraphStack;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolStateComponent" /> class.
        /// </summary>
        public ToolStateComponent()
        {
            m_SubGraphStack = new List<OpenedGraph>();
        }

        /// <inheritdoc />
        protected override void Move(IStateComponent other, IChangeset changeset)
        {
            base.Move(other, changeset);

            if (other is ToolStateComponent toolStateComponent)
            {
                if (!m_CurrentGraph.Equals(toolStateComponent.m_CurrentGraph) ||
                    !m_LastOpenedGraph.Equals(toolStateComponent.m_LastOpenedGraph) ||
                    !m_SubGraphStack.ListEquals(toolStateComponent.m_SubGraphStack))
                {
                    m_CurrentGraph = toolStateComponent.m_CurrentGraph;
                    m_LastOpenedGraph = toolStateComponent.m_LastOpenedGraph;
                    m_SubGraphStack = toolStateComponent.m_SubGraphStack;
                    m_BlackboardGraphModel = null;

                    SetUpdateType(UpdateType.Complete);
                }

                toolStateComponent.m_CurrentGraph = default;
                toolStateComponent.m_LastOpenedGraph = default;
                toolStateComponent.m_BlackboardGraphModel = null;
            }
        }

        /// <inheritdoc />
        public override void UndoRedoPerformed(bool isRedo)
        {
            base.UndoRedoPerformed(isRedo);

            // Check that all referenced graphs still exist (assets may have been deleted).
            if (!m_CurrentGraph.IsValid())
            {
                m_CurrentGraph = new OpenedGraph(null, null);
            }

            if (!m_LastOpenedGraph.IsValid())
            {
                m_LastOpenedGraph = new OpenedGraph(null, null);
            }

            for (var i = m_SubGraphStack.Count - 1; i >= 0; i--)
            {
                if (!m_SubGraphStack[i].IsValid())
                {
                    m_SubGraphStack.RemoveAt(i);
                }
            }
        }
    }
}
