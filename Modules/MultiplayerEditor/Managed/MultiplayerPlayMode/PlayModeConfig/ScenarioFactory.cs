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
using UnityEngine;

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

        public static Scenario CreateScenario(OrchestratedScenario owner, IEnumerable<IInstanceItem> instanceItems)
        {
            var scenario = Scenario.Create(owner != null ? owner.name : "");

            CategorizeInstances(instanceItems, out var serverDescriptList, out var clientDescriptList);

            // [TODO] It's good to report the error if multiple servers are selected but we also need to report it earlier, directly in the configuration UI.
            Assert.IsTrue(serverDescriptList.Count <= 1, "There can only be one server in a scenario");

            // This will ensure the server instance is the first to be added in the scenario.
            if (serverDescriptList.Count > 0)
            {
                var serverInstance = ConnectOrCreateInstance(serverDescriptList[0], owner);
                scenario.AddInstance(serverInstance);
            }

            // Finally, iterate through the rest of the instance descriptions, construct client instances
            // and configure the connection data for when they run.
            foreach (var clientDescript in clientDescriptList)
            {
                var clientInstance = ConnectOrCreateInstance(clientDescript, owner);
                scenario.AddInstance(clientInstance);
            }
            return scenario;
        }

        private static Instance ConnectOrCreateInstance(IInstanceItem instanceItem, OrchestratedScenario owner)
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
            return CreateInstance(instanceItem, owner);
        }

        private static Instance CreateInstance(IInstanceItem instanceItem, OrchestratedScenario owner)
        {
            var controller = instanceItem.CreateController(owner);
            var decorators = CreateDecoratorsForInstance(instanceItem, owner);
            var graphBuilder = new ExecutionGraphBuilder();

            controller.SetupExecutionGraph(graphBuilder);
            foreach (var decorator in decorators)
            {
                decorator.SetupExecutionGraph(graphBuilder);
            }

            var executionGraph = graphBuilder.Build();
            return Instance.Create(instanceItem, controller, decorators, executionGraph);
        }

        private static List<InstanceControllerDecorator> CreateDecoratorsForInstance(IInstanceItem instanceItem, OrchestratedScenario owner)
        {
            var decorators = InstanceExtensionManager.GetDecoratorTypes(instanceItem.GetInstanceType());
            var decoratorList = new List<InstanceControllerDecorator>();

            foreach (var decoratorType in decorators)
            {
                if (!InstanceControllerDecorator.IsDecoratorWithSettings(decoratorType))
                {
                    decoratorList.Add((InstanceControllerDecorator)InstanceController.CreateInstance(decoratorType, instanceItem, owner));
                }
                else
                {
                    if (!instanceItem.HasDecorator(decoratorType))
                    {
                        Debug.LogError($"Instance item '{instanceItem.GetName()}' of type '{instanceItem.GetInstanceType().Name}' is missing a required decorator of type '{decoratorType.Name}'. This decorator will be generated with default settings.");
                        instanceItem.GenerateMissingDecoratorsAndRemoveDuplicates([decoratorType]);
                    }

                    decoratorList.Add(instanceItem.GetDecoratorItem(decoratorType).CreateController(instanceItem, owner));
                }
            }

            return decoratorList;
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
