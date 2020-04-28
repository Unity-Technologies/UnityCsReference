// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.SceneManagement
{
    [Serializable]
    internal class AvatarConfigurationStage : PreviewSceneStage
    {
        AvatarEditor m_AvatarEditor;

        string m_AssetPath;
        public override string assetPath { get { return m_AssetPath; } }

        GameObject m_GameObject;
        public GameObject gameObject { get { return m_GameObject; } }

        internal static AvatarConfigurationStage CreateStage(string assetPath, AvatarEditor avatarEditor)
        {
            AvatarConfigurationStage stage = CreateInstance<AvatarConfigurationStage>();
            stage.Init(assetPath, avatarEditor);
            return stage;
        }

        private void Init(string modelAssetPath, AvatarEditor avatarEditor)
        {
            m_AssetPath = modelAssetPath;
            m_AvatarEditor = avatarEditor;
        }

        protected internal override bool OnOpenStage()
        {
            base.OnOpenStage();

            if (!File.Exists(assetPath))
            {
                Debug.LogError("ActivateStage called on AvatarEditingStage with an invalid path: Model file not found " + assetPath);
                return false;
            }

            // Get Model Prefab
            GameObject prefab = AssetDatabase.LoadMainAssetAtPath(assetPath) as GameObject;
            if (prefab == null)
                return false;

            // Instantiate character
            m_GameObject = Instantiate(prefab) as GameObject;
            SceneManager.MoveGameObjectToScene(m_GameObject, scene);

            return true;
        }

        protected override void OnCloseStage()
        {
            m_AvatarEditor.CleanupEditor();

            base.OnCloseStage();
        }

        protected internal override void OnFirstTimeOpenStageInSceneView(SceneView sceneView)
        {
            Selection.activeObject = m_GameObject;

            // Frame in scene view
            sceneView.FrameSelected(false, true);

            // Setup Scene view state
            sceneView.sceneViewState.showFlares = false;
            sceneView.sceneViewState.alwaysRefresh = false;
            sceneView.sceneViewState.showFog = false;
            sceneView.sceneViewState.showSkybox = false;
            sceneView.sceneViewState.showImageEffects = false;
            sceneView.sceneViewState.showParticleSystems = false;
            sceneView.sceneLighting = false;
        }

        protected internal override GUIContent CreateHeaderContent()
        {
            return new GUIContent(
                "Avatar Configuration",
                EditorGUIUtility.IconContent("Avatar Icon").image);
        }
    }
}
