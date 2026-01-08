// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;
using UnityEngine.Serialization;
using UnityEditor;
using Unity.PlayMode.Editor;
using UnityEngine.Multiplayer.Internal;
using System.Text;
using UnityEditor.PackageManager;
using UnityEditor.Multiplayer.Internal;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class OrchestratedScenario : PlayModeScenario, ISerializationCallbackReceiver
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (MigrationUtility.ShouldDisableMultiplayerPlayMode())
                return;

            PlayModeScenarioManager.RegisterScenarioType<OrchestratedScenario>("Scenario Configuration", "NewPlayModeScenario");

            EditorApplication.playModeStateChanged += UpdateAnalyticsOnPlayModeEnteredFromMppmEvent;
            EditorApplication.playModeStateChanged += SendEnterPlayModeOnTagsAppliedEvent;
        }

        internal static readonly ReadOnlyCollection<string> k_RequiredPackagesForRemoteInstances = new List<string>()
        {
            "com.unity.services.multiplayer",
        }.AsReadOnly();

        [SerializeField] private bool m_EnableEditors = true;
        [SerializeField] private MainEditorInstanceDescription m_MainEditorInstance = new();

        [SerializeField] private List<VirtualEditorInstanceDescription> m_EditorInstances = new();

        [SerializeField] private List<LocalInstanceDescription> m_LocalInstances = new();

        private Scenario m_Scenario;
        private CancellationTokenSource m_CancellationTokenSource;
        private EditorPlayModeGuard m_EditorPlayModeGuard;

        internal Scenario Scenario => m_Scenario;
        internal MainEditorInstanceDescription EditorInstance => m_MainEditorInstance;
        internal ReadOnlyCollection<VirtualEditorInstanceDescription> VirtualEditorInstances => m_EditorInstances.AsReadOnly();
        internal ReadOnlyCollection<LocalInstanceDescription> LocalInstances => m_LocalInstances.AsReadOnly();

        internal override bool SupportsPauseAndStep => true;

        // The following section is for upgrading from 1.0.0-pre.2 to 1.0.0-pre.3.
        // Because m_MainEditorInstance was serialized as reference we need to manually copy the old values to the new instance.
        [SerializeReference, FormerlySerializedAs("m_MainEditorInstance")] private MainEditorInstanceDescription m_MainEditorInstanceObsolete;

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            if (m_MainEditorInstanceObsolete != null)
            {
                var serialized = JsonUtility.ToJson(m_MainEditorInstanceObsolete);
                JsonUtility.FromJsonOverwrite(serialized, m_MainEditorInstance);
                m_MainEditorInstanceObsolete = null;
            }
        }
        // End upgrade section.

        internal List<InstanceDescription> GetAllInstances()
        {
            var instances = new List<InstanceDescription>();

            if (m_EnableEditors)
            {
                Assert.IsNotNull(m_MainEditorInstance);
                Assert.IsNotNull(MultiplayerPlaymode.PlayerOne);
                m_MainEditorInstance.Name = MultiplayerPlaymode.PlayerOne.Name;
                instances.Add(m_MainEditorInstance);
                for (var i = 0; i < m_EditorInstances.Count; i++)
                {
                    var playerIndex = i + 1;// Main editor is PlayerInstanceIndex 0
                    m_EditorInstances[i].PlayerInstanceIndex = playerIndex;
                    m_EditorInstances[i].Name = MultiplayerPlaymode.Players[playerIndex].Name;
                    instances.Add(m_EditorInstances[i]);
                }
            }

            instances.AddRange(m_LocalInstances);
            return instances;
        }

        internal InstanceDescription GetInstanceDescriptionByName(string instanceName)
        {
            var instances = GetAllInstances();
            foreach (var instance in instances)
            {
                if (instance.Name.Equals(instanceName))
                    return instance;
            }

            return null;
        }

        private static void SendEnterPlayModeOnTagsAppliedEvent(PlayModeStateChange state)
        {
            if (VirtualProjectsEditor.IsClone) return;

            var players = MultiplayerPlaymode.Players;
            if (state != PlayModeStateChange.EnteredPlayMode) return;

            foreach (var player in players)
            {
                if (player.PlayerState == PlayerState.Launched && player.Tags.Length > 0)
                {
                    AnalyticsOnTagsAppliedEvent.Send(new OnTagsAppliedData()
                    {
                        PlayerName = player.Name,
                        TagsCount = player.Tags.Length,
                        TagNames = player.Tags,
                        IsFromScenario = PlayModeScenarioManager.ActiveScenario is OrchestratedScenario
                    });
                }
            }
        }

        private static void UpdateAnalyticsOnPlayModeEnteredFromMppmEvent(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                if (PlayModeScenarioManager.ActiveScenario is OrchestratedScenario || VirtualProjectsEditor.IsClone)
                {
                    return;
                }

                List<UnityPlayer> launchedPlayers = new List<UnityPlayer>();
                var players = MultiplayerPlaymode.Players;
                if (MultiplayerPlaymode.Players != null)
                {
                    foreach (var player in players)
                    {
                        //filter all launched virtual players
                        if (player.PlayerState == PlayerState.Launched && player.m_TimeSinceStartingLaunch != DateTime.MinValue)
                        {
                            launchedPlayers.Add(player);
                        }
                    }
                }

                var vpCount = launchedPlayers.Count;
                if (vpCount > 0)
                {
                    AnalyticsEnterPlayModeFromMppmEvent.Send(new EnterPlayModeFromMppmData()
                    {
                        VirtualPlayerCount = vpCount,
                        CloneWindowErrorCount = GetTotalErrorCountForVirtualPlayers(launchedPlayers)
                    });
                }
            }
        }

        private static int GetTotalErrorCountForVirtualPlayers(List<UnityPlayer> players)
        {
            int errorCount = 0;
            foreach (var player in players)
            {
                var logs = MultiplayerPlaymodeLogUtility.PlayerLogs(player.PlayerIdentifier).LogCounts;
                var errorCountForPlayer = logs.Errors;
                errorCount += errorCountForPlayer;

            }
            return errorCount;
        }

        internal override void OnSelected()
        {
            // When a config is selected, initialize and load it into Scenario Runner.
            CreateAndLoadScenario();
            if (m_EditorPlayModeGuard == null)
            {
                m_EditorPlayModeGuard = CreateInstance<EditorPlayModeGuard>();
                m_EditorPlayModeGuard.SetResolutionStrategy(EditorPlayModeGuard.ResolutionStrategy.RevertToDefaultScenario);
            }
        }

        internal override void OnDeselected()
        {
            // Clear any loaded scenario from the scenario runner
            m_Scenario = null;
            ScenarioRunner.LoadScenario(null);
            if (m_EditorPlayModeGuard != null)
            {
                m_EditorPlayModeGuard.Dispose();
                m_EditorPlayModeGuard = null;
            }
        }

        internal override bool WantsToDeselect()
        {
            // If there's no scenario with actively free running instance, nothing to do here.
            if (m_Scenario == null || !m_Scenario.HasActiveFreeRunInstance())
                return true;

            // Else, display a dialog prompting the user to terminate actively running instances.
            var activeInstanceNames = m_Scenario.GetActiveFreeRunInstanceNames();
            string activeInstances = "";
            foreach (var instanceName in activeInstanceNames)
            {
                activeInstances += "- " + instanceName + "\n";
            }
            activeInstances = activeInstances.TrimEnd('\n');
            return EditorUtility.DisplayDialog(
                "Playmode Scenario: Instances(s) are running",
                "Do you want to terminate the following instances and switch scenario? \n \n" + activeInstances,
                "Terminate and Switch",
                "Cancel");
        }

        private void CreateAndLoadScenario()
        {
            if (!IsValid(out string _))
                return;

            // Create the Scenario and load it into the runner
            m_Scenario = CreateScenario();
            SetupEvents();
            ScenarioRunner.LoadScenario(m_Scenario);
        }

        void OnEnable()
        {
            if (m_Scenario != null)
                SetupEvents();
        }

        void SetupEvents()
        {
            m_Scenario.StatusRefreshed -= OnScenarioStatusRefreshed;
            m_Scenario.StatusRefreshed += OnScenarioStatusRefreshed;
        }

        internal void OnScenarioStatusRefreshed(ScenarioStatusData status)
        {
            if (status.OverallStatus.State is ExecutionState.Running or ExecutionState.Active)
            {
                SetState(PlayModeScenarioState.Running);
                if (m_EditorPlayModeGuard != null)
                {
                    m_EditorPlayModeGuard.SetResolutionStrategy(EditorPlayModeGuard.ResolutionStrategy.LogError);
                }
            }
            else
            {
                SetState(PlayModeScenarioState.Idle);
                if (m_EditorPlayModeGuard != null)
                {
                    m_EditorPlayModeGuard.SetResolutionStrategy(EditorPlayModeGuard.ResolutionStrategy.RevertToDefaultScenario);
                }
            }
        }

        protected virtual Scenario CreateScenario()
        {
            return ScenarioFactory.CreateScenario(name, GetAllInstances());
        }

        private void OnValidate()
        {
            // Avoid re-creating the scenario if the scenario is running
            if (ScenarioRunner.GetScenarioStatus().OverallStatus.State == ExecutionState.Running)
                return;

            // Scenario Validation triggered by a domain reload are not considered.
            if (InternalUtilities.IsDomainReloadRequested() ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
            {
                return;
            }

            // If this is the selected config, re-load it into ScenarioRunner
            if (PlayModeScenarioManager.ActiveScenario is OrchestratedScenario config && config == this)
                CreateAndLoadScenario();
        }

        internal override void ExecuteStart()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Quick Sanity check.
            if (m_Scenario == null)
                Debug.LogError("Attempted to start Scenario with none set.");

            StartScenarioAsync().Forget();
        }

        private async Task StartScenarioAsync()
        {
            m_CancellationTokenSource = new CancellationTokenSource();

            // Check instance(s) setup before starting the scenario
            await RunPreStartChecksAsync(m_CancellationTokenSource.Token);

            ScenarioRunner.StartScenario();

            LaunchingScenarioWindow.OnScenarioStarted(this);
        }

        /// <summary>
        /// Performs validation checks on the scenario instance(s) before starting scenario
        /// If validation fails, displays an error dialog, sends an analytics event,
        /// and throws a InvalidOperationException
        /// </summary>
        private async Task RunPreStartChecksAsync(CancellationToken cancellationToken)
        {
            var validationResult = await m_Scenario.ValidateForRunningAsync(cancellationToken);

            if (!validationResult.IsValid)
            {
                var instances = GetInstancesFromDescriptions(GetAllInstances());
                //  Sanity check
                if (instances == null || instances.Count == 0)
                {
                    return;
                }

                var instancesData = Instance.GetAnalyticsDataArray(instances);
                var errorData = Instance.GetValidationErrorData(validationResult);

                // if the validation fails before StartScenario(), send simplified Instances data and validation result as Errors
                AnalyticsOnPlayFromScenarioEvent.SendValidationErrorData(
                    instancesData,
                    new[] { errorData }
                );

                EditorUtility.DisplayDialog(
                    $"Play Mode Scenario - Scenario Setup Error ",
                    $"{validationResult.Message}. Please check the console for more details.",
                    "OK"
                );
                throw new InvalidOperationException($"Scenario validation failed. {validationResult.Message}");
            }
        }

        private static List<Instance> GetInstancesFromDescriptions(List<InstanceDescription> instanceDescriptions)
        {
            var currentConfig = PlayModeScenarioManager.ActiveScenario as OrchestratedScenario;
            if (currentConfig == null || currentConfig.Scenario == null)
                return new List<Instance>();

            var result = new List<Instance>();

            // Get the instances from the list of instance descriptions
            foreach (var desc in instanceDescriptions)
            {
                var instance = currentConfig.Scenario.GetInstanceByName(desc.Name);
                if (instance != null)
                {
                    result.Add(instance);
                }
            }
            return result;
        }

        internal override void ExecuteStop()
        {
            m_CancellationTokenSource?.Cancel();
            ScenarioRunner.StopScenario();
            var statusData = Scenario.StatusData;
            if (statusData.OverallStatus.State == ExecutionState.Failed)
            {
                if (statusData.OverallStatus.FailedNodesCount > 0)
                {
                    var errorText = new StringBuilder();
                    foreach (var error in Scenario.GetAllNonFreeRunNodeErrors())
                    {
                        errorText.AppendLine($"\n\t -> {error.Message}");
                    }
                    Debug.LogError($"Scenario failed with error:{errorText}");
                }
                else
                    Debug.LogError($"Scenario failed with unknown error.");
            }
        }

        internal override VisualElement CreateTopbarUI() => new MultiplayerPlayModeStatusButton(this);
        internal override Texture2D Icon => Icons.GetImage(Icons.ImageName.PlayModeScenario);

        internal override VisualElement CreateScenarioUI()
        {
            return new OrchestratedScenarioStatusElement(this);
        }

        private static bool IsConditionMetForAll<T>(Func<T, bool> condition, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                if (!condition(item))
                    return false;
            }
            return true;
        }

        internal override bool IsValid(out string reasonForInvalidConfiguration)
        {
            reasonForInvalidConfiguration = "";

            // Check if instance's tags are valid and clear them if not.
            var allInstances = GetAllInstances();
            foreach (var instance in allInstances)
            {
                if (instance is EditorInstanceDescription editorInstance)
                {
                    var currTag = editorInstance.PlayerTag;
                    if (!string.IsNullOrEmpty(currTag) && !MultiplayerPlaymode.PlayerTags.Contains(currTag))
                        editorInstance.PlayerTag = "";
                }
            }

            // Check if instance names are unique
            List<string> takenNames = new List<string>();
            bool containsTakenName = false;

            if (allInstances.Count == 0)
            {
                reasonForInvalidConfiguration = "Scenario must have at least one instance.";
                return false;
            }

            foreach (var instance in allInstances)
            {
                if (takenNames.Contains(instance.Name))
                {
                    reasonForInvalidConfiguration = "Instance names must be unique.";
                    containsTakenName = true;
                    break;
                }
                takenNames.Add(instance.Name);
            }

            // Iterate through all local instances.
            // - Check if local mobile device instances have a device selected that is unique.
            // - Track any Local Sim instances that we have for verification later.
            List<LocalInstanceDescription> localMobileDevices = new List<LocalInstanceDescription>();
            foreach (var instance in allInstances)
            {
                if (instance is LocalInstanceDescription localInstance)
                {
                    if (localInstance.BuildProfile != null)
                    {
                        if (InternalUtilities.IsAndroidBuildTarget(localInstance.BuildProfile))
                            localMobileDevices.Add(localInstance);
                    }
                }
            }

            var localMobileDevicesSelected = IsConditionMetForAll(
                instance => instance != null && instance.BuildProfile != null && !string.IsNullOrEmpty(instance.AdvancedConfiguration.DeviceID),
                localMobileDevices);
            if (!localMobileDevicesSelected)
                reasonForInvalidConfiguration += "\nLocal mobile device instance(s) must have a device selected.";

            List<string> takenIDs = new List<string>();
            bool containsTakenDeviceID = false;
            foreach (var instance in localMobileDevices)
            {
                if (takenIDs.Contains(instance.AdvancedConfiguration.DeviceID))
                {
                    reasonForInvalidConfiguration = "Device must be associated with only a single instance.";
                    containsTakenDeviceID = true;
                    break;
                }
                takenIDs.Add(instance.AdvancedConfiguration.DeviceID);
            }

            // Check if local build targets are supported for building
            var localBuildTargetsAreSupported = m_LocalInstances.Count == 0 || IsConditionMetForAll(instance =>
                instance != null && instance.BuildProfile != null && InternalUtilities.IsBuildProfileSupported(instance.BuildProfile),
                m_LocalInstances);
            if (!localBuildTargetsAreSupported)
                reasonForInvalidConfiguration += "\nLocal instance(s) have incorrect build target";

            // Check if local build targets are supported to run.
            // This is necessary because if for example Linux Build Support is available but we are running on Windows, we can build but we cannot start the instance.
            var localBuildTargetsCanRunOnPlatform = m_LocalInstances.Count == 0 || IsConditionMetForAll(instance =>
                instance != null && instance.BuildProfile != null && InternalUtilities.BuildProfileCanRunOnCurrentPlatform(instance.BuildProfile),
                m_LocalInstances);
            if (!localBuildTargetsCanRunOnPlatform)
                reasonForInvalidConfiguration += "\nLocal instance(s) buildtarget cannot run on current platform.";

            // Check if we have more than one server role.
            var configHasMoreServerInstances = ConfigurationHasMaxOneServer();
            if (!configHasMoreServerInstances)
                reasonForInvalidConfiguration += "\nOnly one Server Role is allowed per Configuration.";

            reasonForInvalidConfiguration = reasonForInvalidConfiguration.Trim('\n');
            return localBuildTargetsAreSupported && localBuildTargetsCanRunOnPlatform &&
                   configHasMoreServerInstances && localMobileDevicesSelected &&
                   !containsTakenName && !containsTakenDeviceID;
        }

        bool ConfigurationHasMaxOneServer()
        {
            var allInstances = GetAllInstances();
            int serverCount = 0;
            foreach (var instance in allInstances)
            {
                if (ScenarioFactory.GetRoleForInstance(instance).HasFlag(MultiplayerRoleFlags.Server))
                {
                    serverCount++;
                }
            }
            return serverCount < 2;
        }

        internal static async Task LoadPackagesAsync()
        {
            // Grab required packages as an array of strings
            var packages = k_RequiredPackagesForRemoteInstances;
            string[] packagesArray = new string[packages.Count];
            packages.CopyTo(packagesArray, 0);

            // Perform package install
            var request = Client.AddAndRemove(packagesArray);
            while (!request.IsCompleted)
            {
                // Await and don't block the current thread
                await Task.Delay(100);
                await Task.Yield();
            }

            if (request.Error != null)
                Debug.LogError($"Failed to install packages: {request.Error.message}");
        }

        internal static void PreventScriptableObjectUnload(ScriptableObject obj)
        {
            // The editor can perform some cleaning of all objects and assets during Play Mode transitions.
            // Only persistent objects (the ones attached to an asset file) and those marked with DontSaveInEditor survive this cleaning.
            // We also want to avoid setting the DontUnloadUnusedAsset flag, since that would prevent proper cleanup of
            // ScenarioObjects when their owning Scenario is disposed.
            const HideFlags k_HideFlags = HideFlags.DontSaveInEditor;

            obj.hideFlags |= k_HideFlags;
        }
    }
}
