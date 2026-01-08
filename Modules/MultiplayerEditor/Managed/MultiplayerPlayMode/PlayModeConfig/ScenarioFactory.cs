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
        public static MultiplayerRoleFlags GetRoleForInstance(InstanceDescription instance)
        {
            Assert.IsNotNull(instance, $"Null instance used");
            switch (instance)
            {
                case EditorInstanceDescription editorInstance: return editorInstance.RoleMask;
                case IBuildableInstanceDescription buildableInstance: return buildableInstance.BuildProfile == null ? MultiplayerRoleFlags.Client : MultiplayerRolesSettings.instance.GetMultiplayerRoleForBuildProfile(buildableInstance.BuildProfile);
            }
            return MultiplayerRoleFlags.Client;
        }

        internal static class RemoteNodeConstants
        {
            internal const string k_BuildNodePostFix = "- Build";
            internal const string k_DeployBuildNodePostfix = "- Deploy Build";
            internal const string k_DeployConfigBuildNodePostfix = "- Deploy Build Configuration";
            internal const string k_DeployFleetNodePostfix = "- Deploy Fleet";
            internal const string k_RunNodePostfix = "- Run";
            internal const string k_AllocateNodePostfix = "- Allocate";
        }

        private static void CategorizeInstances(
            List<InstanceDescription> instances,
            out List<InstanceDescription> servers,
            out List<InstanceDescription> clients,
            out bool hasMainEditor)
        {
            servers = new List<InstanceDescription>();
            clients = new List<InstanceDescription>();
            hasMainEditor = false;

            foreach (var instance in instances)
            {
                if (instance is MainEditorInstanceDescription)
                {
                    hasMainEditor = true;
                }

                if (GetRoleForInstance(instance).HasFlag(MultiplayerRoleFlags.Server))
                    servers.Add(instance);
                else
                {
                    clients.Add(instance);
                }
            }
        }

        public static Scenario CreateScenario(string name, List<InstanceDescription> instanceDescriptions)
        {
            var scenario = Scenario.Create(name);

            CategorizeInstances(instanceDescriptions, out var serverDescriptList, out var clientDescriptList, out var hasEditor);

            // [TODO] It's good to report the error if multiple servers are selected but we also need to report it earlier, directly in the configuration UI.
            Assert.IsTrue(serverDescriptList.Count <= 1, "There can only be one server in a scenario");

            // This will ensure the server instance is the first to be added in the scenario.
            if (serverDescriptList.Count > 0)
            {
                var serverInstance = ConnectOrCreateInstance(serverDescriptList[0], hasEditor);
                scenario.AddInstance(serverInstance);
            }

            // Finally, iterate through the rest of the instance descriptions, construct client instances
            // and configure the connection data for when they run.
            foreach (var clientDescript in clientDescriptList)
            {
                var clientInstance = ConnectOrCreateInstance(clientDescript, hasEditor);
                scenario.AddInstance(clientInstance);
            }
            return scenario;
        }

        private static Instance ConnectOrCreateInstance(InstanceDescription instanceDescription, bool hasMainEditor)
        {
            // If an Existing Instance is Actively Free Running, we connect that instance to this new Scenario.
            if (PlayModeScenarioManager.ActiveScenario is OrchestratedScenario config &&
                config.Scenario != null)
            {
                var activeFreeInstance = config.Scenario.GetInstanceByName(instanceDescription.Name, true);
                if (activeFreeInstance != null)
                {
                    // Ensure we remove it from the old Scenario
                    config.Scenario.RemoveInstance(activeFreeInstance);

                    // Finally return the Free run instance to be attached to the new one.
                    return activeFreeInstance;
                }
            }

            // Else, create and rebuild the instance from the description as per usual.
            return CreateInstance(instanceDescription, hasMainEditor);
        }

        private static Instance CreateInstance(InstanceDescription instanceDescription, bool hasMainEditor)
        {
            if (instanceDescription is VirtualEditorInstanceDescription virtualEditorDescription)
            {
                return CreateCloneEditorInstance(virtualEditorDescription);
            }
            if (instanceDescription is EditorInstanceDescription editorDescription)
            {
                return CreateMainEditorInstance(editorDescription);
            }
            if (instanceDescription is LocalInstanceDescription localDescription)
            {
                return CreateLocalInstance(localDescription, hasMainEditor);
            }
            throw new System.NotImplementedException();
        }

        static Instance CreateMainEditorInstance(EditorInstanceDescription editorInstanceDescription)
        {
            var editorController = MainEditorController.CreateInstance(editorInstanceDescription);
            var instance = Instance.Create(editorInstanceDescription, editorController);
            var executionGraph = instance.GetExecutionGraph();

            editorController.SetupExecutionGraph(executionGraph);

            return instance;
        }

        static Instance CreateCloneEditorInstance(VirtualEditorInstanceDescription editorInstanceDescription)
        {
            var editorController = CloneEditorController.CreateInstance(editorInstanceDescription);
            var instance = Instance.Create(editorInstanceDescription, editorController);
            var executionGraph = instance.GetExecutionGraph();

            editorController.SetupExecutionGraph(executionGraph);

            return instance;
        }

        private static Instance CreateLocalInstance(LocalInstanceDescription description, bool hasMainEditor)
        {
            var localController = LocalPlayerController.CreateInstance(description);
            localController.HasEditorInstance = hasMainEditor;
            var instance = Instance.Create(description, localController);
            var executionGraph = instance.GetExecutionGraph();

            localController.SetupExecutionGraph(executionGraph);

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
