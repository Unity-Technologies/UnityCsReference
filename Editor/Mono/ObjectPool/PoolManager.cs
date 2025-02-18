// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
                case PlayModeStateChange.EnteredPlayMode:
                    UnityEngine.Pool.PoolManager.Reset();
                    break;
            }
        }
    }
}
