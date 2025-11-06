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
        [SerializeField] public String m_Name;
        [SerializeField] public String m_InstanceDescriptionType;
        [SerializeField] private CancellationTokenSource m_FreeRunCancelTokenSource;
        [SerializeField] private bool m_HasDeployedAndRun;
        [SerializeField] public string m_BuildTarget;
        [SerializeField] private bool m_Drifted;
        [SerializeField] private RunModeState m_RunModeState;
        [SerializeField] public string m_MultiplayerRole;
        [SerializeReference] private PlayModeController m_PlayModeController;

        // TODO: MTT-10016 Migrate towards a single Monitoring Task per instance.
        private List<Task> m_CurrentMonitoringTasks = new List<Task>();
        private List<NodeStatus> m_CurrentStatus = new List<NodeStatus>();
        private ExecutionStage m_CurrentStage = ExecutionStage.None;

        internal ExecutionGraph GetExecutionGraph() => m_ExecutionGraph;
        internal List<Task> GetCurrentMonitoringTasksForScenario() => m_CurrentMonitoringTasks;
        internal List<NodeStatus> GetCurrentNodeStatus() => m_CurrentStatus;
        internal ExecutionStage GetCurrentStage() => m_CurrentStage;
        internal bool HasDeployedAndRun() => m_HasDeployedAndRun;
        internal bool Drifted
        {
            get => m_Drifted;
            set => m_Drifted = value;
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

                m_RunModeState = value;
            }
        }

        // Listeners for the notification of Execution updates.
        private event Action<Instance, Node> m_InstanceExecutionEventListener;
        internal void AddInstanceExecutionEventListener(Action<Instance, Node> listener)
        {
            m_InstanceExecutionEventListener -= listener;
            m_InstanceExecutionEventListener += listener;
        }

        internal void RemoveInstanceExecutionEventListener(Action<Instance, Node> listener)
        {
            m_InstanceExecutionEventListener -= listener;
        }

        private void OnNodeExecutionEventUpdate(Node node)
        {
            // Refresh this instance's status
            RefreshInstanceStatus();

            // Notify listeners of node events (usually a Scenario)
            m_InstanceExecutionEventListener?.Invoke(this, node);
        }

        internal Task<Scenario.ValidationResult> ValidateForRunningAsync(CancellationToken cancellationToken)
        {
            return m_PlayModeController.ValidateForRunningAsync(cancellationToken);
        }

        internal static Instance Create()
        {
            var description = new MainEditorInstanceDescription();
            var controller = new EditorInstanceController(description);
            return Create(description, controller);
        }

        internal static Instance Create(InstanceDescription description, PlayModeController playModeController)
        {
            Assert.IsNotNull(description);
            Assert.IsNotNull(playModeController);

            // For each instance, wire up an Execution Graph
            var instance = new Instance();
            var executionGraph = new ExecutionGraph();
            executionGraph.SetNodeExecutionEventListener(instance.OnNodeExecutionEventUpdate);
            instance.m_PlayModeController = playModeController;
            instance.m_ExecutionGraph = executionGraph;
            instance.m_InstanceDescriptionType = description.InstanceTypeName;
            instance.m_RunModeState = description.RunModeState;
            instance.m_BuildTarget = description.BuildTargetType;
            instance.m_MultiplayerRole = description.MultiplayerRole;
            instance.m_Name = description.Name;
            return instance;
        }

        public void OnBeforeSerialize()
        {
            // Clear out all listeners before Domain Reload Serialization
            m_CurrentMonitoringTasks.Clear();
            m_ExecutionGraph.SetNodeExecutionEventListener(null);
        }

        public void OnAfterDeserialize()
        {
            // Re-attach listeners after Domain Reload Deserialization
            m_ExecutionGraph.SetNodeExecutionEventListener(OnNodeExecutionEventUpdate);
        }

        internal void Reset()
        {
            // Reset the instance properties for a new run
            m_HasDeployedAndRun = false;
            m_CurrentMonitoringTasks.Clear();
            m_CurrentStatus.Clear();

            // Reset the Execution graph
            m_ExecutionGraph.Reset();
        }

        // Returns the Instance's Description configuration from the current Scenario Config
        internal InstanceDescription GetInstanceDescription()
        {
            // var currentConfig = PlayModeManager.instance.ActivePlayModeConfig as ScenarioConfig;
            // if (currentConfig == null)
            //     return null;

            // return currentConfig.GetInstanceDescriptionByName(m_Name);

            var currentConfig = PlayModeManager.instance.ActivePlayModeConfig;
            if (currentConfig == null || currentConfig.GetType().Name != "ScenarioConfig")
                return null;

            var getDescriptionMethod = currentConfig.GetType().GetMethod("GetInstanceDescriptionByName",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(getDescriptionMethod,
                $"The current config {currentConfig.GetType().Name} does not have a method GetInstanceDescriptionByName");

            return getDescriptionMethod.Invoke(currentConfig, new object[] { m_Name }) as InstanceDescription;
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

            RefreshInstanceStatus();

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
            return GetInstanceExecutionState() is ExecutionState.Running or ExecutionState.Active;
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

        // Refresh and compute the current stage and status of this Instance.
        internal void RefreshInstanceStatus()
        {
            var mStages = Enum.GetValues(typeof(ExecutionStage));
            var errors = new List<Node.Error>();
            m_CurrentStage = ExecutionStage.None;
            m_CurrentStatus.Clear();

            for (var i = mStages.Length - 1; i >= 0; i--)
            {
                var targetExecutionStage = (ExecutionStage)mStages.GetValue(i);
                ComputeStageState(
                    targetExecutionStage,
                    out var _,
                    out var instanceStageState,
                    progressSum: out var _,
                    idleNodes: out var _,
                    runningNodes: out var _,
                    completedNodes: out var _,
                    activeNodes: out var _,
                    failedNodes: out var _,
                    abortedNodes: out var _,
                    errors: ref errors,
                    nodeStatuses: ref m_CurrentStatus);

                // If the Instance Stage is Not Running or Active, Set it as the current
                if (instanceStageState != ExecutionState.Idle && m_CurrentStage == ExecutionStage.None)
                    m_CurrentStage = targetExecutionStage;
            }
        }

        internal void ComputeStageState(
            ExecutionStage targetStage,
            out int totalNodes,
            out ExecutionState state,
            out float progressSum,
            out int idleNodes,
            out int runningNodes,
            out int completedNodes,
            out int activeNodes,
            out int failedNodes,
            out int abortedNodes,
            ref List<Node.Error> errors,
            ref List<NodeStatus> nodeStatuses)
        {
            state = ExecutionState.Invalid;
            progressSum = 0.0f;
            idleNodes = 0;
            runningNodes = 0;
            completedNodes = 0;
            activeNodes = 0;
            failedNodes = 0;
            abortedNodes = 0;

            // Possible for an instance to not have node tasks for a particular
            // Execution stage. Simply mark it as completed and return.
            var nodes = m_ExecutionGraph.GetNodes(targetStage);
            totalNodes = nodes.Count;
            if (totalNodes == 0)
                return;

            foreach (var node in nodes)
            {
                progressSum += node.Progress;

                switch (node.State)
                {
                    case ExecutionState.Idle:
                        idleNodes++;
                        break;
                    case ExecutionState.Running:
                        runningNodes++;
                        break;
                    case ExecutionState.Completed:
                        completedNodes++;
                        break;
                    case ExecutionState.Active:
                        activeNodes++;
                        break;
                    case ExecutionState.Failed:
                        failedNodes++;
                        errors.Add(node.ErrorInfo);
                        break;
                    case ExecutionState.Aborted:
                        abortedNodes++;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                nodeStatuses.Add(new NodeStatus(node.Name, node.State, node.TimeData, node.Progress));
            }

            if (failedNodes > 0)
                state = ExecutionState.Failed;
            else if (abortedNodes > 0)
                state = ExecutionState.Aborted;
            else if (runningNodes > 0)
                state = ExecutionState.Running;
            else if (activeNodes > 0)
                state = ExecutionState.Active;
            else if (idleNodes > 0)
                state = ExecutionState.Idle;
            else if (completedNodes == nodes.Count)
                state = ExecutionState.Completed;
        }

        internal ExecutionState GetInstanceExecutionState()
        {
            var nodes = m_ExecutionGraph.GetAllNodes();
            var executionStates = new List<ExecutionState>();

            foreach (var node in nodes)
            {
                executionStates.Add(node.State);
            }
            return ComputeInstanceState(executionStates);
        }

        internal static ExecutionState ComputeInstanceState(List<ExecutionState> nodeStates)
        {
            var totalNodes = nodeStates.Count;
            var errorNodes = 0;
            var idleNodes = 0;
            var runningNodes = 0;
            var activeNodes = 0;
            var completeNodes = 0;
            var abortedNodes = 0;

            foreach (var nodeState in nodeStates)
            {
                switch (nodeState)
                {
                    case ExecutionState.Failed:
                        errorNodes++;
                        break;
                    case ExecutionState.Idle:
                        idleNodes++;
                        break;
                    case ExecutionState.Running:
                        runningNodes++;
                        break;
                    case ExecutionState.Active:
                        activeNodes++;
                        break;
                    case ExecutionState.Completed:
                        completeNodes++;
                        break;
                    case ExecutionState.Aborted:
                        abortedNodes++;
                        break;
                    default:
                        UnityEngine.Debug.LogError($"Invalid node state {nodeState}");
                        return ExecutionState.Invalid;
                }
            }

            if (errorNodes > 0)
                return ExecutionState.Failed;

            if (idleNodes == totalNodes)
                return ExecutionState.Idle;

            if (abortedNodes > 0)
                return ExecutionState.Aborted;

            if (completeNodes == totalNodes)
                return ExecutionState.Completed;

            if (activeNodes > 0 && activeNodes + completeNodes == totalNodes)
                return ExecutionState.Active;

            return ExecutionState.Running;
        }
    }
}
