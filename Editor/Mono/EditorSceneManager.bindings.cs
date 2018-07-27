// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Bindings;
using uei = UnityEngine.Internal;

namespace UnityEditor.SceneManagement
{
    [NativeHeader("Runtime/SceneManager/SceneManager.h")]
    [NativeHeader("Modules/AssetPipelineEditor/Public/DefaultImporter.h")]
    [NativeHeader("Editor/Mono/EditorSceneManager.bindings.h")]
    public sealed partial class EditorSceneManager : SceneManager
    {
        public extern static int loadedSceneCount
        {
            [StaticAccessor("GetSceneManager()", StaticAccessorType.Dot)]
            [NativeMethod("GetLoadedSceneCount")]
            get;
        }

        public extern static int previewSceneCount
        {
            [StaticAccessor("GetSceneManager()", StaticAccessorType.Dot)]
            [NativeMethod("GetPreviewSceneCount")]
            get;
        }

        public extern static bool preventCrossSceneReferences
        {
            [StaticAccessor("GetSceneManager()", StaticAccessorType.Dot)]
            [NativeMethod("IsPreventingCrossSceneReferences")]
            get;

            [StaticAccessor("GetSceneManager()", StaticAccessorType.Dot)]
            [NativeMethod("SetPreventCrossSceneReferences")]
            set;
        }

        public extern static SceneAsset playModeStartScene
        {
            [StaticAccessor("GetSceneManager()", StaticAccessorType.Dot)]
            [NativeMethod("GetPlayModeStartScene")]
            get;

            [StaticAccessor("GetSceneManager()", StaticAccessorType.Dot)]
            [NativeMethod("SetPlayModeStartScene")]
            set;
        }

        [NativeThrows]
        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("OpenScene")]
        public extern static Scene OpenScene(string scenePath, [uei.DefaultValue("OpenSceneMode.Single")] OpenSceneMode mode);

        [NativeThrows]
        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("OpenPreviewScene")]
        internal extern static Scene OpenPreviewScene(string scenePath);

        [NativeThrows]
        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("NewScene")]
        public extern static Scene NewScene(NewSceneSetup setup, [uei.DefaultValue("NewSceneMode.Single")] NewSceneMode mode);

        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("NewPreviewScene")]
        public extern static Scene NewPreviewScene();

        [StaticAccessor("GetSceneManager()", StaticAccessorType.Dot)]
        [NativeMethod("CreateSceneAsset")]
        private extern static bool CreateSceneAssetInternal(string scenePath, bool createDefaultGameObjects);

        [NativeThrows]
        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("CloseScene")]
        public extern static bool CloseScene(Scene scene, bool removeScene);

        [NativeThrows]
        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("ClosePreviewScene")]
        public extern static bool ClosePreviewScene(Scene scene);

        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("IsPreviewScene")]
        public extern static bool IsPreviewScene(Scene scene);

        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("IsPreviewSceneObject")]
        public extern static bool IsPreviewSceneObject(UnityEngine.Object obj);

        [NativeThrows]
        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("ReloadScene")]
        internal extern static bool ReloadScene(Scene scene);

        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("SetTargetSceneForNewGameObjects")]
        internal extern static void SetTargetSceneForNewGameObjects(int sceneHandle);

        [NativeThrows]
        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetTargetSceneForNewGameObjects")]
        internal extern static Scene GetTargetSceneForNewGameObjects();

        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetSceneByHandle")]
        internal extern static Scene GetSceneByHandle(int handle);

        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("MoveSceneBefore")]
        public extern static void MoveSceneBefore(Scene src, Scene dst);

        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("MoveSceneAfter")]
        public extern static void MoveSceneAfter(Scene src, Scene dst);

        [NativeThrows]
        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("SaveSceneAs")]
        internal extern static bool SaveSceneAs(Scene scene);

        [NativeThrows]
        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("SaveSceneInternal")]
        private extern static bool SaveSceneInternal(Scene scene, string dstScenePath, bool saveAsCopy);

        [NativeThrows]
        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("SaveOpenScenes")]
        public extern static bool SaveOpenScenes();

        [NativeThrows]
        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("SaveScenes")]
        public extern static bool SaveScenes(Scene[] scenes);

        [NativeThrows]
        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("SaveCurrentModifiedScenesIfUserWantsTo")]
        public extern static bool SaveCurrentModifiedScenesIfUserWantsTo();

        [NativeThrows]
        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("SaveModifiedScenesIfUserWantsTo")]
        public extern static bool SaveModifiedScenesIfUserWantsTo(Scene[] scenes);

        [StaticAccessor("GetSceneManager()", StaticAccessorType.Dot)]
        [NativeMethod("EnsureUntitledSceneHasBeenSaved")]
        public extern static bool EnsureUntitledSceneHasBeenSaved(string dialogContent);

        [NativeThrows]
        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("MarkSceneDirty")]
        public extern static bool MarkSceneDirty(Scene scene);

        [NativeThrows]
        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("MarkAllScenesDirty")]
        public extern static void MarkAllScenesDirty();

        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("ClearSceneDirtiness")]
        internal extern static void ClearSceneDirtiness(Scene scene);

        [NativeThrows]
        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetSceneManagerSetup")]
        public extern static SceneSetup[] GetSceneManagerSetup();

        [NativeThrows]
        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("RestoreSceneManagerSetup")]
        public extern static void RestoreSceneManagerSetup(SceneSetup[] value);

        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        [NativeMethod("DetectCrossSceneReferences")]
        public extern static bool DetectCrossSceneReferences(Scene scene);

        [StaticAccessor("EditorSceneManagerBindings", StaticAccessorType.DoubleColon)]
        private extern static AsyncOperation LoadSceneInPlayModeInternal(string path, LoadSceneParameters parameters, bool isSynchronous);
    }
}
