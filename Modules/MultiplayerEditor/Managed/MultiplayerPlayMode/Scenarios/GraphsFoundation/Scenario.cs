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
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    /// <summary>
    /// A Scenario groups together multiple configured Instances representing virtual players and is built by
    /// the ScenarioFactory. Its main role is to orchestrate and synchronize the Execution Stages of all
    /// associated Instances across Preparation, Deployment, and Running phases. It also notifies
    /// all attached callbacks of Scenario status and completion results.
    /// </summary>
    internal partial class Scenario : ScriptableObject
    {
        [SerializeField] private ScenarioStatusData m_StatusData;
        [SerializeField] private bool m_HasStarted;
        [SerializeField] private List<ControllerRuntime> m_Instances = new List<ControllerRuntime>();
        [SerializeField] private List<ControllerRuntime> m_ScenarioControllers = new List<ControllerRuntime>();

        public ScenarioStatusData StatusData => m_StatusData;

        // Scenario Callbacks
        [AutoStaticsCleanupOnCodeReload] // static event; stale handlers after reload pin old ALC
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
            foreach (var runtime in GetAllRuntimes())
            {
                runtime.StatusRefreshed -= OnRuntimeStatusRefreshed;
                runtime.StatusRefreshed += OnRuntimeStatusRefreshed;
            }
        }

        private void OnRuntimeStatusRefreshed(ControllerRuntime runtime, InstanceStatusData status)
        {
            if (!runtime.IsFreeRunMode())
                RefreshAndNotifyStatus();
        }

        internal void Reset()
        {
            m_HasStarted = false;
            m_StatusData.Clear();

            // Reset only the runtimes that are controlled by this Scenario.
            foreach (var runtime in NonFreeRunRuntimes())
                runtime.Reset();
        }

        private void ResetAfterCancellation()
        {
            // After a cancellation, runtimes that failed should remain in their failed state
            // so users can see the failure result. While runtimes that were running or completed
            // should be reset to the idle state.
            foreach (var runtime in NonFreeRunRuntimes())
            {
                if (runtime.StatusData.OverallStatus.State is not ExecutionState.Failed)
                {
                    runtime.Reset();
                }
            }
        }

        internal void AddInstance(ControllerRuntime instance)
        {
            if (m_HasStarted)
                throw new InvalidOperationException("Trying to modify a scenario that has already started.");

            // Don't re-add the instance if we already have it.
            if (m_Instances.Contains(instance))
                return;

            // Add instance and hook up event listeners back to this scenario.
            instance.StatusRefreshed += OnRuntimeStatusRefreshed;
            m_Instances.Add(instance);

            RefreshAndNotifyStatus();
        }

        internal void RemoveInstance(ControllerRuntime instance)
        {
            // Sanity check
            if (instance == null)
                return;

            // Remove the given instance and deatch its listeners from this Scenario, if found.
            if (m_Instances.Remove(instance))
            {
                instance.StatusRefreshed -= OnRuntimeStatusRefreshed;
                return;
            }

            Debug.LogWarning($"Scenario: No instance {instance.Name} was found to be removed!");
        }

        internal List<ControllerRuntime> GetAllInstances()
        {
            return m_Instances;
        }

        internal void AddScenarioController(ControllerRuntime runtime)
        {
            if (m_HasStarted)
                throw new InvalidOperationException("Trying to modify a scenario that has already started.");

            if (runtime == null || m_ScenarioControllers.Contains(runtime))
                return;

            runtime.StatusRefreshed += OnRuntimeStatusRefreshed;
            m_ScenarioControllers.Add(runtime);

            RefreshAndNotifyStatus();
        }

        internal List<ControllerRuntime> GetAllScenarioControllers()
        {
            return m_ScenarioControllers;
        }

        internal IEnumerable<ControllerRuntime> GetAllRuntimes()
        {
            foreach (var instance in m_Instances)
                yield return instance;

            foreach (var runtime in m_ScenarioControllers)
                yield return runtime;
        }

        private IEnumerable<ControllerRuntime> NonFreeRunRuntimes()
        {
            foreach (var runtime in GetAllRuntimes())
            {
                if (!runtime.IsFreeRunMode())
                    yield return runtime;
            }
        }

        internal ControllerRuntime GetInstanceByName(string instanceName, bool targetActiveFreeRun = false)
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

        internal ControllerRuntime GetInstanceById(GUID instanceId)
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
            where TController : PlayModeController
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
            where TController : PlayModeController
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
            else if (!validationSuccess)
            {
                // Validation runs before anything is deployed, so a failed run started nothing and has no result worth showing.
                // The dialog already reported why; leaving instances in their post-run states would strand them as Failed or mid-run in the UI.
                Reset();
            }

            // This will make sure that the status will be updated after the last ExecutionStage is finished
            // even in the case where the scenario has no nodes.
            RefreshAndNotifyStatus();
        }

        async Task<bool> RunStage(ExecutionStage stage, CancellationToken cancellationToken)
        {
            var allRuntimeTasksForStage = new List<Task<bool>>();

            // For each state, execute on all runtimes.
            foreach (var runtime in NonFreeRunRuntimes())
                allRuntimeTasksForStage.Add(runtime.RunOrResumeAsync(stage, cancellationToken));

            await Task.WhenAll(allRuntimeTasksForStage);

            bool success = true;
            foreach (var result in allRuntimeTasksForStage)
                success &= result.Result;

            return success;
        }

        internal ReadOnlyCollection<ExecutionNode> GetNodes(ExecutionStage executionStage)
        {
            var nodes = new List<ExecutionNode>();
            foreach (var runtime in GetAllRuntimes())
                nodes.AddRange(runtime.GetExecutionGraph().GetNodes(executionStage));

            return nodes.AsReadOnly();
        }

        internal IEnumerable<ExecutionNode> GetNonFreeRunNodes(IEnumerable<ExecutionStage> executionStages)
        {
            foreach (var runtime in NonFreeRunRuntimes())
            {
                var graph = runtime.GetExecutionGraph();
                foreach (var stage in executionStages)
                {
                    foreach (var node in graph.GetNodes(stage))
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

            foreach (var runtime in NonFreeRunRuntimes())
            {
                var runtimeStatus = runtime.StatusData;

                if (runtimeStatus.OverallStatus.State == ExecutionState.Invalid)
                    continue;

                m_StatusData.OverallStatus.Aggregate(runtimeStatus.OverallStatus);

                foreach (var stage in ExecutionGraph.k_Stages)
                {
                    m_StatusData.StageStatuses[(int)stage].Aggregate(runtimeStatus.StageStatuses[(int)stage]);

                    if (m_StatusData.StageStatuses[(int)stage].IdleNodesCount < m_StatusData.StageStatuses[(int)stage].NodesCount)
                        m_StatusData.CurrentStage = stage;
                }
            }
        }

        internal IEnumerable<ExecutionNode.Error> GetAllNonFreeRunNodeErrors()
        {
            foreach (var runtime in NonFreeRunRuntimes())
            {
                foreach (var node in runtime.GetExecutionGraph().GetAllNodes())
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
