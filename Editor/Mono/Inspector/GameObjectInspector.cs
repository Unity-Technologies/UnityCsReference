// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityObject = UnityEngine.Object;

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

        class Styles
        {
            public GUIContent typelessIcon = EditorGUIUtility.IconContent("Prefab Icon");

            public GUIContent overridesContent = EditorGUIUtility.TrTextContent("Overrides");

            public GUIContent staticContent = EditorGUIUtility.TrTextContent("Static", "Enable the checkbox to mark this GameObject as static for all systems.\n\nDisable the checkbox to mark this GameObject as not static for all systems.\n\nUse the drop-down menu to mark as this GameObject as static or not static for individual systems.");
            public GUIContent layerContent = EditorGUIUtility.TrTextContent("Layer", "The layer that this GameObject is in.\n\nChoose Add Layer... to edit the list of available layers.");
            public GUIContent tagContent = EditorGUIUtility.TrTextContent("Tag", "The tag that this GameObject has.\n\nChoose Untagged to remove the current tag.\n\nChoose Add Tag... to edit the list of available tags.");

            public float tagFieldWidth;
            public float layerFieldWidth;

            public GUIStyle staticDropdown = "StaticDropdown";
            public GUIStyle layerPopup = new GUIStyle(EditorStyles.popup);
            public GUIStyle overridesDropdown = new GUIStyle("MiniPullDown");
            public GUIStyle prefabButtonsHorizontalLayout = new GUIStyle
            {
                margin = new RectOffset { top = 1 },
                padding = new RectOffset { bottom = 1 }
            };

            public GUIStyle instanceManagementInfo = new GUIStyle(EditorStyles.helpBox);
            public GUIStyle inspectorBig = new GUIStyle(EditorStyles.inspectorBig);

            public GUIContent goTypeLabelMultiple = EditorGUIUtility.TrTextContent("Multiple");

            private static GUIContent regularPrefab = EditorGUIUtility.TrTextContent("Prefab");
            private static GUIContent disconnectedPrefab = EditorGUIUtility.TrTextContent("Prefab", "You have broken the prefab connection. Changes to the prefab will not be applied to this object before you Apply or Revert.");
            private static GUIContent modelPrefab = EditorGUIUtility.TrTextContent("Model");
            private static GUIContent disconnectedModelPrefab =  EditorGUIUtility.TrTextContent("Model", "You have broken the prefab connection. Changes to the model will not be applied to this object before you Revert.");
            private static GUIContent variantPrefab = EditorGUIUtility.TrTextContent("Variant");
            private static GUIContent disconnectedVariantPrefab = EditorGUIUtility.TrTextContent("Variant", "You have broken the prefab connection. Changes to the prefab will not be applied to this object before you Apply or Revert.");
            private static GUIContent missingPrefabAsset = EditorGUIUtility.TrTextContent("Missing", "The source Prefab or Model has been deleted.");

            // Matrix based on two enums:
            // Columns correspond to PrefabTypeUtility.PrefabAssetType (see comments above rows).
            // Rows correspond to PrefabTypeUtility.PrefabInstanceStatus (None, Connected, Disconnected, Missing).
            // If missing, both enums will be "Missing".
            public GUIContent[,] goTypeLabel =
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

            public Styles()
            {
                tagFieldWidth = EditorStyles.boldLabel.CalcSize(tagContent).x;
                layerFieldWidth = EditorStyles.boldLabel.CalcSize(layerContent).x;
                GUIStyle miniButtonMid = "MiniButtonMid";
                instanceManagementInfo.padding = miniButtonMid.padding;
                instanceManagementInfo.alignment = miniButtonMid.alignment;

                // Seems to be a bug in the way controls with margin internal to layout groups with padding calculate position. We'll work around it here.
                layerPopup.margin.right = 0;
                overridesDropdown.margin.right = 0;
            }
        }
        static Styles s_Styles;
        const float kIconSize = 24;

        class PreviewData : IDisposable
        {
            bool m_Disposed;
            GameObject m_GameObject;

            public readonly PreviewRenderUtility renderUtility;
            public GameObject gameObject { get { return m_GameObject; } }

            public PreviewData(UnityObject targetObject)
            {
                renderUtility = new PreviewRenderUtility();
                renderUtility.camera.fieldOfView = 30.0f;
                UpdateGameObject(targetObject);
            }

            public void UpdateGameObject(UnityObject targetObject)
            {
                UnityObject.DestroyImmediate(gameObject);
                m_GameObject = EditorUtility.InstantiateForAnimatorPreview(targetObject);
                renderUtility.AddManagedGO(gameObject);
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;
                renderUtility.Cleanup();
                UnityObject.DestroyImmediate(gameObject);
                m_GameObject = null;
                m_Disposed = true;
            }
        }

        Dictionary<int, PreviewData> m_PreviewInstances = new Dictionary<int, PreviewData>();
        Dictionary<int, Texture> m_PreviewCache;
        Vector2 m_PreviewDir;
        Rect m_PreviewRect;
        bool m_PlayModeObjects;
        bool m_IsAsset;
        bool m_ImmutableSelf;
        bool m_ImmutableSourceAsset;
        bool m_IsDisconnected;
        bool m_IsMissing;
        bool m_IsPrefabInstanceAnyRoot;
        bool m_IsPrefabInstanceOutermostRoot;
        bool m_AllOfSamePrefabType = true;

        public void OnEnable()
        {
            if (EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D)
                m_PreviewDir = new Vector2(0, 0);
            else
                m_PreviewDir = new Vector2(120, -20);

            m_Name = serializedObject.FindProperty("m_Name");
            m_IsActive = serializedObject.FindProperty("m_IsActive");
            m_Layer = serializedObject.FindProperty("m_Layer");
            m_Tag = serializedObject.FindProperty("m_TagString");
            m_StaticEditorFlags = serializedObject.FindProperty("m_StaticEditorFlags");
            m_Icon = serializedObject.FindProperty("m_Icon");

            CalculatePrefabStatus();

            m_PreviewCache = new Dictionary<int, Texture>();
        }

        void CalculatePrefabStatus()
        {
            m_PlayModeObjects = false;
            m_IsAsset = false;
            m_ImmutableSelf = false;
            m_ImmutableSourceAsset = false;
            m_IsDisconnected = false;
            m_IsMissing = false;
            m_IsPrefabInstanceAnyRoot = true;
            m_IsPrefabInstanceOutermostRoot = true;
            m_AllOfSamePrefabType = true;
            PrefabAssetType firstType = PrefabUtility.GetPrefabAssetType(targets[0]);
            PrefabInstanceStatus firstStatus = PrefabUtility.GetPrefabInstanceStatus(targets[0]);

            foreach (GameObject go in targets)
            {
                PrefabAssetType type = PrefabUtility.GetPrefabAssetType(go);
                PrefabInstanceStatus status = PrefabUtility.GetPrefabInstanceStatus(go);
                if (type != firstType || status != firstStatus)
                    m_AllOfSamePrefabType = false;

                if (Application.IsPlaying(go))
                    m_PlayModeObjects = true;
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(go))
                    m_IsPrefabInstanceAnyRoot = false; // Conservative is false if any is false
                if (!m_IsPrefabInstanceAnyRoot || !PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                    m_IsPrefabInstanceOutermostRoot = false; // Conservative is false if any is false
                if (PrefabUtility.IsPartOfPrefabAsset(go))
                    m_IsAsset = true; // Conservative is true if any is true
                if (m_IsAsset && PrefabUtility.IsPartOfImmutablePrefab(go))
                    m_ImmutableSelf = true; // Conservative is true if any is true
                GameObject originalSourceOrVariant = PrefabUtility.GetOriginalSourceOrVariantRoot(go);
                if (originalSourceOrVariant != null && PrefabUtility.IsPartOfImmutablePrefab(originalSourceOrVariant))
                    m_ImmutableSourceAsset = true; // Conservative is true if any is true
                if (PrefabUtility.IsDisconnectedFromPrefabAsset(go))
                    m_IsDisconnected = true;
                if (PrefabUtility.IsPrefabAssetMissing(go))
                    m_IsMissing = true;
            }
        }

        void OnDisable()
        {
            foreach (var previewData in m_PreviewInstances.Values)
                previewData.Dispose();
            ClearPreviewCache();
            m_PreviewCache = null;
        }

        void ClearPreviewCache()
        {
            foreach (var texture in m_PreviewCache.Values)
            {
                DestroyImmediate(texture);
            }
            m_PreviewCache.Clear();
        }

        private static bool ShowMixedStaticEditorFlags(StaticEditorFlags mask)
        {
            uint countedBits = 0;
            uint numFlags = 0;
            foreach (var i in Enum.GetValues(typeof(StaticEditorFlags)))
            {
                numFlags++;
                if ((mask & (StaticEditorFlags)i) > 0)
                    countedBits++;
            }

            //If we have more then one selected... but it is not all the flags
            //All indictates 'everything' which means it should be a tick!
            return countedBits > 0 && countedBits != numFlags;
        }

        protected override void OnHeaderGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            bool enabledTemp = GUI.enabled;
            GUI.enabled = true;
            EditorGUILayout.BeginVertical(s_Styles.inspectorBig);
            GUI.enabled = enabledTemp;
            DrawInspector();
            EditorGUILayout.EndVertical();
        }

        public override void OnInspectorGUI() {}

        static void IterateOverGameObjects(GameObject obj, ref int indent, Action<GameObject, int> action)
        {
            action(obj, indent);
            indent++;
            foreach (Transform child in obj.transform)
            {
                IterateOverGameObjects(child.gameObject, ref indent, action);
            }
            indent--;
        }

        internal bool DrawInspector()
        {
            serializedObject.Update();

            GameObject go = target as GameObject;

            // Don't let icon be null as it will create null reference exceptions.
            Texture2D icon = (Texture2D)(s_Styles.typelessIcon.image);

            // Leave iconContent to be default if multiple objects not the same type.
            if (m_AllOfSamePrefabType)
            {
                icon = PrefabUtility.GetIconForGameObject(go);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUI.ObjectIconDropDown(GUILayoutUtility.GetRect(kIconSize, kIconSize, GUILayout.ExpandWidth(false)), targets, true, icon as Texture2D, m_Icon);
            DrawPostIconContent();

            using (new EditorGUI.DisabledScope(m_ImmutableSelf))
            {
                EditorGUILayout.BeginVertical();

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.BeginHorizontal(GUILayout.Width(s_Styles.tagFieldWidth));

                GUILayout.FlexibleSpace();

                // IsActive
                EditorGUI.PropertyField(GUILayoutUtility.GetRect(EditorStyles.toggle.padding.left, EditorGUIUtility.singleLineHeight, EditorStyles.toggle, GUILayout.ExpandWidth(false)), m_IsActive, GUIContent.none);

                EditorGUILayout.EndHorizontal();

                // Name
                EditorGUILayout.DelayedTextField(m_Name, GUIContent.none);

                // Static flags toggle
                DoStaticToggleField(go);

                // Static flags dropdown
                DoStaticFlagsDropDown(go);

                EditorGUILayout.EndHorizontal();

                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

                EditorGUILayout.BeginHorizontal();

                // Tag
                DoTagsField(go);

                // Layer
                DoLayerField(go);

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();

            // Prefab Toolbar
            if (EditorGUIUtility.comparisonViewMode == EditorGUIUtility.ComparisonViewMode.None)
            {
                DoPrefabButtons();
            }

            serializedObject.ApplyModifiedProperties();

            return true;
        }

        private void DoPrefabButtons()
        {
            if (!m_IsPrefabInstanceAnyRoot)
                return;

            using (new EditorGUI.DisabledScope(m_PlayModeObjects))
            {
                EditorGUILayout.BeginHorizontal(s_Styles.prefabButtonsHorizontalLayout);

                // Prefab information
                PrefabAssetType singlePrefabType = PrefabUtility.GetPrefabAssetType(target);
                PrefabInstanceStatus singleInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(target);
                GUIContent prefixLabel;
                if (targets.Length > 1)
                {
                    prefixLabel = s_Styles.goTypeLabelMultiple;
                }
                else
                {
                    prefixLabel = s_Styles.goTypeLabel[(int)singlePrefabType, (int)singleInstanceStatus];
                }

                if (prefixLabel != null)
                {
                    EditorGUILayout.BeginHorizontal(GUILayout.Width(kIconSize + s_Styles.tagFieldWidth));
                    GUILayout.FlexibleSpace();
                    if (m_IsDisconnected || m_IsMissing)
                    {
                        GUI.contentColor = GUI.skin.GetStyle("CN StatusWarn").normal.textColor;
                        GUILayout.Label(prefixLabel, EditorStyles.whiteLabel, GUILayout.ExpandWidth(false));
                        GUI.contentColor = Color.white;
                    }
                    else
                        GUILayout.Label(prefixLabel, GUILayout.ExpandWidth(false));
                    EditorGUILayout.EndHorizontal();
                }

                if (!m_IsMissing)
                {
                    using (new EditorGUI.DisabledScope(targets.Length > 1))
                    {
                        if (singlePrefabType == PrefabAssetType.Model)
                        {
                            // Open Model Prefab
                            if (GUILayout.Button("Open", "MiniButtonLeft"))
                            {
                                GameObject asset = PrefabUtility.GetOriginalSourceOrVariantRoot(target);
                                AssetDatabase.OpenAsset(asset);
                                GUIUtility.ExitGUI();
                            }
                        }
                        else
                        {
                            // Open non-Model Prefab
                            using (new EditorGUI.DisabledScope(m_ImmutableSourceAsset))
                            {
                                if (GUILayout.Button("Open", "MiniButtonLeft"))
                                {
                                    GameObject asset = PrefabUtility.GetOriginalSourceOrVariantRoot(target);
                                    PrefabStageUtility.OpenPrefab(AssetDatabase.GetAssetPath(asset), (GameObject)target, StageNavigationManager.Analytics.ChangeType.EnterViaInstanceInspectorOpenButton);
                                    GUIUtility.ExitGUI();
                                }
                            }
                        }
                    }

                    // Select prefab
                    if (GUILayout.Button("Select", "MiniButtonRight"))
                    {
                        HashSet<GameObject> selectedAssets = new HashSet<GameObject>();
                        for (int i = 0; i < targets.Length; i++)
                        {
                            GameObject prefabGo = PrefabUtility.GetOriginalSourceOrVariantRoot(targets[i]);

                            // Because of legacy prefab references we have to have this extra step
                            // to make sure we ping the prefab asset correctly.
                            // Reason is that scene files created prior to making prefabs CopyAssets
                            // will reference prefabs as if they are serialized assets. Those references
                            // works fine but we are not able to ping objects loaded directly from the asset
                            // file, so we have to make sure we ping the metadata version of the prefab.
                            var assetPath = AssetDatabase.GetAssetPath(prefabGo);
                            selectedAssets.Add((GameObject)AssetDatabase.LoadMainAssetAtPath(assetPath));
                        }

                        Selection.objects = selectedAssets.ToArray();
                        if (Selection.gameObjects.Length == 1)
                            EditorGUIUtility.PingObject(Selection.activeObject);
                    }

                    // Should be EditorGUILayout.Space, except it does not have ExpandWidth set to false.
                    // Maybe we can change that?
                    GUILayoutUtility.GetRect(6, 6, GUILayout.ExpandWidth(false));

                    // Reserve space regardless of whether the button is there or not to avoid jumps in button sizes.
                    Rect rect = GUILayoutUtility.GetRect(s_Styles.overridesContent, s_Styles.overridesDropdown);
                    if (m_IsPrefabInstanceOutermostRoot)
                    {
                        if (EditorGUI.DropdownButton(rect, s_Styles.overridesContent, FocusType.Passive))
                        {
                            if (targets.Length > 1)
                                PopupWindow.Show(rect, new PrefabOverridesWindow(targets.Select(e => (GameObject)e).ToArray<GameObject>()));
                            else
                                PopupWindow.Show(rect, new PrefabOverridesWindow((GameObject)target));
                            GUIUtility.ExitGUI();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DoLayerField(GameObject go)
        {
            EditorGUIUtility.labelWidth = s_Styles.layerFieldWidth;
            Rect layerRect = GUILayoutUtility.GetRect(GUIContent.none, s_Styles.layerPopup);
            EditorGUI.BeginProperty(layerRect, GUIContent.none, m_Layer);
            EditorGUI.BeginChangeCheck();
            int layer = EditorGUI.LayerField(layerRect, s_Styles.layerContent, go.layer, s_Styles.layerPopup);
            if (EditorGUI.EndChangeCheck())
            {
                GameObjectUtility.ShouldIncludeChildren includeChildren = GameObjectUtility.DisplayUpdateChildrenDialogIfNeeded(targets.OfType<GameObject>(),
                    "Change Layer", string.Format("Do you want to set layer to {0} for all child objects as well?", InternalEditorUtility.GetLayerName(layer)));
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
            string tagName = null;
            try
            {
                tagName = go.tag;
            }
            catch (System.Exception)
            {
                tagName = "Undefined";
            }
            EditorGUIUtility.labelWidth = s_Styles.tagFieldWidth;
            Rect tagRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.popup);
            EditorGUI.BeginProperty(tagRect, GUIContent.none, m_Tag);
            EditorGUI.BeginChangeCheck();
            string tag = EditorGUI.TagField(tagRect, s_Styles.tagContent, tagName);
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
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = m_StaticEditorFlags.hasMultipleDifferentValues;
            int changedFlags;
            bool changedToValue;
            EditorGUI.EnumFlagsField(
                GUILayoutUtility.GetRect(GUIContent.none, s_Styles.staticDropdown, GUILayout.ExpandWidth(false)),
                GUIContent.none,
                GameObjectUtility.GetStaticEditorFlags(go),
                false,
                out changedFlags, out changedToValue,
                s_Styles.staticDropdown
            );
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                SceneModeUtility.SetStaticFlags(targets, changedFlags, changedToValue);
                serializedObject.SetIsDifferentCacheDirty();

                // Displaying the dialog to ask the user whether to update children nukes the gui state (case 962453)
                EditorGUIUtility.ExitGUI();
            }
        }

        private void DoStaticToggleField(GameObject go)
        {
            var staticRect = GUILayoutUtility.GetRect(s_Styles.staticContent, EditorStyles.toggle, GUILayout.ExpandWidth(false));
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
            var toggled = EditorGUI.ToggleLeft(toggleRect, s_Styles.staticContent, go.isStatic);
            if (nonLeftClick)
                evt.type = origType;
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                SceneModeUtility.SetStaticFlags(targets, ~0, toggled);
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
            foreach (GameObject go in objects)
                go.layer = layer;
        }

        public override void ReloadPreviewInstances()
        {
            foreach (var pair in m_PreviewInstances)
            {
                var index = pair.Key;
                if (index > targets.Length)
                    continue;

                var previewData = pair.Value;
                previewData.UpdateGameObject(targets[index]);
            }
        }

        PreviewData GetPreviewData()
        {
            PreviewData previewData;
            if (!m_PreviewInstances.TryGetValue(referenceTargetIndex, out previewData))
            {
                previewData = new PreviewData(target);
                m_PreviewInstances.Add(referenceTargetIndex, previewData);
            }
            if (!previewData.gameObject)
                ReloadPreviewInstances();
            return previewData;
        }

        public static bool HasRenderableParts(GameObject go)
        {
            // Do we have a mesh?
            var renderers = go.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                var filter = renderer.gameObject.GetComponent<MeshFilter>();
                if (filter && filter.sharedMesh)
                    return true;
            }

            // Do we have a skinned mesh?
            var skins = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var skin in skins)
            {
                if (skin.sharedMesh)
                    return true;
            }

            // Do we have a Sprite?
            var sprites = go.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sprite in sprites)
            {
                if (sprite.sprite)
                    return true;
            }

            // Nope, we don't have it.
            return false;
        }

        public static void GetRenderableBoundsRecurse(ref Bounds bounds, GameObject go)
        {
            // Do we have a mesh?
            MeshRenderer renderer = go.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
            MeshFilter filter = go.GetComponent(typeof(MeshFilter)) as MeshFilter;
            if (renderer && filter && filter.sharedMesh)
            {
                // To prevent origo from always being included in bounds we initialize it
                // with renderer.bounds. This ensures correct bounds for meshes with origo outside the mesh.
                if (bounds.extents == Vector3.zero)
                    bounds = renderer.bounds;
                else
                    bounds.Encapsulate(renderer.bounds);
            }

            // Do we have a skinned mesh?
            SkinnedMeshRenderer skin = go.GetComponent(typeof(SkinnedMeshRenderer)) as SkinnedMeshRenderer;
            if (skin && skin.sharedMesh)
            {
                if (bounds.extents == Vector3.zero)
                    bounds = skin.bounds;
                else
                    bounds.Encapsulate(skin.bounds);
            }

            // Do we have a Sprite?
            SpriteRenderer sprite = go.GetComponent(typeof(SpriteRenderer)) as SpriteRenderer;
            if (sprite && sprite.sprite)
            {
                if (bounds.extents == Vector3.zero)
                    bounds = sprite.bounds;
                else
                    bounds.Encapsulate(sprite.bounds);
            }

            // Recurse into children
            foreach (Transform t in go.transform)
            {
                GetRenderableBoundsRecurse(ref bounds, t.gameObject);
            }
        }

        private static float GetRenderableCenterRecurse(ref Vector3 center, GameObject go, int depth, int minDepth, int maxDepth)
        {
            if (depth > maxDepth)
                return 0;

            float ret = 0;

            if (depth > minDepth)
            {
                // Do we have a mesh?
                MeshRenderer renderer = go.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
                MeshFilter filter = go.GetComponent(typeof(MeshFilter)) as MeshFilter;
                SkinnedMeshRenderer skin = go.GetComponent(typeof(SkinnedMeshRenderer)) as SkinnedMeshRenderer;
                SpriteRenderer sprite = go.GetComponent(typeof(SpriteRenderer)) as SpriteRenderer;

                if (renderer == null && filter == null && skin == null && sprite == null)
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

            GameObject go = target as GameObject;

            // Is this a camera?
            Camera camera = go.GetComponent(typeof(Camera)) as Camera;
            if (camera)
                return true;

            return HasRenderableParts(go);
        }

        public override void OnPreviewSettings()
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
                return;
            GUI.enabled = true;
        }

        private void DoRenderPreview()
        {
            var previewData = GetPreviewData();

            Bounds bounds = new Bounds(previewData.gameObject.transform.position, Vector3.zero);
            GetRenderableBoundsRecurse(ref bounds, previewData.gameObject);
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

            var previewUtility = GetPreviewData().renderUtility;
            previewUtility.BeginStaticPreview(new Rect(0, 0, width, height));

            DoRenderPreview();

            return previewUtility.EndStaticPreview();
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 40), "Preview requires\nrender texture support");
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
                DoRenderPreview();
                previewUtility.EndAndDrawPreview(r);
                var copy = new RenderTexture(previewUtility.renderTexture);
                Graphics.CopyTexture(previewUtility.renderTexture, copy);
                m_PreviewCache.Add(referenceTargetIndex, copy);
            }
        }

        // Handle dragging in scene view
        public static GameObject dragObject;

        public void OnSceneDrag(SceneView sceneView)
        {
            GameObject go = target as GameObject;
            if (!PrefabUtility.IsPartOfPrefabAsset(go))
                return;

            var prefabAssetRoot = go.transform.root.gameObject;

            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:

                    Scene destinationScene = sceneView.customScene.IsValid() ? sceneView.customScene : SceneManager.GetActiveScene();
                    if (dragObject == null)
                    {
                        dragObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabAssetRoot, destinationScene);
                        dragObject.hideFlags = HideFlags.HideInHierarchy;
                        dragObject.name = go.name;
                    }

                    if (HandleUtility.ignoreRaySnapObjects == null)
                        HandleUtility.ignoreRaySnapObjects = dragObject.GetComponentsInChildren<Transform>();

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    object hit = HandleUtility.RaySnap(HandleUtility.GUIPointToWorldRay(evt.mousePosition));

                    if (hit != null)
                    {
                        RaycastHit rh = (RaycastHit)hit;
                        float offset = 0;
                        if (Tools.pivotMode == PivotMode.Center)
                        {
                            float geomOffset = HandleUtility.CalcRayPlaceOffset(HandleUtility.ignoreRaySnapObjects, rh.normal);
                            if (geomOffset != Mathf.Infinity)
                                offset = Vector3.Dot(dragObject.transform.position, rh.normal) - geomOffset;
                        }
                        dragObject.transform.position = Matrix4x4.identity.MultiplyPoint(rh.point + (rh.normal * offset));
                    }
                    else
                        dragObject.transform.position = HandleUtility.GUIPointToWorldRay(evt.mousePosition).GetPoint(10);

                    // Use prefabs original z position when in 2D mode
                    if (sceneView.in2DMode)
                    {
                        Vector3 dragPosition = dragObject.transform.position;
                        dragPosition.z = prefabAssetRoot.transform.position.z;
                        dragObject.transform.position = dragPosition;
                    }

                    evt.Use();
                    break;
                case EventType.DragPerform:

                    var stage = StageNavigationManager.instance.currentItem;
                    if (stage.isPrefabStage)
                    {
                        var prefabAssetThatIsAddedTo = AssetDatabase.LoadMainAssetAtPath(stage.prefabAssetPath);
                        if (PrefabUtility.CheckIfAddingPrefabWouldResultInCyclicNesting(prefabAssetThatIsAddedTo, go))
                        {
                            PrefabUtility.ShowCyclicNestingWarningDialog();
                            return;
                        }
                    }

                    Transform parent = sceneView.customParentForDraggedObjects;

                    string uniqueName = GameObjectUtility.GetUniqueNameForSibling(parent, dragObject.name);
                    if (parent != null)
                        dragObject.transform.parent = parent;
                    dragObject.hideFlags = 0;
                    Undo.RegisterCreatedObjectUndo(dragObject, "Place " + dragObject.name);
                    EditorUtility.SetDirty(dragObject);
                    DragAndDrop.AcceptDrag();
                    Selection.activeObject = dragObject;
                    HandleUtility.ignoreRaySnapObjects = null;
                    if (SceneView.mouseOverWindow != null)
                        SceneView.mouseOverWindow.Focus();
                    dragObject.name = uniqueName;
                    dragObject = null;
                    evt.Use();
                    break;
                case EventType.DragExited:
                    if (dragObject)
                    {
                        UnityObject.DestroyImmediate(dragObject, false);
                        HandleUtility.ignoreRaySnapObjects = null;
                        dragObject = null;
                        evt.Use();
                    }
                    break;
            }
        }
    }
}
