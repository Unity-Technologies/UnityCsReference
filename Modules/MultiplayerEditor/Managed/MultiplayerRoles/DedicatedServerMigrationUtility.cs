// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.PackageManager;

namespace Unity.Multiplayer.Internal
{
    internal static class DedicatedServerMigrationUtility
    {
        const string k_ServerPackageName = "com.unity.dedicated-server";

        static bool s_Initialized;
        static bool s_IsDedicatedServerPackageInstalled;

        static void EnsureInitialized()
        {
            if (s_Initialized)
                return;

            s_Initialized = true;

            s_IsDedicatedServerPackageInstalled = PackageInfo.FindForPackageName(k_ServerPackageName) != null;
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
