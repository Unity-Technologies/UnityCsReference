// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.AssetImporters;
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
            public static GUIContent missingScriptsHelpText = EditorGUIUtility.TrTextContent("Prefab has missing scripts. Open Prefab to fix the issue.");
            public static GUIContent missingSerializeReferenceHelpText = EditorGUIUtility.TrTextContent("Prefab has missing SerializeReference Types. Open Prefab to fix the issue. Changing the Prefab directly will cause those types to be lost.");
            public static GUIContent multiSelectionMissingScriptsHelpText = EditorGUIUtility.TrTextContent("Some of the selected Prefabs have missing scripts and needs to be fixed before editing them. Click to Open Prefab to fix the issue.");
            public static GUIContent savingFailedHelpText = EditorGUIUtility.TrTextContent("Saving has failed. Check the Console window to get more insight into what needs to be fixed on the Prefab Asset.\n\nOpen Prefab to fix the issue.");
            public static GUIContent variantOfText = EditorGUIUtility.TrTextContent("Variant Parent");
            public static string localizedTitleMultiplePrefabs = L10n.Tr("Prefab Assets");
            public static string localizedTitleSinglePrefab = L10n.Tr("Prefab Asset");
            public static GUIStyle openButtonStyle = "AC Button";
            public static readonly GUIContent hierarchyIcon = EditorGUIUtility.IconContent("UnityEditor.SceneHierarchyWindow");
            public const int kHierarchyIconWidth = 44;
        }

        int m_HasMixedBaseVariants = -1;
        double m_NextUpdate;
        List<string> m_PrefabsWithMissingScript = new List<string>();
        bool m_SavingHasFailed;
        bool m_ContainsMissingSerializeReferenceType;

        struct TrackedAsset
        {
            public GameObject asset;
            public string guid;
            public Hash128 hash;
        }

        List<Component> m_TempComponentsResults = new List<Component>();
        List<TrackedAsset> m_DirtyPrefabAssets = new List<TrackedAsset>();

        public override bool showImportedObject { get { return !hasMissingScripts; } }

        internal override bool CanOpenMultipleObjects() { return false; }
        internal override bool ShouldTryToMakeEditableOnOpen() { return false; }

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

            m_ContainsMissingSerializeReferenceType = false;
            foreach (var prefabAssetRoot in assetTargets)
            {
                if (PrefabUtility.HasInvalidComponent(prefabAssetRoot))
                {
                    m_PrefabsWithMissingScript.Add(AssetDatabase.GetAssetPath(prefabAssetRoot));
                }

                if (PrefabUtility.IsPartOfPrefabAsset(prefabAssetRoot) && PrefabUtility.HasManagedReferencesWithMissingTypes(prefabAssetRoot))
                {
                    m_ContainsMissingSerializeReferenceType = true;
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
        internal void SaveDirtyPrefabAssets(bool reloadInspectors)
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
                {
                    string currentGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(rootGameObject));
                    var changeTracking = new TrackedAsset()
                    {
                        asset = rootGameObject,
                        guid = currentGuid,
                        hash = AssetDatabase.GetSourceAssetFileHash(currentGuid)
                    };
                    m_DirtyPrefabAssets.Add(changeTracking);
                }
            }

            if (m_DirtyPrefabAssets.Count > 0)
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var trackedAsset in m_DirtyPrefabAssets)
                    {
                        bool savedSuccesfully;
                        PrefabUtility.SavePrefabAsset(trackedAsset.asset, out savedSuccesfully);
                        if (!savedSuccesfully)
                        {
                            string title = L10n.Tr("Saving Failed");
                            string message = L10n.Tr("Check the Console window to get more insight into what needs to be fixed on the Prefab Asset.\n\nYou can open Prefab Mode to fix any issues on child GameObjects");
                            EditorUtility.DisplayDialog(title, message, L10n.Tr("OK"));

                            m_SavingHasFailed = true;
                            break;
                        }
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();

                    if (reloadInspectors)
                    {
                        foreach (var trackedAsset in m_DirtyPrefabAssets)
                        {
                            if (AssetDatabase.GetSourceAssetFileHash(trackedAsset.guid) != trackedAsset.hash)
                            {
                                // We only call ForceReloadInspectors (and not ForceRebuildInspectors) to ensure local inspector state
                                // is not destroyed, such as a foldout state maintained by an editor (case 1255013).
                                // And we need to reload Prefab asset inspectors in order for the preview to be regenerated since the preview shows
                                // an instantiated Prefab. E.g disable a MeshRenderer on a Prefab Asset and the mesh should be hidden in the preview.
                                EditorUtility.ForceReloadInspectors();
                                break;
                            }
                        }
                    }
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

                if (component is Renderer)
                {
                    Renderer r = component as Renderer;
                    foreach (Material mat in r.sharedMaterials)
                    {
                        if (EditorUtility.IsDirty(mat) && AssetDatabase.IsSubAsset(mat))
                            return AssetDatabase.GetAssetPath(mat) == AssetDatabase.GetAssetPath(prefabAssetRoot);
                    }
                }
            }

            return false;
        }

        void CacheHasMixedBaseVariants()
        {
            if (m_HasMixedBaseVariants >= 0)
                return; // already cached

            var firstVariantParent = PrefabUtility.GetCorrespondingObjectFromSource(assetTarget);
            if (firstVariantParent == null)
                return;

            m_HasMixedBaseVariants = 0;
            foreach (var t in assetTargets)
            {
                var variantParent = PrefabUtility.GetCorrespondingObjectFromSource(t);
                if (variantParent != firstVariantParent)
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

        void PrefabFamilyButton()
        {
            if (EditorGUILayout.DropdownButton(GUIContent.none, FocusType.Passive, GUILayout.MaxWidth(Styles.kHierarchyIconWidth)))
            {
                if (!PrefabFamilyPopup.isOpen)
                    PopupWindow.Show(GUILayoutUtility.topLevel.GetLast(), new PrefabFamilyPopup((GameObject)assetTarget));
                GUIUtility.ExitGUI();
            }
            var rect = new Rect(GUILayoutUtility.topLevel.GetLast());
            rect.x += 6;
            EditorGUI.LabelField(rect, Styles.hierarchyIcon);
        }

        internal override void OnHeaderControlsGUI()
        {
            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(targets.Length != 1))
            {
                PrefabFamilyButton();
            }

            if (!ShouldHideOpenButton())
            {
                var assets = assetTargets;
                ShowOpenButton(assets, assetTarget != null);
            }

            var variantParent = PrefabUtility.GetCorrespondingObjectFromSource(assetTarget) as GameObject;
            if (variantParent != null)
            {
                // OnHeaderControlsGUI() is called within a BeginHorizontal() scope so to create a new line
                // we end and start a new BeginHorizontal().
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(true))
                {
                    CacheHasMixedBaseVariants();
                    EditorGUI.showMixedValue = m_HasMixedBaseVariants == 1;
                    var oldLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 90;
                    EditorGUILayout.ObjectField(Styles.variantOfText, variantParent, typeof(GameObject), false);
                    EditorGUIUtility.labelWidth = oldLabelWidth;
                    EditorGUI.showMixedValue = false;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (assetTarget is DefaultAsset)
            {
                return;
            }

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
            else if (m_ContainsMissingSerializeReferenceType)
            {
                EditorGUILayout.HelpBox(Styles.missingSerializeReferenceHelpText.text, MessageType.Warning, true);
            }
        }
    }
}
