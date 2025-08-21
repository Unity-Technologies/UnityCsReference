// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.PackageManager;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class MigrationUtility
    {
        const string k_MppmPackageName = "com.unity.multiplayer.playmode";
        const string k_MultiplayerModuleName = "com.unity.modules.multiplayer";
        const string k_TestPackageName = "com.unity.modules.multiplayer.playmode.editor.tests";

        static bool s_Initialized;
        static bool s_PackageManagerInitialized;
        static bool s_IsMppmPackageInstalled;
        static bool s_IsVirtualProjectsInPackage;
        static bool s_IsMultiplayerModuleInstalled;
        static bool s_TestPackageInstalled;

        static void EnsureInitialized()
        {
            if (s_Initialized)
                return;

            s_Initialized = true;
            s_PackageManagerInitialized = PackageInfo.GetAllRegisteredPackages().Length > 0;

            var mppmPackageInfo = PackageInfo.FindForPackageName(k_MppmPackageName);

            s_IsMultiplayerModuleInstalled = PackageInfo.FindForPackageName(k_MultiplayerModuleName) != null;
            s_TestPackageInstalled = PackageInfo.FindForPackageName(k_TestPackageName) != null;

            if (mppmPackageInfo != null)
            {
                s_IsMppmPackageInstalled = true;
                if (int.TryParse(mppmPackageInfo.version.Split('.')[0], out var majorVersion))
                {
                    s_IsVirtualProjectsInPackage = majorVersion <= 1;
                }
            }
        }

        internal static bool ShouldEnableMultiplayerPlayMode()
        {
            EnsureInitialized();
            return s_TestPackageInstalled || (s_IsMppmPackageInstalled && s_PackageManagerInitialized && !s_IsVirtualProjectsInPackage);
        }

        internal static bool ShouldDisableMultiplayerPlayMode() => !ShouldEnableMultiplayerPlayMode();
    }
}
