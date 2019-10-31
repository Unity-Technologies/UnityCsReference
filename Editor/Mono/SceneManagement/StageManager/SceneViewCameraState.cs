// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    [Serializable]
    class SceneViewCameraState
    {
        public SceneView.CameraMode cameraMode;
        public bool sceneLighting;
        public bool audioPlay;
        public SceneView.SceneViewState sceneViewState;
        public bool in2DMode;
        public Vector3 pivot;
        public Quaternion rotation;
        public float size;
        public bool orthographic;

        public void SaveStateFromSceneView(SceneView view)
        {
            cameraMode = view.cameraMode;
            sceneLighting = view.sceneLighting;
            audioPlay = view.audioPlay;
            sceneViewState = new SceneView.SceneViewState(view.sceneViewState);
            in2DMode = view.in2DMode;
            pivot = view.pivot;
            rotation = view.rotation;
            size = view.size;
            orthographic = view.orthographic;
        }

        public void RestoreStateToSceneView(SceneView view)
        {
            view.cameraMode = cameraMode;
            view.sceneLighting = sceneLighting;
            view.audioPlay = audioPlay;
            view.sceneViewState = new SceneView.SceneViewState(sceneViewState);
            view.in2DMode = in2DMode;
            view.pivot = pivot;
            view.rotation = rotation;
            view.size = size;
            view.orthographic = orthographic;
            view.SkipFading();
        }
    }
}
