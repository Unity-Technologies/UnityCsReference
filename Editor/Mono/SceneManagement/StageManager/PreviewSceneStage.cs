// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.SceneManagement
{
    public abstract class PreviewSceneStage : Stage
    {
        Scene m_Scene;
        public Scene scene { get { return m_Scene; } protected set { m_Scene = value; } }

        internal override int sceneCount { get { return 1; } }

        internal override Scene GetSceneAt(int index)
        {
            if (index != 0)
                throw new IndexOutOfRangeException();
            return m_Scene;
        }

        protected PreviewSceneStage()
        {
        }

        protected internal override bool OnOpenStage()
        {
            m_Scene = EditorSceneManager.NewPreviewScene();
            return true;
        }

        protected override void OnCloseStage()
        {
            if (scene.IsValid())
            {
                Undo.ClearUndoSceneHandle(scene);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        internal override void PlaceGameObjectInStage(GameObject rootGameObject)
        {
            if (this != null && scene.IsValid())
                SceneManager.MoveGameObjectToScene(rootGameObject, scene);
        }

        internal override void SyncSceneHierarchyToStage(SceneHierarchyWindow sceneHierarchyWindow)
        {
            var sceneHierarchy = sceneHierarchyWindow.sceneHierarchy;
            sceneHierarchy.customScenes = new[] { scene };
        }

        public override StageHandle stageHandle
        {
            get { return StageHandle.GetStageHandle(scene); }
        }

        internal override ulong GetSceneCullingMask()
        {
            return EditorSceneManager.GetSceneCullingMask(scene);
        }

        internal override void SyncSceneViewToStage(SceneView sceneView)
        {
            sceneView.customScene = scene;
            sceneView.overrideSceneCullingMask = 0;
        }
    }
}
