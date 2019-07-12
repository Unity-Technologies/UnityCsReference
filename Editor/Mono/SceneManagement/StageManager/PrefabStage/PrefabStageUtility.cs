// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.Experimental.SceneManagement
{
    public class PrefabStageUtility
    {
        [Shortcut("Stage/Enter Prefab Mode", KeyCode.P)]
        static void EnterPrefabModeShortcut()
        {
            var activeGameObject = Selection.activeGameObject;
            if (activeGameObject == null)
                return;

            if (PrefabUtility.IsPartOfAnyPrefab(activeGameObject))
            {
                var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(activeGameObject);
                if (!string.IsNullOrEmpty(prefabPath) && prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    var instanceObject = !EditorUtility.IsPersistent(activeGameObject) ? activeGameObject : null;
                    OpenPrefab(prefabPath, instanceObject);
                }
            }
        }

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

            if (!prefabAssetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Incorrect file extension: " + prefabAssetPath + ". Must be '.prefab'", prefabAssetPath);

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
            if (prefabStage != null && prefabStage.isValid)
            {
                return prefabStage.prefabContentsRoot == gameObject;
            }
            return false;
        }

        [UsedByNativeCode]
        internal static bool IsGameObjectInInvalidPrefabStage(GameObject gameObject)
        {
            PrefabStage prefabStage = GetCurrentPrefabStage();
            return (prefabStage != null && prefabStage.scene == gameObject.scene && !prefabStage.isValid);
        }

        static bool IsDynamicallyCreatedDuringLoad(GameObject gameObject)
        {
            return Unsupported.GetFileIDHint(gameObject) == 0;
        }

        static UInt64 GetPersistentPrefabOrVariantFileIdentifier(GameObject gameObject)
        {
            var handle = PrefabUtility.GetPrefabInstanceHandle(gameObject);
            if (handle != null)
                return Unsupported.GetLocalIdentifierInFileForPersistentObject(handle);

            return Unsupported.GetLocalIdentifierInFileForPersistentObject(gameObject);
        }

        static UInt64 GetPrefabOrVariantFileID(GameObject gameObject)
        {
            var handle = PrefabUtility.GetPrefabInstanceHandle(gameObject);
            if (handle != null)
                return Unsupported.GetFileIDHint(handle);

            return Unsupported.GetFileIDHint(gameObject);
        }

        static GameObject FindFirstGameObjectThatMatchesFileID(Transform searchRoot, UInt64 fileID)
        {
            GameObject result = null;
            var transformVisitor = new TransformVisitor();
            transformVisitor.VisitAndAllowEarlyOut(searchRoot,
                (transform, userdata) =>
                {
                    UInt64 id = GetPrefabOrVariantFileID(transform.gameObject);
                    if (id == fileID)
                    {
                        result = transform.gameObject;
                        return false; // stop searching
                    }
                    return true; // continue searching
                }
                , null);

            return result;
        }

        static void RemoveBrokenPrefabRootsIfNeeded(string prefabAssetPath, GameObject[] environmentRoots, GameObject[] rootsAfterLoadingPrefab, UInt64 prefabAssetRootFileID)
        {
            var rootsLoadedFromFile = rootsAfterLoadingPrefab.Except(environmentRoots).ToList();

            // Filter out the subset of roots that were loaded from the Prefab file (there can be dynamically created environment objects,
            // created from Awake and OnEnable calls from user land, if they use ExecuteAlways or ExecuteInEditMode.
            rootsLoadedFromFile.RemoveAll(x => IsDynamicallyCreatedDuringLoad(x));

            if (rootsLoadedFromFile.Count >= 2)
            {
                Debug.LogError(string.Format("Prefab Mode: Broken Prefab with multiple roots detected ('{0}')", prefabAssetPath));

                // First see if we can find a valid root at all. The root could have been reparenting in User land in Awake.
                // Keep only the same root as the PrefabImporter if possible.
                GameObject root = null;
                foreach (var go in rootsLoadedFromFile)
                {
                    if (GetPrefabOrVariantFileID(go) == prefabAssetRootFileID)
                        root = go;
                }

                // If we found the correct root we can delete the other roots
                if (root != null)
                {
                    foreach (var go in rootsLoadedFromFile)
                    {
                        if (go != root)
                            UnityEngine.Object.DestroyImmediate(go);
                    }
                }
            }
        }

        static GameObject FindPrefabRoot(string prefabAssetPath, GameObject[] environmentRoots, GameObject[] rootsAfterLoadingPrefab)
        {
            var assetRoot = AssetDatabase.LoadMainAssetAtPath(prefabAssetPath) as GameObject;
            if (assetRoot == null)
            {
                Debug.LogError(string.Format("Opening Prefab Mode failed: The Prefab at '{0}' is broken.", prefabAssetPath));
                return null;
            }

            // Find the prefab root or variant root among the roots of the scene (or as a child)
            UInt64 prefabAssetRootFileID = GetPersistentPrefabOrVariantFileIdentifier(assetRoot);

            // Check for broken prefabs with multiple roots
            RemoveBrokenPrefabRootsIfNeeded(prefabAssetPath, environmentRoots, rootsAfterLoadingPrefab, prefabAssetRootFileID);

            // Fast path (most common): check all roots first
            foreach (var prefabRoot in rootsAfterLoadingPrefab)
            {
                if (prefabRoot == null)
                    continue;

                UInt64 id = GetPrefabOrVariantFileID(prefabRoot);
                if (id == prefabAssetRootFileID)
                    return prefabRoot;
            }

            // If not found in list of roots then check descendants
            foreach (var root in rootsAfterLoadingPrefab)
            {
                if (root == null)
                    continue;

                var prefabRoot = FindFirstGameObjectThatMatchesFileID(root.transform, prefabAssetRootFileID);
                if (prefabRoot != null)
                    return prefabRoot;
            }

            Debug.LogError(string.Format("Opening Prefab Mode failed: Could not detect a Prefab root after loading '{0}'.", prefabAssetPath));
            return null;
        }

        internal static GameObject LoadPrefabIntoPreviewScene(string prefabAssetPath, Scene previewScene)
        {
            var prefabName = Path.GetFileNameWithoutExtension(prefabAssetPath);
            previewScene.name = prefabName;

            // Get start roots from scene (before loading in the the Prefab)
            var environmentRoots = previewScene.GetRootGameObjects();

            // Load Prefab into scene
            try
            {
                PrefabUtility.LoadPrefabContentsIntoPreviewScene(prefabAssetPath, previewScene);
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("Loading Prefab failed: {0}", e.Message));
                return null;
            }

            var rootsAfterLoadingPrefab = previewScene.GetRootGameObjects();
            var root = FindPrefabRoot(prefabAssetPath, environmentRoots, rootsAfterLoadingPrefab);
            if (root != null)
            {
                // We need to ensure root instance name is matching filename when loading a prefab as a scene since the prefab file might contain an old root name.
                // The same name-matching is also ensured when importing a prefab: the library prefab asset root gets the same name as the filename of the prefab.
                root.name = prefabName;
            }
            return root;
        }

        internal static void HandleReparentingIfNeeded(GameObject prefabInstanceRoot, bool isUIPrefab)
        {
            // Skip reparenting if the root is already reparented
            if (prefabInstanceRoot.transform.parent != null)
                return;

            if (isUIPrefab)
                HandleUIReparentingIfNeeded(prefabInstanceRoot);
            else
                prefabInstanceRoot.transform.SetAsFirstSibling();
        }

        static string GetEnvironmentScenePathForPrefab(bool isUIPrefab)
        {
            string environmentEditingScenePath = "";

            // If prefab root has RectTransform, try to use UI environment.
            if (isUIPrefab)
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

        static Scene LoadOrCreatePreviewScene(string environmentEditingScenePath)
        {
            Scene previewScene;
            if (!string.IsNullOrEmpty(environmentEditingScenePath))
            {
                previewScene = EditorSceneManager.OpenPreviewScene(environmentEditingScenePath);
                var roots = previewScene.GetRootGameObjects();
                var visitor = new TransformVisitor();
                foreach (var root in roots)
                {
                    visitor.VisitAll(root.transform, AppendEnvironmentName, null);
                }
            }
            else
            {
                previewScene = CreateDefaultPreviewScene();
            }

            return previewScene;
        }

        static void AppendEnvironmentName(Transform transform, object userData)
        {
            transform.gameObject.name += " (Environment)";
        }

        static Scene CreateDefaultPreviewScene()
        {
            Scene previewScene = EditorSceneManager.NewPreviewScene();

            // Setup default render settings for this preview scene
            Unsupported.SetOverrideLightingSettings(previewScene);
            UnityEngine.RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            UnityEngine.RenderSettings.customReflection = GetDefaultReflection();   // ensure chrome materials do not render black
            UnityEngine.RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat") as Material;
            UnityEngine.RenderSettings.ambientMode = AmbientMode.Skybox;
            UnityEditorInternal.InternalEditorUtility.CalculateAmbientProbeFromSkybox();
            Unsupported.RestoreOverrideLightingSettings();

            return previewScene;
        }

        static Cubemap s_DefaultReflection;
        static Cubemap GetDefaultReflection()
        {
            const string path = "PrefabMode/DefaultReflectionForPrefabMode.exr";
            if (s_DefaultReflection == null)
            {
                s_DefaultReflection = EditorGUIUtility.Load(path) as Cubemap;
            }

            if (s_DefaultReflection == null)
                Debug.LogError("Could not find: " + path);
            return s_DefaultReflection;
        }

        internal static void DestroyPreviewScene(Scene previewScene)
        {
            if (previewScene.IsValid())
            {
                Undo.ClearUndoSceneHandle(previewScene);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        internal static Scene GetEnvironmentSceneOrEmptyScene(bool isUIPrefab)
        {
            // Create the environment scene and move the prefab root to this scene to ensure
            // the correct rendersettings (skybox etc) are used in Prefab Mode.
            string environmentEditingScenePath = GetEnvironmentScenePathForPrefab(isUIPrefab);
            Scene environmentScene = LoadOrCreatePreviewScene(environmentEditingScenePath);
            return environmentScene;
        }

        internal static bool IsUIPrefab(string prefabAssetPath)
        {
            // We require a RectTransform and a CanvasRenderer to be considered a UI prefab.
            // E.g 3D TextMeshPro uses RectTransform but a MeshRenderer so should not be considered a UI prefab
            // This function needs to be peformant since it is called every time a prefab is opened in a prefab stage.
            var root = AssetDatabase.LoadMainAssetAtPath(prefabAssetPath) as GameObject;
            if (root == null)
                return false;

            // In principle, RectTransforms can be used for other things than UI,
            // so only treat as UI Prefab if it has both a RectTransform on the root
            // AND either a Canvas on the root or a CanvasRenderer somewhere in the hierarchy.
            bool rectTransformOnRoot = root.GetComponent<RectTransform>() != null;
            bool uiSpecificComponentPresent = (root.GetComponent<Canvas>() != null || root.GetComponentInChildren<CanvasRenderer>(true) != null);
            return rectTransformOnRoot && uiSpecificComponentPresent;
        }

        static void HandleUIReparentingIfNeeded(GameObject instanceRoot)
        {
            // We need a Canvas in order to render UI so ensure the prefab instance is under a Canvas
            Canvas canvas = instanceRoot.GetComponent<Canvas>();
            if (canvas != null)
            {
                // We have a Canvas. Check if it's suitable to use as root Canvas.
                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    // Do nothing; do not early out and use Canvas as root Canvas.
                    // We can't know if a World Space Canvas was a root or not in all cases,
                    // but it's important to not make it a root if its RectTransform
                    // has stretching (non-identical min and max anchors); otherwise it will
                    // be previewed stretching to a single point (or less!).
                    // The downsides of making a World Space Canvas that was a root into a
                    // nested Canvas in Prefab Mode are less severe (a few settings on the
                    // Canvas component will be hidden), so we make it always nested
                    // as the lesser evil. Note that regardless, there is no data loss,
                    // since World Space canvases don't drive their RectTransform.
                }
                else
                {
                    // A Screen Space Canvas whose RectTransform values are not all 0
                    // can't have been driven when it was created, which means it can't
                    // have been a root Canvas. In that case it shouldn't be used as root
                    // Canvas here either, since that would cause its RectTransform values
                    // to be overridden with driven values, and then serialized as 0.
                    RectTransform rt = (RectTransform)canvas.transform;
                    if (rt.sizeDelta == Vector2.zero && rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.zero && rt.pivot == Vector2.zero)
                        return; // Use as root.
                }
            }

            GameObject canvasGameObject = GetOrCreateCanvasGameObject(instanceRoot);
            instanceRoot.transform.SetParent(canvasGameObject.transform, false);
        }

        static GameObject GetOrCreateCanvasGameObject(GameObject instanceRoot)
        {
            Canvas canvas = GetCanvasInScene(instanceRoot);
            if (canvas != null)
                return canvas.gameObject;

            const string kUILayerName = "UI";

            // Create canvas root for the UI
            GameObject root = EditorUtility.CreateGameObjectWithHideFlags("Canvas (Environment)", HideFlags.DontSave);
            root.layer = LayerMask.NameToLayer(kUILayerName);
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            SceneManager.MoveGameObjectToScene(root, instanceRoot.scene);
            return root;
        }

        static Canvas GetCanvasInScene(GameObject instanceRoot)
        {
            Scene scene = instanceRoot.scene;
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
