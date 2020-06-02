// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace UnityEditor
{
    [CustomEditor(typeof(PrefabImporter))]
    [CanEditMultipleObjects]
    internal class PrefabImporterEditor : AssetImporterEditor
    {
        static class Styles
        {
            public static GUIContent openContent = EditorGUIUtility.TrTextContent("Open Prefab");
            public static GUIContent openHelpText = EditorGUIUtility.TrTextContent("Open Prefab for full editing support.");
            public static GUIContent missingScriptsHelpText = EditorGUIUtility.TrTextContent("Prefab has missing scripts. Open Prefab to fix the issue.");
            public static GUIContent multiSelectionMissingScriptsHelpText = EditorGUIUtility.TrTextContent("Some of the selected Prefabs have missing scripts and needs to be fixed before editing them. Click to Open Prefab to fix the issue.");
            public static GUIContent savingFailedHelpText = EditorGUIUtility.TrTextContent("Saving has failed. Check the Console window to get more insight into what needs to be fixed on the Prefab Asset.\n\nOpen Prefab to fix the issue.");
            public static GUIContent baseContent = EditorGUIUtility.TrTextContent("Base");
            public static string localizedTitleMultiplePrefabs = L10n.Tr("Prefab Assets");
            public static string localizedTitleSinglePrefab = L10n.Tr("Prefab Asset");
            public static GUIStyle openButtonStyle = "AC Button";
        }

        int m_HasMixedBaseVariants = -1;
        double m_NextUpdate;
        List<string> m_PrefabsWithMissingScript = new List<string>();
        bool m_SavingHasFailed;
        List<Component> m_TempComponentsResults = new List<Component>();
        List<GameObject> m_DirtyPrefabAssets = new List<GameObject>();

        public override bool showImportedObject { get { return !hasMissingScripts; } }

        public override bool UseDefaultMargins()
        {
            return false;
        }

        bool isTextFieldCaretShowing
        {
            get { return EditorGUI.IsEditingTextField() && !EditorGUIUtility.textFieldHasSelection; }
        }

        bool readyToAutoSave
        {
            get { return !m_SavingHasFailed && !hasMissingScripts && GUIUtility.hotControl == 0 && !isTextFieldCaretShowing && !EditorApplication.isCompiling; }
        }

        bool hasMissingScripts
        {
            get { return m_PrefabsWithMissingScript.Count > 0; }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            EditorApplication.update += Update;
        }

        public override void OnDisable()
        {
            EditorApplication.update -= Update;
            base.OnDisable();
        }

        protected override void Awake()
        {
            base.Awake();

            foreach (var prefabAssetRoot in assetTargets)
            {
                if (PrefabUtility.HasInvalidComponent(prefabAssetRoot))
                {
                    m_PrefabsWithMissingScript.Add(AssetDatabase.GetAssetPath(prefabAssetRoot));
                }
            }
            m_PrefabsWithMissingScript.Sort();
        }

        void OnDestroy()
        {
            // Ensure to save unsaved changes (regardless of hotcontrol etc)
            if (!m_SavingHasFailed && !hasMissingScripts)
                SaveDirtyPrefabAssets(false);
        }

        void Update()
        {
            var time = EditorApplication.timeSinceStartup;
            if (time > m_NextUpdate)
            {
                m_NextUpdate = time + 0.2;

                if (readyToAutoSave && HasDirtyPrefabAssets())
                    SaveDirtyPrefabAssets(true);
            }
        }

        // Internal for testing framework
        internal void SaveDirtyPrefabAssets(bool rebuildInspectors)
        {
            if (assetTargets == null)
                return;

            if (assetTarget == null)
                return;

            m_DirtyPrefabAssets.Clear();
            foreach (var asset in assetTargets)
            {
                // The asset could have been deleted when this method is called from OnDestroy().
                // E.g delete the selected prefab asset from the Project Browser.
                if (asset == null)
                    continue;

                if (!EditorUtility.IsPersistent(asset))
                    continue;

                if (!(asset is GameObject))
                    continue;

                var rootGameObject = (GameObject)asset;
                if (IsDirty(rootGameObject))
                    m_DirtyPrefabAssets.Add(rootGameObject);
            }

            if (m_DirtyPrefabAssets.Count > 0)
            {
                bool sourceFileChangedAfterSaving = false;
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var rootGameObject in m_DirtyPrefabAssets)
                    {
                        var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(rootGameObject));
                        var hashBeforeSaving = AssetDatabase.GetSourceAssetFileHash(guid);

                        bool savedSuccesfully;
                        PrefabUtility.SavePrefabAsset(rootGameObject, out savedSuccesfully);
                        if (!savedSuccesfully)
                        {
                            string title = L10n.Tr("Saving Failed");
                            string message = L10n.Tr("Check the Console window to get more insight into what needs to be fixed on the Prefab Asset.\n\nYou can open Prefab Mode to fix any issues on child GameObjects");
                            EditorUtility.DisplayDialog(title, message, L10n.Tr("OK"));

                            m_SavingHasFailed = true;
                            break;
                        }

                        // Fix case 1239807: Prevent calling ForceRebuildInspectors() if the the user is dirtying the prefab asset on every CustomEditor::OnEnable() with a
                        // value that is the same as before (so the artifact does not change). This fix avoids constant rebuilding of the Inspector window.
                        sourceFileChangedAfterSaving |= AssetDatabase.GetSourceAssetFileHash(guid) != hashBeforeSaving;
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();

                    // All inspectors needs to be rebuild to ensure property changes are reflected after saving the Prefab shown.
                    // (Saving clears the m_DirtyIndex of the target which is used for updating inspectors via SerializedObject::UpdateIfRequiredOrScript()
                    // and thus the cached dirty index in SerializedObject is not updated meaning the source object is not reloaded even though it changed)
                    if (sourceFileChangedAfterSaving && rebuildInspectors)
                        EditorUtility.ForceRebuildInspectors();
                }
            }
        }

        internal bool HasDirtyPrefabAssets()
        {
            if (assetTarget == null)
                return false;

            if (typeof(GameObject) != assetTarget.GetType())
                return false;

            // We just check one target since we assume that a multi-edit will
            // always edit that target. So no need to spend resources on checking
            // all targets in multiselection.
            return IsDirty((GameObject)assetTarget);
        }

        bool IsDirty(GameObject prefabAssetRoot)
        {
            if (prefabAssetRoot == null)
                return false;

            if (EditorUtility.IsDirty(prefabAssetRoot))
                return true;

            // For Prefab Variant Asset we need to also check if the instance handle is dirty
            // since this happens when the list of removed component changes
            var instanceHandle = PrefabUtility.GetPrefabInstanceHandle(prefabAssetRoot);
            if (instanceHandle != null)
                if (EditorUtility.IsDirty(instanceHandle))
                    return true;

            prefabAssetRoot.GetComponents(m_TempComponentsResults);
            foreach (var component in m_TempComponentsResults)
            {
                if (EditorUtility.IsDirty(component))
                    return true;
            }

            return false;
        }

        void CacheHasMixedBaseVariants()
        {
            if (m_HasMixedBaseVariants >= 0)
                return; // already cached

            var firstBaseVarient = PrefabUtility.GetCorrespondingObjectFromSource(assetTarget);
            if (firstBaseVarient == null)
                return;

            m_HasMixedBaseVariants = 0;
            foreach (var t in assetTargets)
            {
                var variantBase = PrefabUtility.GetCorrespondingObjectFromSource(t);
                if (variantBase != firstBaseVarient)
                {
                    m_HasMixedBaseVariants = 1;
                    break;
                }
            }
        }

        protected override bool needsApplyRevert => false;

        internal override string targetTitle
        {
            get
            {
                if (assetTargets == null || assetTargets.Length == 1 || !m_AllowMultiObjectAccess)
                    return assetTarget != null ? assetTarget.name + " (" + Styles.localizedTitleSinglePrefab + ")" : Styles.localizedTitleSinglePrefab;
                else
                    return assetTargets.Length + " " + Styles.localizedTitleMultiplePrefabs;
            }
        }

        internal override void OnHeaderControlsGUI()
        {
            if (assetTarget is DefaultAsset)
            {
                return;
            }

            var variantBase = PrefabUtility.GetCorrespondingObjectFromSource(assetTarget);
            if (variantBase != null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    CacheHasMixedBaseVariants();
                    GUILayout.Label(Styles.baseContent);
                    EditorGUI.showMixedValue = m_HasMixedBaseVariants == 1;
                    EditorGUILayout.ObjectField(variantBase, typeof(GameObject), false);
                    EditorGUI.showMixedValue = false;
                }
            }
            else
            {
                // Ensure we take up the same amount of height as regular controls
                GUILayoutUtility.GetRect(10, 10, 16, 16, EditorStyles.layerMaskField);
            }
        }

        public override void OnInspectorGUI()
        {
            if (assetTarget is DefaultAsset)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.inspectorFullWidthMargins);

            // Allow opening prefab even if file is not open for edit.
            // For things with explicit save operations (scenes, prefabs) we allow editing
            // and handle the potential version control conflict at the time when the user saves.
            bool wasEnabled = GUI.enabled;
            GUI.enabled = true;
            using (new EditorGUI.DisabledScope(assetTargets.Length > 1))
            {
                GUILayout.Space(10);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(Styles.openContent, Styles.openButtonStyle))
                {
                    // We only support opening one prefab at a time (so do not use 'targets')
                    PrefabStageUtility.OpenPrefab(AssetDatabase.GetAssetPath(assetTarget), null, PrefabStage.Mode.InIsolation, StageNavigationManager.Analytics.ChangeType.EnterViaAssetInspectorOpenButton);

                    GUIUtility.ExitGUI();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5);

                if (!hasMissingScripts && !m_SavingHasFailed)
                {
                    EditorGUILayout.HelpBox(Styles.openHelpText.text, MessageType.Info, true);
                }
            }
            GUI.enabled = wasEnabled;

            if (hasMissingScripts)
            {
                if (assetTargets.Length > 1)
                {
                    // List all assets that have missing scripts (but only if we have a multi-selection)
                    GUILayout.Space(5);
                    EditorGUILayout.HelpBox(Styles.multiSelectionMissingScriptsHelpText.text, MessageType.Warning, true);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(10);

                        using (new EditorGUILayout.VerticalScope())
                        {
                            foreach (var prefabAssetPath in m_PrefabsWithMissingScript)
                            {
                                if (GUILayout.Button(prefabAssetPath, EditorStyles.label))
                                {
                                    PrefabStageUtility.OpenPrefab(prefabAssetPath);
                                }
                            }
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(Styles.missingScriptsHelpText.text, MessageType.Warning, true);
                }
            }
            else if (m_SavingHasFailed)
            {
                EditorGUILayout.HelpBox(Styles.savingFailedHelpText.text, MessageType.Warning, true);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
