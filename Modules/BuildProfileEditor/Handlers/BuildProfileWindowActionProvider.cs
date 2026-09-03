// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: BuildSettingsWindow not yet converted
using System;
using System.Collections.Generic;
using UnityEditor.Build.Profile;
using UnityEngine.Bindings;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Build.Profile.Handlers
{
    internal partial class BuildProfileWindowActionProvider
    {
        [AutoStaticsCleanupOnCodeReload]
        static List<BuildProfileWindowAction> s_WindowActions = null;

        public BuildProfileWindowActionProvider()
        {
            if (s_WindowActions != null)
                return;

            FetchActions();
        }

        public void FetchActions()
        {
            var actions = new List<BuildProfileWindowAction>();
            var types = TypeCache.GetTypesDerivedFrom<IBuildProfileWindowAction>();

            foreach (var type in types)
            {
                var packageInfo = PackageManager.PackageInfo.FindForAssembly(type.Assembly);

                if (packageInfo == null)
                    continue;

                // Loaded types must match a Unity registry.
                if (!BuildProfileModuleUtil.IsFromUnityPackageSource(packageInfo))
                {
                    UnityEngine.Debug.LogWarning($"Unsupported package registry type.");
                    continue;
                }

                if (Activator.CreateInstance(type) is IBuildProfileWindowAction action)
                {
                    if (action.GetDisplayName() == BuildProfileActionLabel.CloudBuild)
                    {
                        UnityEngine.Debug.LogWarning("Cloud build is not supported.");
                        continue;
                    }

                    actions.Add(new BuildProfileWindowAction(action));
                }
            }

            s_WindowActions = actions;
        }

        public List<BuildProfileWindowAction> GetAllActions(BuildProfile profile)
        {
            var sdkPlatformExtension = BuildProfileModuleUtil.GetSDKPlatformExtension(profile.platformGuid);
            var sdkPlatformActions = new List<Type>(sdkPlatformExtension.customFooterActions);
            var existingDisplayNames = new HashSet<BuildProfileActionLabel>();
            List<BuildProfileWindowAction> buildProfileActions = new List<BuildProfileWindowAction>();

            foreach (var action in s_WindowActions)
            {
                if (sdkPlatformActions.Contains(action.GetActionType()))
                {
                    if (existingDisplayNames.Contains(action.GetDisplayEnum()))
                    {
                        UnityEngine.Debug.LogWarning("Display name " + action.GetDisplayEnum() + " is already in use.");
                        continue;
                    }

                    buildProfileActions.Add(action);
                    existingDisplayNames.Add(action.GetDisplayEnum());
                }
            }

            // Sort the list by enum value for proper ordering.
            buildProfileActions.Sort((a, b) => a.GetDisplayEnum().CompareTo(b.GetDisplayEnum()));
            return buildProfileActions;
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
