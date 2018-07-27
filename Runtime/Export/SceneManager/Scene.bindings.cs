// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.SceneManagement
{
    [NativeHeader("Runtime/Export/SceneManager/Scene.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct Scene
    {
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static bool IsValidInternal(int sceneHandle);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static string GetPathInternal(int sceneHandle);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static string GetNameInternal(int sceneHandle);

        [NativeThrows]
        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static void SetNameInternal(int sceneHandle, string name);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static string GetGUIDInternal(int sceneHandle);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static bool GetIsLoadedInternal(int sceneHandle);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static Scene.LoadingState GetLoadingStateInternal(int sceneHandle);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static bool GetIsDirtyInternal(int sceneHandle);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static int GetDirtyID(int sceneHandle);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static int GetBuildIndexInternal(int sceneHandle);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static int GetRootCountInternal(int sceneHandle);

        [StaticAccessor("SceneBindings", StaticAccessorType.DoubleColon)]
        extern private static void GetRootGameObjectsInternal(int sceneHandle, object resultRootList);
    }
}
