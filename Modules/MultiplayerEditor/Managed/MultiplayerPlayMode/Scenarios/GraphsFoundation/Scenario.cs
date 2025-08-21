// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Unity.PlayMode.Editor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    /// <summary>
    /// A Scenario groups together multiple configured Instances representing virtual players and is built by
    /// the ScenarioFactory. Its main role is to orchestrate and synchronize the Execution Stages of all
    /// associated Instances across Preparation, Deployment, and Running phases. It also notifies
    /// all attached callbacks of Scenario status and completion results.
    /// </summary>
    [Serializable]
    internal class Scenario : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private string m_Name;
        [SerializeField] private ScenarioStatus m_Status;
        [SerializeField] private bool m_HasStarted;
        [SerializeField] private List<Instance> m_Instances = new List<Instance>();

        public string Name => m_Name;
        public ScenarioStatus Status => m_Status;

        // Scenario Callbacks
        internal static event Action<Scenario> Completed;
        internal static event Action<Scenario> ScenarioStarted;
        internal event Action<ScenarioStatus> StatusRefreshed;

        internal static Scenario Create(string name)
        {
            // Create a scenario
            var scenario = CreateInstance<Scenario>();
            scenario.m_Name = name;
            scenario.hideFlags |= HideFlags.DontSave;
            return scenario;
        }

        public void OnBeforeSerialize()
        {
            // Clear out all listeners before Domain Reload Serialization
            foreach (var instance in m_Instances)
                instance.AddInstanceExecutionEventListener(null);
        }

        public void OnAfterDeserialize()
        {
            // Re-attach listeners after Domain Reload Deserialization
            foreach (var instance in m_Instances)
                instance.AddInstanceExecutionEventListener(OnInstanceExecutionUpdate);
        }

        private void OnInstanceExecutionUpdate(Instance instance, Node node)
        {
            if (!instance.IsFreeRunMode())
                RefreshAndNotifyStatus();
        }

        internal void Reset()
        {
            m_HasStarted = false;
            m_Status = ScenarioStatus.Default;

            // Reset only the instances that are controlled by this Scenario.
            foreach (var instance in m_Instances)
            {
                if (!instance.IsFreeRunMode())
                    instance.Reset();
            }
        }

        internal void AddInstance(Instance instance)
        {
            if (m_HasStarted)
                throw new InvalidOperationException("Trying to modify a scenario that has already started.");

            // Don't re-add the instance if we already have it.
            if (m_Instances.Contains(instance))
                return;

            // Add instance and hook up event listeners back to this scenario.
            instance.AddInstanceExecutionEventListener(OnInstanceExecutionUpdate);
            m_Instances.Add(instance);
        }

        internal void RemoveInstance(Instance instance)
        {
            // Sanity check
            if (instance == null)
                return;

            // Remove the given instance and deatch its listeners from this Scenario, if found.
            if (m_Instances.Remove(instance))
            {
                instance.RemoveInstanceExecutionEventListener(OnInstanceExecutionUpdate);
                return;
            }

            Debug.LogWarning($"Scenario: No instance {instance.m_Name} was found to be removed!");
        }

        internal List<Instance> GetAllInstances()
        {
            return m_Instances;
        }

        internal Instance GetInstanceByName(string instanceName, bool targetActiveFreeRun = false)
        {
            foreach (var instance in m_Instances)
            {
                if (instance.m_Name.Equals(instanceName))
                {
                    if (!targetActiveFreeRun || (instance.IsFreeRunMode() && instance.IsActive()))
                        return instance;
                }
            }

            return null;
        }

        internal bool HasActiveFreeRunInstance(string name = null, Type type = null)
        {
            foreach (var instance in m_Instances)
            {
                if (instance.IsFreeRunMode() && instance.HasStartedAsFreeRunning())
                {
                    if (name == null || type == null)
                        return true;

                    var instanceDescriptType = instance.GetInstanceDescription();
                    if (name.Equals(instance.m_Name) &&
                        instanceDescriptType != null &&
                        instanceDescriptType.GetType() == type)
                        return true;
                }
            }

            return false;
        }

        internal bool HasActiveFreeRunInstanceOfType(Type descriptType)
        {
            foreach (var instance in m_Instances)
            {
                InstanceDescription currDescript = instance.GetInstanceDescription();
                if (currDescript == null)
                    continue;

                if (instance.IsFreeRunMode()
                    && instance.HasStartedAsFreeRunning()
                    && currDescript.GetType() == descriptType)
                    return true;
            }

            return false;
        }

        internal List<string> GetActiveFreeRunInstanceNames()
        {
            var activeInstanceNames = new List<string>();
            foreach (var instance in m_Instances)
            {
                if (instance.IsFreeRunMode() && instance.HasStartedAsFreeRunning())
                {
                    activeInstanceNames.Add(instance.m_Name);
                }
            }

            return activeInstanceNames;
        }

        internal void ResumeFreeRunInstances()
        {
            foreach (var instance in m_Instances)
            {
                if (instance.IsFreeRunMode() && instance.IsActive())
                    instance.StartOrResumeAsFreeRunning(true).Forget();
            }
        }

        internal async Task TerminateAllFreeRunningInstancesAsync()
        {
            // Go through and cancel all Free Running Instances
            foreach (var instance in m_Instances)
            {
                if (instance.IsFreeRunMode() && instance.IsActive())
                    instance.StopAsFreeRunning();
            }

            // Grab the monitors so that we can await and return when
            // they have all successfully stopped.
            var allInstanceMonitors = new List<Task>();
            foreach (var instance in m_Instances)
            {
                if (!instance.IsFreeRunMode())
                    continue;

                allInstanceMonitors.AddRange(instance.GetCurrentMonitoringTasksForScenario());
            }

            await Task.WhenAll(allInstanceMonitors);
        }

        internal async Task RunOrResumeAsync(CancellationToken cancellationToken)
        {
            RefreshStatus();

            var state = Status.State;
            if (state != ScenarioState.Idle && state != ScenarioState.Running)
                throw new InvalidOperationException($"Cannot run or resume a scenario that is not in the idle or running state ({state}).");

            if (!m_HasStarted)
            {
                m_HasStarted = true;
                ScenarioStarted?.Invoke(this);
            }

            // Orchestrate the Scenario across Execution Stages.
            var executionStages = new Queue<ExecutionStage>(new ExecutionStage[]
            {
                ExecutionStage.Prepare,
                ExecutionStage.Deploy,
                ExecutionStage.Run,
            });

            // Now go through each State at a time
            while (executionStages.Count > 0)
            {
                var currentStage = executionStages.Dequeue();
                var allInstanceTaskForStage = new List<Task<ExecutionGraph.ExecutionResult>>();

                // For each state, execute on all instances.
                foreach (var instance in m_Instances)
                {
                    if (instance.IsFreeRunMode())
                        continue;

                    var instanceTask = instance.RunOrResumeAsync(currentStage, cancellationToken);
                    allInstanceTaskForStage.Add(instanceTask);
                }

                await Task.WhenAll(allInstanceTaskForStage);

                bool success = true;
                foreach (var result in allInstanceTaskForStage)
                    success = result.Result.Success & success;

                if (!success)
                    break;

            }
            // Send analytics for active instances and notify listeners upon completion.
            SendOnPlayModeEnteredFromScenarioEvent();
            Completed?.Invoke(this);

            // At this point, all instances ran successfully. Now simply monitor the ExecutionStage until it is complete.
            await MonitorAllInstances();

            // This will make sure that the status will be updated after the last ExecutionStage is finished
            // even in the case where the scenario has no nodes.
            RefreshAndNotifyStatus();
        }

        // TODO MTT-10016 This is part of the Migration - Remove this in favor of a single Monitoring Task at Instance Level
        private async Task MonitorAllInstances()
        {
            var allInstanceMonitors = new List<Task>();
            foreach (var instance in m_Instances)
            {
                if (instance.IsFreeRunMode())
                    continue;

                allInstanceMonitors.AddRange(instance.GetCurrentMonitoringTasksForScenario());
            }

            await Task.WhenAll(allInstanceMonitors);
        }

        internal ReadOnlyCollection<Node> GetNodes(ExecutionStage executionStage)
        {
            var nodes = new List<Node>();
            foreach (var instance in m_Instances)
            {
                var graph = instance.GetExecutionGraph();
                var instanceNodes = graph.GetNodes(executionStage);
                nodes.AddRange(instanceNodes);
            }

            return nodes.AsReadOnly();
        }

        private void RefreshAndNotifyStatus()
        {
            RefreshStatus();
            StatusRefreshed?.Invoke(Status);
        }

        private void RefreshStatus()
        {
            if (!m_HasStarted)
            {
                m_Status = ScenarioStatus.Default;
                return;
            }

            // Iterate through all the Execution Stages of all Instance graphs
            // within a Scenario to get its full status.
            var mStages = Enum.GetValues(typeof(ExecutionStage));
            var totalNodes = 0;
            var progressSum = 0.0f;
            var idleNodes = 0;
            var runningNodes = 0;
            var completedNodes = 0;
            var activeNodes = 0;
            var failedNodes = 0;
            var abortedNodes = 0;
            var currentStageState = ExecutionState.Invalid;
            var currentStage = ExecutionStage.None;
            var errors = new List<Node.Error>();
            var nodeStatuses = new List<NodeStatus>();
            var stageStates = new ExecutionState[mStages.Length];
            var stageProgress = new float[mStages.Length];
            var stagesCount = mStages.Length;

            if (m_Instances.Count == 0)
            {
                m_Status = ScenarioStatus.Invalid;
                return;
            }

            for (var i = stagesCount - 1; i >= 0; i--)
            {
                var targetExecutionStage = (ExecutionStage)mStages.GetValue(i);
                int stageTotalInstanceNodes = 0;
                float stageProgressSum = 0;
                int stageIdleNodes = 0;
                int stageRunningNodes = 0;
                int stageCompletedNodes = 0;
                int stageActiveNodes = 0;
                int stageFailedNodes = 0;
                int stageAbortedNodes = 0;
                ExecutionState stageState = ExecutionState.Idle;

                foreach (var instance in m_Instances)
                {
                    if (instance.IsFreeRunMode())
                        continue;

                    instance.ComputeStageState(
                        targetExecutionStage,
                        out var instanceStageTotalInstanceNodes,
                        out var instanceStageState,
                        progressSum: out var instanceStageProgressSum,
                        idleNodes: out var instanceStageIdleNodes,
                        runningNodes: out var instanceStageRunningNodes,
                        completedNodes: out var instanceStageCompletedNodes,
                        activeNodes: out var instanceStageActiveNodes,
                        failedNodes: out var instanceStageFailedNodes,
                        abortedNodes: out var instanceStageAbortedNodes,
                        errors: ref errors,
                        nodeStatuses: ref nodeStatuses
                    );

                    stageTotalInstanceNodes += instanceStageTotalInstanceNodes;
                    stageProgressSum += instanceStageProgressSum;
                    stageIdleNodes += instanceStageIdleNodes;
                    stageRunningNodes += instanceStageRunningNodes;
                    stageCompletedNodes += instanceStageCompletedNodes;
                    stageActiveNodes += instanceStageActiveNodes;
                    stageFailedNodes += instanceStageFailedNodes;
                    stageAbortedNodes += instanceStageAbortedNodes;
                }

                progressSum += stageProgressSum;
                idleNodes += stageIdleNodes;
                runningNodes += stageRunningNodes;
                completedNodes += stageCompletedNodes;
                activeNodes += stageActiveNodes;
                failedNodes += stageFailedNodes;
                abortedNodes += stageAbortedNodes;

                totalNodes += stageTotalInstanceNodes;

                // For a scenario, examine the accumulation of all nodes from all
                // the instances within it and infer the current Execution state at the
                // scenario level.
                if (stageFailedNodes > 0)
                    stageState = ExecutionState.Failed;
                else if (stageAbortedNodes > 0)
                    stageState = ExecutionState.Aborted;
                else if (stageRunningNodes > 0)
                    stageState = ExecutionState.Running;
                else if (stageActiveNodes > 0)
                    stageState = ExecutionState.Active;
                else if (stageIdleNodes > 0)
                    stageState = ExecutionState.Idle;
                else if (stageCompletedNodes == stageTotalInstanceNodes)
                    stageState = ExecutionState.Completed;
                stageStates[i] = stageState;

                // For a scenario, calculate all the nodes for that stage and extrapolate progress.
                stageProgress[i] = stageTotalInstanceNodes == 0 ? 1 : stageProgressSum / stageTotalInstanceNodes;
                if ((stageState != ExecutionState.Completed && stageState != ExecutionState.Active) || i == stagesCount - 1)
                {
                    currentStage = (ExecutionStage)i;
                    currentStageState = stageState;
                }
            }

            // Now with all accumulated per-stage data, construct the current scenario state
            var progress = progressSum / totalNodes;
            ScenarioState scenarioState;

            if (idleNodes == totalNodes && totalNodes > 0)
                scenarioState = ScenarioState.Idle;
            else if (failedNodes > 0)
                scenarioState = ScenarioState.Failed;
            else if (abortedNodes > 0)
                scenarioState = ScenarioState.Aborted;
            else if (completedNodes == totalNodes)
                scenarioState = ScenarioState.Completed;
            else
                scenarioState = ScenarioState.Running;

            m_Status = new ScenarioStatus
            {
                State = scenarioState,
                CurrentStage = currentStage,
                StageState = currentStageState,
                TotalProgress = progress,
                StageProgress = stageProgress,
                Errors = errors,
                NodeStateReports = nodeStatuses,
                StageStates = stageStates
            };
        }

        private void SendOnPlayModeEnteredFromScenarioEvent()
        {
            var nodeTimeDataList = new List<NodeTimeData>();
            foreach (var nodeStatus in m_Status.NodeStateReports)
            {
                nodeTimeDataList.Add(nodeStatus.TimeData);
            }
            TryGetLaunchingDuration(out var durationMs, nodeTimeDataList);
            var instances = GetAnalyticsInstancesData();
            var errors = GetErrorInfoData();
            AnalyticsOnPlayFromScenarioEvent.Send(new OnPlayFromScenarioData()
            {
                Instances = instances.ToArray(),
                ScenarioState = m_Status.State.ToString(),
                ScenarioLaunchingDurationMs = durationMs,
                Errors = errors.ToArray()
            });
        }

        private List<ErrorData> GetErrorInfoData()
        {
            var result = new List<ErrorData>();
            var errors = Status.Errors;

            foreach (var error in errors)
            {
                var cleanedStackTrace = PreprocessStackTraceToList(error.StackTrace);
                result.Add(new ErrorData()
                {
                    FailureNode = error.FailureNode,
                    ExceptionType = error.ExceptionType,
                    Message = error.Message,
                    StackTrace = cleanedStackTrace
                });
            }
            return result;
        }

        // Preprocess the StackTrace for analytics ErrorData
        private string PreprocessStackTraceToList(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return stackTrace;

            // Match each StackTrace entry that typically starts with "at "
            var matches = Regex.Matches(stackTrace, @"^\s*at .+", RegexOptions.Multiline);

            var entries = new List<string>();
            for (int i = 0; i < matches.Count && i < 10; i++)
            {
                // Clean up the user path before "Packages/" to preserve privacy
                string entry = Regex.Replace(matches[i].Value, @"in .*?Packages/", "in Packages/");
                // Clean up any email address to preserve privacy
                entry = Regex.Replace(entry, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", "***@***.com");

                entries.Add(entry);
            }

            // Join the selected entries into a single string separated by newlines
            return string.Join("\n", entries);
        }

        private List<InstanceData> GetAnalyticsInstancesData()
        {
            var result = new List<InstanceData>();
            var instances = GetAllInstances();

            foreach (var instance in instances)
            {
                result.Add(instance.GetAnalyticsData());
            }
            return result;
        }

        // Get launching duration for scenario
        // Retrieve all nodes from the scenario and calculate the time difference between the earliest start time and the latest end time
        private bool TryGetLaunchingDuration(out long durationMS, List<NodeTimeData> nodesTimeData)
        {
            var hasEnded = true;
            DateTime? startTime = null;
            DateTime? endTime = null;

            foreach (var timeData in nodesTimeData)
            {
                if (timeData.HasEnded)
                {
                    endTime = endTime == null || timeData.EndTime > endTime ? timeData.EndTime : endTime;
                }
                else
                {
                    hasEnded = false;
                }
                if (timeData.HasStarted)
                {
                    startTime = startTime == null || timeData.StartTime < startTime ? timeData.StartTime : startTime;
                }
            }
            if (startTime.HasValue && endTime.HasValue)
            {
                durationMS = (long)Math.Round((endTime.Value - startTime.Value).TotalMilliseconds);
                return hasEnded;
            }
            durationMS = 0;
            return false;
        }
    }
}
