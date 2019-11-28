// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.SceneManagement;

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

        [StaticAccessor("StageUtilityBindings", StaticAccessorType.DoubleColon)]
        extern private static void SetPrefabInstanceHiddenForInContextEditingInternal(GameObject gameObject, bool hide);

        [StaticAccessor("StageUtilityBindings", StaticAccessorType.DoubleColon)]
        extern private static bool IsPrefabInstanceHiddenForInContextEditingInternal(GameObject gameObject);

        [StaticAccessor("StageUtilityBindings", StaticAccessorType.DoubleColon)]
        extern private static void EnableHidingForInContextEditingInSceneViewInternal(bool enable);

        [StaticAccessor("StageUtilityBindings", StaticAccessorType.DoubleColon)]
        extern private static void SetFocusedSceneInternal(int sceneHandle);

        [StaticAccessor("StageUtilityBindings", StaticAccessorType.DoubleColon)]
        extern private static Scene GetFocusedSceneInternal();

        [StaticAccessor("StageUtilityBindings", StaticAccessorType.DoubleColon)]
        extern private static void SetFocusedSceneContextRenderModeInternal(StageUtility.ContextRenderMode contextRenderMode);

        [StaticAccessor("StageUtilityBindings", StaticAccessorType.DoubleColon)]
        extern private static void
        CallAwakeFromLoadOnSubHierarchyInternal(GameObject prefabInstanceRoot);
    }
}
