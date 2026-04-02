// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEditor.Build.Profile
{
    internal static class PackageSettingsProvider
    {
        internal static List<BuildProfileSettingsProvider> Get()
        {
            var foundEntryPoints = TypeCache.GetMethodsWithAttribute<BuildProfileSettingsProviderAttribute>();
            var result = new List<BuildProfileSettingsProvider>(foundEntryPoints.Count);
            var validRequiredComponents = new List<Type>(BuildTargetDiscovery.TryGetSDKRequiredComponents());

            for (int i = 0; i < foundEntryPoints.Count; i++)
            {
                var method = foundEntryPoints[i];
                var attribute = method.GetCustomAttributes(typeof(BuildProfileSettingsProviderAttribute), false)[0] as BuildProfileSettingsProviderAttribute;
                var settingsType = attribute.settingsType;
                var packageInfo = PackageManager.PackageInfo.FindForAssembly(method.DeclaringType.Assembly);

                if (packageInfo == null)
                    continue;

                // Loaded types must match a Unity registry.
                if (!BuildProfileModuleUtil.IsFromUnityPackageSource(packageInfo))
                {
                    Debug.LogWarning($"Unsupported package registry type {settingsType.FullName}");
                    continue;
                }

                var provider = method.Invoke(null, null) as BuildProfileSettingsProvider;
                if (provider == null)
                    continue;

                if(validRequiredComponents.Contains(settingsType))
                    provider.isRequired = true;

                provider.settingsType = settingsType;
                result.Add(provider);
            }

            return result;
        }
    }
}
