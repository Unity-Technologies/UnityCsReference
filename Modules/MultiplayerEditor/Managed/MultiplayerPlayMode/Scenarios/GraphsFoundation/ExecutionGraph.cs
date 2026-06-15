// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
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
            [SerializeReference] public List<ExecutionNode> Nodes;
        }

        internal static readonly ExecutionStage[] k_Stages = Enum.GetValues(typeof(ExecutionStage)) as ExecutionStage[];
        internal static readonly int k_StagesCount = k_Stages.Length;

        // All stages except Cleanup and Validate, which have special handling in the execution flow.
        internal static readonly ReadOnlyCollection<ExecutionStage> k_ExecutionStages = Array.AsReadOnly(
        [
            ExecutionStage.Prepare,
            ExecutionStage.Deploy,
            ExecutionStage.Start,
            ExecutionStage.Run,
        ]);

        internal static readonly ReadOnlyCollection<ExecutionStage> k_LaunchingStages = Array.AsReadOnly(
        [
            ExecutionStage.Validate,
            ExecutionStage.Prepare,
            ExecutionStage.Deploy,
            ExecutionStage.Start,
        ]);

        [SerializeField] private StageData[] m_Stages;
        [SerializeField] private bool m_HasStarted;
        [SerializeReference] private List<NodeInput> m_ConnectedInputs;

        private StageData[] Stages => m_Stages;
        internal bool HasStarted => m_HasStarted;
        internal event Action StatusRefreshed;

        internal void Reset()
        {
            // Reset graph state.
            m_HasStarted = false;

            // Reset graph nodes.
            for (int i = 0; i < m_Stages.Length; i++)
            {
                m_Stages[i].State = ExecutionState.Idle;
                foreach (ExecutionNode node in m_Stages[i].Nodes)
                    node.Reset();
            }

            // Reset connection points
            if (m_ConnectedInputs != null)
            {
                foreach (NodeInput connectedInput in m_ConnectedInputs)
                    connectedInput.Reset();
            }
        }

        private void SetupNodeEvents(ExecutionNode node)
        {
            node.StatusRefreshed -= InvokeStatusRefreshed;
            node.StatusRefreshed += InvokeStatusRefreshed;
        }

        void InvokeStatusRefreshed()
        {
            StatusRefreshed?.Invoke();
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
                    Nodes = new List<ExecutionNode>()
                };
            }
        }

        internal ExecutionState GetStageState(ExecutionStage stage) => Stages[(int)stage].State;

        internal ReadOnlyCollection<ExecutionNode> GetNodes(ExecutionStage stage)
        {
            if (m_Stages == null || m_Stages.Length == 0)
                return new List<ExecutionNode>().AsReadOnly();
            return m_Stages[(int)stage].Nodes.AsReadOnly();
        }

        internal List<ExecutionNode> GetAllNodes()
        {
            var allNodes = new List<ExecutionNode>();
            foreach (var stage in m_Stages)
            {
                allNodes.AddRange(stage.Nodes);
            }
            return allNodes;
        }

        internal int GetAllNodesCount()
        {
            int count = 0;
            foreach (var stage in m_Stages)
            {
                count += stage.Nodes.Count;
            }
            return count;
        }

        internal void AddNode(ExecutionNode node, ExecutionStage stage)
        {
            ValidateHasNotStarted();

            // Sanity check against adding nodes with invalid states
            if (node.State != ExecutionState.Idle)
                throw new InvalidOperationException("Trying to add a node that is not in idle state.");

            // Workaround to avoid logging exceptions for nodes in the Validate stage,
            // as those are expected to throw during validation and we don't want to flood the logs with those exceptions.
            if (stage == ExecutionStage.Validate)
                node.LogExceptions = false;

            Stages[(int)stage].Nodes.Add(node);
            SetupNodeEvents(node);
            InvokeStatusRefreshed();
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

        bool PreviousStageCompleted(ExecutionStage stage)
        {
            var previousStage = stage - 1;
            if (previousStage < 0 || stage == ExecutionStage.Cleanup)
                return true;

            return Stages[(int)previousStage].State == ExecutionState.Completed;
        }

        internal async Task<bool> RunOrResumeAsync(ExecutionStage stage,
                                                              CancellationToken cancellationToken, String owner)
        {
            m_HasStarted = true;

            // First check for dependencies
            if (!PreviousStageCompleted(stage))
            {
                throw new InvalidOperationException($"{owner} Cannot execute stage {stage} before stage {stage - 1} is completed.");
            }

            // Grab the current stage's nodes to run and set it State to running.
            Stages[(int)stage].State = ExecutionState.Running;
            InvokeStatusRefreshed();
            var stageNodes = Stages[(int)stage].Nodes;
            List<ExecutionNode> nodesToRun;
            int completed;

            var runningTasks = new List<Task>(stageNodes.Count);
            while ((nodesToRun = GetNextNodesToRun(stageNodes, runningTasks.Count == 0, stage, out completed)).Count > 0 || runningTasks.Count > 0)
            {
                foreach (var node in nodesToRun)
                {
                    switch (node.State)
                    {
                        case ExecutionState.Idle:
                            FlushInputs(node);
                            runningTasks.Add(node.RunAsync(cancellationToken));
                            break;
                        case ExecutionState.Running:
                            runningTasks.Add(node.ResumeAsync(cancellationToken));
                            break;
                    }
                }

                await Task.WhenAny(runningTasks);
                RemoveCompletedTasks(runningTasks);
            }

            // Update the final stage state with the results.
            var state = ComputeStageStateOnCompletion(stageNodes);
            Stages[(int)stage].State = state;
            InvokeStatusRefreshed();

            return state == ExecutionState.Completed;
        }

        static ExecutionState ComputeStageStateOnCompletion(List<ExecutionNode> nodes)
        {
            var count = nodes.Count;
            var idleCount = 0;
            var runningCount = 0;
            var completedCount = 0;
            var failedCount = 0;
            var abortedCount = 0;

            foreach (var node in nodes)
            {
                switch (node.State)
                {
                    case ExecutionState.Idle:
                        idleCount++;
                        break;
                    case ExecutionState.Running:
                        runningCount++;
                        break;
                    case ExecutionState.Completed:
                        completedCount++;
                        break;
                    case ExecutionState.Failed:
                        failedCount++;
                        break;
                    case ExecutionState.Aborted:
                        abortedCount++;
                        break;
                }
            }

            if (failedCount > 0)
                return ExecutionState.Failed;

            if (abortedCount > 0)
                return ExecutionState.Aborted;

            if (completedCount == count)
                return ExecutionState.Completed;

            // This should not happen as it means some nodes are not in a final state after execution, which should not be the case. Log this for investigation.
            Debug.LogAssertion($"Execution stage completed with some nodes not in completed state. Idle: {idleCount}, Running: {runningCount}, Completed: {completedCount}, Failed: {failedCount}, Aborted: {abortedCount}");
            return ExecutionState.Failed;
        }

        static void RemoveCompletedTasks(List<Task> runningTasks)
        {
            for (int i = runningTasks.Count - 1; i >= 0; i--)
            {
                if (runningTasks[i].IsCompleted)
                {
                    runningTasks.RemoveAt(i);
                }
            }
        }

        private List<ExecutionNode> GetNextNodesToRun(IEnumerable<ExecutionNode> candidates, bool allowIsolation, ExecutionStage stage, out int completed)
        {
            var availableNodes = new List<ExecutionNode>();
            var availableIsolationNode = (ExecutionNode)null;
            completed = 0;

            foreach (var candidate in candidates)
            {
                if (candidate.State == ExecutionState.Completed)
                    completed++;

                if (NodeFinishedExecution(candidate))
                    continue;

                if (candidate.State != ExecutionState.Idle && !candidate.Interrupted)
                    continue;

                if (HasAllDependenciesCompleted(candidate) ||
                    (stage is ExecutionStage.Cleanup && HasAllDependenciesCompleted(candidate, ExecutionStage.Cleanup)))
                {
                    if (HasToRunInIsolation(candidate))
                        availableIsolationNode = candidate;
                    else
                        availableNodes.Add(candidate);
                }
            }

            // If there is a node that needs to run in isolation, we run that one first before any other node.
            if (availableIsolationNode != null && allowIsolation)
            {
                availableNodes.Clear();
                availableNodes.Add(availableIsolationNode);
            }

            return availableNodes;
        }

        private static bool NodeFinishedExecution(ExecutionNode node)
        {
            if (node.State == ExecutionState.Completed ||
                node.State == ExecutionState.Failed ||
                node.State == ExecutionState.Aborted)
                return true;

            return false;
        }

        private bool HasAllDependenciesCompleted(ExecutionNode node)
        {
            var inputs = FindConnectedInputs(node);
            return HasAllConnectionsCompleted(inputs);
        }

        private bool HasAllDependenciesCompleted(ExecutionNode node, ExecutionStage stage)
        {
            var inputs = FindConnectedInputs(node);
            inputs = FilterInputsByStage(inputs, stage);
            return HasAllConnectionsCompleted(inputs);
        }

        private IEnumerable<NodeInput> FilterInputsByStage(IEnumerable<NodeInput> inputs, ExecutionStage stage)
        {
            foreach (var input in inputs)
            {
                var dependency = input.GetSource().GetNode();
                if (IsNodeInStage(dependency, stage))
                    yield return input;
            }
        }

        private bool IsNodeInStage(ExecutionNode node, ExecutionStage stage)
        {
            return Stages[(int)stage].Nodes.Contains(node);
        }

        private bool HasAllConnectionsCompleted(IEnumerable<NodeInput> inputs)
        {
            foreach (var input in inputs)
            {
                var output = input.GetSource();
                Assert.IsNotNull(output, "Node input is not connected to any output");

                var dependency = output.GetNode();
                if (dependency.State is not ExecutionState.Completed)
                    return false;
            }

            return true;
        }

        private IEnumerable<NodeInput> FindConnectedInputs(ExecutionNode node)
        {
            if (m_ConnectedInputs == null)
                yield break;

            foreach (var input in m_ConnectedInputs)
            {
                if (input.GetNode() == node)
                    yield return input;
            }
        }

        private void FlushInputs(ExecutionNode node)
        {
            foreach (var input in FindConnectedInputs(node))
                input.Flush();
        }

        private static bool HasToRunInIsolation(ExecutionNode node)
            => CanRequestDomainReload(node);

        private static bool CanRequestDomainReload(ExecutionNode node)
            => node.GetType().IsDefined(typeof(CanRequestDomainReloadAttribute), true);
    }
}
