// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.PlayMode.Editor;
using UnityEditor;
using UnityEditor.Multiplayer.Internal;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.PlayMode.Editor
{
    /// <summary>
    /// An Instance represents a Virtual player that runs either in Editor, Clone Editors,
    /// Locally, Remotely, or on external devices. It contains an Execution Graph to
    /// coordinate the preparation, deployment and running of an Instance. It also notifies
    /// all attached Execution Event Listeners of progress and results.
    /// Additionally, long-running events will be monitored via Monitoring tasks.
    /// </summary>
    [Serializable]
    internal class Instance : ISerializationCallbackReceiver
    {
        [SerializeReference] private IInstanceItem m_InstanceItem;
        [SerializeField] private ExecutionGraph m_ExecutionGraph;
        [SerializeField] private CancellationTokenSource m_FreeRunCancelTokenSource;
        [SerializeField] private bool m_HasDeployedAndRun;
        [SerializeField] private bool m_Drifted;
        [SerializeField] private InstanceStatusData m_StatusData;
        [SerializeField] private InstanceController m_InstanceController;

        // TODO: MTT-10016 Migrate towards a single Monitoring Task per instance.
        private List<Task> m_CurrentMonitoringTasks = new List<Task>();

        internal event Action<Instance, InstanceStatusData> StatusRefreshed;

        internal string Name => m_InstanceItem.GetName();
        internal GUID Id => m_InstanceItem.GetId();
        internal InstanceController Controller => m_InstanceController;
        internal InstanceStatusData StatusData => m_StatusData;
        internal ExecutionGraph GetExecutionGraph() => m_ExecutionGraph;
        internal List<Task> GetCurrentMonitoringTasksForScenario() => m_CurrentMonitoringTasks;
        internal bool HasDeployedAndRun() => m_HasDeployedAndRun;
        internal bool Drifted
        {
            get => m_Drifted;
            set
            {
                m_Drifted = value;
                RefreshAndNotifyStatus();
            }
        }

        internal RunModeState RunModeState
        {
            get => m_InstanceItem.GetRunMode();
            set
            {
                if (IsActive())
                {
                    Debug.LogWarning("Cannot set RunModeState while the instance is active.");
                    return;
                }

                if (RunModeState == value)
                    return;

                m_InstanceItem = m_InstanceItem.WithRunMode(value);
                var activeScenario = PlayModeScenarioManager.ActiveScenario as OrchestratedScenario;
                if (activeScenario != null)
                {
                    activeScenario.Settings.SetInstanceRunningMode(Id, value);
                    EditorUtility.SetDirty(activeScenario);
                }

                Reset();
            }
        }

        internal Task<Scenario.ValidationResult> ValidateForRunningAsync(CancellationToken cancellationToken)
        {
            return m_InstanceController.ValidateForRunningAsync(cancellationToken);
        }

        internal static Instance Create(IInstanceItem instanceItem, InstanceController playModeController)
        {
            Assert.IsNotNull(instanceItem, "InstanceItem cannot be null");
            Assert.IsNotNull(playModeController, "PlayModeController cannot be null");

            // For each instance, wire up an Execution Graph
            var instance = new Instance();
            var executionGraph = new ExecutionGraph();
            executionGraph.StatusRefreshed += instance.OnGraphStatusRefreshed;
            instance.m_InstanceItem = instanceItem;
            instance.m_InstanceController = playModeController;
            instance.m_ExecutionGraph = executionGraph;
            return instance;
        }

        void OnGraphStatusRefreshed()
        {
            RefreshAndNotifyStatus();
        }

        void RefreshAndNotifyStatus()
        {
            RefreshStatusData();
            StatusRefreshed?.Invoke(this, StatusData);
        }

        public void OnBeforeSerialize()
        {
            // Clear out all listeners before Domain Reload Serialization
            m_CurrentMonitoringTasks.Clear();
        }

        public void OnAfterDeserialize()
        {
            // Re-attach listeners after Domain Reload Deserialization
            m_ExecutionGraph.StatusRefreshed += OnGraphStatusRefreshed;
        }

        internal void Reset()
        {
            // Reset the instance properties for a new run
            m_HasDeployedAndRun = false;
            m_CurrentMonitoringTasks.Clear();
            m_StatusData = default;

            // Reset the Execution graph
            m_ExecutionGraph.Reset();

            RefreshAndNotifyStatus();
        }

        internal bool IsFreeRunMode()
        {
            return RunModeState == RunModeState.ManualControl;
        }

        // Returns the array of analytics InstanceData from Instances
        internal static InstanceData[] GetAnalyticsDataArray(List<Instance> instances)
        {
            var result = new List<InstanceData>();
            foreach (var instance in instances)
            {
                var data = instance.GetAnalyticsData();
                result.Add(data);
            }
            return result.ToArray();
        }

        // Returns the analytics InstanceData from Instance
        internal InstanceData GetAnalyticsData()
        {
            var executionGraph = GetExecutionGraph();
            var isFreeRun = IsFreeRunMode();
            var durationMs = 0L;
            var prepareStageDurationMs = 0L;
            var deployStageDurationMs = 0L;
            var runStageDurationMs = 0L;

            if (!isFreeRun)
            {
                durationMs = ComputeNodeDurations(executionGraph.GetAllNodes());
                prepareStageDurationMs = ComputeNodeDurations(executionGraph.GetNodes(ExecutionStage.Prepare));
                runStageDurationMs = ComputeNodeDurations(executionGraph.GetNodes(ExecutionStage.Run));
                deployStageDurationMs = ComputeNodeDurations(executionGraph.GetNodes(ExecutionStage.Deploy));
            }

            var runningMode = isFreeRun
                ? RunModeState.ManualControl.ToString()
                : RunModeState.ScenarioControl.ToString();

            return new InstanceData
            {
                Type = Controller.GetTypeNameForAnalytics(),
                BuildTarget = GetBuildTargetForAnalytics(),
                InstanceLaunchingDuration = durationMs,
                RunningMode = runningMode,
                IsActive = IsActive(),
                InstancePrepareStageDurationMs = prepareStageDurationMs,
                InstanceDeployStageDurationMs = deployStageDurationMs,
                InstanceRunStageDurationMs = runStageDurationMs,
                MultiplayerRole = GetMultiplayerRoleForAnalytics(),
            };
        }

        string GetBuildTargetForAnalytics()
        {
            if (Controller is LocalPlayerController localController)
            {
                return InternalUtilities.GetBuildTargetType(localController.Settings.BuildProfile);
            }

            return InternalUtilities.GetBuildTargetType(EditorUserBuildSettings.activeBuildTarget);
        }

        string GetMultiplayerRoleForAnalytics()
        {
            if (Controller is LocalPlayerController localController)
            {
                var buildProfile = localController.Settings.BuildProfile;
                if (buildProfile == null)
                    return string.Empty;
                return MultiplayerRolesSettings.instance.GetMultiplayerRoleForBuildProfile(buildProfile).ToString();
            }

            if (Controller is CloneEditorController cloneController)
            {
                return cloneController.Settings.RoleMask.ToString();
            }

            if (Controller is MainEditorController mainEditorController)
            {
                return mainEditorController.Settings.RoleMask.ToString();
            }

            return string.Empty;
        }

        // calculate the sum of time difference between the earliest start time and the latest end time for a given list of nodes
        private long ComputeNodeDurations(IEnumerable<Node> nodes)
        {
            long duration = 0;
            foreach (var node in nodes)
            {
                if (node.TimeData.StartTime != default)
                {
                    var nodeDuration = (long)Math.Round((node.TimeData.EndTime - node.TimeData.StartTime).TotalMilliseconds);
                    duration += nodeDuration;
                }
            }
            return duration;
        }


        internal async Task StartOrResumeAsFreeRunning(bool shouldResume)
        {
            // Create a cancellation source by which to stop the Free run instance
            m_FreeRunCancelTokenSource = new CancellationTokenSource();

            // Check instance setup before it starts running
            var preStartCheck = await TryRunPreStartChecksAsync(m_FreeRunCancelTokenSource.Token);
            if (!preStartCheck)
            {
                StopAsFreeRunning();
                return;
            }

            // Refresh this instance if starting for the first time.
            if (!shouldResume)
                Reset();

            RefreshStatusData();

            // Prepare the stages that this Instance, once started, will execute on.
            var executionStages = new Queue<ExecutionStage>(new ExecutionStage[]
            {
                ExecutionStage.Prepare,
                ExecutionStage.Deploy,
                ExecutionStage.Run,
            });

            // Start the execution
            while (executionStages.Count > 0)
            {
                var currentStage = executionStages.Dequeue();
                var result = await RunOrResumeAsync(currentStage, m_FreeRunCancelTokenSource.Token);

                // If we cancelled, exit out
                if (m_FreeRunCancelTokenSource == null || m_FreeRunCancelTokenSource.Token.IsCancellationRequested)
                    return;

                // Else, continue on to the next stage once successful
                if (result.Success)
                    continue;

                // Else Stop execution and log errors where needed
                Debug.LogError($"Instance {Name} encountered an error in Manual Mode, " +
                               $"please refer to the Editor logs for more information.");
                break;
            }
        }

        private async Task<bool> TryRunPreStartChecksAsync(CancellationToken cancellationToken)
        {
            Scenario.ValidationResult validationResult;
            try
            {
                validationResult = await ValidateForRunningAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            if (!validationResult.IsValid)
            {
                var instanceData = new[] { GetAnalyticsData() };
                var errorData = GetValidationErrorData(validationResult);

                AnalyticsOnPlayFromScenarioEvent.SendValidationErrorData(
                    instanceData,
                    new[] { errorData }
                );

                EditorUtility.DisplayDialog(
                    "Play Mode Scenario - Manual Control Instance Setup Error",
                    $"{validationResult.Message}. Please check the console for more details.",
                    "OK"
                );
                return false;
            }
            return true;
        }

        internal static ErrorData GetValidationErrorData(Scenario.ValidationResult result)
        {
            // Create an ErrorData object from the validation result
            return new ErrorData
            {
                ExceptionType = typeof(InvalidOperationException).ToString(),
                Message = result.Message,
            };
        }

        internal void StopAsFreeRunning()
        {
            // Sanity checks from stopping twice.
            if (m_FreeRunCancelTokenSource == null)
                return;

            m_FreeRunCancelTokenSource.Cancel();
            m_FreeRunCancelTokenSource = null;
            m_Drifted = false;
        }

        internal bool HasStartedAsFreeRunning()
        {
            return m_FreeRunCancelTokenSource != null;
        }

        internal bool IsActive()
        {
            return m_StatusData.OverallStatus.State is ExecutionState.Running or ExecutionState.Active;
        }

        internal async Task<ExecutionGraph.ExecutionResult> RunOrResumeAsync(ExecutionStage executionStage,
                                                                             CancellationToken cancellationToken)
        {
            // Run the Executed stage and wait for the result.
            ExecutionGraph.ExecutionResult stageResult
                = await m_ExecutionGraph.RunOrResumeAsync(executionStage, cancellationToken, Name);

            // If successful, track tasks needed to monitor this instance.
            if (stageResult.Success)
            {
                // Grab all monitoring tasks produced from the run and track it in this instance.
                // TODO MTT-10016 Monitor Task migration - Remove this in favor of a single Monitoring Task at Instance Level
                m_CurrentMonitoringTasks.AddRange(stageResult.MonitoringTasks);
            }

            // Await and signal Instance completion after final stage
            if (executionStage == ExecutionStage.Run)
                AwaitInstanceCompletion().Forget();

            return stageResult;
        }

        private async Task AwaitInstanceCompletion()
        {
            m_HasDeployedAndRun = true;
            await Task.WhenAll(GetCurrentMonitoringTasksForScenario());
            m_FreeRunCancelTokenSource = null;
        }

        void RefreshStatusData()
        {
            m_StatusData.Clear();

            foreach (var stage in ExecutionGraph.k_Stages)
            {
                ref var stageStatus = ref m_StatusData.StageStatuses[(int)stage];

                stageStatus.Aggregate(m_ExecutionGraph.GetNodes(stage));
                m_StatusData.OverallStatus.Aggregate(stageStatus);

                if (stageStatus.IdleNodesCount < stageStatus.NodesCount)
                    m_StatusData.CurrentStage = stage;
            }
        }
    }
}
