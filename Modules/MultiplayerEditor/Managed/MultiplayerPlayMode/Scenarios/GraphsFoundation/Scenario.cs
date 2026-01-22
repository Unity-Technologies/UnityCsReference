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
    internal class Scenario : ScriptableObject
    {
        [SerializeField] private ScenarioStatusData m_StatusData;
        [SerializeField] private bool m_HasStarted;
        [SerializeField] private List<Instance> m_Instances = new List<Instance>();

        public ScenarioStatusData StatusData => m_StatusData;

        // Scenario Callbacks
        internal static event Action<Scenario> Completed;
        internal static event Action<Scenario> ScenarioStarted;
        internal event Action<ScenarioStatusData> StatusRefreshed;

        internal static Scenario Create(string name)
        {
            // Create a scenario
            var scenario = CreateInstance<Scenario>();
            scenario.name = name;
            OrchestratedScenario.PreventScriptableObjectUnload(scenario);
            return scenario;
        }

        void OnEnable()
        {
            // Re-attach listeners after Domain Reload
            foreach (var instance in m_Instances)
            {
                instance.StatusRefreshed -= OnInstanceStatusRefreshed;
                instance.StatusRefreshed += OnInstanceStatusRefreshed;
            }
        }

        private void OnInstanceStatusRefreshed(Instance instance, InstanceStatusData status)
        {
            if (!instance.IsFreeRunMode())
                RefreshAndNotifyStatus();
        }

        internal void Reset()
        {
            m_HasStarted = false;
            m_StatusData.Clear();

            // Reset only the instances that are controlled by this Scenario.
            foreach (var instance in m_Instances)
            {
                if (!instance.IsFreeRunMode())
                    instance.Reset();
            }
        }

        private void ResetAfterCancellation()
        {
            // After a cancellation, instances that failed should remain in their failed state
            // so users can see the failure result. While instances that were running or completed
            // should be reset to the idle state.
            foreach (var instance in m_Instances)
            {
                if (instance.IsFreeRunMode())
                    continue;
                
                if (instance.StatusData.OverallStatus.State is not ExecutionState.Failed)
                {
                    instance.Reset();
                }
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
            instance.StatusRefreshed += OnInstanceStatusRefreshed;
            m_Instances.Add(instance);

            RefreshAndNotifyStatus();
        }

        internal void RemoveInstance(Instance instance)
        {
            // Sanity check
            if (instance == null)
                return;

            // Remove the given instance and deatch its listeners from this Scenario, if found.
            if (m_Instances.Remove(instance))
            {
                instance.StatusRefreshed -= OnInstanceStatusRefreshed;
                return;
            }

            Debug.LogWarning($"Scenario: No instance {instance.Name} was found to be removed!");
        }

        internal List<Instance> GetAllInstances()
        {
            return m_Instances;
        }

        internal Instance GetInstanceByName(string instanceName, bool targetActiveFreeRun = false)
        {
            foreach (var instance in m_Instances)
            {
                if (instance.Name.Equals(instanceName))
                {
                    if (!targetActiveFreeRun || (instance.IsFreeRunMode() && instance.IsActive()))
                        return instance;
                }
            }

            return null;
        }

        internal bool HasActiveFreeRunInstance()
        {
            foreach (var instance in m_Instances)
            {
                if (instance.IsFreeRunMode() && instance.HasStartedAsFreeRunning())
                {
                    return true;
                }
            }

            return false;
        }

        internal bool HasActiveFreeRunInstance<TController>(string name)
            where TController : InstanceController
        {
            foreach (var instance in m_Instances)
            {
                if (instance.IsFreeRunMode() && instance.HasStartedAsFreeRunning())
                {
                    if (name.Equals(instance.Name) && instance.Controller is TController)
                        return true;
                }
            }

            return false;
        }

        internal bool HasActiveFreeRunInstanceOfType<TController>()
            where TController : InstanceController
        {
            foreach (var instance in m_Instances)
            {
                if (instance.IsFreeRunMode()
                    && instance.HasStartedAsFreeRunning()
                    && instance.Controller is TController)
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
                    activeInstanceNames.Add(instance.Name);
                }
            }

            return activeInstanceNames;
        }

        internal struct ValidationResult
        {
            public bool IsValid;
            public string Message;

            public ValidationResult(bool isValid, string message)
            {
                IsValid = isValid;
                Message = message;
            }
        }

        /// <summary>
        /// Validates each instance sequentially to ensure it is ready to run
        /// Stops and returns immediately if any instance fails validation checks
        /// </summary>
        internal async Task<ValidationResult> ValidateForRunningAsync(CancellationToken cancellationToken)
        {
            foreach (var instance in m_Instances)
            {
                // Skip validation for Free Run instances from scenario
                if (instance.IsFreeRunMode())
                {
                    continue;
                }

                var result = await instance.ValidateForRunningAsync(cancellationToken);
                if (!result.IsValid)
                {
                    return result;
                }
            }

            return new ValidationResult(true, string.Empty);
        }

        internal void ResumeFreeRunInstances()
        {
            foreach (var instance in m_Instances)
            {
                if (instance.IsFreeRunMode() && instance.IsActive())
                    instance.StartOrResumeAsFreeRunning(true).Forget();
            }
        }

        internal void NotifyDrift()
        {
            // If the scenario is deploying, it is in a state of flux
            // and thus avoid drift notifications while in this state.
            if (StatusData.OverallStatus.State == ExecutionState.Running &&
                (StatusData.CurrentStage != ExecutionStage.Run ||
                 StatusData.CurrentStageState != ExecutionState.Active))
                return;

            // Only Perform Drift detection for active free running instances.
            foreach (var instance in m_Instances)
            {
                if (!instance.IsFreeRunMode() || !instance.IsActive())
                    continue;

                var isClone = instance.Controller is CloneEditorController;
                if (!isClone && instance.HasDeployedAndRun())
                    instance.Drifted = true;
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

            var state = StatusData.OverallStatus.State;
            if (state != ExecutionState.Idle && state != ExecutionState.Running)
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

            if (cancellationToken.IsCancellationRequested)
                ResetAfterCancellation();

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
            StatusRefreshed?.Invoke(StatusData);
        }

        private void RefreshStatus()
        {
            m_StatusData.Clear();

            foreach (var instance in m_Instances)
            {
                if (instance.IsFreeRunMode())
                    continue;

                var instanceStatus = instance.StatusData;

                if (instanceStatus.OverallStatus.State == ExecutionState.Invalid)
                    continue;

                m_StatusData.OverallStatus.Aggregate(instanceStatus.OverallStatus);

                foreach (var stage in ExecutionGraph.k_Stages)
                {
                    m_StatusData.StageStatuses[(int)stage].Aggregate(instanceStatus.StageStatuses[(int)stage]);

                    if (m_StatusData.StageStatuses[(int)stage].IdleNodesCount < m_StatusData.StageStatuses[(int)stage].NodesCount)
                        m_StatusData.CurrentStage = stage;
                }
            }
        }

        internal IEnumerable<Node.Error> GetAllNonFreeRunNodeErrors()
        {
            foreach (var instance in m_Instances)
            {
                if (instance.IsFreeRunMode())
                    continue;

                foreach (var node in instance.GetExecutionGraph().GetAllNodes())
                {
                    if (node.ErrorInfo != null)
                        yield return node.ErrorInfo;
                }
            }
        }

        private IEnumerable<NodeTimeData> GetAllNonFreeRunNodesTimeData()
        {
            foreach (var instance in m_Instances)
            {
                if (instance.IsFreeRunMode())
                    continue;

                var graph = instance.GetExecutionGraph().GetAllNodes();
                foreach (var node in graph)
                {
                    yield return node.TimeData;
                }
            }
        }

        private void SendOnPlayModeEnteredFromScenarioEvent()
        {
            TryGetLaunchingDuration(out var durationMs, GetAllNonFreeRunNodesTimeData());
            var instances = GetAnalyticsInstancesData();
            var errors = GetErrorInfoData();

            var state = m_StatusData.OverallStatus.State;
            // TODO: Active state is considered as Running. The active state will likely be removed.
            if (state == ExecutionState.Active)
                state = ExecutionState.Running;

            AnalyticsOnPlayFromScenarioEvent.Send(new OnPlayFromScenarioData()
            {
                Instances = instances.ToArray(),
                ScenarioState = state.ToString(),
                ScenarioLaunchingDurationMs = durationMs,
                Errors = errors.ToArray()
            });
        }

        private List<ErrorData> GetErrorInfoData()
        {
            var result = new List<ErrorData>();

            foreach (var error in GetAllNonFreeRunNodeErrors())
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
        private bool TryGetLaunchingDuration(out long durationMS, IEnumerable<NodeTimeData> nodesTimeData)
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
