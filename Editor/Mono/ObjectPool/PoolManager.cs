// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: ScriptingRuntime not yet converted
namespace UnityEditor.ObjectPool
{
    [InitializeOnLoad]
    static class PoolManager
    {
        static PoolManager() => EditorApplication.playModeStateChanged += OnEditorStateChange;

        static void OnEditorStateChange(PlayModeStateChange stateChange)
        {
            if(!EditorSettings.enterPlayModeOptionsEnabled
                || !EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableDomainReload))
            {
                return;
            }

            switch (stateChange)
            {
                case PlayModeStateChange.EnteredEditMode:
                case PlayModeStateChange.ExitingEditMode:
                    UnityEngine.Pool.PoolManager.Reset();
                    break;
            }
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
