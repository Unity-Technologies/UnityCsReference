// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditor.VersionControl;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

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

            public GUIContent staticContent = EditorGUIUtility.TextContent("Static");

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

        PreviewRenderUtility m_PreviewUtility;
        List<GameObject> m_PreviewInstances;

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

                    // Reconnect prefab
                    if (prefabType == PrefabType.DisconnectedModelPrefabInstance || prefabType == PrefabType.DisconnectedPrefabInstance)
                    {
                        if (GUILayout.Button("Revert", "MiniButtonMid"))
                        {
                            List<Object> hierarchy = new List<Object>();
                            GetObjectListFromHierarchy(hierarchy, go);

                            Undo.RegisterFullObjectHierarchyUndo(go, "Revert to prefab");
                            PrefabUtility.ReconnectToLastPrefab(go);
                            Undo.RegisterCreatedObjectUndo(PrefabUtility.GetPrefabObject(go), "Revert to prefab");
                            PrefabUtility.RevertPrefabInstance(go);
                            CalculatePrefabStatus();

                            List<Object> newHierarchy = new List<Object>();
                            GetObjectListFromHierarchy(newHierarchy, go);
                            RegisterNewComponents(newHierarchy, hierarchy);
                        }
                    }

                    using (new EditorGUI.DisabledScope(AnimationMode.InAnimationMode()))
                    {
                        // Revert this gameobject and components to prefab
                        if (prefabType == PrefabType.ModelPrefabInstance || prefabType == PrefabType.PrefabInstance)
                        {
                            if (GUILayout.Button("Revert", "MiniButtonMid"))
                            {
                                RevertAndCheckForNewComponents(go);
                            }
                        }

                        // Apply to prefab
                        if (prefabType == PrefabType.PrefabInstance || prefabType == PrefabType.DisconnectedPrefabInstance)
                        {
                            GameObject rootUploadGameObject = PrefabUtility.FindValidUploadPrefabInstanceRoot(go);

                            GUI.enabled = rootUploadGameObject != null && !AnimationMode.InAnimationMode();

                            if (GUILayout.Button("Apply", "MiniButtonRight"))
                            {
                                Object prefabParent = PrefabUtility.GetPrefabParent(rootUploadGameObject);
                                string prefabAssetPath = AssetDatabase.GetAssetPath(prefabParent);

                                bool editablePrefab = Provider.PromptAndCheckoutIfNeeded(
                                        new string[] {prefabAssetPath},
                                        "The version control requires you to check out the prefab before applying changes.");

                                if (editablePrefab)
                                {
                                    PrefabUtility.ReplacePrefab(rootUploadGameObject, prefabParent, ReplacePrefabOptions.ConnectToPrefab);
                                    CalculatePrefabStatus();

                                    // Preferably we would do
                                    // Undo.RegisterFullObjectHierarchyUndo (prefabParent, "Apply instance to prefab");
                                    // Undo.RegisterFullObjectHierarchyUndo (rootUploadGameObject, "Apply instance to prefab");
                                    // before calling ReplacePrefab in order make the action undoable, but unfortunately this cannot be done
                                    // untill we also make the prefab changes undoable. The problem with the prefab is that Apply might add
                                    // new objects which RegisterFullObjectHierarchyUndo can't handle.
                                    // So for now we simply mark the scene dirty (case 757027)
                                    EditorSceneManager.MarkSceneDirty(rootUploadGameObject.scene);

                                    // This is necessary because ReplacePrefab can potentially destroy game objects and components
                                    // In that case the Editor classes would be destroyed but still be invoked. (case 468434)
                                    GUIUtility.ExitGUI();
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

        public void RevertAndCheckForNewComponents(GameObject gameObject)
        {
            // Take a snapshot of the GO hierarchy before the revert
            var hierarchy = new List<Object>();
            GetObjectListFromHierarchy(hierarchy, gameObject);

            Undo.RegisterFullObjectHierarchyUndo(gameObject, "Revert Prefab Instance");
            PrefabUtility.RevertPrefabInstance(gameObject);
            CalculatePrefabStatus();

            // Take a snapshot of the GO hierarchy after the revert
            var newHierarchy = new List<Object>();
            GetObjectListFromHierarchy(newHierarchy, gameObject);

            // Add RegisterCreatedObjectUndo for any new components added during the revert so that they are removed if undo is triggered
            RegisterNewComponents(newHierarchy, hierarchy);
        }

        private void GetObjectListFromHierarchy(List<Object> hierarchy, GameObject gameObject)
        {
            Transform transform = null;
            List<Component> components = new List<Component>();
            gameObject.GetComponents(components);
            foreach (var component in components)
            {
                if (component is Transform)
                {
                    transform = component as Transform;
                }
                else
                {
                    hierarchy.Add(component);
                }
            }

            if (transform != null)
            {
                int childCount = transform.childCount;
                for (var i = 0; i < childCount; i++)
                {
                    GetObjectListFromHierarchy(hierarchy, transform.GetChild(i).gameObject);
                }
            }
        }

        private void RegisterNewComponents(List<Object> newHierarchy, List<Object> hierarchy)
        {
            var danglingComponents = new List<Component>();

            foreach (var i in newHierarchy)
            {
                var found = false;
                foreach (var j in hierarchy)
                {
                    if (j.GetInstanceID() == i.GetInstanceID())
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    danglingComponents.Add(i as Component);
                }
            }

            // We need to ensure that dangling components are registered in an acceptable order regarding dependencies. For example, if we're adding RigidBody and ConfigurableJoint, the RigidBody will need to be added first (as the ConfigurableJoint depends upon it existing)
            var addedTypes = new HashSet<Type>()
            {
                typeof(Transform)
            };
            var emptyPass = false;
            while (danglingComponents.Count > 0 && !emptyPass)
            {
                emptyPass = true;
                for (var i = 0; i < danglingComponents.Count; i++)
                {
                    var component = danglingComponents[i];
                    var reqs = component.GetType().GetCustomAttributes(typeof(RequireComponent), inherit: true);
                    var requiredComponentsExist = true;
                    foreach (RequireComponent req in reqs)
                    {
                        if ((req.m_Type0 != null && !addedTypes.Contains(req.m_Type0)) || (req.m_Type1 != null && !addedTypes.Contains(req.m_Type1)) || (req.m_Type2 != null && !addedTypes.Contains(req.m_Type2)))
                        {
                            requiredComponentsExist = false;
                            break;
                        }
                    }
                    if (requiredComponentsExist)
                    {
                        Undo.RegisterCreatedObjectUndo(component, "Dangling component");
                        addedTypes.Add(component.GetType());
                        danglingComponents.RemoveAt(i);
                        i--;
                        emptyPass = false;
                    }
                }
            }

            Debug.Assert(danglingComponents.Count == 0, "Dangling components have unfulfilled dependencies");
            foreach (var component in danglingComponents)
            {
                Undo.RegisterCreatedObjectUndo(component, "Dangling component");
            }
        }

        private void DoLayerField(GameObject go)
        {
            EditorGUIUtility.labelWidth = s_Styles.layerFieldWidth;
            Rect layerRect = GUILayoutUtility.GetRect(GUIContent.none, s_Styles.layerPopup);
            EditorGUI.BeginProperty(layerRect, GUIContent.none, m_Layer);
            EditorGUI.BeginChangeCheck();
            int layer = EditorGUI.LayerField(layerRect, EditorGUIUtility.TempContent("Layer"), go.layer, s_Styles.layerPopup);
            if (EditorGUI.EndChangeCheck())
            {
                GameObjectUtility.ShouldIncludeChildren includeChildren = GameObjectUtility.DisplayUpdateChildrenDialogIfNeeded(targets.OfType<GameObject>(),
                        "Change Layer", "Do you want to set layer to " + InternalEditorUtility.GetLayerName(layer) + " for all child objects as well?");
                if (includeChildren != GameObjectUtility.ShouldIncludeChildren.Cancel)
                {
                    m_Layer.intValue = layer;
                    SetLayer(layer, includeChildren == GameObjectUtility.ShouldIncludeChildren.IncludeChildren);
                }
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
            string tag = EditorGUI.TagField(tagRect, EditorGUIUtility.TempContent("Tag"), tagName);
            if (EditorGUI.EndChangeCheck())
            {
                m_Tag.stringValue = tag;
                Undo.RecordObjects(targets, "Change Tag of " + targetTitle);
                foreach (Object obj in targets)
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
            EditorGUI.EnumMaskField(
                GUILayoutUtility.GetRect(GUIContent.none, s_Styles.staticDropdown, GUILayout.ExpandWidth(false)),
                GameObjectUtility.GetStaticEditorFlags(go),
                s_Styles.staticDropdown,
                out changedFlags, out changedToValue
                );
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                SceneModeUtility.SetStaticFlags(targets, changedFlags, changedToValue);
                serializedObject.SetIsDifferentCacheDirty();
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
            }
            EditorGUI.EndProperty();
        }

        Object[] GetObjects(bool includeChildren)
        {
            return SceneModeUtility.GetObjects(targets, includeChildren);
        }

        void SetLayer(int layer, bool includeChildren)
        {
            Object[] objects = GetObjects(includeChildren);
            Undo.RecordObjects(objects, "Change Layer of " + targetTitle);
            foreach (GameObject go in objects)
                go.layer = layer;
        }

        public static void SetEnabledRecursive(GameObject go, bool enabled)
        {
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
                renderer.enabled = enabled;
        }

        public override void ReloadPreviewInstances()
        {
            CreatePreviewInstances();
        }

        void CreatePreviewInstances()
        {
            DestroyPreviewInstances();

            if (m_PreviewInstances == null)
                m_PreviewInstances = new List<GameObject>(targets.Length);

            for (int i = 0; i < targets.Length; ++i)
            {
                GameObject instance = EditorUtility.InstantiateForAnimatorPreview(targets[i]);
                SetEnabledRecursive(instance, false);
                m_PreviewInstances.Add(instance);
            }
        }

        void DestroyPreviewInstances()
        {
            if (m_PreviewInstances == null || m_PreviewInstances.Count == 0)
                return;

            foreach (GameObject instance in m_PreviewInstances)
                Object.DestroyImmediate(instance);
            m_PreviewInstances.Clear();
        }

        void InitPreview()
        {
            if (m_PreviewUtility == null)
            {
                m_PreviewUtility = new PreviewRenderUtility(true);
                m_PreviewUtility.m_CameraFieldOfView = 30.0f;
                m_PreviewUtility.m_Camera.cullingMask = 1 << Camera.PreviewCullingLayer;
                CreatePreviewInstances();
            }
        }

        public void OnDestroy()
        {
            DestroyPreviewInstances();
            if (m_PreviewUtility != null)
            {
                m_PreviewUtility.Cleanup();
                m_PreviewUtility = null;
            }
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
            InitPreview();
        }

        private void DoRenderPreview()
        {
            GameObject go = m_PreviewInstances[referenceTargetIndex];

            Bounds bounds = new Bounds(go.transform.position, Vector3.zero);
            GetRenderableBoundsRecurse(ref bounds, go);
            float halfSize = Mathf.Max(bounds.extents.magnitude, 0.0001f);
            float distance = halfSize * 3.8f;

            Quaternion rot = Quaternion.Euler(-previewDir.y, -previewDir.x, 0);
            Vector3 pos = bounds.center - rot * (Vector3.forward * distance);

            m_PreviewUtility.m_Camera.transform.position = pos;
            m_PreviewUtility.m_Camera.transform.rotation = rot;
            m_PreviewUtility.m_Camera.nearClipPlane = distance - halfSize * 1.1f;
            m_PreviewUtility.m_Camera.farClipPlane = distance + halfSize * 1.1f;

            m_PreviewUtility.m_Light[0].intensity = .7f;
            m_PreviewUtility.m_Light[0].transform.rotation = rot * Quaternion.Euler(40f, 40f, 0);
            m_PreviewUtility.m_Light[1].intensity = .7f;
            m_PreviewUtility.m_Light[1].transform.rotation = rot * Quaternion.Euler(340, 218, 177);
            Color amb = new Color(.1f, .1f, .1f, 0);

            InternalEditorUtility.SetCustomLighting(m_PreviewUtility.m_Light, amb);
            bool oldFog = RenderSettings.fog;
            Unsupported.SetRenderSettingsUseFogNoDirty(false);

            SetEnabledRecursive(go, true);
            m_PreviewUtility.m_Camera.Render();
            SetEnabledRecursive(go, false);

            Unsupported.SetRenderSettingsUseFogNoDirty(oldFog);
            InternalEditorUtility.RemoveCustomLighting();
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            if (!HasStaticPreview() || !ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                return null;
            }

            InitPreview();

            m_PreviewUtility.BeginStaticPreview(new Rect(0, 0, width, height));

            DoRenderPreview();

            return m_PreviewUtility.EndStaticPreview();
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 40), "Preview requires\nrender texture support");
                return;
            }
            InitPreview();

            previewDir = PreviewGUI.Drag2D(previewDir, r);

            if (Event.current.type != EventType.Repaint)
                return;

            m_PreviewUtility.BeginPreview(r, background);

            DoRenderPreview();

            m_PreviewUtility.EndAndDrawPreview(r);
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
                    SceneView.mouseOverWindow.Focus();
                    dragObject.name = uniqueName;
                    dragObject = null;
                    evt.Use();
                    break;
                case EventType.DragExited:
                    if (dragObject)
                    {
                        Object.DestroyImmediate(dragObject, false);
                        HandleUtility.ignoreRaySnapObjects = null;
                        dragObject = null;
                        evt.Use();
                    }
                    break;
            }
        }
    }
}
