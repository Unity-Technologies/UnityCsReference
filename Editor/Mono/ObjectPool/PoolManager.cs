// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.ObjectPool
{
    static partial class PoolManager
    {
        [OnCodeLoaded]
        static void Initialize()
        {
            EditorApplication.playModeStateChanged += OnEditorStateChange;
        }

        [OnCodeUnloading]
        static void Shutdown()
        {
            EditorApplication.playModeStateChanged -= OnEditorStateChange;
        }

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
