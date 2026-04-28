// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI.Internal;
using UnityEngine;

namespace UnityEditor.Build.Profile
{
    internal static class PackageSettingsProvider
    {
        internal static List<BuildProfileSettingsProvider> Get()
        {
            var foundEntryPoints = TypeCache.GetMethodsWithAttribute<BuildProfileSettingsProviderAttribute>();
            var result = new List<BuildProfileSettingsProvider>(foundEntryPoints.Count);

            for (int i = 0; i < foundEntryPoints.Count; i++)
            {
                var method = foundEntryPoints[i];
                var attribute = method.GetCustomAttributes(typeof(BuildProfileSettingsProviderAttribute), false)[0] as BuildProfileSettingsProviderAttribute;
                var settingsType = attribute.settingsType;
                var packageInfo = PackageManager.PackageInfo.FindForAssembly(method.DeclaringType.Assembly);

                if (packageInfo == null)
                    continue;

                // Loaded types must match a Unity registry.
                if (!IsFromUnityPackageSource(packageInfo))
                {
                    Debug.LogWarning($"Unsupported package registry type {settingsType.FullName}");
                    continue;
                }

                var provider = method.Invoke(null, null) as BuildProfileSettingsProvider;
                if (provider == null)
                    continue;

                provider.settingsType = settingsType;
                result.Add(provider);
            }

            return result;
        }

        /// <returns>True if running a source built editor, or if <see cref="PackageInfo"/> describes a Unity registry sourced package.</returns>
        static bool IsFromUnityPackageSource(PackageManager.PackageInfo packageInfo)
        {
            if (Unsupported.IsSourceBuild())
                return true;

            return packageInfo.GetAvailableRegistryType() == RegistryType.UnityRegistry;
        }
    }
}
