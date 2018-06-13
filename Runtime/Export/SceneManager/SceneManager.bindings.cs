// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine.SceneManagement
{
    [NativeHeader("Runtime/Export/SceneManager/SceneManager.bindings.h")]
    [RequiredByNativeCode]
    public partial class SceneManager
    {
        public extern static int sceneCount
        {
            [NativeHeader("Runtime/SceneManager/SceneManager.h")]
            [StaticAccessor("GetSceneManager()", StaticAccessorType.Dot)]
            [NativeMethod("GetSceneCount")]
            get;
        }

        public extern static int sceneCountInBuildSettings
        {
            [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
            [NativeMethod("GetNumScenesInBuildSettings")]
            get;
        }

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        extern public static Scene GetActiveScene();

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern public static bool SetActiveScene(Scene scene);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        extern public static Scene GetSceneByPath(string scenePath);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        extern public static Scene GetSceneByName(string name);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern public static Scene GetSceneByBuildIndex(int buildIndex);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern public static Scene GetSceneAt(int index);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern public static Scene CreateScene([NotNull] string sceneName, CreateSceneParameters parameters);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern private static bool UnloadSceneInternal(Scene scene);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern private static AsyncOperation UnloadSceneAsyncInternal(Scene scene);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern private static AsyncOperation LoadSceneAsyncNameIndexInternal(string sceneName, int sceneBuildIndex, LoadSceneParameters parameters, bool mustCompleteNextFrame);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern private static AsyncOperation UnloadSceneNameIndexInternal(string sceneName, int sceneBuildIndex, bool immediately, out bool outSuccess);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern public static void MergeScenes(Scene sourceScene, Scene destinationScene);

        [StaticAccessor("SceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeThrows]
        extern public static void MoveGameObjectToScene([NotNull] GameObject go, Scene scene);
    }
}
