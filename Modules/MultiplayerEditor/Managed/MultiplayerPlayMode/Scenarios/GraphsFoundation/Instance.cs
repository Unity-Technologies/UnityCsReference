// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private CancellationTokenSource m_FreeRunCancelTokenSource;
        [SerializeField] private bool m_HasDeployedAndRun;
        [SerializeField] private bool m_Drifted;
        [SerializeField] private InstanceStatusData m_StatusData;
        [SerializeField] private InstanceController m_InstanceController;
        [SerializeField] private List<InstanceControllerDecorator> m_DecoratorsControllers;
        private Task m_FreeRunningTask;

        internal event Action<Instance, InstanceStatusData> StatusRefreshed;

        internal string Name => m_InstanceItem.GetName();
        internal GUID Id => m_InstanceItem.GetId();
        internal InstanceController Controller => m_InstanceController;
        internal IEnumerable<InstanceControllerDecorator> DecoratorsControllers => m_DecoratorsControllers;
        internal InstanceStatusData StatusData => m_StatusData;
        internal ExecutionGraph GetExecutionGraph() => m_ExecutionGraph;
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

        internal static Instance Create(
            IInstanceItem instanceItem,
            InstanceController playModeController,
            List<InstanceControllerDecorator> decorators,
            ExecutionGraph executionGraph)
        {
            Assert.IsNotNull(instanceItem, "InstanceItem cannot be null");
            Assert.IsNotNull(playModeController, "PlayModeController cannot be null");
            Assert.IsNotNull(executionGraph, "ExecutionGraph cannot be null");

            // For each instance, wire up an Execution Graph
            var instance = new Instance();
            instance.m_InstanceItem = instanceItem;
            instance.m_InstanceController = playModeController;
            instance.m_ExecutionGraph = executionGraph;
            instance.m_DecoratorsControllers = decorators;

            executionGraph.StatusRefreshed += instance.OnGraphStatusRefreshed;
            instance.OnGraphStatusRefreshed();

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

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            // Re-attach listeners after Domain Reload Deserialization
            m_ExecutionGraph.StatusRefreshed += OnGraphStatusRefreshed;
        }

        internal void Reset()
        {
            // Reset the instance properties for a new run
            m_HasDeployedAndRun = false;
            m_StatusData = default;

            // Reset the Execution graph
            m_ExecutionGraph.Reset();

            RefreshAndNotifyStatus();
        }

        internal bool IsFreeRunMode()
        {
            // TODO only local instances support free running currently, we should move this to a decorator
            if (m_InstanceController is not LocalPlayerController)
                return false;
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

            var durationMs = ExecutionNode.ComputeExecutionDuration(executionGraph.GetAllNodes());
            var prepareStageDurationMs = ExecutionNode.ComputeExecutionDuration(executionGraph.GetNodes(ExecutionStage.Prepare));
            var deployStageDurationMs = ExecutionNode.ComputeExecutionDuration(executionGraph.GetNodes(ExecutionStage.Deploy));
            var runStageDurationMs = ExecutionNode.ComputeExecutionDuration(executionGraph.GetNodes(ExecutionStage.Run))
                + ExecutionNode.ComputeExecutionDuration(executionGraph.GetNodes(ExecutionStage.Start));

            var runningMode = isFreeRun
                ? RunModeState.ManualControl.ToString()
                : RunModeState.ScenarioControl.ToString();

            var hasBeenActive = IsActive() || durationMs > 0;

            return new InstanceData
            {
                Type = Controller.GetTypeNameForAnalytics(),
                BuildTarget = GetBuildTargetForAnalytics(),
                InstanceLaunchingDuration = durationMs,
                RunningMode = runningMode,
                IsActive = hasBeenActive,
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

        internal async Task StartOrResumeAsFreeRunning(bool shouldResume)
        {
            Assert.IsTrue(m_FreeRunningTask == null || m_FreeRunningTask.IsCompleted, "Instance is already running in Free Run mode.");
            m_FreeRunningTask = null;

            // Create a cancellation source by which to stop the Free run instance
            m_FreeRunCancelTokenSource = new CancellationTokenSource();

            m_FreeRunningTask = FreeRun(shouldResume);
            await m_FreeRunningTask;
        }

        async Task FreeRun(bool shouldResume)
        {
            // Refresh this instance if starting for the first time.
            if (!shouldResume)
                Reset();

            RefreshStatusData();

            var validationSuccess = await RunOrResumeAsync(ExecutionStage.Validate, m_FreeRunCancelTokenSource.Token);
            if (!validationSuccess)
            {
                OrchestratedScenario.NotifyValidationFailure(this);
            }
            else
            {
                // Prepare the stages that this Instance, once started, will execute on.
                var executionStages = new Queue<ExecutionStage>(ExecutionGraph.k_ExecutionStages);
                // Start the execution
                while (executionStages.Count > 0)
                {
                    var currentStage = executionStages.Dequeue();
                    var success = await RunOrResumeAsync(currentStage, m_FreeRunCancelTokenSource.Token);

                    // If we cancelled, exit out
                    if (m_FreeRunCancelTokenSource == null || m_FreeRunCancelTokenSource.Token.IsCancellationRequested)
                        break;

                    // Else, continue on to the next stage once successful
                    if (success)
                        continue;

                    // Else Stop execution and log errors where needed
                    Debug.LogError($"Instance {Name} encountered an error in Manual Mode, " +
                                   $"please refer to the Editor logs for more information.");
                    break;
                }
            }

            await RunOrResumeAsync(ExecutionStage.Cleanup, CancellationToken.None);
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

        internal async Task StopAsFreeRunning()
        {
            // Sanity checks from stopping twice.
            if (m_FreeRunCancelTokenSource == null)
                return;

            m_FreeRunCancelTokenSource.Cancel();
            m_FreeRunCancelTokenSource = null;
            m_Drifted = false;

            if (m_FreeRunningTask != null)
            {
                await m_FreeRunningTask;
                m_FreeRunningTask = null;
            }
        }

        internal bool HasStartedAsFreeRunning()
        {
            return m_FreeRunCancelTokenSource != null;
        }

        internal bool IsActive()
        {
            return m_StatusData.IsExecuting();
        }

        internal async Task<bool> RunOrResumeAsync(ExecutionStage executionStage, CancellationToken cancellationToken)
        {
            // Run the Executed stage and wait for the result.
            bool stageSuccess = await m_ExecutionGraph.RunOrResumeAsync(executionStage, cancellationToken, Name);

            // Await and signal Instance completion after final stage
            if (executionStage == ExecutionStage.Run)
            {
                m_HasDeployedAndRun = true;
                m_FreeRunCancelTokenSource = null;
            }

            return stageSuccess;
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
