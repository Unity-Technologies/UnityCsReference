// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace UnityEditor.Experimental.SceneManagement
{
    public class PrefabStageUtility
    {
        internal static PrefabStage OpenPrefab(string prefabAssetPath)
        {
            return OpenPrefab(prefabAssetPath, null);
        }

        internal static PrefabStage OpenPrefab(string prefabAssetPath, GameObject instanceRoot)
        {
            return OpenPrefab(prefabAssetPath, instanceRoot, StageNavigationManager.Analytics.ChangeType.EnterViaUnknown);
        }

        internal static PrefabStage OpenPrefab(string prefabAssetPath, GameObject instanceRoot, StageNavigationManager.Analytics.ChangeType changeTypeAnalytics)
        {
            if (string.IsNullOrEmpty(prefabAssetPath))
                throw new ArgumentNullException(prefabAssetPath);

            if (AssetDatabase.LoadMainAssetAtPath(prefabAssetPath) == null)
                throw new ArgumentException("Prefab not found at path " + prefabAssetPath, prefabAssetPath);

            return StageNavigationManager.instance.OpenPrefabMode(prefabAssetPath, instanceRoot, changeTypeAnalytics);
        }

        public static PrefabStage GetCurrentPrefabStage()
        {
            return StageNavigationManager.instance.GetCurrentPrefabStage();
        }

        public static PrefabStage GetPrefabStage(GameObject gameObject)
        {
            // Currently there's at most one prefab stage. Refactor in future if we have multiple.
            PrefabStage prefabStage = GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.scene == gameObject.scene)
                return prefabStage;

            return null;
        }

        [RequiredByNativeCode]
        internal static bool SaveCurrentModifiedPrefabStagesIfUserWantsTo()
        {
            // Returns false if the user clicked Cancel to save otherwise returns true
            return StageNavigationManager.instance.AskUserToSaveModifiedPrefabStageBeforeDestroyingStage(PrefabStageUtility.GetCurrentPrefabStage());
        }

        [UsedByNativeCode]
        internal static bool IsGameObjectThePrefabRootInAnyPrefabStage(GameObject gameObject)
        {
            PrefabStage prefabStage = GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                return prefabStage.prefabContentsRoot == gameObject;
            }
            return false;
        }

        internal static GameObject LoadPrefabIntoPreviewScene(string prefabAssetPath, Scene previewScene)
        {
            Assert.AreEqual(0, previewScene.GetRootGameObjects().Length);

            PrefabUtility.LoadPrefabContentsIntoPreviewScene(prefabAssetPath, previewScene);
            var roots = previewScene.GetRootGameObjects();
            if (roots.Length == 0)
            {
                Debug.LogError("No Prefab instance root loaded..");
                return null;
            }

            GameObject prefabInstanceRoot = null;
            if (roots.Length == 1)
                prefabInstanceRoot = roots[0];
            else
            {
                prefabInstanceRoot = ObjectFactory.CreateGameObject("Replacement Root");
                var rootTransform = prefabInstanceRoot.GetComponent<Transform>();

                for (int i = 0; i < roots.Length; ++i)
                {
                    roots[i].GetComponent<Transform>().parent = rootTransform;
                }
            }

            // We need to ensure root instance name is matching filename when loading a prefab as a scene since the prefab file might contain an old root name.
            // The same name-matching is also ensured when importing a prefab: the library prefab asset root gets the same name as the filename of the prefab.
            var prefabName = Path.GetFileNameWithoutExtension(prefabAssetPath);
            prefabInstanceRoot.name = prefabName;
            return prefabInstanceRoot;
        }

        internal static string GetEnvironmentScenePathForPrefab(GameObject prefabInstanceRoot)
        {
            string environmentEditingScenePath = "";

            // If prefab root has RectTransform, try to use UI environment.
            if (prefabInstanceRoot.GetComponent<RectTransform>() != null)
            {
                if (EditorSettings.prefabUIEnvironment != null)
                    environmentEditingScenePath = AssetDatabase.GetAssetPath(EditorSettings.prefabUIEnvironment);
            }
            // Else try to use regular environment.
            // Note, if the prefab is a UI object we deliberately don't use the regular environment as fallback,
            // since our empty scene with auto-generated Canvas is likely a better fallback than an environment
            // designed for non-UI objects.
            else
            {
                if (EditorSettings.prefabRegularEnvironment != null)
                    environmentEditingScenePath = AssetDatabase.GetAssetPath(EditorSettings.prefabRegularEnvironment);
            }

            return environmentEditingScenePath;
        }

        internal static Scene CreatePreviewScene(string environmentEditingScenePath)
        {
            Scene previewScene;
            if (!string.IsNullOrEmpty(environmentEditingScenePath))
            {
                previewScene = EditorSceneManager.OpenPreviewScene(environmentEditingScenePath);
                var roots = previewScene.GetRootGameObjects();
                var visitor = new TransformVisitor();
                foreach (var root in roots)
                {
                    visitor.VisitAndAllowEarlyOut(root.transform, UnpackPrefabInstance, null);
                    visitor.VisitAll(root.transform, AppendEnvironmentName, null);
                }
            }
            else
            {
                previewScene = CreateDefaultPreviewScene();
            }

            return previewScene;
        }

        static bool UnpackPrefabInstance(Transform transform, object userData)
        {
            if (PrefabUtility.IsPartOfNonAssetPrefabInstance(transform))
            {
                PrefabUtility.UnpackPrefabInstance(transform.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                return false;
            }
            return true;
        }

        static void AppendEnvironmentName(Transform transform, object userData)
        {
            transform.gameObject.name += " (Environment)";
        }

        static Scene CreateDefaultPreviewScene()
        {
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            Unsupported.SetOverrideRenderSettings(previewScene);

            // Setup default render settings for this preview scene
            UnityEngine.RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            UnityEngine.RenderSettings.customReflection = GetDefaultReflection();   // ensure chrome materials do not render balck
            UnityEngine.RenderSettings.skybox = null;                               // do not use skybox for the default previewscene, we want the flat Prefab Mode background color to let it stand out from normal scenes
            UnityEngine.RenderSettings.ambientMode = AmbientMode.Trilight;          // do not use skybox ambient but simple trilight ambient for simplicity
            Unsupported.RestoreOverrideRenderSettings();

            return previewScene;
        }

        static Cubemap s_DefaultHDRI;
        static Cubemap GetDefaultReflection()
        {
            if (s_DefaultHDRI == null)
            {
                s_DefaultHDRI = EditorGUIUtility.Load("LookDevView/DefaultHDRI.exr") as Cubemap;
                if (s_DefaultHDRI == null)
                    s_DefaultHDRI = EditorGUIUtility.Load("LookDevView/DefaultHDRI.asset") as Cubemap;
            }

            if (s_DefaultHDRI == null)
                Debug.LogError("Could not find DefaultHDRI");
            return s_DefaultHDRI;
        }

        internal static void DestroyPreviewScene(Scene previewScene)
        {
            if (previewScene.IsValid())
            {
                Undo.ClearUndoSceneHandle(previewScene);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        internal static bool HandleAutoReparenting(GameObject instanceRoot, Scene scene)
        {
            if (instanceRoot == null)
                return false;

            if (HandleUIReparenting(instanceRoot, scene))
                return true;

            return false;
        }

        static bool HandleUIReparenting(GameObject instanceRoot, Scene scene)
        {
            // We need a Canvas in order to render UI so below we ensure the prefab instance is under a Canvas
            Canvas canvas = instanceRoot.GetComponent<Canvas>();
            if (canvas != null)
                return true;

            bool rootHasRectTransform = instanceRoot.transform is RectTransform;
            if (rootHasRectTransform)
            {
                GameObject canvasGameObject = GetOrCreateCanvasGameObject(instanceRoot, scene);
                instanceRoot.transform.SetParent(canvasGameObject.transform, false);
                return true;
            }

            return false;
        }

        // TODO: make hooks for extensions to handle setting up
        static GameObject GetOrCreateCanvasGameObject(GameObject instanceRoot, Scene scene)
        {
            Canvas canvas = GetCanvasInScene(instanceRoot, scene);
            if (canvas != null)
                return canvas.gameObject;

            const string kUILayerName = "UI";

            // Create canvas root for the UI
            GameObject root = EditorUtility.CreateGameObjectWithHideFlags("Canvas (Environment)", HideFlags.DontSave);
            root.layer = LayerMask.NameToLayer(kUILayerName);
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            SceneManager.MoveGameObjectToScene(root, scene);
            return root;
        }

        static Canvas GetCanvasInScene(GameObject instanceRoot, Scene scene)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                // Do not search for Canvas's under the prefab root since we want to
                // have a Canvas for the prefab root
                if (go == instanceRoot)
                    continue;
                var canvas = go.GetComponentInChildren<Canvas>();
                if (canvas != null)
                    return canvas;
            }
            return null;
        }

        static GameObject CreateLight(Color color, float intensity, Quaternion orientation)
        {
            GameObject lightGO = EditorUtility.CreateGameObjectWithHideFlags("Directional Light", HideFlags.HideAndDontSave, typeof(Light));
            lightGO.transform.rotation = orientation;
            var light = lightGO.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            light.enabled = true;
            light.shadows = LightShadows.Soft;
            return lightGO;
        }

        static void CreateDefaultLights(Scene scene)
        {
            var light = CreateLight(new Color(0.769f, 0.769f, 0.769f, 1), 0.7f, Quaternion.Euler(40f, 40f, 0));
            var light2 = CreateLight(new Color(.4f, .4f, .45f, 0f) * .7f, 0.7f, Quaternion.Euler(340, 218, 177));

            SceneManager.MoveGameObjectToScene(light, scene);
            SceneManager.MoveGameObjectToScene(light2, scene);
        }
    }
}
