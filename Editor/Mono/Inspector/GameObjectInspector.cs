// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;

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
            public GUIContent goIcon = EditorGUIUtility.IconContent("GameObject Icon");
            public GUIContent typelessIcon = EditorGUIUtility.IconContent("Prefab Icon");
            public GUIContent prefabIcon = EditorGUIUtility.IconContent("PrefabNormal Icon");
            public GUIContent modelIcon = EditorGUIUtility.IconContent("PrefabModel Icon");

            public GUIContent staticContent = EditorGUIUtility.TextContent("Static|Enable the checkbox to mark this GameObject as static for all systems.\n\nDisable the checkbox to mark this GameObject as not static for all systems.\n\nUse the drop-down menu to mark as this GameObject as static or not static for individual systems.");
            public GUIContent layerContent = EditorGUIUtility.TextContent("Layer|The layer that this GameObject is in.\n\nChoose Add Layer... to edit the list of available layers.");
            public GUIContent tagContent = EditorGUIUtility.TextContent("Tag|The tag that this GameObject has.\n\nChoose Untagged to remove the current tag.\n\nChoose Add Tag... to edit the list of available tags.");

            public float tagFieldWidth = EditorStyles.boldLabel.CalcSize(EditorGUIUtility.TempContent("Tag")).x;
            public float layerFieldWidth = EditorStyles.boldLabel.CalcSize(EditorGUIUtility.TempContent("Layer")).x;

            public GUIStyle staticDropdown = "StaticDropdown";
            public GUIStyle header = new GUIStyle("IN GameObjectHeader");
            public GUIStyle layerPopup = new GUIStyle(EditorStyles.popup);

            public GUIStyle instanceManagementInfo = new GUIStyle(EditorStyles.helpBox);

            public GUIContent goTypeLabelMultiple = new GUIContent("Multiple");

            public GUIContent[] goTypeLabel =
            {
                null,//             None = 0,
                EditorGUIUtility.TextContent("Prefab"),           // Prefab = 1
                EditorGUIUtility.TextContent("Model"),            // ModelPrefab = 2
                EditorGUIUtility.TextContent("Prefab"),           // PrefabInstance = 3
                EditorGUIUtility.TextContent("Model"),            // ModelPrefabInstance = 4
                EditorGUIUtility.TextContent("Missing|The source Prefab or Model has been deleted."),          // MissingPrefabInstance
                EditorGUIUtility.TextContent("Prefab|You have broken the prefab connection. Changes to the prefab will not be applied to this object before you Apply or Revert."), // DisconnectedPrefabInstance
                EditorGUIUtility.TextContent("Model|You have broken the prefab connection. Changes to the model will not be applied to this object before you Revert."), // DisconnectedModelPrefabInstance
            };

            public Styles()
            {
                GUIStyle miniButtonMid = "MiniButtonMid";
                instanceManagementInfo.padding = miniButtonMid.padding;
                instanceManagementInfo.alignment = miniButtonMid.alignment;

                // Seems to be a bug in the way controls with margin internal to layout groups with padding calculate position. We'll work around it here.
                layerPopup.margin.right = 0;
                header.padding.bottom -= 3;
            }
        }
        static Styles s_Styles;
        const float kIconSize = 24;
        Vector2 previewDir;

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
                m_Disposed = true;
            }
        }

        Dictionary<int, PreviewData> m_PreviewInstances = new Dictionary<int, PreviewData>();

        bool m_HasInstance = false;
        bool m_AllOfSamePrefabType = true;

        public void OnEnable()
        {
            if (EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D)
                previewDir = new Vector2(0, 0);
            else
                previewDir = new Vector2(120, -20);

            m_Name = serializedObject.FindProperty("m_Name");
            m_IsActive = serializedObject.FindProperty("m_IsActive");
            m_Layer = serializedObject.FindProperty("m_Layer");
            m_Tag = serializedObject.FindProperty("m_TagString");
            m_StaticEditorFlags = serializedObject.FindProperty("m_StaticEditorFlags");
            m_Icon = serializedObject.FindProperty("m_Icon");

            CalculatePrefabStatus();
        }

        void CalculatePrefabStatus()
        {
            m_HasInstance = false;
            m_AllOfSamePrefabType = true;
            PrefabType firstType = PrefabUtility.GetPrefabType(targets[0] as GameObject);
            foreach (GameObject go in targets)
            {
                PrefabType type = PrefabUtility.GetPrefabType(go);
                if (type != firstType)
                    m_AllOfSamePrefabType = false;
                if (type != PrefabType.None && type != PrefabType.Prefab && type != PrefabType.ModelPrefab)
                    m_HasInstance = true;
            }
        }

        void OnDisable() {}


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
            EditorGUILayout.BeginVertical(s_Styles.header);
            GUI.enabled = enabledTemp;
            DrawInspector();
            EditorGUILayout.EndVertical();
        }

        public override void OnInspectorGUI() {}

        internal bool DrawInspector()
        {
            serializedObject.Update();

            GameObject go = target as GameObject;

            GUIContent iconContent = null;

            PrefabType prefabType = PrefabType.None;
            // Leave iconContent to be null if multiple objects not the same type.
            if (m_AllOfSamePrefabType)
            {
                prefabType = PrefabUtility.GetPrefabType(go);
                switch (prefabType)
                {
                    case PrefabType.None:
                        iconContent = s_Styles.goIcon;
                        break;
                    case PrefabType.Prefab:
                    case PrefabType.PrefabInstance:
                    case PrefabType.DisconnectedPrefabInstance:
                    case PrefabType.MissingPrefabInstance:
                        iconContent = s_Styles.prefabIcon;
                        break;
                    case PrefabType.ModelPrefab:
                    case PrefabType.ModelPrefabInstance:
                    case PrefabType.DisconnectedModelPrefabInstance:
                        iconContent = s_Styles.modelIcon;
                        break;
                }
            }
            else
                iconContent = s_Styles.typelessIcon;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.ObjectIconDropDown(GUILayoutUtility.GetRect(kIconSize, kIconSize, GUILayout.ExpandWidth(false)), targets, true, iconContent.image as Texture2D, m_Icon);
            DrawPostIconContent();

            using (new EditorGUI.DisabledScope(prefabType == PrefabType.ModelPrefab))
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

                EditorGUILayout.BeginHorizontal();

                // Tag
                DoTagsField(go);

                // Layer
                DoLayerField(go);

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();

            // Seems to be a bug in margin not being applied consistently as tag/layer line, account for it here
            GUILayout.Space(2f);

            // Prefab Toolbar
            using (new EditorGUI.DisabledScope(prefabType == PrefabType.ModelPrefab))
                DoPrefabButtons(prefabType, go);

            serializedObject.ApplyModifiedProperties();

            return true;
        }

        private void DoPrefabButtons(PrefabType prefabType, GameObject go)
        {
            // @TODO: If/when we support multi-editing of prefab/model instances,
            // handle it here. Only show prefab bar if all are same type?
            if (!m_HasInstance) return;

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUILayout.BeginHorizontal();

                // Prefab information
                GUIContent prefixLabel = targets.Length > 1 ? s_Styles.goTypeLabelMultiple : s_Styles.goTypeLabel[(int)prefabType];

                if (prefixLabel != null)
                {
                    EditorGUILayout.BeginHorizontal(GUILayout.Width(kIconSize + s_Styles.tagFieldWidth));
                    GUILayout.FlexibleSpace();
                    if (prefabType == PrefabType.DisconnectedModelPrefabInstance || prefabType == PrefabType.MissingPrefabInstance || prefabType == PrefabType.DisconnectedPrefabInstance)
                    {
                        GUI.contentColor = GUI.skin.GetStyle("CN StatusWarn").normal.textColor;
                        GUILayout.Label(prefixLabel, EditorStyles.whiteLabel, GUILayout.ExpandWidth(false));
                        GUI.contentColor = Color.white;
                    }
                    else
                        GUILayout.Label(prefixLabel, GUILayout.ExpandWidth(false));
                    EditorGUILayout.EndHorizontal();
                }

                if (targets.Length > 1)
                    GUILayout.Label("Instance Management Disabled", s_Styles.instanceManagementInfo);
                else
                {
                    // Select prefab
                    if (prefabType != PrefabType.MissingPrefabInstance)
                    {
                        if (GUILayout.Button("Select", "MiniButtonLeft"))
                        {
                            Selection.activeObject = PrefabUtility.GetPrefabParent(target);
                            EditorGUIUtility.PingObject(Selection.activeObject);
                        }
                    }

                    using (new EditorGUI.DisabledScope(AnimationMode.InAnimationMode()))
                    {
                        if (prefabType != PrefabType.MissingPrefabInstance)
                        {
                            // Revert this gameobject and components to prefab
                            if (GUILayout.Button("Revert", "MiniButtonMid"))
                            {
                                PrefabUtility.RevertPrefabInstanceWithUndo(go);

                                // case931300 - The selected gameobject might get destroyed by RevertPrefabInstance
                                if (go != null)
                                {
                                    CalculatePrefabStatus();
                                }

                                // This is necessary because Revert can potentially destroy game objects and components
                                // In that case the Editor classes would be destroyed but still be invoked. (case 837113)
                                GUIUtility.ExitGUI();
                            }

                            // Apply to prefab
                            if (prefabType == PrefabType.PrefabInstance || prefabType == PrefabType.DisconnectedPrefabInstance)
                            {
                                GameObject rootUploadGameObject = PrefabUtility.FindValidUploadPrefabInstanceRoot(go);

                                GUI.enabled = rootUploadGameObject != null && !AnimationMode.InAnimationMode();

                                if (GUILayout.Button("Apply", "MiniButtonRight"))
                                {
                                    UnityObject prefabParent = PrefabUtility.GetPrefabParent(rootUploadGameObject);
                                    string prefabAssetPath = AssetDatabase.GetAssetPath(prefabParent);

                                    bool editablePrefab = Provider.PromptAndCheckoutIfNeeded(
                                            new string[] { prefabAssetPath },
                                            "The version control requires you to check out the prefab before applying changes.");

                                    if (editablePrefab)
                                    {
                                        PrefabUtility.ReplacePrefabWithUndo(rootUploadGameObject);

                                        CalculatePrefabStatus();

                                        // This is necessary because ReplacePrefab can potentially destroy game objects and components
                                        // In that case the Editor classes would be destroyed but still be invoked. (case 468434)
                                        GUIUtility.ExitGUI();
                                    }
                                }
                            }
                        }
                    }

                    // Edit model prefab
                    if (prefabType == PrefabType.DisconnectedModelPrefabInstance || prefabType == PrefabType.ModelPrefabInstance)
                    {
                        if (GUILayout.Button("Open", "MiniButtonRight"))
                        {
                            AssetDatabase.OpenAsset(PrefabUtility.GetPrefabParent(target));
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

            return previewData;
        }

        public void OnDestroy()
        {
            foreach (var previewData in m_PreviewInstances.Values)
                previewData.Dispose();
            m_PreviewInstances.Clear();
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

            Quaternion rot = Quaternion.Euler(-previewDir.y, -previewDir.x, 0);
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

            previewDir = PreviewGUI.Drag2D(previewDir, r);

            if (Event.current.type != EventType.Repaint)
                return;

            var previewUtility = GetPreviewData().renderUtility;
            previewUtility.BeginPreview(r, background);

            DoRenderPreview();

            previewUtility.EndAndDrawPreview(r);
        }

        // Handle dragging in scene view
        public static GameObject dragObject;

        public void OnSceneDrag(SceneView sceneView)
        {
            GameObject go = target as GameObject;
            PrefabType prefabType = PrefabUtility.GetPrefabType(go);
            if (prefabType != PrefabType.Prefab && prefabType != PrefabType.ModelPrefab)
                return;

            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                    if (dragObject == null)
                    {
                        dragObject = (GameObject)PrefabUtility.InstantiatePrefab(PrefabUtility.FindPrefabRoot(go));
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
                        dragPosition.z = PrefabUtility.FindPrefabRoot(go).transform.position.z;
                        dragObject.transform.position = dragPosition;
                    }

                    evt.Use();
                    break;
                case EventType.DragPerform:
                    string uniqueName = GameObjectUtility.GetUniqueNameForSibling(null, dragObject.name);
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
