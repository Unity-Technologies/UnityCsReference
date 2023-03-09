// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityObject = UnityEngine.Object;
using UnityEditor.Experimental;
using System.IO;

namespace UnityEditor
{
    [CustomEditor(typeof(GameObject))]
    [CanEditMultipleObjects]
    internal class GameObjectInspector : Editor
    {
        SerializedProperty m_Name;
        SerializedProperty m_IsActive;
        SerializedProperty m_Layer;
        SerializedProperty m_Tag;
        SerializedProperty m_StaticEditorFlags;
        SerializedProperty m_Icon;
        string m_GOPreviousName;

        static class Styles
        {
            public static GUIContent typelessIcon = EditorGUIUtility.IconContent("Prefab Icon");
            public static GUIContent overridesContent = EditorGUIUtility.TrTextContent("Overrides");
            public static GUIContent staticContent = EditorGUIUtility.TrTextContent("Static", "Enable the checkbox to mark this GameObject as static for all systems.\n\nDisable the checkbox to mark this GameObject as not static for all systems.\n\nUse the drop-down menu to mark as this GameObject as static or not static for individual systems.");
            public static GUIContent layerContent = EditorGUIUtility.TrTextContent("Layer", "The layer that this GameObject is in.\n\nChoose Add Layer... to edit the list of available layers.");
            public static GUIContent tagContent = EditorGUIUtility.TrTextContent("Tag", "The tag that this GameObject has.\n\nChoose Untagged to remove the current tag.\n\nChoose Add Tag... to edit the list of available tags.");
            public static GUIContent staticPreviewContent = EditorGUIUtility.TrTextContent("Static Preview", "This asset is greater than 8MB so, by default, the Asset Preview displays a static preview.\nTo view the asset interactively, click the Asset Preview.");
            
            public static float tagFieldWidth = EditorGUI.CalcPrefixLabelWidth(Styles.tagContent, EditorStyles.boldLabel);
            public static float layerFieldWidth = EditorGUI.CalcPrefixLabelWidth(Styles.layerContent, EditorStyles.boldLabel);

            public static GUIStyle staticDropdown = "StaticDropdown";
            public static GUIStyle tagPopup = new GUIStyle(EditorStyles.popup);
            public static GUIStyle layerPopup = new GUIStyle(EditorStyles.popup);
            public static GUIStyle overridesDropdown = new GUIStyle("MiniPullDown");
            public static GUIStyle prefabButtonsHorizontalLayout = new GUIStyle { fixedHeight = 17, margin = new RectOffset { top = 1, bottom = 1 } };

            public static GUIContent goTypeLabelMultiple = EditorGUIUtility.TrTextContent("Multiple");
            private static GUIContent regularPrefab = EditorGUIUtility.TrTextContent("Prefab");
            private static GUIContent disconnectedPrefab = EditorGUIUtility.TrTextContent("Prefab", "You have broken the prefab connection. Changes to the prefab will not be applied to this object before you Apply or Revert.");
            private static GUIContent modelPrefab = EditorGUIUtility.TrTextContent("Prefab");
            private static GUIContent disconnectedModelPrefab =  EditorGUIUtility.TrTextContent("Prefab", "You have broken the prefab connection. Changes to the model will not be applied to this object before you Revert.");
            private static GUIContent variantPrefab = EditorGUIUtility.TrTextContent("Prefab");
            private static GUIContent disconnectedVariantPrefab = EditorGUIUtility.TrTextContent("Prefab", "You have broken the prefab connection. Changes to the prefab will not be applied to this object before you Apply or Revert.");
            private static GUIContent missingPrefabAsset = EditorGUIUtility.TrTextContent("Prefab", "The source Prefab or Model has been deleted.");
            public static GUIContent openModel = EditorGUIUtility.TrTextContent("Open", "Open Model in external tool.");
            public static GUIContent openPrefab = EditorGUIUtility.TrTextContent("Open", "Open Prefab Asset '{0}'\nPress modifier key [Alt] to open in isolation.");
            public static GUIContent tooltipForObjectFieldForRootInPrefabContents = EditorGUIUtility.TrTextContent("", "Replacing the root Prefab instance in a Variant is not supported since it will break all overrides for existing instances of this Variant, including their positions and rotations.");
            public static GUIContent tooltipForObjectFieldForNestedPrefabs = EditorGUIUtility.TrTextContent("", "You can only replace outermost Prefab instances. Open Prefab Mode to replace a nested Prefab instance.");
            public static string selectString = L10n.Tr("Select");

            public static readonly float kIconSize = 24;
            public static readonly float column1Width = kIconSize + Styles.tagFieldWidth + 10;

            // Matrix based on two enums:
            // Columns correspond to PrefabTypeUtility.PrefabAssetType (see comments above rows).
            // Rows correspond to PrefabTypeUtility.PrefabInstanceStatus (None, Connected, Disconnected, Missing).
            // If missing, both enums will be "Missing".
            static public GUIContent[,] goTypeLabel =
            {
                // None
                { null, null, null, null },
                // Prefab
                { regularPrefab, regularPrefab, disconnectedPrefab, null },
                // Model
                { modelPrefab , modelPrefab, disconnectedModelPrefab, null},
                // Variant
                { variantPrefab, variantPrefab, disconnectedVariantPrefab, null },
                // Missing
                { null, null, null, missingPrefabAsset }
            };

            static Styles()
            {
                // Seems to be a bug in the way controls with margin internal to layout groups with padding calculate position. We'll work around it here.
                layerPopup.margin.right = 0;
                overridesDropdown.margin.right = 0;
            }
        }

        const long kMaxPreviewFileSizeInKB = 8000; // 8 MB

        class PreviewData : IDisposable
        {
            bool m_Disposed;

            public readonly PreviewRenderUtility renderUtility;
            public GameObject gameObject { get; private set; }

            public string prefabAssetPath { get; private set; }

            public Bounds renderableBounds { get; private set; }

            public bool useStaticAssetPreview { get; set; }

            public PreviewData(UnityObject targetObject, bool creatingStaticPreview = false)
            {
                renderUtility = new PreviewRenderUtility();
                renderUtility.camera.fieldOfView = 30.0f;
                if (!creatingStaticPreview)
                    useStaticAssetPreview = IsPrefabFileTooLargeForInteractivePreview(targetObject);
                if (!useStaticAssetPreview)
                    UpdateGameObject(targetObject);
            }

            public void UpdateGameObject(UnityObject targetObject)
            {
                UnityObject.DestroyImmediate(gameObject);
                gameObject = EditorUtility.InstantiateForAnimatorPreview(targetObject);
                renderUtility.AddManagedGO(gameObject);
                renderableBounds = GetRenderableBounds(gameObject);
            }

            // Very large prefabs takes too long to instantiate for the interactive preview so we
            // fall back to the static preview for such prefabs
            bool IsPrefabFileTooLargeForInteractivePreview(UnityObject prefabObject)
            {
                string prefabAssetPath = AssetDatabase.GetAssetPath(prefabObject);
                if (string.IsNullOrEmpty(prefabAssetPath))
                    return false;

                string guidString = AssetDatabase.AssetPathToGUID(prefabAssetPath);
                if (string.IsNullOrEmpty(guidString))
                    return false;

                var artifactKey = new ArtifactKey(new GUID(guidString));
                var artifactID = AssetDatabaseExperimental.LookupArtifact(artifactKey);
                // The artifactID can be invalid if we are in the middle of an AssetDatabase.Refresh.
                if (!artifactID.isValid)
                    return false;
                AssetDatabaseExperimental.GetArtifactPaths(artifactID, out var paths);
                if (paths.Length != 1)
                {
                    Array.Sort(paths);
                    int validatedPathCount = 1;
                    for (int i = 1; i < paths.Length; i++)
                    {
                        if (paths[i].EndsWith(".materialinfo") || paths[i].EndsWith(".importpathinfo") || paths[i].EndsWith(".alphapathinfo"))
                            validatedPathCount++;
                    }

                    if (validatedPathCount != paths.Length)
                    {
                        Debug.LogError("Prefabs should just have one artifact");
                        return false;
                    }
                }

                string importedPrefabPath = Path.GetFullPath(paths[0]);
                if (!System.IO.File.Exists(importedPrefabPath))
                {
                    Debug.LogError("Could not find prefab artifact on disk");
                    return false;
                }

                long length = new System.IO.FileInfo(importedPrefabPath).Length;
                long fileSizeInKB = length / 1024;

                // Keep for debugging
                //Debug.Log("Imported prefab: " + prefabAssetPath + ". File size: " + fileSizeInKB + " KB" + " (guid " + importedPrefabPath + ")");

                return fileSizeInKB > kMaxPreviewFileSizeInKB;
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;
                renderUtility.Cleanup();
                UnityObject.DestroyImmediate(gameObject);
                gameObject = null;
                m_Disposed = true;
            }
        }

        Dictionary<int, PreviewData> m_PreviewInstances = new Dictionary<int, PreviewData>();
        Dictionary<int, Texture> m_PreviewCache;
        Vector2 m_PreviewDir;
        Vector2 m_StaticPreviewLabelSize;
        Rect m_PreviewRect;

        [Flags]
        enum ButtonStates
        {
            None = 0,
            Openable = 1 << 0,
            Selectable = 1 << 1,
            CanShowOverrides = 1 << 2,
        }

        ButtonStates m_ButtonStates;
        bool m_PlayModeObjects;
        bool m_IsAsset;
        bool m_ImmutableSelf;
        bool m_IsMissingArtifact;
        bool m_IsVariantParentMissingOrCorrupted;
        bool m_IsPrefabInstanceAnyRoot;
        bool m_IsPrefabInstanceOutermostRoot;
        bool m_IsAssetRoot;
        bool m_AllOfSamePrefabType = true;
        GameObject m_AllPrefabInstanceRootsAreFromThisAsset;
        bool m_IsInstanceRootInPrefabContents;
        bool m_HasRenderableParts = true;
        GUIContent m_OpenPrefabContent;
        GUIContent m_SelectedObjectCountContent;
        GameObject m_MissingGameObject;

        public void OnEnable()
        {
            if (EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D)
                m_PreviewDir = new Vector2(0, 0);
            else
            {
                m_PreviewDir = new Vector2(120, -20);

                //Fix for FogBugz case : 1364821 Inspector Model Preview orientation is reversed when Bake Axis Conversion is enabled
                UnityObject importedObject = PrefabUtility.IsPartOfVariantPrefab(target)
                    ? PrefabUtility.GetCorrespondingObjectFromSource(target) as GameObject
                    : target;

                var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(importedObject)) as ModelImporter;
                if (importer && importer.bakeAxisConversion)
                {
                    m_PreviewDir += new Vector2(180,0);
                }
            }


            m_StaticPreviewLabelSize = new Vector2(0, 0);

            m_Name = serializedObject.FindProperty("m_Name");
            m_IsActive = serializedObject.FindProperty("m_IsActive");
            m_Layer = serializedObject.FindProperty("m_Layer");
            m_Tag = serializedObject.FindProperty("m_TagString");
            m_StaticEditorFlags = serializedObject.FindProperty("m_StaticEditorFlags");
            m_Icon = serializedObject.FindProperty("m_Icon");

            SetSelectedObjectCountLabelContent();
            CalculatePrefabStatus();
            CaculateHasRenderableParts();

            m_MissingGameObject = EditorUtility.CreateGameObjectWithHideFlags("Missing GameObject for Object Field", HideFlags.HideAndDontSave);
            DestroyImmediate(m_MissingGameObject);

            m_PreviewCache = new Dictionary<int, Texture>();

            if (EditorUtility.IsPersistent(target))
                AssetEvents.assetsChangedOnHDD += OnAssetsChangedOnHDD;
        }

        void CalculatePrefabStatus()
        {
            m_PlayModeObjects = false;
            m_IsAsset = false;
            m_ImmutableSelf = false;
            m_IsMissingArtifact = false;
            m_ButtonStates = ButtonStates.Openable | ButtonStates.Selectable | ButtonStates.CanShowOverrides;
            m_IsVariantParentMissingOrCorrupted = false;
            m_IsPrefabInstanceAnyRoot = true;
            m_IsPrefabInstanceOutermostRoot = true;
            m_AllOfSamePrefabType = true;
            m_IsAssetRoot = false;
            PrefabAssetType firstType = PrefabUtility.GetPrefabAssetType(targets[0]);
            PrefabInstanceStatus firstStatus = PrefabUtility.GetPrefabInstanceStatus(targets[0]);
            m_OpenPrefabContent = null;
            m_AllPrefabInstanceRootsAreFromThisAsset = PrefabUtility.GetOriginalSourceOrVariantRoot(targets[0]);
            m_IsInstanceRootInPrefabContents = false;

            foreach (var o in targets)
            {
                var go = (GameObject)o;
                if (m_AllOfSamePrefabType)
                {
                    PrefabAssetType type = PrefabUtility.GetPrefabAssetType(go);
                    PrefabInstanceStatus status = PrefabUtility.GetPrefabInstanceStatus(go);
                    if (type != firstType || status != firstStatus)
                        m_AllOfSamePrefabType = false;
                }

                if (Application.IsPlaying(go))
                    m_PlayModeObjects = true;
                if (m_IsPrefabInstanceAnyRoot)
                {
                    if (!PrefabUtility.IsAnyPrefabInstanceRoot(go))
                    {
                        m_IsPrefabInstanceAnyRoot = false; // Conservative is false if any is false
                    }
                }
                if (m_IsPrefabInstanceOutermostRoot)
                {
                    if (!m_IsPrefabInstanceAnyRoot || !PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                    {
                        m_IsPrefabInstanceOutermostRoot = false; // Conservative is false if any is false
                    }
                }

                if (m_AllPrefabInstanceRootsAreFromThisAsset != null && PrefabUtility.GetOriginalSourceOrVariantRoot(go) != m_AllPrefabInstanceRootsAreFromThisAsset)
                    m_AllPrefabInstanceRootsAreFromThisAsset = null;

                if (m_IsPrefabInstanceOutermostRoot && targets.Length == 1)
                {
                    if (PrefabStageUtility.IsGameObjectThePrefabRootInAnyPrefabStage(go))
                        m_IsInstanceRootInPrefabContents = true; // Replacing base instance in a Variant will break all overrides of existing instances of the Variant
                }

                if (PrefabUtility.IsPartOfPrefabAsset(go))
                {
                    m_IsAsset = true; // Conservative is true if any is true
                    if (go.transform.parent == null)
                        m_IsAssetRoot = true;
                }

                if (m_IsAsset)
                {
                    if (PrefabUtility.IsPartOfImmutablePrefab(go))
                    {
                        m_ImmutableSelf = true; // Conservative is true if any is true
                    }
                }

                if (PrefabUtility.IsPrefabAssetMissing(go))
                {
                    m_IsMissingArtifact = true;

                    m_ButtonStates &= ~ButtonStates.CanShowOverrides;

                    var brokenAsset = GetMainAssetFromBrokenPrefabInstanceRoot(go) as BrokenPrefabAsset;
                    if(brokenAsset == null)
                        m_ButtonStates = ButtonStates.None;
                    else
                    {
                        if (brokenAsset.isVariant)
                            m_IsVariantParentMissingOrCorrupted = true;
                        else if (!brokenAsset.isPrefabFileValid)
                            m_ButtonStates &= ~ButtonStates.Openable;
                    }
                }
            }
        }

        internal void SetSelectedObjectCountLabelContent()
        {
            m_SelectedObjectCountContent = new GUIContent($"({targets.Length})", $"{targets.Length} Objects Selected");
        }

        internal void OnDisable()
        {
            foreach (var previewData in m_PreviewInstances.Values)
                previewData.Dispose();
            ClearPreviewCache();
            m_PreviewCache = null;

            if (string.IsNullOrEmpty(m_Name.stringValue) && !(string.IsNullOrEmpty(m_GOPreviousName)))
            {
                Debug.LogWarning("A GameObject name cannot be set to an empty string.");
                m_Name.stringValue = m_GOPreviousName;
                serializedObject.ApplyModifiedProperties();
            }

            AssetEvents.assetsChangedOnHDD -= OnAssetsChangedOnHDD;
        }

        void OnAssetsChangedOnHDD(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var importedAssetPath in importedAssets)
                ReloadPreviewInstance(importedAssetPath);
        }

        internal override void OnForceReloadInspector()
        {
            base.OnForceReloadInspector();
            CalculatePrefabStatus();
            CaculateHasRenderableParts();
            ReloadPreviewInstances();
            SetSelectedObjectCountLabelContent();
        }

        void ClearPreviewCache()
        {
            if (m_PreviewCache == null)
            {
                return;
            }

            foreach (var texture in m_PreviewCache.Values)
            {
                DestroyImmediate(texture);
            }
            m_PreviewCache.Clear();
        }

        private static StaticEditorFlags[] s_StaticEditorFlagValues;

        private static bool ShowMixedStaticEditorFlags(StaticEditorFlags mask)
        {
            uint countedBits = 0;
            uint numFlags = 0;

            if (s_StaticEditorFlagValues == null)
            {
                var values = Enum.GetValues(typeof(StaticEditorFlags));
                s_StaticEditorFlagValues = new StaticEditorFlags[values.Length];
                for (var i = 0; i < values.Length; ++i)
                    s_StaticEditorFlagValues[i] = (StaticEditorFlags)values.GetValue(i);
            }

            foreach (var i in s_StaticEditorFlagValues)
            {
                numFlags++;
                if ((mask & i) > 0)
                    countedBits++;
            }

            //If we have more then one selected... but it is not all the flags
            //All indicates 'everything' which means it should be a tick!
            return countedBits > 0 && countedBits != numFlags;
        }

        public override bool UseDefaultMargins()
        {
            return false;
        }

        protected override void OnHeaderGUI()
        {
            bool enabledTemp = GUI.enabled;
            GUI.enabled = true;
            EditorGUILayout.BeginVertical(EditorStyles.inspectorBig);
            GUI.enabled = enabledTemp;
            DrawInspector();
            EditorGUILayout.EndVertical();
        }

        public override void OnInspectorGUI() {}

        internal bool DrawInspector()
        {
            serializedObject.Update();

            GameObject go = target as GameObject;

            // Don't let icon be null as it will create null reference exceptions.
            Texture2D icon = (Texture2D)(Styles.typelessIcon.image);

            // Leave iconContent to be default if multiple objects not the same type.
            if (m_AllOfSamePrefabType)
            {
                icon = PrefabUtility.GetIconForGameObject(go);
            }

            // Can't do this in OnEnable since it will cause Styles static initializer to be called and
            // access properties on EditorStyles static class before that one has been initialized.
            if (m_OpenPrefabContent == null)
            {
                if (targets.Length == 1)
                {
                    GameObject originalSourceOrVariant = PrefabUtility.GetOriginalSourceOrVariantRoot((GameObject)target);
                    if (originalSourceOrVariant != null)
                        m_OpenPrefabContent = new GUIContent(
                            Styles.openPrefab.text,
                            string.Format(Styles.openPrefab.tooltip, originalSourceOrVariant.name));
                }
                if (m_OpenPrefabContent == null)
                    m_OpenPrefabContent = new GUIContent(Styles.openPrefab.text);
            }

            EditorGUILayout.BeginHorizontal();
            Vector2 dropDownSize = EditorGUI.GetObjectIconDropDownSize(Styles.kIconSize, Styles.kIconSize);
            var iconRect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(false));
            iconRect.width = dropDownSize.x;
            iconRect.height = dropDownSize.y;
            EditorGUI.ObjectIconDropDown(iconRect, targets, true, icon, m_Icon);
            DrawPostIconContent(iconRect);

            using (new EditorGUI.DisabledScope(m_ImmutableSelf))
            {
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.BeginHorizontal(GUILayout.Width(Styles.column1Width));
                        {
                            GUILayout.FlexibleSpace();

                            // IsActive
                            EditorGUI.PropertyField(
                                GUILayoutUtility.GetRect(EditorStyles.toggle.padding.left, EditorGUIUtility.singleLineHeight, EditorStyles.toggle,
                                    GUILayout.ExpandWidth(false)), m_IsActive, GUIContent.none);
                        }
                        EditorGUILayout.EndHorizontal();

                        // Disable the name field of root GO in prefab asset
                        using (new EditorGUI.DisabledScope(m_IsAsset && m_IsAssetRoot))
                        {
                            // Name
                            // Resets the game object name when attempted to set it to an empty string
                            if (string.IsNullOrEmpty(m_Name.stringValue) && !(string.IsNullOrEmpty(m_GOPreviousName)))
                            {
                                Debug.LogWarning("A GameObject name cannot be set to an empty string.");
                                m_Name.stringValue = m_GOPreviousName;
                            }
                            else
                                m_GOPreviousName = m_Name.stringValue;

                            EditorGUILayout.DelayedTextField(m_Name, GUIContent.none, EditorStyles.boldTextField);
                        }

                        if (targets.Length > 1)
                        {
                            var maxW = GUI.skin.label.CalcSize(m_SelectedObjectCountContent).x;
                            GUILayout.Label(m_SelectedObjectCountContent, GUILayout.MaxWidth(maxW));
                        }

                        // Static flags toggle
                        DoStaticToggleField(go);

                        // Static flags dropdown
                        DoStaticFlagsDropDown(go);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(EditorGUI.kControlVerticalSpacing);

                    EditorGUILayout.BeginHorizontal();
                    {
                        // Tag
                        GUILayout.Space(Styles.column1Width - Styles.tagFieldWidth);
                        DoTagsField(go);

                        EditorGUILayout.Space(EditorGUI.kDefaultSpacing, false);

                        // Layer
                        DoLayerField(go);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            // Prefab Toolbar
            if (EditorGUIUtility.comparisonViewMode == EditorGUIUtility.ComparisonViewMode.None)
            {
                EditorGUILayout.Space(EditorGUI.kControlVerticalSpacing);

                DoPrefabButtons();
            }

            serializedObject.ApplyModifiedProperties();

            return true;
        }

        void DoPrefixLabel(GUIContent label, GUIStyle style)
        {
            var rect = GUILayoutUtility.GetRect(label, style, GUILayout.ExpandWidth(false));

            rect.height = Math.Max(EditorGUI.kSingleLineHeight, rect.height);

            GUI.Label(rect, label, style);
        }

        void IndentToColumn1()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(Styles.column1Width));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        bool HandleDragPrefabAssetOverObjectFieldPopupMenu(Rect rect)
        {
            var evt = Event.current;
            bool perform = evt.type == EventType.DragPerform;
            if (rect.Contains(evt.mousePosition) && (perform ||evt.type == EventType.DragUpdated))
            {
                DragAndDropVisualMode visualMode;
                if (PrefabReplaceUtility.GetDragVisualModeAndShowMenuWithReplaceMenuItemsWhenNeeded((GameObject)target, true, perform, false, false, out visualMode))
                {
                    DragAndDrop.visualMode = visualMode;
                    return perform;
                }
            }
            return false;
        }

        void PrefabObjectField()
        {
            // Change Prefab asset for instance
            if (m_IsPrefabInstanceAnyRoot)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.showMixedValue = m_AllPrefabInstanceRootsAreFromThisAsset == null && !m_IsMissingArtifact;
                using (var scope = new EditorGUI.ChangeCheckScope())
                {
                    GameObject newAsset;
                    bool disabled = !m_IsPrefabInstanceOutermostRoot || m_IsInstanceRootInPrefabContents;
                    using (new EditorGUI.DisabledScope(disabled))
                    {
                        Rect r = EditorGUILayout.GetControlRect(false, EditorGUI.kSingleLineHeight);
                        if (HandleDragPrefabAssetOverObjectFieldPopupMenu(r))
                        {
                            GUIUtility.ExitGUI(); // handled by popup menu
                        }
                        if (m_IsMissingArtifact)
                            newAsset = EditorGUI.ObjectField(r, m_MissingGameObject, typeof(GameObject), false) as GameObject;
                        else
                            newAsset = EditorGUI.ObjectField(r, m_AllPrefabInstanceRootsAreFromThisAsset, typeof(GameObject), false) as GameObject;
                    }

                    if (disabled)
                    {
                        // Tooltips (should have tooltips that matches each of the conditions in the above DisabledScope)
                        var rect = EditorGUILayout.s_LastRect;
                        if (rect.Contains(Event.current.mousePosition))
                        {
                            if (m_IsInstanceRootInPrefabContents)
                                GUI.Label(rect, Styles.tooltipForObjectFieldForRootInPrefabContents);
                            else if (!m_IsPrefabInstanceOutermostRoot)
                                GUI.Label(rect, Styles.tooltipForObjectFieldForNestedPrefabs);
                        }
                    }


                    if (scope.changed)
                    {
                        if (newAsset != null)
                        {
                            string errorMsg = string.Empty;
                            try
                            {
                                PrefabUtility.ThrowIfInvalidAssetForReplacePrefabInstance(newAsset, InteractionMode.UserAction);
                                foreach (var t in targets)
                                    PrefabUtility.ThrowIfInvalidArgumentsForReplacePrefabInstance((GameObject)t, newAsset, false, InteractionMode.UserAction);
                            }
                            catch (InvalidOperationException e)
                            {
                                errorMsg = e.Message;
                            }

                            if (string.IsNullOrEmpty(errorMsg))
                            {
                                // targets are in reverse order from the Hierarchy selection so we reverse it here so we get the same result as repalcing from the Hierarchy
                                var replaceTargets = new List<UnityObject>(targets);
                                replaceTargets.Reverse();
                                if (targets.Length > 1)
                                    PrefabUtility.ReplacePrefabAssetOfPrefabInstances(replaceTargets.Select(e => (GameObject)e).ToArray(), newAsset, InteractionMode.UserAction);
                                else
                                    PrefabUtility.ReplacePrefabAssetOfPrefabInstance((GameObject)target, newAsset, InteractionMode.UserAction);
                                CalculatePrefabStatus(); // Updates the cached m_FirstPrefabInstanceOutermostRootAsset to the newly selected Prefab
                                GUIUtility.ExitGUI();
                            }
                            else
                            {
                                var gameObjectsToPing = new List<GameObject>();
                                var assetGameObjectsWithInvalidComponents = PrefabUtility.FindGameObjectsWithInvalidComponent(newAsset);
                                var instanceGameObjectsWithInvalidComponents = PrefabUtility.FindGameObjectsWithInvalidComponent((GameObject)target);

                                if (assetGameObjectsWithInvalidComponents.Count > 0)
                                    gameObjectsToPing.Add(assetGameObjectsWithInvalidComponents[0]);
                                if (instanceGameObjectsWithInvalidComponents.Count > 0)
                                    gameObjectsToPing.Add(instanceGameObjectsWithInvalidComponents[0]);

                                Debug.LogWarning(errorMsg, gameObjectsToPing.Count > 0 ? gameObjectsToPing[0] : null);
                                foreach(var go in gameObjectsToPing)
                                    EditorGUIUtility.PingObject(go);
                            }
                        }
                        else
                        {
                            // 'newAsset' is null: Replacing with null Asset should just be ignored (no need to show a dialog)
                        }
                    }
                }
                EditorGUI.showMixedValue = false;
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DoPrefabButtons()
        {
            if (!m_IsPrefabInstanceAnyRoot || m_IsAsset)
                return;

            // Vertical spacing to group Prefab related UI from the GameObject's UI
            EditorGUILayout.Space(12);

            using (new EditorGUI.DisabledScope(m_PlayModeObjects))
            {
                // Prefab label and asset field
                EditorGUILayout.BeginHorizontal();
                PrefabAssetType singlePrefabType = PrefabUtility.GetPrefabAssetType(target);
                PrefabInstanceStatus singleInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(target);
                GUIContent prefixLabel;
                if (targets.Length > 1)
                {
                    prefixLabel = Styles.goTypeLabelMultiple;
                }
                else
                {
                    prefixLabel = Styles.goTypeLabel[(int)singlePrefabType, (int)singleInstanceStatus];
                }

                if (prefixLabel != null)
                {
                    EditorGUILayout.BeginHorizontal(GUILayout.Width(Styles.column1Width));
                    GUILayout.FlexibleSpace();
                    DoPrefixLabel(prefixLabel, EditorStyles.label);
                    EditorGUILayout.EndHorizontal();
                }
                PrefabObjectField();
                EditorGUILayout.EndHorizontal();

                if (m_ButtonStates != ButtonStates.None)
                {
                    EditorGUILayout.Space(EditorGUI.kControlVerticalSpacing);
                    EditorGUILayout.BeginHorizontal(Styles.prefabButtonsHorizontalLayout);
                    IndentToColumn1();

                    // Overrides Popup. Reserve space regardless of whether the button is there or not to avoid jumps in button sizes.
                    Rect rect = GUILayoutUtility.GetRect(Styles.overridesContent, Styles.overridesDropdown);
                    if (m_ButtonStates.HasFlag(ButtonStates.CanShowOverrides) && m_IsPrefabInstanceOutermostRoot)
                    {
                        if (EditorGUI.DropdownButton(rect, Styles.overridesContent, FocusType.Passive))
                        {
                            if (targets.Length > 1)
                                PopupWindow.Show(rect, new PrefabOverridesWindow(targets.Select(e => (GameObject)e).ToArray()));
                            else
                                PopupWindow.Show(rect, new PrefabOverridesWindow((GameObject)target));
                            GUIUtility.ExitGUI();
                        }
                    }

                    // Spacing between buttons
                    GUILayoutUtility.GetRect(20, 6, GUILayout.MaxWidth(30), GUILayout.MinWidth(5));

                    // Select prefab
                    if (GUILayout.Button(Styles.selectString, EditorStyles.miniButton))
                    {
                        HashSet<UnityObject> selectedAssets = new HashSet<UnityObject>();
                        for (int i = 0; i < targets.Length; i++)
                        {
                            GameObject targetGo = targets[i] as GameObject;
                            GameObject prefabGo = PrefabUtility.GetOriginalSourceOrVariantRoot(targetGo);
                            if (prefabGo != null)
                            {
                                // Because of legacy prefab references we have to have this extra step
                                // to make sure we ping the prefab asset correctly.
                                // Reason is that scene files created prior to making prefabs CopyAssets
                                // will reference prefabs as if they are serialized assets. Those references
                                // works fine but we are not able to ping objects loaded directly from the asset
                                // file, so we have to make sure we ping the metadata version of the prefab.
                                var assetPath = AssetDatabase.GetAssetPath(prefabGo);
                                selectedAssets.Add((GameObject)AssetDatabase.LoadMainAssetAtPath(assetPath));
                            }
                            else
                            {
                                UnityObject mainAsset = GetMainAssetFromBrokenPrefabInstanceRoot(targetGo);
                                if (mainAsset != null)
                                    selectedAssets.Add(mainAsset);
                            }
                        }

                        Selection.objects = selectedAssets.ToArray();
                        if (Selection.objects.Length != 0)
                            EditorGUIUtility.PingObject(Selection.activeObject);
                    }

                    // Open Prefab
                    using (new EditorGUI.DisabledScope(targets.Length > 1 || !m_ButtonStates.HasFlag(ButtonStates.Openable)))
                    {
                        if (singlePrefabType == PrefabAssetType.Model)
                        {
                            // Open Model Prefab
                            if (GUILayout.Button(Styles.openModel, EditorStyles.miniButton))
                            {
                                GameObject asset = PrefabUtility.GetOriginalSourceOrVariantRoot(target);
                                AssetDatabase.OpenAsset(asset);
                                GUIUtility.ExitGUI();
                            }
                        }
                        else
                        {
                            // Open non-Model Prefab
                            if (GUILayout.Button(m_OpenPrefabContent, EditorStyles.miniButton))
                            {
                                var prefabStageMode = PrefabStageUtility.GetPrefabStageModeFromModifierKeys();
                                UnityObject asset = null;
                                if (!m_IsVariantParentMissingOrCorrupted)
                                    asset = PrefabUtility.GetOriginalSourceOrVariantRoot(target);
                                else
                                    asset = GetMainAssetFromBrokenPrefabInstanceRoot(target as GameObject);
                                PrefabStageUtility.OpenPrefab(AssetDatabase.GetAssetPath(asset), (GameObject)target, prefabStageMode, StageNavigationManager.Analytics.ChangeType.EnterViaInstanceInspectorOpenButton);
                                GUIUtility.ExitGUI();
                            }
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private void DoLayerField(GameObject go)
        {
            EditorGUIUtility.labelWidth = Styles.layerFieldWidth;
            Rect layerRect = GUILayoutUtility.GetRect(GUIContent.none, Styles.layerPopup);
            EditorGUI.BeginProperty(layerRect, GUIContent.none, m_Layer);
            EditorGUI.BeginChangeCheck();
            int layer = EditorGUI.LayerField(layerRect, Styles.layerContent, go.layer, Styles.layerPopup);
            if (EditorGUI.EndChangeCheck())
            {
                GameObjectUtility.ShouldIncludeChildren includeChildren = GameObjectUtility.DisplayUpdateChildrenDialogIfNeeded(targets.OfType<GameObject>(),
                    L10n.Tr("Change Layer"), string.Format(L10n.Tr("Do you want to set layer to {0} for all child objects as well?"), InternalEditorUtility.GetLayerName(layer)));
                if (includeChildren != GameObjectUtility.ShouldIncludeChildren.Cancel)
                {
                    m_Layer.intValue = layer;
                    SetLayer(layer, includeChildren == GameObjectUtility.ShouldIncludeChildren.IncludeChildren);
                }
                // Displaying the dialog to ask the user whether to update children nukes the gui state
                EditorGUIUtility.ExitGUI();
            }
            EditorGUI.EndProperty();
        }

        private void DoTagsField(GameObject go)
        {
            string tagName = go.tag;
            EditorGUIUtility.labelWidth = Styles.tagFieldWidth;
            Rect tagRect = GUILayoutUtility.GetRect(GUIContent.none, Styles.tagPopup);
            EditorGUI.BeginProperty(tagRect, GUIContent.none, m_Tag);
            EditorGUI.BeginChangeCheck();
            string tag = EditorGUI.TagField(tagRect, Styles.tagContent, tagName);
            if (EditorGUI.EndChangeCheck())
            {
                m_Tag.stringValue = tag;
                Undo.RecordObjects(targets, "Change Tag of " + targetTitle);
                foreach (UnityObject obj in targets)
                    (obj as GameObject).tag = tag;
            }
            EditorGUI.EndProperty();
        }

        private void DoStaticFlagsDropDown(GameObject go)
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, Styles.staticDropdown, GUILayout.ExpandWidth(false));

            rect.height = Math.Max(EditorGUIUtility.singleLineHeight, rect.height);

            bool toggled = EditorGUILayout.DropdownButton(GUIContent.none, FocusType.Keyboard, Styles.staticDropdown);
            if (toggled)
            {
                rect = GUILayoutUtility.topLevel.GetLast();
                // We do not pass the serializedProperty directly, as its parent serializedObject
                // can get destroyed when references to parent windows are lost, thus we use
                // the target object & the path to reconstruct the property inside the window itself
                PopupWindow.Show(rect, new StaticFieldDropdown(m_StaticEditorFlags.serializedObject.targetObjects, m_StaticEditorFlags.propertyPath));
                GUIUtility.ExitGUI();
            }
        }

        private void DoStaticToggleField(GameObject go)
        {
            var staticRect = GUILayoutUtility.GetRect(Styles.staticContent, EditorStyles.toggle, GUILayout.ExpandWidth(false));

            staticRect.height = Math.Max(EditorGUIUtility.singleLineHeight, staticRect.height);
            staticRect.width += 3; //offset for the bold text when displaying prefab instances.
            EditorGUI.BeginProperty(staticRect, GUIContent.none, m_StaticEditorFlags);
            EditorGUI.BeginChangeCheck();
            var toggleRect = staticRect;
            EditorGUI.showMixedValue |= ShowMixedStaticEditorFlags((StaticEditorFlags)m_StaticEditorFlags.intValue);
            // Ignore mouse clicks that are not with the primary (left) mouse button so those can be grabbed by other things later.
            Event evt = Event.current;
            EventType origType = evt.type;
            bool nonLeftClick = (evt.type == EventType.MouseDown && evt.button != 0);
            if (nonLeftClick)
                evt.type = EventType.Ignore;
            var toggled = EditorGUI.ToggleLeft(toggleRect, Styles.staticContent, go.isStatic);
            if (nonLeftClick)
                evt.type = origType;
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                SceneModeUtility.SetStaticFlags(targets, int.MaxValue, toggled);
                serializedObject.SetIsDifferentCacheDirty();

                // Displaying the dialog to ask the user whether to update children nukes the gui state (case 962453)
                EditorGUIUtility.ExitGUI();
            }
            EditorGUI.EndProperty();
        }

        UnityObject[] GetObjects(bool includeChildren)
        {
            return SceneModeUtility.GetObjects(targets, includeChildren);
        }

        void SetLayer(int layer, bool includeChildren)
        {
            UnityObject[] objects = GetObjects(includeChildren);
            Undo.RecordObjects(objects, "Change Layer of " + targetTitle);
            foreach (var o in objects)
            {
                var go = (GameObject)o;
                go.layer = layer;
            }
        }

        void ReloadPreviewInstance(string prefabAssetPath)
        {
            foreach (var pair in m_PreviewInstances)
            {
                var index = pair.Key;
                if (index > targets.Length)
                    continue;

                var previewData = pair.Value;
                if (previewData.prefabAssetPath == prefabAssetPath)
                {
                    previewData.UpdateGameObject(targets[index]);
                    ClearPreviewCache();
                    return;
                }
            }
        }

        public override void ReloadPreviewInstances()
        {
            foreach (var pair in m_PreviewInstances)
            {
                var index = pair.Key;
                if (index > targets.Length)
                    continue;

                var previewData = pair.Value;
                if (!previewData.useStaticAssetPreview)
                    previewData.UpdateGameObject(targets[index]);
            }
            ClearPreviewCache();
        }

        PreviewData GetPreviewData(bool creatingStaticPreview = false)
        {
            PreviewData previewData;
            if (!m_PreviewInstances.TryGetValue(referenceTargetIndex, out previewData))
            {
                previewData = new PreviewData(target, creatingStaticPreview);
                m_PreviewInstances.Add(referenceTargetIndex, previewData);
            }
            if (!previewData.gameObject && !previewData.useStaticAssetPreview)
                ReloadPreviewInstances();
            return previewData;
        }

        static readonly List<Renderer> s_RendererComponentsList = new List<Renderer>();

        static bool IsRendererUsableForPreview(Renderer r)
        {
            switch (r)
            {
                case MeshRenderer mr:
                    mr.gameObject.TryGetComponent<MeshFilter>(out var mf);
                    if (mf == null || mf.sharedMesh == null)
                        return false;
                    break;
                case SkinnedMeshRenderer skin:
                    if (skin.sharedMesh == null)
                        return false;
                    break;
                case SpriteRenderer sprite:
                    if (sprite.sprite == null)
                        return false;
                    break;
                case BillboardRenderer billboard:
                    if (billboard.billboard == null || billboard.sharedMaterial == null)
                        return false;
                    break;
            }
            return true;
        }

        void CaculateHasRenderableParts()
        {
            m_HasRenderableParts = HasRenderableParts(target as GameObject);
        }

        public static bool HasRenderableParts(GameObject go)
        {
            if (!go)
                return false;
            go.GetComponentsInChildren(s_RendererComponentsList);
            return s_RendererComponentsList.Any(IsRendererUsableForPreview);
        }

        public static Bounds GetRenderableBounds(GameObject go)
        {
            var b = new Bounds();
            if (!go)
                return b;
            go.GetComponentsInChildren(s_RendererComponentsList);
            foreach (var r in s_RendererComponentsList)
            {
                if (!IsRendererUsableForPreview(r))
                    continue;
                if (b.extents == Vector3.zero)
                    b = r.bounds;
                else
                    b.Encapsulate(r.bounds);
            }

            return b;
        }

        private static float GetRenderableCenterRecurse(ref Vector3 center, GameObject go, int depth, int minDepth, int maxDepth)
        {
            if (depth > maxDepth)
                return 0;

            float ret = 0;

            if (depth > minDepth)
            {
                // Do we have a mesh?
                var renderer = go.GetComponent<MeshRenderer>();
                var filter = go.GetComponent<MeshFilter>();
                var skin = go.GetComponent<SkinnedMeshRenderer>();
                var sprite = go.GetComponent<SpriteRenderer>();
                var billboard = go.GetComponent<BillboardRenderer>();

                if (renderer == null && filter == null && skin == null && sprite == null && billboard == null)
                {
                    ret = 1;
                    center = center + go.transform.position;
                }
                else if (renderer != null && filter != null)
                {
                    // case 542145, epsilon is too small. Accept up to 1 centimeter before discarding this model.
                    if (Vector3.Distance(renderer.bounds.center, go.transform.position) < 0.01F)
                    {
                        ret = 1;
                        center = center + go.transform.position;
                    }
                }
                else if (skin != null)
                {
                    // case 542145, epsilon is too small. Accept up to 1 centimeter before discarding this model.
                    if (Vector3.Distance(skin.bounds.center, go.transform.position) < 0.01F)
                    {
                        ret = 1;
                        center = center + go.transform.position;
                    }
                }
                else if (sprite != null)
                {
                    if (Vector3.Distance(sprite.bounds.center, go.transform.position) < 0.01F)
                    {
                        ret = 1;
                        center = center + go.transform.position;
                    }
                }
                else if (billboard != null)
                {
                    if (Vector3.Distance(billboard.bounds.center, go.transform.position) < 0.01F)
                    {
                        ret = 1;
                        center = center + go.transform.position;
                    }
                }
            }

            depth++;
            // Recurse into children
            foreach (Transform t in go.transform)
            {
                ret += GetRenderableCenterRecurse(ref center, t.gameObject, depth, minDepth, maxDepth);
            }

            return ret;
        }

        public static Vector3 GetRenderableCenterRecurse(GameObject go, int minDepth, int maxDepth)
        {
            Vector3 center = Vector3.zero;

            float sum = GetRenderableCenterRecurse(ref center, go, 0, minDepth, maxDepth);

            if (sum > 0)
            {
                center = center / sum;
            }
            else
            {
                center = go.transform.position;
            }

            return center;
        }

        public override bool HasPreviewGUI()
        {
            if (!EditorUtility.IsPersistent(target))
                return false;

            return HasStaticPreview();
        }

        private bool HasStaticPreview()
        {
            if (targets.Length > 1)
                return true;

            if (target == null)
                return false;

            return m_HasRenderableParts;
        }

        public override void OnPreviewSettings()
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
                return;
            GUI.enabled = true;
        }

        private void DoRenderPreview(PreviewData previewData)
        {
            var bounds = previewData.renderableBounds;
            float halfSize = Mathf.Max(bounds.extents.magnitude, 0.0001f);
            float distance = halfSize * 3.8f;

            Quaternion rot = Quaternion.Euler(-m_PreviewDir.y, -m_PreviewDir.x, 0);
            Vector3 pos = bounds.center - rot * (Vector3.forward * distance);

            previewData.renderUtility.camera.transform.position = pos;
            previewData.renderUtility.camera.transform.rotation = rot;
            previewData.renderUtility.camera.nearClipPlane = distance - halfSize * 1.1f;
            previewData.renderUtility.camera.farClipPlane = distance + halfSize * 1.1f;

            previewData.renderUtility.lights[0].intensity = .7f;
            previewData.renderUtility.lights[0].transform.rotation = rot * Quaternion.Euler(40f, 40f, 0);
            previewData.renderUtility.lights[1].intensity = .7f;
            previewData.renderUtility.lights[1].transform.rotation = rot * Quaternion.Euler(340, 218, 177);

            previewData.renderUtility.ambientColor = new Color(.1f, .1f, .1f, 0);

            previewData.renderUtility.Render(true);
        }

        public override Texture2D RenderStaticPreview(string assetPath, UnityObject[] subAssets, int width, int height)
        {
            if (!HasStaticPreview() || !ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                return null;
            }

            var previewData = GetPreviewData(true);

            previewData.renderUtility.BeginStaticPreview(new Rect(0, 0, width, height));

            DoRenderPreview(previewData);

            return previewData.renderUtility.EndStaticPreview();
        }

        void DrawAssetPreviewTexture(Rect rect)
        {
            Texture2D icon = AssetPreview.GetAssetPreview(target);
            if (!icon)
            {
                // We have a static preview it just hasn't been loaded yet. Repaint until we have it loaded.
                if (AssetPreview.IsLoadingAssetPreview(target.GetInstanceID()))
                    Repaint();
            }
            else
            {
                var scaleMode = ScaleMode.ScaleToFit;
                GUI.DrawTexture(rect, icon, scaleMode);

                if (m_StaticPreviewLabelSize.x == 0.0f && m_StaticPreviewLabelSize.y == 0.0f)
                    m_StaticPreviewLabelSize = GUI.skin.label.CalcSize(Styles.staticPreviewContent);

                // Only render overlay text if there is space enough
                if (rect.width >= m_StaticPreviewLabelSize.x && rect.height >= m_StaticPreviewLabelSize.y + GUI.skin.label.padding.vertical)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        GUI.Label(new Rect(rect.x, rect.yMax - (m_StaticPreviewLabelSize.y + GUI.skin.label.padding.vertical), rect.width, m_StaticPreviewLabelSize.y), Styles.staticPreviewContent, EditorStyles.centeredGreyMiniLabel);
                    }
                }
            }
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            var previewData = GetPreviewData();

            if (previewData.useStaticAssetPreview && GUI.Button(r, GUIContent.none))
            {
                previewData.useStaticAssetPreview = false;
                previewData.UpdateGameObject(target);
            }

            if (previewData.useStaticAssetPreview || !ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                if (Event.current.type == EventType.Repaint)
                    DrawAssetPreviewTexture(r);
                return;
            }

            var direction = PreviewGUI.Drag2D(m_PreviewDir, r);
            if (direction != m_PreviewDir)
            {
                // None of the preview are valid since the camera position has changed.
                ClearPreviewCache();
                m_PreviewDir = direction;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            if (m_PreviewRect != r)
            {
                ClearPreviewCache();
                m_PreviewRect = r;
            }

            var previewUtility = GetPreviewData().renderUtility;
            Texture previewTexture;
            if (m_PreviewCache.TryGetValue(referenceTargetIndex, out previewTexture))
            {
                PreviewRenderUtility.DrawPreview(r, previewTexture);
            }
            else
            {
                previewUtility.BeginPreview(r, background);
                DoRenderPreview(previewData);
                previewUtility.EndAndDrawPreview(r);
                var copy = new RenderTexture(previewUtility.renderTexture);
                var previous = RenderTexture.active;
                Graphics.Blit(previewUtility.renderTexture, copy);
                RenderTexture.active = previous;
                m_PreviewCache.Add(referenceTargetIndex, copy);
            }
        }

        // Handle dragging in scene view
        public GameObject m_DragObject;
        static bool s_ShouldClearSelection;
        internal static bool s_CyclicNestingDetected;
        static bool s_PlaceObject;
        static Vector3 s_PlaceObjectPoint;
        static Vector3 s_PlaceObjectNormal;
        public void OnSceneDrag(SceneView sceneView, int index)
        {
            Event evt = Event.current;
            OnSceneDragInternal(sceneView, index, evt.type, evt.mousePosition, evt.alt);
        }

        static Scene GetDestinationSceneForNewGameObjectsForSceneView(SceneView sceneView)
        {
            if (sceneView.customParentForNewGameObjects != null)
                return sceneView.customParentForNewGameObjects.gameObject.scene;

            if (sceneView.customScene.IsValid())
                return sceneView.customScene;

            return SceneManager.GetActiveScene();
        }

        internal void OnSceneDragInternal(SceneView sceneView, int index, EventType type, Vector2 mousePosition, bool alt)
        {
            GameObject go = target as GameObject;
            if (!PrefabUtility.IsPartOfPrefabAsset(go))
                return;

            var prefabAssetRoot = go.transform.root.gameObject;

            switch (type)
            {
                case EventType.DragUpdated:

                    if (m_DragObject == null)
                    {
                        // While dragging the instantiated prefab we do not want to record undo for this object
                        // this will cause a remerge of the instance since changes are undone while dragging.
                        // The DrivenRectTransformTracker by default records Undo when used when driving
                        // UI components. This breaks our hideflag setup below due to a remerge of the dragged instance.
                        // StartRecordingUndo() is called on DragExited. Fixes case 1223793.
                        DrivenRectTransformTracker.StopRecordingUndo();

                        Scene destinationScene = GetDestinationSceneForNewGameObjectsForSceneView(sceneView);
                        if (!EditorApplication.isPlaying || EditorSceneManager.IsPreviewScene(destinationScene))
                        {
                            m_DragObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabAssetRoot, destinationScene);
                            m_DragObject.name = go.name;
                        }
                        else
                        {
                            // Instatiate as regular GameObject in Play Mode so runtime logic
                            // won't run into restrictions on restructuring Prefab instances.
                            m_DragObject = Instantiate(prefabAssetRoot);
                            SceneManager.MoveGameObjectToScene(m_DragObject, destinationScene);
                        }
                        m_DragObject.hideFlags = HideFlags.HideInHierarchy;

                        if (HandleUtility.ignoreRaySnapObjects == null)
                            HandleUtility.ignoreRaySnapObjects = m_DragObject.GetComponentsInChildren<Transform>();
                        else
                            HandleUtility.ignoreRaySnapObjects = HandleUtility.ignoreRaySnapObjects.Union(m_DragObject.GetComponentsInChildren<Transform>()).ToArray();

                        PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                        if (prefabStage != null)
                        {
                            GameObject prefab = AssetDatabase.LoadMainAssetAtPath(prefabStage.assetPath) as GameObject;

                            if (prefab != null)
                            {
                                if (PrefabUtility.CheckIfAddingPrefabWouldResultInCyclicNesting(prefab, target))
                                {
                                    s_CyclicNestingDetected = true;
                                }
                            }
                        }
                    }

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    Vector3 point, normal;
                    float offset = 0;

                    if (index == 0)
                    {
                        s_PlaceObject = HandleUtility.PlaceObject(mousePosition, out s_PlaceObjectPoint, out s_PlaceObjectNormal);
                    }

                    point = s_PlaceObjectPoint;
                    normal = s_PlaceObjectNormal;

                    if (s_PlaceObject)
                    {
                        if (Tools.pivotMode == PivotMode.Center)
                        {
                            float geomOffset = HandleUtility.CalcRayPlaceOffset(m_DragObject.GetComponentsInChildren<Transform>(), normal);
                            if (geomOffset != Mathf.Infinity)
                                offset = Vector3.Dot(m_DragObject.transform.position, normal) - geomOffset;
                        }
                        m_DragObject.transform.position = Matrix4x4.identity.MultiplyPoint(point + (normal * offset));
                    }
                    else
                        m_DragObject.transform.position = HandleUtility.GUIPointToWorldRay(mousePosition).GetPoint(10);

                    if (alt)
                    {
                        if (offset != 0)
                        {
                            m_DragObject.transform.position = point;
                        }
                        m_DragObject.transform.position += prefabAssetRoot.transform.localPosition;
                    }

                    // Use prefabs original z position when in 2D mode
                    if (sceneView.in2DMode)
                    {
                        Vector3 dragPosition = m_DragObject.transform.position;
                        dragPosition.z = prefabAssetRoot.transform.position.z;
                        m_DragObject.transform.position = dragPosition;
                    }

                    // Schedule selection clearing for when we start performing the actual drag action
                    s_ShouldClearSelection = true;
                    break;
                case EventType.DragPerform:
                    DragPerform(sceneView, m_DragObject, go);
                    m_DragObject = null;
                    break;
                case EventType.DragExited:
                    // DragExited is always fired after DragPerform so we do no need to call StartRecordingUndo
                    // in DragPerform
                    DrivenRectTransformTracker.StartRecordingUndo();

                    if (m_DragObject)
                    {
                        DestroyImmediate(m_DragObject, false);
                        HandleUtility.ignoreRaySnapObjects = null;
                        m_DragObject = null;
                    }
                    s_ShouldClearSelection = false;
                    s_CyclicNestingDetected = false;
                    break;
            }
        }

        internal static void DragPerform(SceneView sceneView, GameObject draggedObject, GameObject go)
        {
            var defaultParentObject = SceneView.GetDefaultParentObjectIfSet();
            var parent = defaultParentObject != null ? defaultParentObject : sceneView.customParentForDraggedObjects;

            string uniqueName = GameObjectUtility.GetUniqueNameForSibling(parent, draggedObject.name);
            if (parent != null)
                draggedObject.transform.parent = parent;
            draggedObject.hideFlags = 0;
            Undo.RegisterCreatedObjectUndo(draggedObject, "Place " + draggedObject.name);
            DragAndDrop.AcceptDrag();
            if (s_ShouldClearSelection)
            {
                Selection.objects = new[] { draggedObject };
                s_ShouldClearSelection = false;
            }
            else
            {
                // Since this inspector code executes for each dragged GameObject we should retain
                // selection to all of them by joining them to the previous selection list
                Selection.objects = Selection.gameObjects.Union(new[] { draggedObject }).ToArray();
            }
            HandleUtility.ignoreRaySnapObjects = null;
            if (SceneView.mouseOverWindow != null)
                SceneView.mouseOverWindow.Focus();
            if (!Application.IsPlaying(draggedObject))
                draggedObject.name = uniqueName;
            s_CyclicNestingDetected = false;
        }

        internal UnityObject GetMainAssetFromBrokenPrefabInstanceRoot(GameObject targetGo)
        {
            //Handle cases where you have a variant with a parent that is missing
            var path = PrefabUtility.GetAssetPathOfSourcePrefab(targetGo);
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            return asset;
        }
    }
}
