// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class RegistryInfoExtension
    {
        public static bool AnyScopeMatchesPackageName(this RegistryInfo registryInfo, string packageName)
        {
            foreach (var scope in registryInfo.scopes)
                // We can't simply check with `packageName.StartsWith(scope)` because that'll be true for package name `com.unity.a` and scope `com.uni`
                // while `com.uni` should only match package `com.uni` or `com.uni.*`
                if (scope == packageName || packageName.StartsWith($"{scope}."))
                    return true;
            return false;
        }

        public static bool IsEquivalentTo(this RegistryInfo registry, RegistryInfo otherRegistry)
        {
            if (registry == otherRegistry)
                return true;

            if (registry.isDefault != otherRegistry.isDefault ||
                (registry.id ?? string.Empty) != (otherRegistry.id ?? string.Empty) ||
                (registry.name ?? string.Empty) != (otherRegistry.name ?? string.Empty) ||
                (registry.url ?? string.Empty) != (otherRegistry.url ?? string.Empty) ||
                !registry.compliance.IsEquivalentTo(otherRegistry.compliance) ||
                registry.scopes.Length != otherRegistry.scopes.Length)
                return false;

            for (var i = 0; i < registry.scopes.Length; i++)
                if ((registry.scopes[i] ?? string.Empty) != (otherRegistry.scopes[i] ?? string.Empty))
                    return false;

            return true;
        }

        public static bool IsEquivalentTo(this IList<RegistryInfo> registries, IList<RegistryInfo> otherRegistries)
        {
            if (registries.Count != otherRegistries.Count)
                return false;

            for(var i = 0; i < registries.Count; i++)
                if (!registries[i].IsEquivalentTo(otherRegistries[i]))
                    return false;

            return true;
        }
    }
}
