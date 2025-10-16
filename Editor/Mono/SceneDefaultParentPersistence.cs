// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics.CodeAnalysis;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditorInternal
{
    static class SceneDefaultParents
    {
        const string k_SessionStateKey = "SceneHierarchyWindow_SceneDefaultParents";
        const char k_KeyValueSeparator = ':';
        const char k_EntrySeparator = ';';

        [InitializeOnLoadMethod]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members")]
        static void Initialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredEditMode:
                case PlayModeStateChange.EnteredPlayMode:
                    RestoreSceneDefaultParentsFromSessionState();
                    break;

                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.ExitingPlayMode:
                    StoreSceneDefaultParentsInSessionState();
                    break;

                default:
                    break;
            }
        }

        static void StoreSceneDefaultParentsInSessionState()
        {
            var builder = new StringBuilder();
            for (int i = 0, c = EditorSceneManager.sceneCount; i < c; ++i)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.defaultParent != EntityId.None)
                {
                    if (builder.Length > 0)
                        builder.Append(k_EntrySeparator);

                    builder.Append($"{scene.handle}{k_KeyValueSeparator}{scene.defaultParent}");
                }
            }

            var sceneDefaultParents = builder.ToString();
            if (!string.IsNullOrEmpty(sceneDefaultParents))
                SessionState.SetString(k_SessionStateKey, sceneDefaultParents);
            else
                SessionState.EraseString(k_SessionStateKey);
        }

        static void RestoreSceneDefaultParentsFromSessionState()
        {
            var sceneDefaultParents = SessionState.GetString(k_SessionStateKey, string.Empty);
            if (string.IsNullOrEmpty(sceneDefaultParents))
                return;

            var entries = sceneDefaultParents.Split(k_EntrySeparator);
            foreach (var entry in entries)
            {
                var kvp = entry.Split(k_KeyValueSeparator);
                if (kvp.Length != 2)
                    continue;

                // Try get scene
                var sceneEntityId = EntityId.Parse(kvp[0]);
                if (sceneEntityId == EntityId.None)
                    continue;

                var sceneHandle = SceneHandle.From(sceneEntityId);
                if (sceneHandle == SceneHandle.None)
                    continue;

                var scene = EditorSceneManager.GetSceneByHandle(sceneHandle);
                if (!scene.IsValid())
                    continue;

                // Try get default parent
                var parentEntityId = EntityId.Parse(kvp[1]);
                if (parentEntityId == EntityId.None)
                    continue;

                var parentObject = EditorUtility.EntityIdToObject(parentEntityId);
                if (!parentObject || parentObject == null)
                    continue;

                // Apply default parent to scene
                scene.defaultParent = parentEntityId;
            }
        }
    }
}
