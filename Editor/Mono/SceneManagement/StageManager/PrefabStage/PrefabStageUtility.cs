// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;


namespace UnityEditor.SceneManagement
{
    [MovedFrom("UnityEditor.Experimental.SceneManagement")]
    public static class PrefabStageUtility
    {
        [Shortcut("Stage/Enter Prefab Mode", KeyCode.P, displayName = "Stage/Edit Prefab in Context")]
        static void EnterInContextPrefabModeShortcut()
        {
            EnterPrefabModeFromCurrentSelection(PrefabStage.Mode.InContext);
        }

        [Shortcut("Stage/Edit Prefab in Isolation")]
        static void EnterIsolationPrefabModeShortcut()
        {
            EnterPrefabModeFromCurrentSelection(PrefabStage.Mode.InIsolation);
        }

        static void EnterPrefabModeFromCurrentSelection(PrefabStage.Mode preferredMode)
        {
            var activeGameObject = Selection.activeGameObject;
            if (activeGameObject == null)
                return;

            if (PrefabUtility.IsPartOfAnyPrefab(activeGameObject))
            {
                var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(activeGameObject);
                if (!string.IsNullOrEmpty(prefabPath) && prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    PrefabStage.Mode prefabStageMode = preferredMode;
                    GameObject openedFromInstance = null;
                    if (preferredMode == PrefabStage.Mode.InContext)
                    {
                        openedFromInstance = !EditorUtility.IsPersistent(activeGameObject) ? activeGameObject : null;
                        prefabStageMode = openedFromInstance != null ? PrefabStage.Mode.InContext : PrefabStage.Mode.InIsolation;
                    }

                    OpenPrefab(prefabPath, openedFromInstance, prefabStageMode);
                }
            }
        }

        public static PrefabStage OpenPrefab(string prefabAssetPath)
        {
            return OpenPrefab(prefabAssetPath, null, PrefabStage.Mode.InIsolation);
        }

        public static PrefabStage OpenPrefab(string prefabAssetPath, GameObject openedFromInstance)
        {
            var prefabStageMode = openedFromInstance != null ? PrefabStage.Mode.InContext : PrefabStage.Mode.InIsolation;
            return OpenPrefab(prefabAssetPath, openedFromInstance, prefabStageMode, StageNavigationManager.Analytics.ChangeType.EnterViaUnknown);
        }

        internal static PrefabStage OpenPrefab(string prefabAssetPath, GameObject openedFromInstance, StageNavigationManager.Analytics.ChangeType changeTypeAnalytics)
        {
            var prefabStageMode = openedFromInstance != null ? PrefabStage.Mode.InContext : PrefabStage.Mode.InIsolation;
            return OpenPrefab(prefabAssetPath, openedFromInstance, prefabStageMode, changeTypeAnalytics);
        }

        public static PrefabStage OpenPrefab(string prefabAssetPath, GameObject openedFromInstance, PrefabStage.Mode prefabStageMode)
        {
            return OpenPrefab(prefabAssetPath, openedFromInstance, prefabStageMode, StageNavigationManager.Analytics.ChangeType.EnterViaUnknown);
        }

        internal static PrefabStage OpenPrefab(string prefabAssetPath, GameObject openedFromInstance, PrefabStage.Mode prefabStageMode, StageNavigationManager.Analytics.ChangeType changeTypeAnalytics)
        {
            if (string.IsNullOrEmpty(prefabAssetPath))
                throw new ArgumentNullException(nameof(prefabAssetPath));

            if (openedFromInstance != null && !PrefabUtility.IsPartOfPrefabInstance(openedFromInstance))
                throw new ArgumentException("GameObject must be part of a Prefab instance, or null.", nameof(openedFromInstance));

            if (!prefabAssetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Incorrect file extension: " + prefabAssetPath + ". Must be '.prefab'", nameof(prefabAssetPath));

            if (AssetDatabase.LoadMainAssetAtPath(prefabAssetPath) == null)
                throw new ArgumentException("Prefab not found at path " + prefabAssetPath, nameof(prefabAssetPath));

            return OpenPrefabMode(prefabAssetPath, openedFromInstance, prefabStageMode, changeTypeAnalytics);
        }

        static PrefabStage GetExistingPrefabStage(string prefabAssetPath, GameObject openedFromInstance, PrefabStage.Mode prefabStageMode)
        {
            var stageHistory = StageNavigationManager.instance.stageHistory;
            for (int i = 1; i < stageHistory.Count; i++)
            {
                var prefabStage = stageHistory[i] as PrefabStage;
                if (prefabStage != null && prefabStage.assetPath == prefabAssetPath)
                {
                    // If PrefabStage.Mode did not match on existing PrefabStage we do not reuse the stage
                    // so we create a new PrefabStage with the correct mode
                    if (prefabStage.mode == prefabStageMode)
                        return prefabStage;
                }
            }
            return null;
        }

        internal static PrefabStage OpenPrefabMode(string prefabAssetPath, GameObject openedFromInstance, PrefabStage.Mode prefabStageMode, StageNavigationManager.Analytics.ChangeType changeTypeAnalytics)
        {
            if (EditorApplication.isPlaying)
            {
                bool blockPrefabModeInPlaymode = CheckIfAnyComponentShouldBlockPrefabModeInPlayMode(prefabAssetPath);
                if (blockPrefabModeInPlaymode)
                    return null;
            }

            PrefabStage prevPrefabStage = GetCurrentPrefabStage();
            bool setAsFirstItemAfterMainStage = prevPrefabStage == null || !IsPartOfPrefabStage(openedFromInstance, prevPrefabStage);

            var previousSelection = Selection.activeGameObject;
            UInt64 previousFileID = (openedFromInstance != null) ? GetFileIDForCorrespondingObjectFromSourceAtPath(previousSelection, prefabAssetPath) : 0;

            // Ensure valid prefabStageMode (if no context then do not allow Prefab Mode in Context)
            if (openedFromInstance == null && prefabStageMode != PrefabStage.Mode.InIsolation)
                prefabStageMode = PrefabStage.Mode.InIsolation;

            Stage contextStage = null;
            if (prefabStageMode == PrefabStage.Mode.InContext)
            {
                var stageHistory = StageNavigationManager.instance.stageHistory;
                contextStage = stageHistory[stageHistory.Count - 1].GetContextStage();
            }

            var prefabStage = GetExistingPrefabStage(prefabAssetPath, openedFromInstance, prefabStageMode);
            if (prefabStage == null)
                prefabStage = PrefabStage.CreatePrefabStage(prefabAssetPath, openedFromInstance, prefabStageMode, contextStage);
            if (StageNavigationManager.instance.SwitchToStage(prefabStage, setAsFirstItemAfterMainStage, changeTypeAnalytics))
            {
                // If selection did not change by switching stage by us or user (or if current selection is not part of
                // the opened prefab stage) then handle automatic selection in new prefab mode.
                if (Selection.activeGameObject == previousSelection || !prefabStage.IsPartOfPrefabContents(Selection.activeGameObject))
                {
                    HandleSelectionWhenSwithingToNewPrefabMode(GetCurrentPrefabStage().prefabContentsRoot, previousFileID);
                }

                SceneView.RepaintAll();
                return prefabStage;
            }
            else
            {
                // Failed to switch to new stage
                return null;
            }
        }

        static void HandleSelectionWhenSwithingToNewPrefabMode(GameObject prefabContentsRoot, UInt64 previousFileID)
        {
            GameObject newSelection = null;

            if (previousFileID != 0)
                newSelection = FindFirstGameObjectThatMatchesFileID(prefabContentsRoot.transform, previousFileID, true);

            if (newSelection == null)
                newSelection = prefabContentsRoot;

            Selection.activeGameObject = newSelection;

            // For Prefab Mode we restore the last expanded tree view state for the opened Prefab. For usability
            // if a child GameObject on the Prefab Instance is selected when entering the Prefab Asset Mode we select the corresponding
            // child GameObject in the Asset. Here we ensure that selction is revealed and framed in the Scene hierarchy.
            if (newSelection != prefabContentsRoot)
            {
                foreach (SceneHierarchyWindow shw in SceneHierarchyWindow.GetAllSceneHierarchyWindows())
                    shw.FrameObject(newSelection.GetInstanceID(), false);
            }
        }

        static bool IsPartOfPrefabStage(GameObject gameObject, PrefabStage prefabStage)
        {
            if (gameObject == null)
                return false;
            return FindGameObjectRecursive(prefabStage.prefabContentsRoot.transform, gameObject);
        }

        static bool FindGameObjectRecursive(Transform transform, GameObject gameObject)
        {
            if (transform.gameObject == gameObject)
                return true;

            for (int i = 0; i < transform.childCount; ++i)
            {
                if (FindGameObjectRecursive(transform.GetChild(i), gameObject))
                    return true;
            }
            return false;
        }

        static UInt64 GetFileIDForCorrespondingObjectFromSourceAtPath(GameObject gameObject, string prefabAssetPath)
        {
            if (gameObject == null)
                return 0;

            if (EditorUtility.IsPersistent(gameObject))
                return 0;

            if (!PrefabUtility.IsPartOfNonAssetPrefabInstance(gameObject))
                return 0;

            GameObject assetGameObject = PrefabUtility.GetCorrespondingObjectFromSourceAtPath(gameObject, prefabAssetPath);
            if (assetGameObject == null)
                return 0;

            return Unsupported.GetOrGenerateFileIDHint(assetGameObject);
        }

        // Returns true any component in prefab is blocking Prefab Mode in Play Mode
        internal static bool CheckIfAnyComponentShouldBlockPrefabModeInPlayMode(string prefabAssetPath)
        {
            var assetRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);
            if (assetRoot == null)
                return false;
            var monoBehaviors = assetRoot.GetComponentsInChildren<MonoBehaviour>(true);  // also check the inactive since these can be made active while in play mode
            var warnList = new List<MonoBehaviour>();
            foreach (var m in monoBehaviors)
            {
                if (m != null && !m.allowPrefabModeInPlayMode)
                    warnList.Add(m);
            }

            if (warnList.Count > 0)
            {
                string blockingNames = string.Join(", ", warnList.Select(e => MonoScript.FromMonoBehaviour(e).name).Distinct().ToArray());
                if (blockingNames.Length > 1000)
                    blockingNames = blockingNames.Substring(0, 1000) + "\n...";
                return EditorUtility.DisplayDialog(
                    L10n.Tr("Risk of unwanted modifications"),
                    string.Format(
                        L10n.Tr("The following scripts on the Prefab open in Prefab Mode use the [ExecuteInEditMode] attribute which means they may accidentally affect or be affected by Play Mode:\n\n{0}\n\nSee the documentation for [ExecuteInEditMode] and [ExecuteAlways] for info on how to make scripts compatible with Prefab Mode during Play Mode."),
                        blockingNames),
                    L10n.Tr("Exit Prefab Mode"), L10n.Tr("Ignore"));
            }
            return false;
        }

        [RequiredByNativeCode]
        public static PrefabStage GetCurrentPrefabStage()
        {
            return StageNavigationManager.instance.currentStage as PrefabStage;
        }

        public static PrefabStage GetPrefabStage(GameObject gameObject)
        {
            return StageUtility.GetStage(gameObject) as PrefabStage;
        }

        [RequiredByNativeCode]
        internal static bool SaveCurrentModifiedPrefabStagesIfUserWantsTo()
        {
            // Returns false if the user clicked Cancel to save otherwise returns true
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
                return prefabStage.AskUserToSaveModifiedStageBeforeSwitchingStage();

            return true;
        }

        [UsedByNativeCode]
        internal static bool IsAssetPathOpenInAnyPrefabStage(string path)
        {
            foreach (var stage in StageNavigationManager.instance.stageHistory)
            {
                var prefabStage = stage as PrefabStage;
                if (prefabStage != null && prefabStage.isValid && prefabStage.assetPath == path)
                {
                    return true;
                }
            }

            return false;
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
        internal static bool IsPrefabStageScene(Scene scene)
        {
            foreach (var prefabStage in PrefabStage.allPrefabStages)
                if (prefabStage.scene == scene)
                    return true;
            return false;
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

        internal static GameObject FindFirstGameObjectThatMatchesFileID(Transform searchRoot, UInt64 fileID, bool generate)
        {
            GameObject result = null;
            var transformVisitor = new TransformVisitor();
            transformVisitor.VisitAndAllowEarlyOut(searchRoot,
                (transform, userdata) =>
                {
                    UInt64 id = generate ?
                        Unsupported.GetOrGenerateFileIDHint(transform.gameObject) :
                        GetPrefabOrVariantFileID(transform.gameObject);
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

        static void RemoveBrokenPrefabRootsIfNeeded(string prefabAssetPath, GameObject[] environmentRoots, GameObject[] rootsAfterLoadingPrefab, GameObject rootGameObject)
        {
            var rootsLoadedFromFile = rootsAfterLoadingPrefab.Except(environmentRoots).ToList();

            // Filter out the subset of roots that were loaded from the Prefab file (there can be dynamically created environment objects,
            // created from Awake and OnEnable calls from user land, if they use ExecuteAlways or ExecuteInEditMode.
            rootsLoadedFromFile.RemoveAll(x => IsDynamicallyCreatedDuringLoad(x));

            if (rootsLoadedFromFile.Count >= 2)
            {
                Debug.LogError(string.Format("Prefab Mode: Broken Prefab with multiple roots detected ('{0}')", prefabAssetPath));
                foreach (var go in rootsLoadedFromFile)
                {
                    if (go != rootGameObject)
                    { 
                        UnityEngine.Object.DestroyImmediate(go);
                    }
                }
            }
        }

        internal static GameObject LoadPrefabIntoPreviewScene(string prefabAssetPath, Scene previewScene)
        {
            var prefabName = Path.GetFileNameWithoutExtension(prefabAssetPath);
            previewScene.name = prefabName;

            // Get start roots from scene (before loading in the the Prefab)
            var environmentRoots = previewScene.GetRootGameObjects();
            GameObject rootGameObject = null;

            // Load Prefab into scene
            try
            {
                PrefabUtility.LoadPrefabContentsIntoPreviewScene(prefabAssetPath, previewScene, out rootGameObject);
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("Loading Prefab failed: {0}", e.Message));
                return null;
            }

            var rootsAfterLoadingPrefab = previewScene.GetRootGameObjects();
            RemoveBrokenPrefabRootsIfNeeded(prefabAssetPath, environmentRoots, rootsAfterLoadingPrefab, rootGameObject);

            return rootGameObject;
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
            UnityEngine.RenderSettings.customReflectionTexture = GetDefaultReflection();   // ensure chrome materials do not render black
            UnityEngine.RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
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

        internal static void HandleUIReparentingIfNeeded(GameObject instanceRoot, int stagePriority)
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

            GameObject canvasGameObject = GetOrCreateCanvasGameObject(instanceRoot, stagePriority);
            instanceRoot.transform.SetParent(canvasGameObject.transform, false);
        }

        static GameObject GetOrCreateCanvasGameObject(GameObject instanceRoot, int stagePriority)
        {
            Canvas canvas = GetCanvasInScene(instanceRoot);
            if (canvas != null)
            {
                canvas.stagePriority = (byte)stagePriority;
                return canvas.gameObject;
            }

            const string kUILayerName = "UI";

            // Create canvas root for the UI
            GameObject root = EditorUtility.CreateGameObjectWithHideFlags("Canvas (Environment)", HideFlags.DontSave);
            root.layer = LayerMask.NameToLayer(kUILayerName);
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.stagePriority = (byte)stagePriority;
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

        internal static PrefabStage.Mode GetPrefabStageModeFromModifierKeys()
        {
            // Update GetPrefabButtonContent if this logic changes
            var defaultPrefabMode = PreferencesProvider.GetDefaultPrefabModeForHierarchy();
            var alternativePrefabMode = (defaultPrefabMode == PrefabStage.Mode.InContext) ? PrefabStage.Mode.InIsolation : PrefabStage.Mode.InContext;

            return Event.current.alt ? alternativePrefabMode : defaultPrefabMode;
        }

        internal static GUIContent GetPrefabButtonContent(int instanceID)
        {
            GUIContent result;
            var defaultPrefabMode = PreferencesProvider.GetDefaultPrefabModeForHierarchy();
            var modifierKey = Application.platform == RuntimePlatform.OSXEditor ? "Option" : "Alt";
            switch (defaultPrefabMode)
            {
                case PrefabStage.Mode.InContext:
                    result = new GUIContent("", null, $"Open Prefab Asset in context.\nPress the {modifierKey} modifier key to open in isolation.");
                    break;
                case PrefabStage.Mode.InIsolation:
                    result = new GUIContent("", null, $"Open Prefab Asset in isolation.\nPress the {modifierKey} modifier key to open in context.");
                    break;
                default:
                    result = new GUIContent("");
                    Debug.LogError("Unhandled defaultPrefabMode enum");
                    break;
            }
            return result;
        }
    }
}


// We keep a dummy UnityEditor.Experimental.SceneManagement namespace declared because the API Updater does
// not yet support removing the 'using some.namespace.xyz' declarations in files if there is no usage of
// classes of that namespace in the file. E.g consider this user script which currently is not updated:
//
// using UnityEditor.Experimental.SceneManagement;
// public class SomeClass: MonoBehaviour
// {
//      void Update() { };
// }
//
// We can remove the UnityEditor.Experimental.SceneManagement namespace below when the APIUpdater supports updating the above test script
namespace UnityEditor.Experimental.SceneManagement
{
    class Dummy
    {
    }
}
