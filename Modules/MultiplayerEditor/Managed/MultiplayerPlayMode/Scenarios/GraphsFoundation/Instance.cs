// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.PlayMode.Editor;
using UnityEditor;
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
        [SerializeField] private ExecutionGraph m_ExecutionGraph;
        [SerializeField] private string m_Name;
        [SerializeField] private string m_InstanceDescriptionType;
        [SerializeField] private CancellationTokenSource m_FreeRunCancelTokenSource;
        [SerializeField] private bool m_HasDeployedAndRun;
        [SerializeField] private string m_BuildTarget;
        [SerializeField] private bool m_Drifted;
        [SerializeField] private RunModeState m_RunModeState;
        [SerializeField] private string m_MultiplayerRole;
        [SerializeField] private InstanceStatusData m_StatusData;
        [SerializeReference] private PlayModeController m_PlayModeController;
        [SerializeReference] private InstanceDescription m_InstanceDescription;

        // TODO: MTT-10016 Migrate towards a single Monitoring Task per instance.
        private List<Task> m_CurrentMonitoringTasks = new List<Task>();

        internal event Action<Instance, InstanceStatusData> StatusRefreshed;

        internal string Name => m_Name;
        internal PlayModeController Controller => m_PlayModeController;
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
            set
            {
                if (IsActive())
                {
                    Debug.LogWarning("Cannot set RunModeState while the instance is active.");
                    return;
                }

                if (m_RunModeState == value)
                    return;

                m_RunModeState = value;
                Reset();
            }
        }

        internal Task<Scenario.ValidationResult> ValidateForRunningAsync(CancellationToken cancellationToken)
        {
            return m_PlayModeController.ValidateForRunningAsync(cancellationToken);
        }

        internal static Instance Create()
        {
            var description = new MainEditorInstanceDescription() { Name = "Main Editor" };
            var controller = new EditorInstanceController(description);
            return Create(description, controller);
        }

        internal static Instance Create(InstanceDescription description, PlayModeController playModeController)
        {
            Assert.IsNotNull(description, "InstanceDescription cannot be null");
            Assert.IsNotNull(playModeController, "PlayModeController cannot be null");

            // For each instance, wire up an Execution Graph
            var instance = new Instance();
            var executionGraph = new ExecutionGraph();
            executionGraph.StatusRefreshed += instance.OnGraphStatusRefreshed;
            instance.m_PlayModeController = playModeController;
            instance.m_InstanceDescription = description;
            instance.m_ExecutionGraph = executionGraph;
            instance.m_InstanceDescriptionType = description.InstanceTypeName;
            instance.m_RunModeState = description.RunModeState;
            instance.m_BuildTarget = description.BuildTargetType;
            instance.m_MultiplayerRole = description.MultiplayerRole;
            instance.m_Name = description.Name;
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

        // Returns the Instance's Description configuration from the current Scenario Config
        internal InstanceDescription GetInstanceDescription()
        {
            return m_InstanceDescription;
        }

        internal bool IsFreeRunMode()
        {
            return m_RunModeState == RunModeState.ManualControl;
        }

        // This is used for the Analytics OnPlayFromScenario event's UseMultiplay data
        // TODO: We'll need to update this once we support different server deployment modes for editor and remote instances.
        internal static string GetInstanceUseMultiplayDataFromServerDeployMode(InstanceDescription instanceDescription)
        {
            return instanceDescription switch
            {
                LocalInstanceDescription localInstanceDescription when localInstanceDescription.IsServer() =>
                    ServerSettings.GetUseMultiplayString(localInstanceDescription
                        .ServerSettings.DeployMode),
                RemoteInstanceDescription remoteInstanceDescription => "Multiplay",
                _ => string.Empty
            };
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

            var isRunning = isFreeRun && IsActive();

            string serverDeployMode = null;
            var instanceDescription = GetInstanceDescription();
            if (instanceDescription != null)
            {
                serverDeployMode = GetInstanceUseMultiplayDataFromServerDeployMode(instanceDescription);
            }

            return new InstanceData
            {
                Type = m_InstanceDescriptionType,
                BuildTarget = m_BuildTarget,
                InstanceLaunchingDuration = durationMs,
                RunningMode = runningMode,
                IsActive = isRunning,
                InstancePrepareStageDurationMs = prepareStageDurationMs,
                InstanceDeployStageDurationMs = deployStageDurationMs,
                InstanceRunStageDurationMs = runStageDurationMs,
                MultiplayerRole = m_MultiplayerRole,
                UseMultiplay = serverDeployMode
            };
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
                Debug.LogError($"Instance {m_Name} encountered an error in Manual Mode, " +
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
                = await m_ExecutionGraph.RunOrResumeAsync(executionStage, cancellationToken, m_Name);

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
