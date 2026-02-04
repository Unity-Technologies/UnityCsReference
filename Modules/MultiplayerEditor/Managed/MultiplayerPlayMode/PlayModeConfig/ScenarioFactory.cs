// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEditor.Build.Profile;
using System.Text.RegularExpressions;
using Unity.PlayMode.Editor;
using UnityEngine.Multiplayer.Internal;
using UnityEditor.Multiplayer.Internal;

namespace Unity.Multiplayer.PlayMode.Editor
{
    /// <summary>
    /// Creates a scenario graph from a list of instance descriptions.
    /// </summary>
    internal static class ScenarioFactory
    {
        public static MultiplayerRoleFlags GetRoleForInstance(IInstanceItem instance)
        {
            if (!EditorMultiplayerManager.enableMultiplayerRoles)
                return MultiplayerRoleFlags.Client;

            if (instance.IsInstanceType(typeof(CloneEditorController))) return instance.GetSettings<CloneEditorController.InstanceSettings>().RoleMask;
            if (instance.IsInstanceType(typeof(MainEditorController))) return instance.GetSettings<MainEditorController.InstanceSettings>().RoleMask;
            if (instance.IsInstanceType(typeof(LocalPlayerController)))
            {
                var buildProfile = instance.GetSettings<LocalPlayerController.InstanceSettings>().BuildProfile;
                return buildProfile == null ? MultiplayerRoleFlags.Client : MultiplayerRolesSettings.instance.GetMultiplayerRoleForBuildProfile(buildProfile);
            }
            return MultiplayerRoleFlags.Client;
        }

        private static void CategorizeInstances(
            IEnumerable<IInstanceItem> instanceItems,
            out List<IInstanceItem> servers,
            out List<IInstanceItem> clients)
        {
            servers = new List<IInstanceItem>();
            clients = new List<IInstanceItem>();
            foreach (var instance in instanceItems)
            {
                if (GetRoleForInstance(instance).HasFlag(MultiplayerRoleFlags.Server))
                {
                    servers.Add(instance);
                }
                else
                {
                    clients.Add(instance);
                }
            }
        }

        public static Scenario CreateScenario(string name, IEnumerable<IInstanceItem> instanceItems)
        {
            var scenario = Scenario.Create(name);

            CategorizeInstances(instanceItems, out var serverDescriptList, out var clientDescriptList);

            // [TODO] It's good to report the error if multiple servers are selected but we also need to report it earlier, directly in the configuration UI.
            Assert.IsTrue(serverDescriptList.Count <= 1, "There can only be one server in a scenario");

            // This will ensure the server instance is the first to be added in the scenario.
            if (serverDescriptList.Count > 0)
            {
                var serverInstance = ConnectOrCreateInstance(serverDescriptList[0]);
                scenario.AddInstance(serverInstance);
            }

            // Finally, iterate through the rest of the instance descriptions, construct client instances
            // and configure the connection data for when they run.
            foreach (var clientDescript in clientDescriptList)
            {
                var clientInstance = ConnectOrCreateInstance(clientDescript);
                scenario.AddInstance(clientInstance);
            }
            return scenario;
        }

        private static Instance ConnectOrCreateInstance(IInstanceItem instanceItem)
        {
            // If an Existing Instance is Actively Free Running, we connect that instance to this new Scenario.
            if (PlayModeScenarioManager.ActiveScenario is OrchestratedScenario config &&
                config.Scenario != null)
            {
                var activeFreeInstance = config.Scenario.GetInstanceByName(instanceItem.GetName(), true);
                if (activeFreeInstance != null)
                {
                    // Ensure we remove it from the old Scenario
                    config.Scenario.RemoveInstance(activeFreeInstance);

                    // Finally return the Free run instance to be attached to the new one.
                    return activeFreeInstance;
                }
            }

            // Else, create and rebuild the instance from the description as per usual.
            return CreateInstance(instanceItem);
        }

        private static Instance CreateInstance(IInstanceItem instanceItem)
        {
            var controller = instanceItem.CreateController();
            var instance = Instance.Create(instanceItem, controller);
            var executionGraph = instance.GetExecutionGraph();

            controller.SetupExecutionGraph(executionGraph);

            return instance;
        }

        internal static string GenerateBuildPath(BuildProfile profile)
        {
            // It is important that all builds are in its own folder because we might want to deploy it or move it elsewhere,
            // If we have multiple builds in the same folder, we will move all of them.
            var escapedProfileName = EscapeProfileName(profile.name);
            return $"Builds/PlayModeScenarios/{escapedProfileName}/{escapedProfileName}";
        }

        private static string EscapeProfileName(string path) => Regex.Replace(path, @"[^\w\d]", "_");
    }
}
