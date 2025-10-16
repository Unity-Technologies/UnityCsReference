// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.PlayMode.Editor;
using UnityEditor;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal class ScenarioRunner : ScriptableSingleton<ScenarioRunner>
    {
        [InitializeOnLoadMethod]
        private static void OnDomainReload()
        {
            if (MigrationUtility.ShouldDisableMultiplayerPlayMode())
                return;

            // Hook up Unity Editor Exit callbacks to clean up Scenario's Instances.
            EditorApplication.wantsToQuit += OnApplicationQuit;

            // Listen for cases where the Play Mode Manager may flush its config, so that we stop the active scenario.
            ScenarioManagerProvider.instance.ConfigAssetChanged += () =>
            {
                var playModeManager = ScenarioManagerProvider.instance;
                var hasNoSecenarioConfig = playModeManager.ActivePlayModeConfig is not OrchestratedScenario;
                if (hasNoSecenarioConfig && instance.m_Scenario != null)
                    LoadScenario(null);
            };

            if (instance.m_Scenario == null)
                return;

            // Notify all Scenario mode instances running in a Scenario
            if (instance.m_Scenario.StatusData.OverallStatus.State == ExecutionState.Running)
                instance.RunOrResume();

            // Notify all Manual mode instances that are free running.
            instance.m_Scenario.ResumeFreeRunInstances();
        }

        private Scenario m_Scenario;
        private CancellationTokenSource m_CancellationTokenSource;
        private bool m_IsRunning;

        internal Scenario ActiveScenario => m_Scenario;
        internal bool IsRunning => m_IsRunning;

        internal static event Action<ScenarioStatusData> StatusChanged;

        public static void LoadScenario(Scenario scenario)
        {
            // If clearing the scenario, ensure all Free Run instances with this scenario are terminated
            if (scenario == null && instance.m_Scenario != null)
            {
                instance.m_Scenario.TerminateAllFreeRunningInstancesAsync().Forget();

                // If the Scenario is running, ensure it is stopped.
                if (instance.IsRunning)
                    StopScenario();
            }

            instance.m_Scenario = scenario;
        }

        private bool OnExitStopAllInstances()
        {
            // Stop all Scenario Instances if it is running.
            if (m_IsRunning)
                StopScenario();

            // If no Free Running Instances are active, we are done - return true and resume Editor exit.
            if (!instance.m_Scenario.HasActiveFreeRunInstance() && instance.m_Scenario.StatusData.OverallStatus.State != ExecutionState.Running)
                return true;

            // Else, Stop all Active Free Running Instances as per usual (without "Force kill")
            // Create the tasks to do so and run them async.
            var stopTask = Task.WhenAll(
                instance.m_Scenario.TerminateAllFreeRunningInstancesAsync(),
                UntilScenarioIsNotRunning()
            );
            var freeRunStopTimeoutTask = Task.Delay(5000);

            Task.WhenAny(stopTask, freeRunStopTimeoutTask)
                .ContinueWith((result) =>
                {
                    // If the timeout occurs, mark that as a failure and continue with Editor Exit.
                    var taskId = result.Result.Id;
                    var hasFailed = taskId == freeRunStopTimeoutTask.Id || !result.Result.IsCompletedSuccessfully;
                    if (hasFailed)
                        Debug.LogWarning("MPPM Scenario: Failed to terminate active free running instances " +
                                         "- they may still be running!");
                    EditorApplication.Exit(hasFailed ? -1 : 0);
                }, TaskScheduler.FromCurrentSynchronizationContext());

            return false;
        }

        /// <summary>
        /// Also track to asset changes (textures, meshes, prefabs) that occur in the Main Editor
        /// to notify of Drift.
        /// </summary>
        class ScenarioDriftAssetsTracker : AssetPostprocessor
        {
            public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
                string[] movedAssets, string[] movedFromAssetPaths)
            {
                var scenario = instance.m_Scenario;
                if (scenario != null)
                    scenario.NotifyDrift();
            }
        }

        private async Task UntilScenarioIsNotRunning()
        {
            while (instance.m_Scenario.StatusData.OverallStatus.State == ExecutionState.Running)
            {
                await Task.Delay(100);
            }
        }

        private static bool OnApplicationQuit()
        {
            if (instance.m_Scenario == null)
                return true;

            // Signal Instance cleanup
            return instance.OnExitStopAllInstances();
        }

        public static void StartScenario()
        {
            if (instance.m_Scenario != null && instance.m_Scenario.StatusData.OverallStatus.State == ExecutionState.Running)
            {
                instance.m_CancellationTokenSource?.Cancel();
                instance.m_Scenario.StatusRefreshed -= OnStatusChanged;
                instance.m_Scenario.Reset();
                instance.m_IsRunning = false;

                throw new InvalidOperationException("There is already a scenario running. Stopping it before starting a new one.");
            }

            instance.m_Scenario.Reset();
            instance.RunOrResume();
        }

        private void RunOrResume()
        {
            instance.m_IsRunning = true;
            m_CancellationTokenSource = new CancellationTokenSource();
            instance.m_Scenario.StatusRefreshed += OnStatusChanged;
            instance.m_Scenario.RunOrResumeAsync(m_CancellationTokenSource.Token).Forget();
        }

        private static void OnStatusChanged(ScenarioStatusData status)
        {
            StatusChanged?.Invoke(status);
        }

        public static ScenarioStatusData GetScenarioStatus()
        {
             return instance.m_IsRunning ? instance.m_Scenario.StatusData : default;
        }

        public static void StopScenario()
        {
            AssetMonitor.Reset();

            if (instance.m_Scenario != null)
            {
                instance.m_CancellationTokenSource?.Cancel();

                // We cannot guarantee that the scenario will be stopped as it depends on the nodes implementation.
                // We assume they consume the cancellation token properly.
                instance.m_CancellationTokenSource = null;
                instance.m_IsRunning = false;
            }
            else
            {
                UnityEngine.Debug.LogWarning("Trying to stop an scenario but there is currently no scenario running.");
            }
        }
    }
}
