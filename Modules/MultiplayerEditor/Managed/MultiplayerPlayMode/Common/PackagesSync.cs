// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class PackagesSync
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (MigrationUtility.ShouldDisableMultiplayerPlayMode())
                return;

            Events.registeredPackages += CheckVersionsInSync;
        }

        private static void CheckVersionsInSync(PackageRegistrationEventArgs args)
        {
            var dedicatedServer = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.dedicated-server");
            var playMode = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.multiplayer.playmode");

            if (dedicatedServer != null && playMode != null)
            {
                if (dedicatedServer.version != playMode.version)
                {
                    Debug.LogWarning($"Dedicated Server and Multiplayer Play Mode packages have different versions: {dedicatedServer.version} and {playMode.version}. For better compatibility please update both packages to the same version.");
                }
            }
        }
    }
}
