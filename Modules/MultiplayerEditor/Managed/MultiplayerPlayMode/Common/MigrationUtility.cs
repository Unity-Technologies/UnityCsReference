// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.PackageManager;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class MigrationUtility
    {
        const string k_MppmPackageName = "com.unity.multiplayer.playmode";
        const string k_MultiplayerModuleName = "com.unity.modules.multiplayer";
        const string k_TestPackageName = "com.unity.modules.multiplayer.playmode.editor.tests";

        static bool s_Initialized;
        static bool s_IsMppmPackageInstalled;
        static bool s_IsVirtualProjectsInPackage;
        static bool s_IsMultiplayerModuleInstalled;
        static bool s_TestPackageInstalled;

        // Watches for the MPPM package being installed/removed so the feature can be enabled
        // or disabled without an editor restart. The package contains no scripts, so installing
        // it triggers no recompilation or domain reload on its own.
        static MigrationUtility()
        {
            Events.registeredPackages += OnRegisteredPackages;
        }

        static void EnsureInitialized()
        {
            if (s_Initialized)
                return;

            s_Initialized = true;

            var mppmPackageInfo = UnityEditor.PackageManager.PackageInfo.FindForPackageName(k_MppmPackageName);

            s_IsMultiplayerModuleInstalled = UnityEditor.PackageManager.PackageInfo.FindForPackageName(k_MultiplayerModuleName) != null;
            s_TestPackageInstalled = UnityEditor.PackageManager.PackageInfo.FindForPackageName(k_TestPackageName) != null;

            s_IsMppmPackageInstalled = mppmPackageInfo != null;
            s_IsVirtualProjectsInPackage = false;
            if (mppmPackageInfo != null && int.TryParse(mppmPackageInfo.version.Split('.')[0], out var majorVersion))
            {
                s_IsVirtualProjectsInPackage = majorVersion <= 1;
            }
        }

        internal static bool ShouldEnableMultiplayerPlayMode()
        {
            EnsureInitialized();
            return s_TestPackageInstalled || (s_IsMppmPackageInstalled && !s_IsVirtualProjectsInPackage);
        }

        internal static bool ShouldDisableMultiplayerPlayMode() => !ShouldEnableMultiplayerPlayMode();

        static void OnRegisteredPackages(PackageRegistrationEventArgs args)
        {
            // The MPPM package contains no scripts, so installing/removing it triggers no
            // recompilation or domain reload on its own. Bust the cache and re-read: if the
            // enabled state changed, force a reload so every gated [InitializeOnLoad(Method)]
            // re-runs against the updated package set.
            var previousValue = ShouldEnableMultiplayerPlayMode();
            s_Initialized = false;
            if (ShouldEnableMultiplayerPlayMode() != previousValue)
                EditorUtility.RequestScriptReload();
        }
    }
}
