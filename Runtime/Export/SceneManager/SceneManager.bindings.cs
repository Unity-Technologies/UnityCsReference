// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Loading;
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

        [NativeThrows]
        public static extern Scene GetSceneByBuildIndex(int buildIndex);

        [NativeThrows]
        public static extern AsyncOperation LoadSceneAsyncNameIndexInternal(string sceneName, int sceneBuildIndex, LoadSceneParameters parameters, bool mustCompleteNextFrame);

        [NativeThrows]
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
        [NativeThrows]
        public static extern bool SetActiveScene(Scene scene);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        public static extern Scene GetSceneByPath(string scenePath);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        public static extern Scene GetSceneByName(string name);

        public static Scene GetSceneByBuildIndex(int buildIndex)
        {
            return SceneManagerAPI.ActiveAPI.GetSceneByBuildIndex(buildIndex);
        }

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        public static extern Scene GetSceneAt(int index);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        public static extern Scene CreateScene([NotNull] string sceneName, CreateSceneParameters parameters);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        private static extern bool UnloadSceneInternal(Scene scene, UnloadSceneOptions options);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
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

        /// <summary>
        /// Asynchronously loads a Scene from built content using a LoadableScene reference.
        /// </summary>
        /// <remarks>
        /// This overload of LoadSceneAsync enables loading scenes that have been built as part of a Content Directory. The scene
        /// must be available through a registered content directory (via
        /// <see cref="Loading.ContentLoadManager.RegisterContentDirectory"/>), or it must have been built into the player.
        ///
        /// This method works in both the Editor (when content directories are registered) and at runtime.
        /// </remarks>
        /// <param name="loadableScene">
        /// The LoadableScene reference identifying which scene to load.
        /// </param>
        /// <param name="parameters">
        /// Various parameters used during the loading operation, such as load mode.
        /// </param>
        /// <returns>
        /// A ContentLoadSceneOperation that can be used to track the progress of the scene loading operation. Returns null if
        /// scene loading is not allowed in the current context.
        /// </returns>
        /// <seealso cref="LoadableScene"/>
        /// <seealso cref="Loading.ContentLoadManager"/>
        /// <seealso cref="Loading.ContentLoadSceneOperation"/>
        /*UCBP-PUBLIC*/ internal static ContentLoadSceneOperation LoadSceneAsync(LoadableScene loadableScene, LoadSceneParameters parameters = new LoadSceneParameters())
        {
            if (!s_AllowLoadScene)
                return null;

            return LoadSceneByLoadableAsync(loadableScene, parameters, false);
        }

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        private static extern ContentLoadSceneOperation LoadSceneByLoadableAsync(LoadableScene loadableScene, LoadSceneParameters parameters, bool mustCompleteNextFrame);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        public static extern void MergeScenes(Scene sourceScene, Scene destinationScene);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        public static extern void MoveGameObjectToScene([NotNull] GameObject go, Scene scene);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        private extern static void MoveGameObjectsToSceneByInstanceId(IntPtr instanceIds, int instanceCount, Scene scene);

        [System.Obsolete("Please use MoveGameObjectsToScene(NativeArray<EntityId>, Scene scene) with the EntityId parameter type instead.", false)]
        public static unsafe void MoveGameObjectsToScene(NativeArray<int> instanceIDs, Scene scene)
        {
            if (!instanceIDs.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(instanceIDs));

            if (instanceIDs.Length == 0)
                return;

            Debug.Assert(sizeof(EntityId) == sizeof(int), "EntityId size mismatch. This method should be removed, as it relies on this size.");

            MoveGameObjectsToSceneByInstanceId((IntPtr)instanceIDs.GetUnsafeReadOnlyPtr(), instanceIDs.Length, scene);
        }

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
