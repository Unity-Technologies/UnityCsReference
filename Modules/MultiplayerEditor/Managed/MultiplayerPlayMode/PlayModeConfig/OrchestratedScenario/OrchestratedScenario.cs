// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;
using UnityEditor;
using Unity.PlayMode.Editor;
using UnityEngine.Multiplayer.Internal;
using System.Text;
using UnityEditor.PackageManager;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.PlayMode.Editor
{
    sealed partial class OrchestratedScenario : PlayModeScenario, ISerializationCallbackReceiver
    {
        const string k_ValidationDialogTitle = "Play Mode Scenario - Validation Failed";
        const string k_ValidationDialogScenarioMessage = "The scenario cannot be started because validation failed with the following message:";
        const string k_ValidationDialogInstanceMessage = "The scenario instance cannot be started because validation failed with the following message:";
        const string k_ValidationDialogOKLabel = "OK";
        const int k_CurrentSerializedVersion = 1;
        const string k_MainEditorName = "Main Editor";
        internal const string k_SettingsPropertyName = nameof(m_Settings);
        internal const int k_MaxCloneEditorInstances = 3;
        internal const int k_MaxPlayerInstances = 4;

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

        [SerializeField] private int m_SerializedVersion;
#pragma warning disable 169
        [SerializeField] private OrchestratedScenarioSettings m_Settings;
#pragma warning restore 169

        private Scenario m_Scenario;
        private CancellationTokenSource m_CancellationTokenSource;
        private EditorPlayModeGuard m_EditorPlayModeGuard;

        internal Scenario Scenario => m_Scenario;
        internal ref OrchestratedScenarioSettings Settings => ref m_Settings;

        internal override bool SupportsPauseAndStep => true;

        public void OnBeforeSerialize()
        {
            m_SerializedVersion = k_CurrentSerializedVersion;
        }

        public void OnAfterDeserialize()
        {
            if (m_SerializedVersion != k_CurrentSerializedVersion)
                UpgradeSerialization();

            // External edits to the serialization file (e.g. git merges) may lead to inconsistent settings
            // as for example having multiple Main Editor instances or more than the allowed number of Clone Editor instances.
            MakeSettingsConsistent();
        }

        void Awake()
        {
            // OnAfterDeserialize is not called when the ScriptableObject is first created, in such case
            // calling MakeSettingsConsistent creates the initial consistent state (One Main Editor instance).
            MakeSettingsConsistent();
        }

        internal IEnumerable<IInstanceItem> GetAllInstances()
        {
            for (var i = 0; i < m_Settings.InstanceCount; i++)
            {
                var instance = m_Settings[i];
                if (!IsInstanceEnabled(instance))
                    continue;

                yield return instance;
            }
        }

        internal bool IsInstanceEnabled(IInstanceItem instance)
        {
            return m_EnableEditors || !instance.IsInstanceType(typeof(EditorController<>));
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
            CreateAndLoadScenario(false);
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

        private void CreateAndLoadScenario(bool logErrorIfInvalid)
        {
            if (!IsValid(out var reasonForInvalidConfiguration))
            {
                if (logErrorIfInvalid)
                    Debug.LogError($"Cannot load Scenario Configuration '{name}': {reasonForInvalidConfiguration}");

                return;
            }

            // Create the Scenario and load it into the runner
            m_Scenario = CreateScenario();
            SetupEvents();
            ScenarioRunner.LoadScenario(m_Scenario);

            // Refresh the active scenario window as visual elements could have references to the Instance objects.
            // Those objects were recreated when creating the new Scenario, so we need to refresh the window to avoid unsynced references.
            var windows = Resources.FindObjectsOfTypeAll<ActiveScenarioWindow>();
            foreach (var window in windows)
            {
                window.Refresh();
            }
        }

        void OnEnable()
        {
            // The settings internal hash comparison should take care of most common cases of it needing a refresh,
            // but to cover potential external edits to the scenario asset (e.g. git merges)
            // we force a refresh when the object is loaded (OnEnable), which happens less often.
            m_Settings.RefreshDecorators(force: true);

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
            if (status.IsExecuting())
            {
                SetState(status.CurrentStage is ExecutionStage.Cleanup ? PlayModeScenarioState.Stopping : PlayModeScenarioState.Running);
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

        Scenario CreateScenario()
        {
            return ScenarioFactory.CreateScenario(this, GetAllInstances());
        }

        private void OnValidate()
        {
            // Validation happens very often, so we don't want to force a refresh of the decorators every time.
            // As long as the file is not externally edited, the hash comparison inside RefreshDecorators will prevent unnecessary refreshes,
            // still, for potential external edits, we force a refresh when the object is loaded (OnEnable), which happens less often.
            m_Settings.RefreshDecorators(force: false);

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
                CreateAndLoadScenario(false);
        }

        void MakeSettingsConsistent()
        {
            var cloneEditorCount = 0;
            var foundMainEditor = false;
            for (var i = 0; i < m_Settings.InstanceCount; i++)
            {
                var instanceItem = m_Settings[i];
                var isEditorInstance = instanceItem.IsInstanceType(typeof(EditorController<>));

                if (instanceItem.IsInstanceType(typeof(MainEditorController)))
                {
                    if (foundMainEditor)
                    {
                        m_Settings.RemoveInstanceAt(i);
                        i--;
                        continue;
                    }

                    foundMainEditor = true;
                }

                if (!m_EnableEditors && isEditorInstance)
                    continue;

                if (instanceItem.IsInstanceType(typeof(CloneEditorController)))
                {
                    cloneEditorCount++;

                    if (cloneEditorCount > k_MaxCloneEditorInstances)
                        continue;

                    var settings = instanceItem.GetSettings<CloneEditorController.InstanceSettings>();
                    var cloneEditorName = $"Player {cloneEditorCount + 1}";

                    if (instanceItem.GetName() == cloneEditorName && settings.PlayerInstanceIndex == cloneEditorCount)
                        continue;

                    var newName = cloneEditorName;
                    settings.PlayerInstanceIndex = cloneEditorCount;

                    m_Settings[i] = instanceItem
                        .WithName(newName)
                        .WithSettings(settings);
                }
            }

            if (!foundMainEditor)
            {
                m_Settings.AddInstance<MainEditorController, MainEditorController.InstanceSettings>(k_MainEditorName);
            }
        }

        internal override void ExecuteStart()
        {
            // Ensure the scenario represents the latest configuration
            CreateAndLoadScenario(true);

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            StartScenarioAsync();
        }

        private void StartScenarioAsync()
        {
            m_CancellationTokenSource = new CancellationTokenSource();

            ScenarioRunner.StartScenario();

            LaunchingScenarioWindow.OnScenarioStarted(this);
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

            // Iterate through all local instances.
            // - Check if local mobile device instances have a device selected that is unique.
            // - Track any Local Sim instances that we have for verification later.
            List<IInstanceItem> localMobileInstances = new List<IInstanceItem>();
            List<IInstanceItem> localInstances = new List<IInstanceItem>();
            var instanceNames = new HashSet<string>();
            var duplicateNamesFound = false;
            var instancesCount = 0;
            foreach (var instance in GetAllInstances())
            {
                if (instanceNames.Contains(instance.GetName()))
                {
                    duplicateNamesFound = true;
                }
                else
                {
                    instanceNames.Add(instance.GetName());

                }

                if (instance.IsInstanceType(typeof(LocalPlayerController)))
                {
                    localInstances.Add(instance);

                    var settings = instance.GetSettings<LocalPlayerController.InstanceSettings>();
                    if (settings.BuildProfile != null)
                    {
                        if (InternalUtilities.IsAndroidBuildTarget(settings.BuildProfile))
                            localMobileInstances.Add(instance);
                    }
                }
                instancesCount++;
            }

            if (duplicateNamesFound)
            {
                reasonForInvalidConfiguration += "Instance names must be unique.";
            }

            if (instancesCount == 0)
            {
                reasonForInvalidConfiguration += "Scenario must have at least one instance.";
                return false;
            }

            var localMobileDevicesSelected = IsConditionMetForAll(
                instance => instance != null && instance.GetSettings<LocalPlayerController.InstanceSettings>().BuildProfile != null && !string.IsNullOrEmpty(instance.GetUserSettings<LocalPlayerController.UserSettings>(this).DeviceID),
                localMobileInstances);
            if (!localMobileDevicesSelected)
                reasonForInvalidConfiguration += "\nLocal mobile device instance(s) must have a device selected.";

            List<string> takenIDs = new List<string>();
            bool containsTakenDeviceID = false;
            foreach (var instance in localMobileInstances)
            {
                if (takenIDs.Contains(instance.GetUserSettings<LocalPlayerController.UserSettings>(this).DeviceID))
                {
                    reasonForInvalidConfiguration = "Device must be associated with only a single instance.";
                    containsTakenDeviceID = true;
                    break;
                }
                takenIDs.Add(instance.GetUserSettings<LocalPlayerController.UserSettings>(this).DeviceID);
            }

            // Check if local build targets are supported for building
            var localBuildTargetsAreSupported = localInstances.Count == 0 || IsConditionMetForAll(instance =>
                instance != null && instance.GetSettings<LocalPlayerController.InstanceSettings>().BuildProfile != null && InternalUtilities.IsBuildProfileSupported(instance.GetSettings<LocalPlayerController.InstanceSettings>().BuildProfile),
                localInstances);
            if (!localBuildTargetsAreSupported)
                reasonForInvalidConfiguration += "\nLocal instance(s) have incorrect build target";

            // Check if local build targets are supported to run.
            // This is necessary because if for example Linux Build Support is available but we are running on Windows, we can build but we cannot start the instance.
            var localBuildTargetsCanRunOnPlatform = localInstances.Count == 0 || IsConditionMetForAll(instance =>
                instance != null && instance.GetSettings<LocalPlayerController.InstanceSettings>().BuildProfile != null && InternalUtilities.BuildProfileCanRunOnCurrentPlatform(instance.GetSettings<LocalPlayerController.InstanceSettings>().BuildProfile),
                localInstances);
            if (!localBuildTargetsCanRunOnPlatform)
                reasonForInvalidConfiguration += "\nLocal instance(s) buildtarget cannot run on current platform.";

            // Check if we have more than one server role.
            var configHasMoreServerInstances = ConfigurationHasMaxOneServer();
            if (!configHasMoreServerInstances)
                reasonForInvalidConfiguration += "\nOnly one Server Role is allowed per Configuration.";

            reasonForInvalidConfiguration = reasonForInvalidConfiguration.Trim('\n');
            return localBuildTargetsAreSupported && localBuildTargetsCanRunOnPlatform &&
                   configHasMoreServerInstances && localMobileDevicesSelected && !containsTakenDeviceID && !duplicateNamesFound;
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

        internal static void NotifyValidationFailure(Scenario scenario)
        {
            ScenarioDialog.DisplayDialog(k_ValidationDialogTitle,
                GetValidationMessage(k_ValidationDialogScenarioMessage, scenario.GetNodes(ExecutionStage.Validate)),
                k_ValidationDialogOKLabel);
        }

        internal static void NotifyValidationFailure(Instance instance)
        {
            ScenarioDialog.DisplayDialog(k_ValidationDialogTitle,
                GetValidationMessage(k_ValidationDialogInstanceMessage, instance.GetExecutionGraph().GetNodes(ExecutionStage.Validate)),
                k_ValidationDialogOKLabel);
        }

        static string GetValidationMessage(string prefix, IEnumerable<ExecutionNode> nodes)
        {
            var message = $"{prefix}\n";
            var failedNodesCount = 0;
            foreach (var node in nodes)
            {
                if (node.State is ExecutionState.Failed)
                {
                    failedNodesCount++;

                    if (string.IsNullOrEmpty(node.ErrorInfo.Message))
                    {
                        message += $"\n- {node.Name} ({node.GetType().Name}) failed without an error message.";
                        continue;
                    }

                    message += $"\n- {node.ErrorInfo.Message}";
                }
            }
            Assert.IsTrue(failedNodesCount > 0, "Trying to notify a scenario validation failure but no failed nodes were found");
            return message;
        }
    }
}
