// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.PackageManager;

namespace Unity.Multiplayer.Internal
{
    internal static partial class DedicatedServerMigrationUtility
    {
        const string k_ServerPackageName = "com.unity.dedicated-server";

        [AutoStaticsCleanupOnCodeReload] // init gate; must re-query package state after reload
        static bool s_Initialized;
        [AutoStaticsCleanupOnCodeReload]
        static bool s_IsDedicatedServerPackageInstalled;
        [AutoStaticsCleanupOnCodeReload] // reseeded from the package manager by Initialize below
        static bool s_LastObservedServerPackageInstalled;

        // Watches for the Dedicated Server package being installed/removed
        // so the feature can be enabled or disabled without an editor restart.
        [OnCodeLoaded]
        static void Initialize()
        {
            s_LastObservedServerPackageInstalled =
                UnityEditor.PackageManager.PackageInfo.FindForPackageName(k_ServerPackageName) != null;

            Events.registeredPackages += OnRegisteredPackages;
        }

        [OnCodeUnloading]
        static void Uninitialize()
        {
            Events.registeredPackages -= OnRegisteredPackages;
        }

        static void OnRegisteredPackages(PackageRegistrationEventArgs args)
        {
            // Keyed off the package diff rather than off a before/after comparison of the cached
            // state: other registeredPackages handlers also reset that cache, and TypeCache does
            // not guarantee this handler runs before them, so by the time it does the cache may
            // already report the new value and the change would go undetected.
            if (!TryGetServerPackageState(args, out var installed))
                return;

            if (installed == s_LastObservedServerPackageInstalled)
                return;

            s_LastObservedServerPackageInstalled = installed;

            // Gated [InitializeOnLoad(Method)] initializers latch ShouldEnableDedicatedServer()
            // once per domain, so a reload is what lets the feature enable or disable itself
            // without an editor restart.
            Reinitialize();
            EditorUtility.RequestScriptReload();
        }

        static bool TryGetServerPackageState(PackageRegistrationEventArgs args, out bool installed)
        {
            foreach (var package in args.added)
            {
                if (package.name == k_ServerPackageName)
                {
                    installed = true;
                    return true;
                }
            }

            foreach (var package in args.removed)
            {
                if (package.name == k_ServerPackageName)
                {
                    installed = false;
                    return true;
                }
            }

            installed = false;
            return false;
        }

        static void EnsureInitialized()
        {
            if (s_Initialized)
                return;

            s_Initialized = true;

            s_IsDedicatedServerPackageInstalled = UnityEditor.PackageManager.PackageInfo.FindForPackageName(k_ServerPackageName) != null;
        }

        internal static void Reinitialize()
        {
            s_Initialized = false;
        }

        internal static bool ShouldEnableDedicatedServer()
        {
            EnsureInitialized();
            return s_IsDedicatedServerPackageInstalled;
        }

        internal static bool ShouldDisableDedicatedServer() => !ShouldEnableDedicatedServer();
    }
}
