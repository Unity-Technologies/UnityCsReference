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

namespace Unity.Multiplayer.PlayMode.Editor
{
    class ScenarioConfig : PlayModeConfiguration, ISerializationCallbackReceiver
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (MigrationUtility.ShouldDisableMultiplayerPlayMode())
                return;

            PlayModeConfigurationUtils.RegisterPlayModeConfigurationType<ScenarioConfig>("Scenario Configuration", "NewPlayModeScenario");

            EditorApplication.playModeStateChanged += UpdateAnalyticsOnPlayModeEnteredFromMppmEvent;
            EditorApplication.playModeStateChanged += SendEnterPlayModeOnTagsAppliedEvent;
        }

        public static readonly ReadOnlyCollection<string> k_RequiredPackagesForRemoteInstances = new List<string>()
        {
            "com.unity.services.multiplayer@1.1.4",
        }.AsReadOnly();

        [SerializeField] private bool m_EnableEditors = true;
        [SerializeField] private MainEditorInstanceDescription m_MainEditorInstance = new();

        [Tooltip("Initial Editor Instances when entering playmode. Editor Instances will only have limited authoring capabilities.")]
        [SerializeField] private List<VirtualEditorInstanceDescription> m_EditorInstances = new();

        [Tooltip("Local Instances are builds that will run on the same machine as the editor.")]
        [SerializeField] private List<LocalInstanceDescription> m_LocalInstances = new();

        [Tooltip("Remote Instances are builds that will get deployed to UGS and will run there.")]
        [SerializeField] private List<RemoteInstanceDescription> m_RemoteInstances = new();

        private Scenario m_Scenario;

        public Scenario Scenario => m_Scenario;
        public MainEditorInstanceDescription EditorInstance => m_MainEditorInstance;
        public ReadOnlyCollection<VirtualEditorInstanceDescription> VirtualEditorInstances => m_EditorInstances.AsReadOnly();
        public ReadOnlyCollection<LocalInstanceDescription> LocalInstances => m_LocalInstances.AsReadOnly();
        public ReadOnlyCollection<RemoteInstanceDescription> RemoteInstances => m_RemoteInstances.AsReadOnly();

        public override bool SupportsPauseAndStep => true;

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

        public List<InstanceDescription> GetAllInstances()
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
            instances.AddRange(m_RemoteInstances);
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
                        IsFromScenario = PlayModeManager.instance.ActivePlayModeConfig is ScenarioConfig
                    });
                }
            }
        }

        private static void UpdateAnalyticsOnPlayModeEnteredFromMppmEvent(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                if (PlayModeManager.instance.ActivePlayModeConfig is ScenarioConfig || VirtualProjectsEditor.IsClone)
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

        public override void OnConfigSelected()
        {
            // When a config is selected, initialize and load it into Scenario Runner.
            CreateAndLoadScenario();
        }

        public override void OnConfigDeselected()
        {
            // Clear any loaded scenario from the scenario runner
            m_Scenario = null;
            ScenarioRunner.LoadScenario(null);
        }

        public override bool WantsToDeselectConfiguration()
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
            if (!IsConfigurationValid(out string _))
                return;

            // Create the Scenario and load it into the runner
            m_Scenario = CreateScenario();
            ScenarioRunner.LoadScenario(m_Scenario);
        }

        protected virtual Scenario CreateScenario()
        {
            return ScenarioFactory.CreateScenario(name, GetAllInstances());
        }

        private void OnValidate()
        {
            // Avoid re-creating the scenario if the scenario is running
            if (ScenarioRunner.GetScenarioStatus().State == ScenarioState.Running)
                return;

            // Scenario Validation triggered by a domain reload are not considered.
            if (InternalUtilities.IsDomainReloadRequested() ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
            {
                return;
            }

            // If this is the selected config, re-load it into ScenarioRunner
            if (PlayModeManager.instance.ActivePlayModeConfig is ScenarioConfig config && config == this)
                CreateAndLoadScenario();
        }

        public override async Task ExecuteStartAsync(CancellationToken cancellationToken)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new TaskCanceledException();

            // Quick Sanity check.
            if (m_Scenario == null)
                Debug.LogError("Attempted to start Scenario with none set.");

            // Check instance(s) setup before starting the scenario
            await RunPreStartChecksAsync(cancellationToken);

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
            var currentConfig = PlayModeManager.instance.ActivePlayModeConfig as ScenarioConfig;
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

        public override void ExecuteStop()
        {
            ScenarioRunner.StopScenario();
            var state = ScenarioRunner.GetScenarioStatus();
            if (state.State == ScenarioState.Failed)
            {
                if (state.Errors != null)
                {
                    var errors = new StringBuilder();
                    foreach (var error in state.Errors)
                    {
                        errors.AppendLine($"\n\t -> {error.Message}");
                    }
                    Debug.LogError($"Scenario failed with error:{errors}");
                }
                else
                    Debug.LogError($"Scenario failed with unknown error.");
            }
        }

        public override VisualElement CreateTopbarUI() => new MultiplayerPlayModeStatusButton(this);
        public override Texture2D Icon => Icons.GetImage(Icons.ImageName.PlayModeScenario);

        public override string Description
        {
            get
            {
                var summary = "\n1 Editor instance\n";

                var localInstanceCount = m_LocalInstances.Count;
                if (localInstanceCount > 0)
                    summary += $"{localInstanceCount} Local instance{(localInstanceCount > 1 ? "s" : "")}\n";
                var remoteInstanceCount = m_RemoteInstances.Count;
                if (remoteInstanceCount > 0)
                    summary += $"{remoteInstanceCount} Remote instance{(remoteInstanceCount > 1 ? "s" : "")}\n";

                return (base.Description + summary).Trim('\n');
            }
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

        public override bool IsConfigurationValid(out string reasonForInvalidConfiguration)
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

            // Check if local mobile device instances have a device selected that is unique
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

            // Check if we have the correct packages installed for running a remote server.
            if (!PackagesForRemoteDeployInstalled(out var missingPacks) && m_RemoteInstances.Count > 0)
                reasonForInvalidConfiguration += "\nPackages are missing:\n" + string.Join("\n", missingPacks);

            // Check if remote build targets are supported to be build.
            var remoteBuildTargetsCorrect = m_RemoteInstances.Count == 0 || IsConditionMetForAll(instance =>
                instance != null && instance.BuildProfile != null &&
                InternalUtilities.IsBuildProfileSupported(instance.BuildProfile) &&
                !InternalUtilities.IsAndroidBuildTarget(instance.BuildProfile),
                m_RemoteInstances);
            if (!remoteBuildTargetsCorrect)
                reasonForInvalidConfiguration += "\nRemote instance(s) have incorrect build target.";

            // Check if remote instances have incorrect multiplayer role
            var remoteInstancesHaveServerRole = m_RemoteInstances.Count == 0 || IsConditionMetForAll(instance =>
                instance != null && LocalDeploymentUtility.IsServerProfileOrRole(instance.BuildProfile),
                m_RemoteInstances);

            if (!remoteInstancesHaveServerRole)
                reasonForInvalidConfiguration += "\nRemote instance(s) must have Server Role or a Server Build Profile.";

            // Check if we have more than one server role.
            var configHasMoreServerInstances = ConfigurationHasMaxOneServer();
            if (!configHasMoreServerInstances)
                reasonForInvalidConfiguration += "\nOnly one Server Role is allowed per Configuration.";

            reasonForInvalidConfiguration = reasonForInvalidConfiguration.Trim('\n');
            return localBuildTargetsAreSupported && remoteBuildTargetsCorrect && localBuildTargetsCanRunOnPlatform &&
                   configHasMoreServerInstances && localMobileDevicesSelected && !containsTakenName &&
                   !containsTakenDeviceID && remoteInstancesHaveServerRole;
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

        public static bool PackagesForRemoteDeployInstalled(out List<string> missingPacks)
        {
            missingPacks = new List<string>();

            foreach (var packIds in k_RequiredPackagesForRemoteInstances)
            {
                var nameParts = packIds.Split('@');
                var packageName = nameParts[0];
                var requiredVersion = nameParts[1];
                var packInfo = UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageName);
                var packageIsInstalled = false;
                if (packInfo != null)
                {
                    var installedVersion = packInfo.version;
                    packageIsInstalled = IsPackageVersionCompatible(installedVersion, requiredVersion);
                }

                var packInstalled = packInfo != null && packageIsInstalled;
                if (!packInstalled)
                    missingPacks.Add(packIds);
            }
            return missingPacks.Count == 0;
        }

        private static bool IsPackageVersionCompatible(string installedVersion, string requiredVersion)
        {
            SplitPackageVersion(installedVersion, out var installedMajor, out var installedMinor, out var installedPatch, out var installedPre, out var isInstalledPre);
            SplitPackageVersion(requiredVersion, out var requiredMajor, out var requiredMinor, out var requiredPatch, out var requiredPre, out var isRequiredPre);

            if (installedMajor < requiredMajor)
                return false;

            if (installedMajor > requiredMajor)
                return true;

            if (installedMinor < requiredMinor)
                return false;

            if (installedMinor > requiredMinor)
                return true;

            if (installedPatch < requiredPatch)
                return false;

            if (installedPatch > requiredPatch)
                return true;

            if (isInstalledPre && !isRequiredPre)
                return false;

            if (isInstalledPre && isRequiredPre && installedPre < requiredPre)
                return false;

            return true;
        }

        private static void SplitPackageVersion(string version, out int major, out int minor, out int patch, out int pre, out bool isPre)
        {
            var regex = new System.Text.RegularExpressions.Regex(@"(\d+)\.(\d+)\.(\d+)(?:-pre\.(\d+))?");
            var match = regex.Match(version);

            if (!match.Success)
                throw new ArgumentException($"Invalid version string: {version}");

            major = int.Parse(match.Groups[1].Value);
            minor = int.Parse(match.Groups[2].Value);
            patch = int.Parse(match.Groups[3].Value);

            isPre = match.Groups[4].Success;
            pre = isPre ? int.Parse(match.Groups[4].Value) : -1;
        }
    }
}
