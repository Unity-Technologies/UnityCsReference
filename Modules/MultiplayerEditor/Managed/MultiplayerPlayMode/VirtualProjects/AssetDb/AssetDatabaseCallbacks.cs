// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;

namespace Unity.Multiplayer.PlayMode.Editor
{
    partial class AssetDatabaseCallbacks : AssetPostprocessor
    {
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once Unity.IncorrectMethodSignature
        // ReSharper disable SuggestBaseTypeForParameter
        /* OnPostProcessAllAssets has two signatures and the one with didDomainReload is hidden */
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths,
            bool didDomainReload)
        {
            if (MigrationUtility.ShouldDisableMultiplayerPlayMode())
                return;

            var totalCount = importedAssets.Length + deletedAssets.Length + movedAssets.Length + movedFromAssetPaths.Length;
            // AssetPostprocessingInternal::PostprocessAllAssets also does logic based off of containing no assets.
            // We need to pass this through to mirror the same logic
            // NOTE: This does mean this event fires more times than before!
            OnPostprocessAllAssetsCallback?.Invoke(didDomainReload, totalCount);
        }

        [AutoStaticsCleanupOnCodeReload] // static event; stale handlers after reload pin old ALC
        // The main editor systems subscribe from their Listen call, which the editor contexts run again from
        // their [OnCodeLoaded] initializer, so the handler is re-registered on every code load.
        [IgnoreForUAL0015("Event re-subscribed by MainEditorSystems.Listen from the [OnCodeLoaded] context initializer")]
        public static event Action<bool, int> OnPostprocessAllAssetsCallback;
    }
}
