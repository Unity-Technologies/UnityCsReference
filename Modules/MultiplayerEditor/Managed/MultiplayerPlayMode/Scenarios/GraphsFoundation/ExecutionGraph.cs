// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.PlayMode.Editor
{
    /// <summary>
    /// An Execution Graph represents the sequence of tasks and required parameters
    /// to be executed on an instance. These tasks are captured in lists of Nodes,
    /// mapped to an ExecutionStage. They can be executed per-stage and are
    /// configured by Scenario Factory.
    /// </summary>
    [Serializable]
    internal class ExecutionGraph : ISerializationCallbackReceiver
    {
        [Serializable]
        private struct StageData
        {
            [SerializeField] public ExecutionState State;
            [SerializeReference] public List<Node> Nodes;
        }

        internal static readonly ExecutionStage[] k_Stages = Enum.GetValues(typeof(ExecutionStage)) as ExecutionStage[];
        internal static readonly int k_StagesCount = k_Stages.Length;

        [SerializeField] private StageData[] m_Stages;
        [SerializeField] private bool m_HasStarted;
        [SerializeReference] private List<NodeInput> m_ConnectedInputs;

        private StageData[] Stages => m_Stages;
        internal bool HasStarted => m_HasStarted;
        internal event Action StatusRefreshed;

        // The results of an executed stage
        internal struct ExecutionResult
        {
            public bool Success;
            public List<Task> MonitoringTasks;
        }

        internal void Reset()
        {
            // Reset graph state.
            m_HasStarted = false;

            // Reset graph nodes.
            for (int i = 0; i < m_Stages.Length; i++)
            {
                m_Stages[i].State = ExecutionState.Idle;
                foreach (Node node in m_Stages[i].Nodes)
                    node.Reset();
            }

            // Reset connection points
            if (m_ConnectedInputs != null)
            {
                foreach (NodeInput connectedInput in m_ConnectedInputs)
                    connectedInput.Reset();
            }
        }

        private void SetupNodeEvents(Node node)
        {
            node.StatusRefreshed -= StatusRefreshed;
            node.StatusRefreshed += StatusRefreshed;
        }

        public void OnBeforeSerialize()
        {
            // No-op
        }

        public void OnAfterDeserialize()
        {
            // Upon deserialization, ensure that we re-attach listeners for all nodes
            foreach (var stage in m_Stages)
                foreach (var node in stage.Nodes)
                    SetupNodeEvents(node);
        }

        public ExecutionGraph()
        {
            // On init, prepare m_stages to be configured with Node data.
            m_Stages = new StageData[k_StagesCount];
            for (int i = 0; i < k_StagesCount; i++)
            {
                m_Stages[i] = new StageData()
                {
                    State = ExecutionState.Idle,
                    Nodes = new List<Node>()
                };
            }
        }

        internal ReadOnlyCollection<Node> GetNodes(ExecutionStage stage)
        {
            if (m_Stages == null || m_Stages.Length == 0)
                return new List<Node>().AsReadOnly();
            return m_Stages[(int)stage].Nodes.AsReadOnly();
        }

        internal List<Node> GetAllNodes()
        {
            var allNodes = new List<Node>();
            allNodes.AddRange(GetNodes(ExecutionStage.Prepare));
            allNodes.AddRange(GetNodes(ExecutionStage.Deploy));
            allNodes.AddRange(GetNodes(ExecutionStage.Run));
            return allNodes;
        }

        internal void AddNode(Node node, ExecutionStage stage)
        {
            ValidateHasNotStarted();

            // Sanity check against adding nodes for invalid stages.
            if (stage == ExecutionStage.None)
                throw new ArgumentException("Stage cannot be None", nameof(stage));

            // Ensure we don't have duplicate nodes
            foreach (var s in m_Stages)
            {
                foreach (var existingNode in s.Nodes)
                {
                    if (existingNode.Name == node.Name)
                        throw new ArgumentException($"Node with the same name already exists [{node.Name}].");
                }
            }

            // Sanity check against adding nodes with invalid states
            if (node.State != ExecutionState.Idle)
                throw new InvalidOperationException("Trying to add a node that is not in idle state.");

            Stages[(int)stage].Nodes.Add(node);
            SetupNodeEvents(node);
            StatusRefreshed?.Invoke();
        }

        private void ValidateHasNotStarted()
        {
            if (m_HasStarted)
                throw new InvalidOperationException("Cannot modify the execution graph after it has started.");
        }

        internal void ConnectConstant<T>(NodeInput<T> input, T value, bool reconnectConstant = false)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            if (!reconnectConstant)
                ValidateHasNotStarted();

            input.SetValue(value);
        }

        internal void Connect<T>(NodeOutput<T> from, NodeInput<T> to)
        {
            if (from is null)
                throw new ArgumentNullException(nameof(from));

            if (to is null)
                throw new ArgumentNullException(nameof(to));

            ValidateHasNotStarted();

            if (GetInputConnectionSlot(to) != -1)
                throw new InvalidOperationException("Input is already connected");

            to.Connect(from);

            if (m_ConnectedInputs == null)
                m_ConnectedInputs = new List<NodeInput>(1);

            m_ConnectedInputs.Add(to);
        }

        private int GetInputConnectionSlot<T>(NodeInput<T> input)
        {
            if (m_ConnectedInputs == null)
                return -1;

            for (int i = 0; i < m_ConnectedInputs.Count; i++)
                if (input == m_ConnectedInputs[i])
                    return i;

            return -1;
        }

        internal async Task<ExecutionResult> RunOrResumeAsync(ExecutionStage stage,
                                                              CancellationToken cancellationToken, String owner)
        {
            m_HasStarted = true;

            // First check for dependencies
            var previousStage = stage - 1;
            if (previousStage > 0)
            {
                if (Stages[(int)previousStage].State != ExecutionState.Completed)
                    throw new InvalidOperationException($"{owner} Cannot execute stage {stage} before stage " +
                                                        $"{previousStage} is completed.");
            }

            // Grab the current stage's nodes to run and set it State to running.
            Stages[(int)stage].State = ExecutionState.Running;
            var stageNodes = Stages[(int)stage].Nodes;
            List<Node> nodesToRun;
            int completed;

            // Start executing nodes and consolidate any resulting monitoring tasks.
            var monitoringTasks = new List<Task>();
            while ((nodesToRun = GetNextNodesToRun(stageNodes, out completed)).Count > 0)
            {
                var tasks = new List<Task>();
                foreach (var node in nodesToRun)
                {
                    switch (node.State)
                    {
                        case ExecutionState.Idle:
                            FlushInputs(node);
                            tasks.Add(node.RunAsync(cancellationToken)
                                          .ContinueWith(t => monitoringTasks.Add(t.Result.MonitoringTask)));
                            break;
                        case ExecutionState.Running:
                        case ExecutionState.Active:
                            tasks.Add(node.ResumeAsync(cancellationToken)
                                          .ContinueWith(t => monitoringTasks.Add(t.Result.MonitoringTask)));
                            break;
                    }
                }

                await Task.WhenAll(tasks);
            }

            // Update the final stage state with the results.
            var success = completed == stageNodes.Count;
            Stages[(int)stage].State = success ? ExecutionState.Completed : ExecutionState.Failed;

            // Finally return the result and monitoring tasks if any.
            return new ExecutionResult
            {
                Success = success,
                MonitoringTasks = monitoringTasks
            };
        }

        private List<Node> GetNextNodesToRun(IEnumerable<Node> candidates, out int completed)
        {
            var availableNodes = new List<Node>();
            var availableIsolationNode = (Node)null;
            completed = 0;

            foreach (var candidate in candidates)
            {
                if (candidate.State == ExecutionState.Completed || candidate.State == ExecutionState.Active)
                    completed++;

                if (NodeFinishedExecution(candidate))
                    continue;

                if (candidate.State != ExecutionState.Idle && !candidate.Interrupted)
                    continue;

                if (HasAllDependenciesCompleted(candidate))
                {
                    if (HasToRunInIsolation(candidate))
                        availableIsolationNode = candidate;
                    else
                        availableNodes.Add(candidate);
                }
            }

            // If there is a node that needs to run in isolation, we run that one first before any other node.
            if (availableIsolationNode != null)
            {
                availableNodes.Clear();
                availableNodes.Add(availableIsolationNode);
            }

            return availableNodes;
        }

        private static bool NodeFinishedExecution(Node node)
        {
            if (node.State == ExecutionState.Completed ||
                node.State == ExecutionState.Failed ||
                node.State == ExecutionState.Aborted)
                return true;

            return false;
        }

        private bool HasAllDependenciesCompleted(Node node)
        {
            var inputs = FindConnectedInputs(node);
            return HasAllConnectionsCompleted(inputs);
        }

        private bool HasAllConnectionsCompleted(IEnumerable<NodeInput> inputs)
        {
            foreach (var input in inputs)
            {
                var output = input.GetSource();
                Assert.IsNotNull(output, "Node input is not connected to any output");

                var dependency = output.GetNode();
                if (dependency.State is not ExecutionState.Completed && dependency.State is not ExecutionState.Active)
                    return false;
            }

            return true;
        }

        private IEnumerable<NodeInput> FindConnectedInputs(Node node)
        {
            if (m_ConnectedInputs == null)
                yield break;

            foreach (var input in m_ConnectedInputs)
            {
                if (input.GetNode() == node)
                    yield return input;
            }
        }

        private void FlushInputs(Node node)
        {
            foreach (var input in FindConnectedInputs(node))
                input.Flush();
        }

        private static bool HasToRunInIsolation(Node node)
            => CanRequestDomainReload(node);

        private static bool CanRequestDomainReload(Node node)
            => node.GetType().IsDefined(typeof(CanRequestDomainReloadAttribute), true);

        private static bool CanRequestDomainReload(List<Node> nodes)
            => nodes.Count == 1 && CanRequestDomainReload(nodes[0]);
    }
}
