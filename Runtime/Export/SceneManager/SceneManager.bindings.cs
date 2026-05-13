// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Loading;
using UnityEngine.Bindings;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine.SceneManagement
{
    [NativeHeader("Runtime/Export/SceneManager/SceneManager.bindings.h")]
    [NativeHeader("Runtime/SceneManager/SceneManager.h")]
    [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
    internal static class SceneManagerAPIInternal
    {
        // Slim players do not contain scenes, so build settings will always return the wrong values, need to remap these to asset bundle sceneName lookup
        public static extern int GetNumScenesInBuildSettings();

        [NativeMethod(ThrowsException = true)]
        public static extern Scene GetSceneByBuildIndex(int buildIndex);

        [NativeMethod(ThrowsException = true)]
        public static extern AsyncOperation LoadSceneAsyncNameIndexInternal(string sceneName, int sceneBuildIndex, LoadSceneParameters parameters, bool mustCompleteNextFrame);

        [NativeMethod(ThrowsException = true)]
        public static extern AsyncOperation UnloadSceneNameIndexInternal(string sceneName, int sceneBuildIndex, bool immediately, UnloadSceneOptions options, out bool outSuccess);
    }

    public class SceneManagerAPI
    {
        static SceneManagerAPI s_DefaultAPI = new SceneManagerAPI();
        // Internal code must use ActiveAPI over overrideAPI to properly fallback to default api handling
        internal static SceneManagerAPI ActiveAPI => overrideAPI ?? s_DefaultAPI;

        public static SceneManagerAPI overrideAPI { get; set; }

        protected internal SceneManagerAPI() {}
        protected internal virtual int GetNumScenesInBuildSettings() => SceneManagerAPIInternal.GetNumScenesInBuildSettings();
        protected internal virtual Scene GetSceneByBuildIndex(int buildIndex) => SceneManagerAPIInternal.GetSceneByBuildIndex(buildIndex);
        protected internal virtual AsyncOperation LoadSceneAsyncByNameOrIndex(string sceneName, int sceneBuildIndex, LoadSceneParameters parameters, bool mustCompleteNextFrame) =>
            SceneManagerAPIInternal.LoadSceneAsyncNameIndexInternal(sceneName, sceneBuildIndex, parameters, mustCompleteNextFrame);
        protected internal virtual AsyncOperation UnloadSceneAsyncByNameOrIndex(string sceneName, int sceneBuildIndex, bool immediately, UnloadSceneOptions options, out bool outSuccess) =>
            SceneManagerAPIInternal.UnloadSceneNameIndexInternal(sceneName, sceneBuildIndex, immediately, options, out outSuccess);
        protected internal virtual AsyncOperation LoadFirstScene(bool mustLoadAsync) => null;
    }

    [NativeHeader("Runtime/Export/SceneManager/SceneManager.bindings.h")]
    [RequiredByNativeCode]
    public partial class SceneManager
    {
        static internal bool s_AllowLoadScene = true;

        public static extern int sceneCount
        {
            [NativeHeader("Runtime/SceneManager/SceneManager.h")]
            [StaticAccessor("GetSceneManager()", StaticAccessorType.Dot)]
            [NativeMethod("GetSceneCount")]
            get;
        }

        public extern static int loadedSceneCount
        {
            [NativeHeader("Runtime/SceneManager/SceneManager.h")]
            [StaticAccessor("GetSceneManager()", StaticAccessorType.Dot)]
            [NativeMethod("GetLoadedSceneCount")]
            get;
        }

        public static int sceneCountInBuildSettings
        {
            get { return SceneManagerAPI.ActiveAPI.GetNumScenesInBuildSettings(); }
        }

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        internal static extern bool CanSetAsActiveScene(Scene scene);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        public static extern Scene GetActiveScene();

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod(ThrowsException = true)]
        public static extern bool SetActiveScene(Scene scene);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        public static extern Scene GetSceneByPath(string scenePath);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        public static extern Scene GetSceneByName(string name);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        public static extern Scene GetSceneByLoadableSceneId(LoadableSceneId loadableSceneId);

        public static Scene GetSceneByBuildIndex(int buildIndex)
        {
            return SceneManagerAPI.ActiveAPI.GetSceneByBuildIndex(buildIndex);
        }

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod(ThrowsException = true)]
        public static extern Scene GetSceneAt(int index);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod(ThrowsException = true)]
        public static extern Scene CreateScene([NotNull] string sceneName, CreateSceneParameters parameters);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod(ThrowsException = true)]
        private static extern bool UnloadSceneInternal(Scene scene, UnloadSceneOptions options);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod(ThrowsException = true)]
        private static extern AsyncOperation UnloadSceneAsyncInternal(Scene scene, UnloadSceneOptions options);

        private static AsyncOperation LoadSceneAsyncNameIndexInternal(string sceneName, int sceneBuildIndex, LoadSceneParameters parameters, bool mustCompleteNextFrame)
        {
            if (!s_AllowLoadScene)
                return null;

            return SceneManagerAPI.ActiveAPI.LoadSceneAsyncByNameOrIndex(sceneName, sceneBuildIndex, parameters, mustCompleteNextFrame);
        }

        private static AsyncOperation UnloadSceneNameIndexInternal(string sceneName, int sceneBuildIndex, bool immediately, UnloadSceneOptions options, out bool outSuccess)
        {
            if (!s_AllowLoadScene)
            {
                outSuccess = false;
                return null;
            }

            return SceneManagerAPI.ActiveAPI.UnloadSceneAsyncByNameOrIndex(sceneName, sceneBuildIndex, immediately, options, out outSuccess);
        }

        public static AsyncOperation LoadSceneAsync(LoadableSceneId loadableSceneId, LoadSceneParameters parameters = new LoadSceneParameters())
        {
            if (!s_AllowLoadScene)
                return null;

            return LoadSceneByLoadableSceneIdAsync(loadableSceneId, parameters, false);
        }

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod(ThrowsException = true)]
        private static extern AsyncOperation LoadSceneByLoadableSceneIdAsync(LoadableSceneId loadableSceneId, LoadSceneParameters parameters, bool mustCompleteNextFrame);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod(ThrowsException = true)]
        public static extern void MergeScenes(Scene sourceScene, Scene destinationScene);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod(ThrowsException = true)]
        public static extern void MoveGameObjectToScene([NotNull] GameObject go, Scene scene);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod(ThrowsException = true)]
        private extern static void MoveGameObjectsToSceneByInstanceId(IntPtr instanceIds, int instanceCount, Scene scene);

        [System.Obsolete("Please use MoveGameObjectsToScene(NativeArray<EntityId>, Scene scene) with the EntityId parameter type instead.", true)]
        public static unsafe void MoveGameObjectsToScene(NativeArray<int> instanceIDs, Scene scene) =>
            throw new NotImplementedException("Please use MoveGameObjectsToScene(NativeArray<EntityId>, Scene scene) with the EntityId parameter type instead.");

        public static unsafe void MoveGameObjectsToScene(NativeArray<EntityId> entityIds, Scene scene)
        {
            if (!entityIds.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(entityIds));

            if (entityIds.Length == 0)
                return;

            MoveGameObjectsToSceneByInstanceId((IntPtr)entityIds.GetUnsafeReadOnlyPtr(), entityIds.Length, scene);
        }

        [RequiredByNativeCode]
        internal static AsyncOperation LoadFirstScene_Internal(bool async)
        {
            return SceneManagerAPI.ActiveAPI.LoadFirstScene(async);
        }
    }
}
