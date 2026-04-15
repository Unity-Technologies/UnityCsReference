// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Profile.Handlers;
using UnityEngine;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Build profile provider for required components. Syncs required components
    /// on a build profile by removing stale entries and adding missing ones.
    /// </summary>
    [InitializeOnLoad]
    internal class RequiredComponentsProvider
    {
        static RequiredComponentsProvider()
        {
            BuildProfile.AddOnBuildProfileEnable(SyncComponents);
        }

        /// <summary>
        /// Synchronizes required components on a build profile by removing stale
        /// entries and adding any that are missing. Called on every profile OnEnable.
        /// </summary>
        internal static void SyncComponents(BuildProfile profile)
        {
            // We cannot add components unless build profile exists in asset db
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(profile)))
                return;

            RemoveStaleRequiredComponents(profile);

            var addSettings = new AddSettingsDataProvider(profile);
            foreach (var settings in addSettings.FetchRequiredPackageSettings())
            {
                settings.OnAdd(profile);
            }
        }

        /// <summary>
        /// Removes entries from the profile's requiredComponents array that are no
        /// longer listed in the platform SDK's required components. The sub-assets
        /// themselves are left intact so users can still remove them manually.
        /// </summary>
        internal static void RemoveStaleRequiredComponents(BuildProfile profile)
        {
            if (profile?.requiredComponents is not { Length: > 0 })
                return;

            // If the SDK extension can't be resolved, preserve existing entries
            // to avoid accidentally removing all required components.
            if (!BuildTargetDiscovery.TryGetSDKPlatformExtension(profile.platformGuid, out var sdkExtension))
                return;

            var validRequiredTypes = sdkExtension.requiredComponents != null
                ? new HashSet<Type>(sdkExtension.requiredComponents)
                : new HashSet<Type>();

            var cleaned = new List<ScriptableObject>(profile.requiredComponents.Length);
            foreach (var component in profile.requiredComponents)
            {
                if (component != null && validRequiredTypes.Contains(component.GetType()))
                    cleaned.Add(component);
            }

            if (cleaned.Count != profile.requiredComponents.Length)
            {
                profile.requiredComponents = cleaned.ToArray();
                EditorUtility.SetDirty(profile);
            }
        }
    }
}
