// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;


namespace UnityEditor.Experimental.SceneManagement
{
    // For the initial Improved Prefabs release we didn't have time to generalize the stage concept to also work
    // for other things than main scenes and Prefabs. But it makes sense to use also for e.g. Avatar Setup.
    // Until we have gone through the work of generalizing it by making e.g. Avatar Setup use it too,
    // we're not confident that the API is right, hence it's experimental for now.

    [Serializable]
    public class PrefabStage
    {
        public static event Action<PrefabStage> prefabStageOpened;
        public static event Action<PrefabStage> prefabStageClosing;
        public static event Action<GameObject> prefabSaving;
        public static event Action<GameObject> prefabSaved;
        internal static event Action<PrefabStage> prefabIconChanged;
        internal static event Action<PrefabStage> prefabStageSavedAsNewPrefab;

        GameObject m_PrefabContentsRoot; // Prefab asset being edited
        Scene m_PreviewScene;
        string m_PrefabAssetPath;
        int m_InitialSceneDirtyID;
        int m_LastSceneDirtyID;
        bool m_IgnoreNextAssetImportedEventForCurrentPrefab;
        bool m_PrefabWasChangedOnDisk;
        HideFlagUtility m_HideFlagUtility;
        Texture2D m_PrefabFileIcon;
        bool m_TemporarilyDisableAutoSave;
        float m_LastSavingDuration = 0f;
        const float kDurationBeforeShowingSavingBadge = 1.0f;

        bool m_AnalyticsDidUserModify;
        bool m_AnalyticsDidUserSave;

        internal bool analyticsDidUserModify { get { return m_AnalyticsDidUserModify; } }
        internal bool analyticsDidUserSave { get { return m_AnalyticsDidUserSave; } }

        static class Icons
        {
            public static Texture2D prefabVariantIcon = EditorGUIUtility.LoadIconRequired("PrefabVariant Icon");
            public static Texture2D prefabIcon = EditorGUIUtility.LoadIconRequired("Prefab Icon");
        }

        public Scene scene
        {
            get { return m_PreviewScene; }
        }

        public GameObject prefabContentsRoot
        {
            get
            {
                if (m_PrefabContentsRoot == null)
                    throw new InvalidOperationException("Requesting 'prefabContentsRoot' from Awake and OnEnable are not supported"); // The prefab stage's m_PrefabContentsRoot is not yet set when we call Awake and OnEnable on user scripts when loading a prefab
                return m_PrefabContentsRoot;
            }
        }

        public StageHandle stageHandle
        {
            get { return StageHandle.GetStageHandle(scene); }
        }

        public bool IsPartOfPrefabContents(GameObject gameObject)
        {
            if (gameObject == null)
                return false;
            Transform tr = gameObject.transform;
            Transform instanceRootTransform = prefabContentsRoot.transform;
            while (tr != null)
            {
                if (tr == instanceRootTransform)
                    return true;
                tr = tr.parent;
            }
            return false;
        }

        public string prefabAssetPath
        {
            get { return m_PrefabAssetPath; }
        }

        internal bool showingSavingLabel
        {
            get;
            private set;
        }

        internal Texture2D prefabFileIcon
        {
            get { return m_PrefabFileIcon; }
        }

        string GetPrefabFileName()
        {
            return Path.GetFileNameWithoutExtension(m_PrefabAssetPath);
        }

        internal bool autoSave
        {
            get { return !m_TemporarilyDisableAutoSave && StageNavigationManager.instance.autoSave; }
            set { m_TemporarilyDisableAutoSave = false; StageNavigationManager.instance.autoSave = value; }
        }

        internal bool temporarilyDisableAutoSave
        {
            get { return m_TemporarilyDisableAutoSave; }
        }

        internal bool initialized
        {
            get { return m_PrefabContentsRoot != null; }
        }

        internal PrefabStage()
        {
        }

        internal bool LoadStage(string prefabPath)
        {
            if (initialized)
                Cleanup();

            m_PrefabAssetPath = prefabPath;

            // Ensure m_PreviewScene is set before calling LoadPrefabIntoPreviewScene() so the user can request the current
            // the PrefabStage in their OnEnable and other callbacks (if they use ExecuteInEditMode or ExecuteAlways)
            bool isUIPrefab = PrefabStageUtility.IsUIPrefab(m_PrefabAssetPath);
            m_PreviewScene = PrefabStageUtility.GetEnvironmentSceneOrEmptyScene(isUIPrefab);

            m_PrefabContentsRoot = PrefabStageUtility.LoadPrefabIntoPreviewScene(prefabAssetPath, m_PreviewScene);
            if (m_PrefabContentsRoot != null)
            {
                PrefabStageUtility.HandleReparentingIfNeeded(m_PrefabContentsRoot, isUIPrefab);
                m_PrefabFileIcon = DeterminePrefabFileIconFromInstanceRootGameObject();
                m_InitialSceneDirtyID = m_PreviewScene.dirtyID;
                UpdateEnvironmentHideFlags();
            }
            else
            {
                // Invalid setup
                Cleanup();
            }

            return initialized;
        }

        // Returns true if opened successfully
        internal bool OpenStage(string prefabPath)
        {
            if (LoadStage(prefabPath))
            {
                if (prefabStageOpened != null)
                {
                    prefabStageOpened(this);

                    // Update environment scene objects after the 'prefabStageOpened' user callback so we can
                    // ensure: correct hideflags and that our prefab root is not under a prefab instance (which would mark it as an added object).
                    // Note: The user can have reparented and created new GameObjects in the environment scene during this callback.
                    EnsureParentOfPrefabRootIsUnpacked();
                    UpdateEnvironmentHideFlags();
                }
                return true;
            }
            return false;
        }

        internal void CloseStage()
        {
            if (!initialized)
                return;

            prefabStageClosing?.Invoke(this);

            Cleanup();
        }

        void Cleanup()
        {
            if (m_PreviewScene.IsValid())
            {
                List<GameObject> roots = new List<GameObject>();
                m_PreviewScene.GetRootGameObjects(roots);
                foreach (var go in roots)
                    UnityEngine.Object.DestroyImmediate(go);
                PrefabStageUtility.DestroyPreviewScene(m_PreviewScene);
            }

            m_PrefabContentsRoot = null;
            m_HideFlagUtility = null;
            m_PrefabAssetPath = null;
            m_InitialSceneDirtyID = 0;
            m_LastSceneDirtyID = 0;
            m_IgnoreNextAssetImportedEventForCurrentPrefab = false;
            m_PrefabWasChangedOnDisk = false;
        }

        void ReloadStage()
        {
            if (SceneHierarchy.s_DebugPrefabStage)
                Debug.Log("RELOADING Prefab at " + m_PrefabAssetPath);

            StageNavigationManager.instance.PrefabStageReloading(this);
            if (OpenStage(m_PrefabAssetPath))
                StageNavigationManager.instance.PrefabStageReloaded(this);

            if (SceneHierarchy.s_DebugPrefabStage)
                Debug.Log("RELOADING done");
        }

        internal void Update()
        {
            if (!initialized)
                return;

            if (HasSceneBeenModified())
                m_AnalyticsDidUserModify = true;

            UpdateEnvironmentHideFlagsIfNeeded();
            HandleAutoSave();
            HandlePrefabChangedOnDisk();
            DetectSceneDirtinessChange();
            DetectPrefabFileIconChange();
        }

        void DetectPrefabFileIconChange()
        {
            var icon = DeterminePrefabFileIconFromInstanceRootGameObject();
            if (icon != m_PrefabFileIcon)
            {
                m_PrefabFileIcon = icon;

                if (prefabIconChanged != null)
                    prefabIconChanged(this);
            }
        }

        void DetectSceneDirtinessChange()
        {
            if (m_PreviewScene.dirtyID != m_LastSceneDirtyID)
                StageNavigationManager.instance.PrefabStageDirtinessChanged(this);
            m_LastSceneDirtyID = m_PreviewScene.dirtyID;
        }

        void UpdateEnvironmentHideFlagsIfNeeded()
        {
            if (m_HideFlagUtility == null)
                m_HideFlagUtility = new HideFlagUtility(m_PreviewScene, m_PrefabContentsRoot);
            m_HideFlagUtility.UpdateEnvironmentHideFlagsIfNeeded();
        }

        void UpdateEnvironmentHideFlags()
        {
            if (m_HideFlagUtility == null)
                m_HideFlagUtility = new HideFlagUtility(m_PreviewScene, m_PrefabContentsRoot);
            m_HideFlagUtility.UpdateEnvironmentHideFlags();
        }

        void EnsureParentOfPrefabRootIsUnpacked()
        {
            var parent = m_PrefabContentsRoot.transform.parent;
            if (parent != null)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(parent))
                {
                    var outerMostPrefabInstance = PrefabUtility.GetOutermostPrefabInstanceRoot(parent);
                    PrefabUtility.UnpackPrefabInstance(outerMostPrefabInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
            }
        }

        bool isTextFieldCaretShowing
        {
            get { return EditorGUI.IsEditingTextField() && !EditorGUIUtility.textFieldHasSelection; }
        }

        bool readyToAutoSave
        {
            get { return m_PrefabContentsRoot != null && HasSceneBeenModified() && GUIUtility.hotControl == 0 && !isTextFieldCaretShowing && !EditorApplication.isCompiling; }
        }

        void HandleAutoSave()
        {
            if (autoSave && readyToAutoSave)
            {
                AutoSave();
            }
        }

        void HandlePrefabChangedOnDisk()
        {
            if (m_PrefabWasChangedOnDisk)
            {
                m_PrefabWasChangedOnDisk = false;
                if (HasSceneBeenModified())
                {
                    var title = L10n.Tr("Prefab Has Been Changed on Disk");
                    var message = string.Format(L10n.Tr("You have modifications to the Prefab '{0}' that was changed on disk while in Prefab Mode. Do you want to keep your changes or reload the Prefab and discard your changes?"), m_PrefabContentsRoot.name);
                    bool keepChanges = EditorUtility.DisplayDialog(title, message, L10n.Tr("Keep Changes"), L10n.Tr("Discard Changes"));
                    if (!keepChanges)
                        ReloadStage();
                }
                else
                {
                    ReloadStage();
                }
            }
        }

        public void ClearDirtiness()
        {
            EditorSceneManager.ClearSceneDirtiness(m_PreviewScene);
            m_InitialSceneDirtyID = m_PreviewScene.dirtyID;
        }

        bool PromptIfMissingBasePrefabForVariant()
        {
            if (PrefabUtility.IsPrefabAssetMissing(m_PrefabContentsRoot))
            {
                string title = L10n.Tr("Saving Variant Failed");
                string message = L10n.Tr("Can't save the Prefab Variant when its base Prefab is missing. You have to unpack the root GameObject or recover the missing base Prefab in order to save the Prefab Variant");
                if (autoSave)
                    message += L10n.Tr("\n\nAuto Save has been temporarily disabled.");
                EditorUtility.DisplayDialog(title, message, L10n.Tr("OK"));
                m_TemporarilyDisableAutoSave = true;
                return true;
            }
            return false;
        }

        // Returns true if saved succesfully (internal so we can use it in Tests)
        internal bool SavePrefab()
        {
            if (!initialized)
                return false;

            m_AnalyticsDidUserSave = true;
            m_AnalyticsDidUserModify = true;

            if (SceneHierarchy.s_DebugPrefabStage)
                Debug.Log("SAVE PREFAB");

            if (prefabSaving != null)
                prefabSaving(m_PrefabContentsRoot);

            var startTime = EditorApplication.timeSinceStartup;

            if (PromptIfMissingBasePrefabForVariant())
                return false;

            // The user can have deleted required folders
            var folder = Path.GetDirectoryName(m_PrefabAssetPath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            bool savedSuccesfully;
            PrefabUtility.SaveAsPrefabAsset(m_PrefabContentsRoot, m_PrefabAssetPath, out savedSuccesfully);
            m_LastSavingDuration = (float)(EditorApplication.timeSinceStartup - startTime);

            if (savedSuccesfully)
            {
                ClearDirtiness();

                if (prefabSaved != null)
                    prefabSaved(m_PrefabContentsRoot);

                // After saving the Prefab any Prefab instances in the environment scene
                // that are dependent on the saved Prefab will have lost their hideflags
                // so here we set them again.
                UpdateEnvironmentHideFlags();
            }
            else
            {
                string title = L10n.Tr("Saving Failed");
                string message = L10n.Tr("Saving failed. Check the Console window to get more insight into what needs to be fixed.");
                if (autoSave)
                    message += L10n.Tr("\n\nAuto Save has been temporarily disabled.");
                EditorUtility.DisplayDialog(title, message, L10n.Tr("OK"));

                m_TemporarilyDisableAutoSave = true;
                m_IgnoreNextAssetImportedEventForCurrentPrefab = false;
            }

            if (SceneHierarchy.s_DebugPrefabStage)
                Debug.Log("SAVE PREFAB ended");

            showingSavingLabel = false;

            return savedSuccesfully;
        }

        internal bool SaveAsNewPrefab(string newPath, bool asCopy)
        {
            if (!initialized)
                return false;

            if (newPath == m_PrefabAssetPath)
            {
                throw new ArgumentException("Cannot save as new prefab using the same path");
            }

            if (asCopy)
            {
                // Keep current open prefab and save copy at newPath
                string oldName = m_PrefabContentsRoot.name;
                m_PrefabContentsRoot.name = Path.GetFileNameWithoutExtension(newPath);
                PrefabUtility.SaveAsPrefabAsset(m_PrefabContentsRoot, newPath);
                m_PrefabContentsRoot.name = oldName;
            }
            else
            {
                // Change the current open prefab and save
                m_PrefabContentsRoot.name = Path.GetFileNameWithoutExtension(newPath);
                m_PrefabAssetPath = newPath;
                ClearDirtiness();
                PrefabUtility.SaveAsPrefabAsset(m_PrefabContentsRoot, newPath);

                if (prefabStageSavedAsNewPrefab != null)
                    prefabStageSavedAsNewPrefab(this);
            }

            return true;
        }

        internal bool SaveAsNewPrefabWithSavePanel()
        {
            Assert.IsTrue(m_PrefabContentsRoot != null, "We should have a valid m_PrefabContentsRoot when saving to prefab asset");
            bool editablePrefab = !AnimationMode.InAnimationMode();
            if (!editablePrefab)
            {
                EditorUtility.DisplayDialog(L10n.Tr("Cannot Save Prefab"), L10n.Tr("Cannot save prefab in Animation Preview Mode"), L10n.Tr("OK"));
                return false;
            }

            string directoryOfCurrentPrefab = Path.GetDirectoryName(m_PrefabAssetPath);
            string nameOfCurrentPrefab = Path.GetFileNameWithoutExtension(m_PrefabAssetPath);
            string relativePath;
            while (true)
            {
                relativePath = EditorUtility.SaveFilePanelInProject("Save Prefab", nameOfCurrentPrefab + " Copy", "prefab", "", directoryOfCurrentPrefab);

                // Cancel pressed
                if (string.IsNullOrEmpty(relativePath))
                    return false;

                if (relativePath == m_PrefabAssetPath)
                {
                    if (EditorUtility.DisplayDialog(L10n.Tr("Save Prefab has failed"), L10n.Tr("Overwriting the same path as another open prefab is not allowed."), L10n.Tr("Try Again"), L10n.Tr("Cancel")))
                        continue;

                    return false;
                }
                break;
            }

            return SaveAsNewPrefab(relativePath, false);
        }

        void PerformDelayedAutoSave()
        {
            EditorApplication.update -= PerformDelayedAutoSave;
            SavePrefabWithVersionControlDialogAndRenameDialog();
        }

        void AutoSave()
        {
            showingSavingLabel = m_LastSavingDuration > kDurationBeforeShowingSavingBadge;
            if (showingSavingLabel)
            {
                // Save delayed if we want to show the save badge while saving.
                foreach (SceneView sceneView in SceneView.sceneViews)
                    sceneView.Repaint();

                EditorApplication.update += PerformDelayedAutoSave;
            }
            else
            {
                // Save directly if we don't want to show the saving badge
                SavePrefabWithVersionControlDialogAndRenameDialog();
            }
        }

        // Returns true if prefab was saved.
        internal bool SavePrefabWithVersionControlDialogAndRenameDialog()
        {
            if (m_PrefabContentsRoot == null)
            {
                Debug.LogError("We should have a valid m_PrefabContentsRoot when saving to prefab asset");
                return false;
            }

            if (!PrefabUtility.PromptAndCheckoutPrefabIfNeeded(m_PrefabAssetPath, PrefabUtility.SaveVerb.Save))
            {
                // If user doesn't want to check out prefab asset, or it cannot be,
                // it doesn't make sense to keep auto save on.
                m_TemporarilyDisableAutoSave = true;
                return false;
            }

            bool showCancelButton = !autoSave;
            if (!CheckRenamedPrefabRootWhenSaving(showCancelButton))
                return false;

            return SavePrefab();
        }

        // Returns true if we should continue saving
        bool CheckRenamedPrefabRootWhenSaving(bool showCancelButton)
        {
            var prefabFilename = GetPrefabFileName();
            if (m_PrefabContentsRoot.name != prefabFilename)
            {
                string folder = Path.GetDirectoryName(m_PrefabAssetPath);
                string extension = Path.GetExtension(m_PrefabAssetPath);
                string newPath = Path.Combine(folder, m_PrefabContentsRoot.name + extension);
                string errorMsg = AssetDatabase.ValidateMoveAsset(m_PrefabAssetPath, newPath);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    var t = L10n.Tr("Rename Prefab File Not Possible");
                    var m = string.Format(L10n.Tr("The Prefab file name must match the Prefab root GameObject name but there is already a Prefab asset with the file name '{0}' in the same folder. The root GameObject name will therefore be changed back to match the Prefab file name when saving."), m_PrefabContentsRoot.name);

                    if (showCancelButton)
                    {
                        if (EditorUtility.DisplayDialog(t, m, L10n.Tr("OK"), L10n.Tr("Cancel Save")))
                        {
                            RenameInstanceRootToMatchPrefabFile();
                            return true;
                        }
                        else
                        {
                            return false; // Cancel saving
                        }
                    }

                    EditorUtility.DisplayDialog(t, m, L10n.Tr("OK"));
                    RenameInstanceRootToMatchPrefabFile();
                    return true;
                }

                var title = L10n.Tr("Rename Prefab File?");
                var message = string.Format(L10n.Tr("The Prefab file name must match the Prefab root GameObject name. Do you want to rename the file to '{0}' or use the old name '{1}' for both?"), m_PrefabContentsRoot.name, prefabFilename);

                if (showCancelButton)
                {
                    int option = EditorUtility.DisplayDialogComplex(title, message, L10n.Tr("Rename File"), L10n.Tr("Use Old Name"), L10n.Tr("Cancel Save"));
                    switch (option)
                    {
                        // Rename prefab file
                        case 0:
                            RenamePrefabFileToMatchPrefabInstanceName();
                            return true;
                        // Rename the root GameObject to file name
                        case 1:
                            RenameInstanceRootToMatchPrefabFile();
                            return true;
                        // Cancel saving
                        case 2:
                            return false;
                    }
                }
                else
                {
                    bool renameFile = EditorUtility.DisplayDialog(title, message, L10n.Tr("Rename File"), L10n.Tr("Use Old Name"));
                    if (renameFile)
                        RenamePrefabFileToMatchPrefabInstanceName();
                    else
                        RenameInstanceRootToMatchPrefabFile();
                    return true;
                }
            }
            return true;
        }

        internal void RenameInstanceRootToMatchPrefabFile()
        {
            m_PrefabContentsRoot.name = GetPrefabFileName();
        }

        internal void RenamePrefabFileToMatchPrefabInstanceName()
        {
            string newPrefabFileName = m_PrefabContentsRoot.name;

            if (SceneHierarchy.s_DebugPrefabStage)
                Debug.LogFormat("RENAME Prefab Asset: '{0} to '{1}'", m_PrefabAssetPath, newPrefabFileName);

            // If we don't ignore the next import event we will reload the current prefab and hereby loosing the current selection (inspector goes blank)
            // Since we have made the change ourselves (rename) we don't need to reload our instances.
            m_IgnoreNextAssetImportedEventForCurrentPrefab = true;

            string errorMsg = AssetDatabase.RenameAsset(m_PrefabAssetPath, newPrefabFileName);
            if (!string.IsNullOrEmpty(errorMsg))
                Debug.LogError(errorMsg);

            if (SceneHierarchy.s_DebugPrefabStage)
                Debug.Log("RENAME done");
        }

        // Returns true if ok to continue to destroying scene
        internal bool AskUserToSaveDirtySceneBeforeDestroyingScene()
        {
            if (!HasSceneBeenModified() || m_PrefabContentsRoot == null)
                return true; // no changes to save or no root to save; continue

            // Rare condition. Prefab should have already been saved if auto-save is enabled,
            // but it's possible it hasn't, so save when exiting just in case.
            if (autoSave)
                return SavePrefabWithVersionControlDialogAndRenameDialog();

            int dialogResult = EditorUtility.DisplayDialogComplex("Prefab Has Been Modified", "Do you want to save the changes you made in Prefab Mode? Your changes will be lost if you don't save them.", "Save", "Discard Changes", "Cancel");
            switch (dialogResult)
            {
                case 0:
                    return SavePrefabWithVersionControlDialogAndRenameDialog(); // save changes and continue if possible
                case 1:
                    return true; // discard changes and continue
                case 2:
                    return false; // cancel and discontinue current operation
                default:
                    throw new InvalidOperationException("Unhandled dialog result " + dialogResult);
            }
        }

        internal void OnSavingPrefab(GameObject gameObject, string path)
        {
            // We care about 'irrelevant prefab paths' since the 'irrelevant' prefab could be a nested prefab to the
            // current prefab being edited (this would result in our current prefab being reimported due to the dependency).
            // For nested prefabs the prefab merging code will take care of updating the current GameObjects loaded
            // so in this case do not reload the prefab even though the path shows up in AssetsChangedOnHDD event.

            bool savedBySelf = gameObject == m_PrefabContentsRoot;
            bool irrelevantPath = path != m_PrefabAssetPath;
            if (savedBySelf || irrelevantPath)
            {
                m_IgnoreNextAssetImportedEventForCurrentPrefab = true;
            }
        }

        internal void OnAssetsChangedOnHDD(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (SceneHierarchy.s_DebugPrefabStage)
                Debug.LogFormat("AssetDatabase Event: Current Prefab: {0}, *ImportedAssets: {1}, *DeletedAssets: {2}, *MovedAssets: {3}, *MovedFromAssetPaths: {4}", m_PrefabAssetPath, DebugUtils.ListToString(importedAssets), DebugUtils.ListToString(deletedAssets), DebugUtils.ListToString(movedAssets), DebugUtils.ListToString(movedFromAssetPaths));

            // Prefab was moved (update cached path)
            for (int i = 0; i < movedFromAssetPaths.Length; ++i)
            {
                if (movedFromAssetPaths[i].Equals(m_PrefabAssetPath, StringComparison.OrdinalIgnoreCase))
                {
                    m_PrefabAssetPath = movedAssets[i];
                    break;
                }
            }

            // Detect if our Prefab was modified on HDD outside Prefab Mode (in that case we should ask the user if he wants to reload it)
            for (int i = 0; i < importedAssets.Length; ++i)
            {
                if (importedAssets[i] == m_PrefabAssetPath)
                {
                    if (!m_IgnoreNextAssetImportedEventForCurrentPrefab)
                        m_PrefabWasChangedOnDisk = true;

                    // Reset the ignore flag when we finally have imported the saved prefab (We set this flag when saving the Prefab from Prefab Mode)
                    // Note we can get multiple OnAssetsChangedOnHDD events before the Prefab imported event if e.g folders of the Prefab path needs to be reimported first.
                    m_IgnoreNextAssetImportedEventForCurrentPrefab = false;
                    break;
                }
            }
        }

        void DestroyPrefabInstance()
        {
            if (m_PrefabContentsRoot == null)
                return;
            UnityEngine.Object.DestroyImmediate(m_PrefabContentsRoot);
        }

        internal bool HasSceneBeenModified()
        {
            return m_PreviewScene.dirtyID != m_InitialSceneDirtyID;
        }

        Texture2D DeterminePrefabFileIconFromInstanceRootGameObject()
        {
            bool partOfInstance = PrefabUtility.IsPartOfNonAssetPrefabInstance(prefabContentsRoot);
            bool disconnected = PrefabUtility.GetPrefabInstanceStatus(prefabContentsRoot) == PrefabInstanceStatus.Disconnected;
            if (partOfInstance && !disconnected)
                return Icons.prefabVariantIcon;
            return Icons.prefabIcon;
        }

        internal const HideFlags kVisibleEnvironmentObjectHideFlags = HideFlags.DontSave | HideFlags.NotEditable;
        internal const HideFlags kNotVisibleEnvironmentObjectHideFlags = HideFlags.DontSave | HideFlags.NotEditable | HideFlags.HideInHierarchy;

        class HideFlagUtility
        {
            int m_LastSceneDirtyID = -1;
            List<GameObject> m_Roots = new List<GameObject>();
            GameObject m_PrefabInstanceRoot;
            Scene m_Scene;

            public HideFlagUtility(Scene scene, GameObject prefabInstanceRoot)
            {
                m_Scene = scene;
                m_PrefabInstanceRoot = prefabInstanceRoot;
            }

            public void UpdateEnvironmentHideFlagsIfNeeded()
            {
                if (m_LastSceneDirtyID == m_Scene.dirtyID)
                    return;
                m_LastSceneDirtyID = m_Scene.dirtyID;
                UpdateEnvironmentHideFlags();
            }

            public void UpdateEnvironmentHideFlags()
            {
                ValidatePreviewSceneConsistency();

                // We use hideflags to hide all environment objects (and make them non-editable since the user cannot save them)
                GameObject rootOfPrefabInstance = GetRoot(m_PrefabInstanceRoot);

                // Set all environment root hierarchies
                m_Scene.GetRootGameObjects(m_Roots);
                foreach (GameObject go in m_Roots)
                {
                    if (go == rootOfPrefabInstance)
                        continue;

                    SetHideFlagsRecursively(go.transform, kNotVisibleEnvironmentObjectHideFlags);
                }

                if (rootOfPrefabInstance != m_PrefabInstanceRoot)
                {
                    // Our prefab instance root might be a child of an environment object. Here we set those environment objects hidden (leaving the root prefab instance unchanged)
                    SetHideFlagsRecursivelyWithIgnore(rootOfPrefabInstance.transform, m_PrefabInstanceRoot.transform, kNotVisibleEnvironmentObjectHideFlags);

                    // And finally we ensure the ancestors of the prefab root are visible
                    Transform current = m_PrefabInstanceRoot.transform.parent;
                    while (current != null)
                    {
                        if (current.hideFlags != kVisibleEnvironmentObjectHideFlags)
                            current.gameObject.hideFlags = kVisibleEnvironmentObjectHideFlags;
                        current = current.parent;
                    }
                }
            }

            static GameObject GetRoot(GameObject go)
            {
                Transform current = go.transform;
                while (true)
                {
                    if (current.parent == null)
                        return current.gameObject;
                    current = current.parent;
                }
            }

            static void SetHideFlagsRecursively(Transform transform, HideFlags hideFlags)
            {
                if (transform.hideFlags != hideFlags)
                    transform.gameObject.hideFlags = hideFlags;

                for (int i = 0; i < transform.childCount; i++)
                    SetHideFlagsRecursively(transform.GetChild(i), hideFlags);
            }

            static void SetHideFlagsRecursivelyWithIgnore(Transform transform, Transform ignoreThisTransform, HideFlags hideFlags)
            {
                if (transform == ignoreThisTransform)
                    return;

                if (transform.gameObject.hideFlags != hideFlags)
                    transform.gameObject.hideFlags = hideFlags;  // GameObject also sets all components so check before setting

                for (int i = 0; i < transform.childCount; i++)
                    SetHideFlagsRecursivelyWithIgnore(transform.GetChild(i), ignoreThisTransform, hideFlags);
            }

            void ValidatePreviewSceneConsistency()
            {
                if (!ValidatePreviewSceneState.ValidatePreviewSceneObjectState(m_PrefabInstanceRoot))
                {
                    ValidatePreviewSceneState.LogErrors();
                }
            }
        }

        static class ValidatePreviewSceneState
        {
            static List<string> m_Errors = new List<string>();

            public static void LogErrors()
            {
                string combinedErrors = string.Join("\n", m_Errors.ToArray());
                Debug.LogError("Inconsistent preview object state:\n" + combinedErrors);
            }

            static public bool ValidatePreviewSceneObjectState(GameObject root)
            {
                m_Errors.Clear();
                TransformVisitor visitor = new TransformVisitor();
                visitor.VisitAll(root.transform, ValidateGameObject, null);
                return m_Errors.Count == 0;
            }

            static void ValidateGameObject(Transform transform, object userData)
            {
                GameObject go = transform.gameObject;
                if (!EditorSceneManager.IsPreviewSceneObject(go))
                {
                    m_Errors.Add("   GameObject not correctly marked as PreviewScene object: " + go.name);
                }

                var components = go.GetComponents<Component>();
                foreach (var c in components)
                {
                    if (c == null)
                    {
                        // This can happen if a monobehaviour has a missing script.
                        // In this case the check can not be made. Could be fixed
                        // by moving the component iteration to native code.
                        continue;
                    }
                    if (!EditorSceneManager.IsPreviewSceneObject(c))
                        m_Errors.Add(string.Format("   Component {0} not correctly marked as PreviewScene object: (GameObject: {1})", c.GetType().Name, go.name));
                }
            }
        }
    }
}
