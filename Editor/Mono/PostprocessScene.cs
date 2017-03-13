// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Build;

namespace UnityEditor
{
    internal class UnityBuildPostprocessor : IProcessScene
    {
        public int callbackOrder { get { return 0; } }
        public void OnProcessScene(UnityEngine.SceneManagement.Scene scene)
        {
            int staticBatching, dynamicBatching;
            PlayerSettings.GetBatchingForPlatform(EditorUserBuildSettings.activeBuildTarget, out staticBatching, out dynamicBatching);
            if (staticBatching != 0)
            {
                InternalStaticBatchingUtility.Combine(null, true, true);
            }
        }
    }
}
