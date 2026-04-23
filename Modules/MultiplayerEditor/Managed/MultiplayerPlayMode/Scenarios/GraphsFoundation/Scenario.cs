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

        internal Instance GetInstanceById(GUID instanceId)
        {
            foreach (var instance in m_Instances)
            {
                if (instance.Id == instanceId)
                {
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
            if (StatusData.IsExecutingLaunchingStages())
                return;

            // Only Perform Drift detection for active free running instances.
            foreach (var instance in m_Instances)
            {
                if (!instance.IsFreeRunMode() || !instance.IsActive())
                    continue;

                var isClone = instance.Controller is CloneEditorController;
                if (!isClone && instance.HasReachedRunStage())
                    instance.Drifted = true;
            }
        }

        internal async Task TerminateAllFreeRunningInstancesAsync()
        {
            var stopTasks = new List<Task>();
            foreach (var instance in m_Instances)
            {
                if (instance.IsFreeRunMode() && instance.IsActive())
                {
                    stopTasks.Add(instance.StopAsFreeRunning());
                }
            }

            await Task.WhenAll(stopTasks);
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

            var validationSuccess = await RunStage(ExecutionStage.Validate, cancellationToken);
            if (!validationSuccess)
            {
                OrchestratedScenario.NotifyValidationFailure(this);
            }
            else
            {
                var executionStages = new Queue<ExecutionStage>(ExecutionGraph.k_ExecutionStages);
                while (executionStages.Count > 0)
                {
                    var currentStage = executionStages.Dequeue();

                    var success = await RunStage(currentStage, cancellationToken);

                    if (!success)
                        break;
                }
            }

            // Regardless of success or failure, always run the Cleanup stage.
            await RunStage(ExecutionStage.Cleanup, CancellationToken.None);

            SendPlayModeCompletedEvent();

            if (cancellationToken.IsCancellationRequested)
                ResetAfterCancellation();

            // This will make sure that the status will be updated after the last ExecutionStage is finished
            // even in the case where the scenario has no nodes.
            RefreshAndNotifyStatus();
        }

        async Task<bool> RunStage(ExecutionStage stage, CancellationToken cancellationToken)
        {
            var allInstanceTaskForStage = new List<Task<bool>>();

            // For each state, execute on all instances.
            foreach (var instance in m_Instances)
            {
                if (instance.IsFreeRunMode())
                    continue;

                var instanceTask = instance.RunOrResumeAsync(stage, cancellationToken);
                allInstanceTaskForStage.Add(instanceTask);
            }

            await Task.WhenAll(allInstanceTaskForStage);

            bool success = true;
            foreach (var result in allInstanceTaskForStage)
                success &= result.Result;

            return success;
        }

        internal ReadOnlyCollection<ExecutionNode> GetNodes(ExecutionStage executionStage)
        {
            var nodes = new List<ExecutionNode>();
            foreach (var instance in m_Instances)
            {
                var graph = instance.GetExecutionGraph();
                var instanceNodes = graph.GetNodes(executionStage);
                nodes.AddRange(instanceNodes);
            }

            return nodes.AsReadOnly();
        }

        IEnumerable<ExecutionNode> GetNonFreeRunNodes(IEnumerable<ExecutionStage> executionStages)
        {
            foreach (var instance in m_Instances)
            {
                if (instance.IsFreeRunMode())
                    continue;

                var graph = instance.GetExecutionGraph();
                foreach (var stage in executionStages)
                {
                    var instanceNodes = graph.GetNodes(stage);
                    foreach (var node in instanceNodes)
                        yield return node;
                }
            }
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

        internal IEnumerable<ExecutionNode.Error> GetAllNonFreeRunNodeErrors()
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

        private void SendPlayModeCompletedEvent()
        {
            var launchingDuration = ExecutionNode.ComputeExecutionDuration(GetNonFreeRunNodes(ExecutionGraph.k_LaunchingStages));
            var instances = GetAnalyticsInstancesData();
            var errors = GetErrorInfoData();
            var state = m_StatusData.OverallStatus.State;

            AnalyticsOnPlayFromScenarioEvent.Send(new OnPlayFromScenarioData()
            {
                Instances = instances.ToArray(),
                ScenarioState = state.ToString(),
                ScenarioLaunchingDurationMs = launchingDuration,
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
    }
}
