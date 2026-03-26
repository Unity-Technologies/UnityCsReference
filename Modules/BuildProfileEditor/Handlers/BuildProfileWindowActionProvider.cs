// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Build.Profile;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Profile.Handlers
{
    internal class BuildProfileWindowActionProvider
    {
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

            return buildProfileActions;
        }
    }
}
