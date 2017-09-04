// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    [System.Serializable]
    internal class AvatarSubEditor : ScriptableObject
    {
        /*
        // Will be used to patch animation when handiness changes.
        public class AvatarSetter : AssetPostprocessor
        {
            public void OnPostprocessModel(GameObject go)
            {
                ModelImporter modelImporter = (ModelImporter)assetImporter;
                ModelImporterEditor inspector = ActiveEditorTracker.MakeCustomEditor(modelImporter) as ModelImporterEditor;

                SerializedProperty humanDescription = inspector.serializedObject.FindProperty("m_HumanDescription");

                Avatar avatar = AssetDatabase.LoadAssetAtPath("Assets/1_Characters/Dude/Dude.fbx", typeof(UnityEngine.Avatar)) as Avatar;
                if (avatar == null)
                    Debug.Log("Could not find avatar when importing : " + modelImporter.assetPath);

                if (avatar != null && modelImporter != null)
                    modelImporter.UpdateHumanDescription(avatar, humanDescription);

                EditorUtility.SetDirty(inspector);
                EditorUtility.SetDirty(modelImporter);
            }
        }
        */

        //[MenuItem ("Mecanim/Write All Assets")]
        static void DoWriteAllAssets()
        {
            UnityEngine.Object[] objects = Resources.FindObjectsOfTypeAll(typeof(UnityEngine.Object));
            foreach (UnityEngine.Object asset in objects)
            {
                if (AssetDatabase.Contains(asset))
                    EditorUtility.SetDirty(asset);
            }
            AssetDatabase.SaveAssets();
        }

        protected AvatarEditor m_Inspector;
        protected GameObject gameObject { get { return m_Inspector.m_GameObject; } }
        protected GameObject prefab { get { return m_Inspector.prefab; } }
        protected Dictionary<Transform, bool> modelBones { get { return m_Inspector.m_ModelBones; } }
        protected Transform root { get { return gameObject == null ? null : gameObject.transform; } }
        protected SerializedObject serializedObject { get { return m_Inspector.serializedObject; } }
        protected Avatar avatarAsset { get { return m_Inspector.avatar; } }

        public virtual void Enable(AvatarEditor inspector)
        {
            this.m_Inspector = inspector;
        }

        public virtual void Disable()
        {
        }

        public virtual void OnDestroy()
        {
            if (HasModified())
            {
                AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(avatarAsset));
                if (importer)
                {
                    if (EditorUtility.DisplayDialog("Unapplied import settings", "Unapplied import settings for \'" + importer.assetPath + "\'", "Apply", "Revert"))
                        ApplyAndImport();
                    else
                        ResetValues();
                }
            }
        }

        public virtual void OnInspectorGUI()
        {
        }

        public virtual void OnSceneGUI()
        {
        }

        protected bool HasModified()
        {
            if (serializedObject.hasModifiedProperties)
                return true;

            return false;
        }

        protected virtual void ResetValues()
        {
            serializedObject.Update();
        }

        protected void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }

        public void ApplyAndImport()
        {
            Apply();

            string assetPath = AssetDatabase.GetAssetPath(avatarAsset);
            AssetDatabase.ImportAsset(assetPath);

            ResetValues();
        }

        protected void ApplyRevertGUI()
        {
            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(!HasModified()))
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Revert"))
                {
                    ResetValues();
                    if (HasModified())
                        Debug.LogError("Avatar tool reports modified values after reset.");
                }

                if (GUILayout.Button("Apply"))
                {
                    ApplyAndImport();
                }
            }

            if (GUILayout.Button("Done"))
            {
                m_Inspector.SwitchToAssetMode();
                GUIUtility.ExitGUI();
            }

            GUILayout.EndHorizontal();
        }
    }

    [CustomEditor(typeof(Avatar))]
    internal class AvatarEditor : Editor
    {
        private class Styles
        {
            public GUIContent[] tabs =
            {
                EditorGUIUtility.TextContent("Mapping"),
                EditorGUIUtility.TextContent("Muscles & Settings"),
                //EditorGUIUtility.TextContent ("Handle"),
                //EditorGUIUtility.TextContent ("Collider")
            };

            public GUIContent editCharacter = EditorGUIUtility.TextContent("Configure Avatar");

            public GUIContent reset = EditorGUIUtility.TextContent("Reset");
        }

        static Styles styles { get { if (s_Styles == null) s_Styles = new Styles(); return s_Styles; } }
        static Styles s_Styles;

        enum EditMode
        {
            NotEditing,
            Starting,
            Editing,
            Stopping
        }

        protected int m_TabIndex;
        internal GameObject m_GameObject;
        internal Dictionary<Transform, bool> m_ModelBones = null;
        private EditMode m_EditMode = EditMode.NotEditing;
        internal bool m_CameFromImportSettings = false;
        private bool m_SwitchToEditMode = false;
        internal static bool s_EditImmediatelyOnNextOpen = false;

        private SceneSetup[] sceneSetup;

        // These member are used when the avatar is part of an asset
        internal Avatar avatar { get { return target as Avatar; } }

        protected bool m_InspectorLocked;

        [Serializable]
        protected class SceneStateCache
        {
            public SceneView view;
            public SceneView.SceneViewState state;
        }

        protected List<SceneStateCache> m_SceneStates;

        // We'd prefer to have just one AvatarEditor member but due to Serialization issues,
        // we need to keep one of each type. It should still be treated as a single one though.
        // I.e. only one should be used at a time; the rest should be null.
        protected AvatarSubEditor editor
        {
            get
            {
                switch (m_TabIndex)
                {
                    case sMuscleTab: return m_MuscleEditor;
                    case sHandleTab: return m_HandleEditor;
                    case sColliderTab: return m_ColliderEditor;
                    default:
                    case sMappingTab: return m_MappingEditor;
                }
            }
            set
            {
                switch (m_TabIndex)
                {
                    case sMuscleTab: m_MuscleEditor = value as AvatarMuscleEditor; break;
                    case sHandleTab: m_HandleEditor = value as AvatarHandleEditor; break;
                    case sColliderTab: m_ColliderEditor = value as AvatarColliderEditor; break;
                    default:
                    case sMappingTab: m_MappingEditor = value as AvatarMappingEditor; break;
                }
            }
        }
        private AvatarMuscleEditor m_MuscleEditor;
        private AvatarHandleEditor m_HandleEditor;
        private AvatarColliderEditor m_ColliderEditor;
        private AvatarMappingEditor m_MappingEditor;

        const int sMappingTab = 0;
        const int sMuscleTab = 1;
        const int sHandleTab = 2;
        const int sColliderTab = 3;
        const int sDefaultTab = sMappingTab;

        public GameObject prefab
        {
            get
            {
                string path = AssetDatabase.GetAssetPath(target);
                return AssetDatabase.LoadMainAssetAtPath(path) as GameObject;
            }
        }

        internal override SerializedObject GetSerializedObjectInternal()
        {
            if (m_SerializedObject == null)
                m_SerializedObject = SerializedObject.LoadFromCache(GetInstanceID());
            // Override to make serializedObject be the model importer instead of being the avatar
            if (m_SerializedObject == null)
                m_SerializedObject = new SerializedObject(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(target)));
            return m_SerializedObject;
        }

        void OnEnable()
        {
            EditorApplication.update += Update;
            m_SwitchToEditMode = false;
            if (m_EditMode == EditMode.Editing)
            {
                m_ModelBones = AvatarSetupTool.GetModelBones(m_GameObject.transform, false, null);
                editor.Enable(this);
            }
            else if (m_EditMode == EditMode.NotEditing)
            {
                editor = null;

                if (s_EditImmediatelyOnNextOpen)
                {
                    m_CameFromImportSettings = true;
                    s_EditImmediatelyOnNextOpen = false;
                }
            }
        }

        void OnDisable()
        {
            if (m_EditMode == EditMode.Editing)
                editor.Disable();

            EditorApplication.update -= Update;
            if (m_SerializedObject != null)
            {
                m_SerializedObject.Cache(GetInstanceID());
                m_SerializedObject = null;
            }
        }

        void OnDestroy()
        {
            // If we are in Edit mode, we need to switch back to asset mode first
            if (m_EditMode == EditMode.Editing)
                SwitchToAssetMode();
        }

        void SelectAsset()
        {
            UnityEngine.Object obj;
            if (m_CameFromImportSettings)
            {
                string path = AssetDatabase.GetAssetPath(target);
                obj = AssetDatabase.LoadMainAssetAtPath(path);
            }
            else
                obj = target;

            Selection.activeObject = obj;
        }

        protected void CreateEditor()
        {
            switch (m_TabIndex)
            {
                case sMuscleTab: editor = ScriptableObject.CreateInstance<AvatarMuscleEditor>(); break;
                case sHandleTab: editor = ScriptableObject.CreateInstance<AvatarHandleEditor>(); break;
                case sColliderTab: editor = ScriptableObject.CreateInstance<AvatarColliderEditor>(); break;
                default:
                case sMappingTab: editor = ScriptableObject.CreateInstance<AvatarMappingEditor>(); break;
            }

            editor.hideFlags = HideFlags.HideAndDontSave;
            editor.Enable(this);
        }

        protected void DestroyEditor()
        {
            editor.OnDestroy();
            editor = null;
        }

        // @TODO@MECANIM: Implement context Reset - probably best in C++
        /*[MenuItem ("CONTEXT/Avatar/Reset")]
        static void ResetValues (MenuCommand command)
        {
            Debug.Log ("Reset");

            AssetImporter importer = AssetImporter.GetAtPath (AssetDatabase.GetAssetPath (command.context));
            SerializedObject serializedObject = new SerializedObject (importer);

            if (importer && serializedObject != null)
            {
                string sHuman = "m_HumanDescription.m_Human";
                string sSkeleton = "m_HumanDescription.m_Skeleton";
                SerializedProperty human = serializedObject.FindProperty (sHuman);
                if (human != null && human.isArray)
                    human.ClearArray ();

                SerializedProperty skeleton = serializedObject.FindProperty (sSkeleton);
                if (skeleton != null && skeleton.isArray)
                    skeleton.ClearArray ();

                if (GetCurrentEditor () != null)
                {
                    GetCurrentEditor ().ApplyAndImport ();
                    GetCurrentEditor ().OnEnable (this);
                }
            }
        }*/

        public override bool UseDefaultMargins() { return false; }

        public override void OnInspectorGUI()
        {
            GUI.enabled = true;

            EditorGUILayout.BeginVertical(EditorStyles.inspectorFullWidthMargins);

            if (m_EditMode == EditMode.Editing)
            {
                EditingGUI();
            }
            else if (!m_CameFromImportSettings)
            {
                EditButtonGUI();
            }
            else
            {
                if (m_EditMode == EditMode.NotEditing && Event.current.type == EventType.Repaint)
                {
                    m_SwitchToEditMode = true;
                }
            }

            EditorGUILayout.EndVertical();
        }

        void EditButtonGUI()
        {
            if (avatar == null || !avatar.isHuman)
                return;

            // Can only edit avatar from a model importer
            string assetPath = AssetDatabase.GetAssetPath(avatar);
            ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (modelImporter == null)
                return;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(styles.editCharacter, GUILayout.Width(120)) &&
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                SwitchToEditMode();
                GUIUtility.ExitGUI();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        void EditingGUI()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            {
                int tabIndex = m_TabIndex;
                bool wasEnable = GUI.enabled;
                GUI.enabled = !(avatar == null || !avatar.isHuman);
                tabIndex = GUILayout.Toolbar(tabIndex, styles.tabs, "LargeButton", GUI.ToolbarButtonSize.FitToContents);
                GUI.enabled = wasEnable;
                if (tabIndex != m_TabIndex)
                {
                    DestroyEditor();
                    if (avatar != null && avatar.isHuman)
                        m_TabIndex = tabIndex;

                    CreateEditor();
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            editor.OnInspectorGUI();
        }

        public void OnSceneGUI()
        {
            if (m_EditMode == EditMode.Editing)
                editor.OnSceneGUI();
        }

        internal void SwitchToEditMode()
        {
            m_EditMode = EditMode.Starting;

            // Lock inspector
            ChangeInspectorLock(true);

            // Store current setup in hierarchy
            sceneSetup = EditorSceneManager.GetSceneManagerSetup();

            // Load temp scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
            scene.name = "Avatar Configuration";

            // Instantiate character
            m_GameObject = Instantiate(prefab) as GameObject;
            if (serializedObject.FindProperty("m_OptimizeGameObjects").boolValue)
                AnimatorUtility.DeoptimizeTransformHierarchy(m_GameObject);

            Animator animator = m_GameObject.GetComponent<Animator>();

            if (animator != null && animator.runtimeAnimatorController == null)
            {
                Animations.AnimatorController controller = new Animations.AnimatorController();
                controller.hideFlags = HideFlags.DontSave;
                controller.AddLayer("preview");
                controller.layers[0].stateMachine.hideFlags = HideFlags.DontSave;
                animator.runtimeAnimatorController = controller;
            }

            // First get all available modelBones
            Dictionary<Transform, bool> modelBones = AvatarSetupTool.GetModelBones(m_GameObject.transform, true, null);
            AvatarSetupTool.BoneWrapper[] humanBones = AvatarSetupTool.GetHumanBones(serializedObject, modelBones);

            m_ModelBones = AvatarSetupTool.GetModelBones(m_GameObject.transform, false, humanBones);

            Selection.activeObject = m_GameObject;

            // Unfold all nodes in hierarchy
            // TODO@MECANIM: Only expand actual bones
            foreach (SceneHierarchyWindow shw in Resources.FindObjectsOfTypeAll(typeof(SceneHierarchyWindow)))
                shw.SetExpandedRecursive(m_GameObject.GetInstanceID(), true);
            CreateEditor();

            m_EditMode = EditMode.Editing;

            // Frame in scene view
            m_SceneStates = new List<SceneStateCache>();
            foreach (SceneView s in SceneView.sceneViews)
            {
                m_SceneStates.Add(new SceneStateCache {state = new SceneView.SceneViewState(s.sceneViewState), view = s});
                s.sceneViewState.showFlares = false;
                s.sceneViewState.showMaterialUpdate = false;
                s.sceneViewState.showFog = false;
                s.sceneViewState.showSkybox = false;
                s.sceneViewState.showImageEffects = false;
                s.sceneViewState.showParticleSystems = false;
                s.FrameSelected();
            }
        }

        internal void SwitchToAssetMode()
        {
            foreach (var state in m_SceneStates)
            {
                if (state.view == null)
                    continue;

                state.view.sceneViewState.showFog = state.state.showFog;
                state.view.sceneViewState.showFlares = state.state.showFlares;
                state.view.sceneViewState.showMaterialUpdate = state.state.showMaterialUpdate;
                state.view.sceneViewState.showSkybox = state.state.showSkybox;
                state.view.sceneViewState.showImageEffects = state.state.showImageEffects;
                state.view.sceneViewState.showParticleSystems = state.state.showParticleSystems;
            }

            m_EditMode = EditMode.Stopping;

            DestroyEditor();

            ChangeInspectorLock(m_InspectorLocked);

            // TODO@sonny cannot call newscene or openscene when editor is updating the asset database
            // it can occur when the user is currently editing an avatar and delete the asset file containing this avatar
            // Also cannot call EditorApplication.NewScene(); when editor is been destroy when user click on unlock inspector because it does delete the inspector twice
            if (!EditorApplication.isUpdating && !Unsupported.IsDestroyScriptableObject(this))
            {
                string currentScene = SceneManager.GetActiveScene().path;
                if (currentScene.Length > 0)
                {
                    // in this case the user did save manually the current scene and want to keep it or
                    // he did open a new scene
                    // do nothing
                }
                else if (sceneSetup != null && sceneSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
                    sceneSetup = null;
                }
                else
                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
            }
            else if (Unsupported.IsDestroyScriptableObject(this))
            {
                EditorApplication.CallbackFunction CleanUpSceneOnDestroy = null;
                CleanUpSceneOnDestroy = () =>
                    {
                        string currentScene = SceneManager.GetActiveScene().path;
                        if (currentScene.Length > 0)
                        {
                            // in this case the user did save manually the current scene and want to keep it or
                            // he did open a new scene
                            // do nothing
                        }
                        else if (sceneSetup != null && sceneSetup.Length > 0)
                        {
                            EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
                            sceneSetup = null;
                        }
                        else
                            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);

                        EditorApplication.update -= CleanUpSceneOnDestroy;
                    };

                EditorApplication.update += CleanUpSceneOnDestroy;
            }

            m_GameObject = null;
            m_ModelBones = null;

            SelectAsset();

            if (!m_CameFromImportSettings)
                m_EditMode = EditMode.NotEditing;
        }

        void ChangeInspectorLock(bool locked)
        {
            foreach (InspectorWindow i in InspectorWindow.GetAllInspectorWindows())
            {
                ActiveEditorTracker activeEditor = i.tracker;
                foreach (Editor e in activeEditor.activeEditors)
                {
                    if (e == this)
                    {
                        m_InspectorLocked = i.isLocked;
                        i.isLocked = locked;
                    }
                }
            }
        }

        public void Update()
        {
            if (m_SwitchToEditMode)
            {
                m_SwitchToEditMode = false;
                SwitchToEditMode();

                Repaint();
            }

            if (m_EditMode == EditMode.Editing)
            {
                if (m_GameObject == null || m_ModelBones == null || EditorApplication.isPlaying)
                    SwitchToAssetMode();

                else if (m_ModelBones != null)
                {
                    foreach (KeyValuePair<Transform, bool> pair in m_ModelBones)
                    {
                        if (pair.Key == null)
                        {
                            SwitchToAssetMode();
                            return;
                        }
                    }
                }
            }
        }

        public bool HasFrameBounds()
        {
            if (m_ModelBones != null)
            {
                foreach (KeyValuePair<Transform, bool> pair in m_ModelBones)
                {
                    if (pair.Key == Selection.activeTransform)
                        return true;
                }
            }

            return false;
        }

        public Bounds OnGetFrameBounds()
        {
            Transform bone = Selection.activeTransform;
            Bounds bounds = new Bounds(bone.position, new Vector3(0, 0, 0));
            foreach (Transform child in bone)
                bounds.Encapsulate(child.position);

            if (bone.parent) bounds.Encapsulate(bone.parent.position);

            return bounds;
        }
    }
}
