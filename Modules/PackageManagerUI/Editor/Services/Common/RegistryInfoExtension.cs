// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
    }
}
