// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.SceneManagement
{
    [NativeHeader("Editor/Src/SceneManager/StageUtility.bindings.h")]
    public static partial class StageUtility
    {
        [NativeThrows]
        [StaticAccessor("StageUtilityBindings", StaticAccessorType.DoubleColon)]
        extern private static bool IsGameObjectRenderedInSameStageAsSceneInternal(GameObject gameObject, int sceneHandle);

        [NativeThrows]
        [StaticAccessor("StageUtilityBindings", StaticAccessorType.DoubleColon)]
        extern private static bool IsGameObjectRenderedByCameraInternal(GameObject gameObject, Camera camera);

        [NativeThrows]
        [StaticAccessor("StageUtilityBindings", StaticAccessorType.DoubleColon)]
        extern private static bool IsTheMainStageRenderedByCameraInternal(Camera camera);

        [NativeThrows]
        [StaticAccessor("StageUtilityBindings", StaticAccessorType.DoubleColon)]
        extern private static void SetSceneToRenderInSameStageAsOtherSceneInternal(int sceneHandle, int otherSceneHandle);

        [NativeThrows]
        [StaticAccessor("StageUtilityBindings", StaticAccessorType.DoubleColon)]
        extern private static void SetSceneToRenderInMainStageInternal(int sceneHandle);


        [NativeThrows]
        [StaticAccessor("StageUtilityBindings", StaticAccessorType.DoubleColon)]
        extern private static bool IsGameObjectInTheMainStageInternal(GameObject gameObject);

        [NativeThrows]
        [StaticAccessor("StageUtilityBindings", StaticAccessorType.DoubleColon)]
        extern private static bool IsGameObjectInSameStageAsSceneInternal(GameObject gameObject, int sceneHandle);
    }
}
