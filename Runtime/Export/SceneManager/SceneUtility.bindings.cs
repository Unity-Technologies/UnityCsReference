// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.SceneManagement
{
    [NativeHeader("Runtime/Export/SceneManager/SceneUtility.bindings.h")]
    public static partial class SceneUtility
    {
        [StaticAccessor("SceneUtilityBindings", StaticAccessorType.DoubleColon)]
        extern public static string GetScenePathByBuildIndex(int buildIndex);

        [StaticAccessor("SceneUtilityBindings", StaticAccessorType.DoubleColon)]
        extern public static int GetBuildIndexByScenePath(string scenePath);
    }
}
