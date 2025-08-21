// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class SceneEvents
    {
        public event Action<SceneHierarchy> SceneHierarchyChanged;
        public event Action<string> SceneSaved;

        internal void InvokeSceneHierarchyChanged(SceneHierarchy sceneHierarchy)
        {
            SceneHierarchyChanged?.Invoke(sceneHierarchy);
        }

        internal void InvokeSceneSaved(string savedScene)
        {
            SceneSaved?.Invoke(savedScene);
        }
    }
}
