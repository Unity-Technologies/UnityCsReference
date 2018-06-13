// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using uei = UnityEngine.Internal;

namespace UnityEditor.SceneManagement
{
    // Must match same enums in C++
    public enum OpenSceneMode { Single, Additive, AdditiveWithoutLoading };
    public enum NewSceneMode { Single, Additive };
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects };

    public sealed partial class EditorSceneManager : SceneManager
    {
        internal static bool CreateSceneAsset(string scenePath, bool createDefaultGameObjects)
        {
            if (!Utils.Paths.CheckValidAssetPathAndThatDirectoryExists(scenePath, ".unity"))
                return false;

            return CreateSceneAssetInternal(scenePath, createDefaultGameObjects);
        }

        [uei.ExcludeFromDocs]
        public static Scene OpenScene(string scenePath)
        {
            Scene result;
            result = OpenScene(scenePath, OpenSceneMode.Single);
            return result;
        }

        [uei.ExcludeFromDocs]
        public static Scene NewScene(NewSceneSetup setup)
        {
            Scene result;
            result = NewScene(setup, NewSceneMode.Single);
            return result;
        }

        [uei.ExcludeFromDocs]
        public static bool SaveScene(Scene scene, string dstScenePath)
        {
            bool saveAsCopy = false;
            return SaveScene(scene, dstScenePath, saveAsCopy);
        }

        [uei.ExcludeFromDocs]
        public static bool SaveScene(Scene scene)
        {
            bool saveAsCopy = false;
            string dstScenePath = "";
            return SaveScene(scene, dstScenePath, saveAsCopy);
        }

        public static bool SaveScene(Scene scene, [uei.DefaultValue("\"\"")] string dstScenePath, [uei.DefaultValue("false")] bool saveAsCopy)
        {
            if (!string.IsNullOrEmpty(dstScenePath))
                if (!Utils.Paths.CheckValidAssetPathAndThatDirectoryExists(dstScenePath, ".unity"))
                    return false;

            return SaveSceneInternal(scene, dstScenePath, saveAsCopy);
        }

        internal static UnityAction<Scene, NewSceneMode> sceneWasCreated;
        internal static UnityAction<Scene, OpenSceneMode> sceneWasOpened;

        private static void Internal_NewSceneWasCreated(Scene scene, NewSceneMode mode)
        {
            if (sceneWasCreated != null)
                sceneWasCreated(scene, mode);
        }

        private static void Internal_SceneWasOpened(Scene scene, OpenSceneMode mode)
        {
            if (sceneWasOpened != null)
                sceneWasOpened(scene, mode);
        }

        public static event UnityAction<Scene, Scene> activeSceneChangedInEditMode;

        [RequiredByNativeCode]
        private static void Internal_ActiveSceneChangedInEditor(Scene previousActiveScene, Scene newActiveScene)
        {
            if (activeSceneChangedInEditMode != null)
            {
                activeSceneChangedInEditMode(previousActiveScene, newActiveScene);
            }
        }

        public static Scene LoadSceneInPlayMode(string path, LoadSceneParameters parameters)
        {
            AsyncOperation op = LoadSceneInPlayModeInternal(path, parameters, true);
            if (op != null)
                return GetSceneAt(sceneCount - 1);

            return new Scene();
        }

        public static AsyncOperation LoadSceneAsyncInPlayMode(string path, LoadSceneParameters parameters)
        {
            return LoadSceneInPlayModeInternal(path, parameters, false);
        }
    }
}
