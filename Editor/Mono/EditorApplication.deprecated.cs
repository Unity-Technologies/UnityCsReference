// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityEditor
{
    public sealed partial class EditorApplication
    {
        [Obsolete("Use EditorSceneManager.NewScene (NewSceneSetup.DefaultGameObjects)")]
        public static void NewScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
        }

        [Obsolete("Use EditorSceneManager.NewScene (NewSceneSetup.EmptyScene)")]
        public static void NewEmptyScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }

        // Opens the scene at /path/.
        [Obsolete("Use EditorSceneManager.OpenScene")]
        public static bool OpenScene(string path)
        {
            // Check that we're not in play mode first before opening the scene.
            if (!isPlaying)
            {
                Scene scene = EditorSceneManager.OpenScene(path);
                return scene.IsValid();
            }
            else
            {
                throw new InvalidOperationException(
                    "EditorApplication.OpenScene() cannot be called when in the Unity Editor is in play mode.");
            }
        }

        [Obsolete("Use EditorSceneManager.OpenScene")]
        public static void OpenSceneAdditive(string path)
        {
            // Case 712517:
            // Behaviour change introduced in 5.3
            // Previously we allowed OpenSceneAdditive to be called during playmode.
            // This is no longer allowed, if it happens we exit playmode which is
            // consistent with EditorSceneManager.OpenScene(path, OpenSceneMode.Additive)

            if (Application.isPlaying)
            {
                Debug.LogWarning("Exiting playmode.\n" +
                    "OpenSceneAdditive was called at a point where there was no active scene.\n" +
                    "This usually means it was called in a PostprocessScene function during scene loading or it was called during playmode.\n" +
                    "This is no longer allowed. Use SceneManager.LoadScene to load scenes at runtime or in playmode.");
            }

            Scene srcScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Scene dstScene = EditorSceneManager.GetActiveScene();
            SceneManager.MergeScenes(srcScene, dstScene);
        }

        [Obsolete("Use EditorSceneManager.SaveScene")]
        public static bool SaveScene()
        {
            return EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "", false);
        }

        [Obsolete("Use EditorSceneManager.SaveScene")]
        public static bool SaveScene(string path)
        {
            return EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path, false);
        }

        [Obsolete("Use EditorSceneManager.SaveScene")]
        public static bool SaveScene(string path, bool saveAsCopy)
        {
            return EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path, saveAsCopy);
        }

        [Obsolete("Use EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo")]
        public static bool SaveCurrentSceneIfUserWantsTo()
        {
            return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        [Obsolete("This function is internal and no longer supported")]
        static internal bool SaveCurrentSceneIfUserWantsToForce()
        {
            return false;
        }

        [Obsolete("Use EditorSceneManager.MarkSceneDirty or EditorSceneManager.MarkAllScenesDirty")]
        public static void MarkSceneDirty()
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        [Obsolete("Use Scene.isDirty instead. Use EditorSceneManager.GetScene API to get each open scene")]
        public static bool isSceneDirty
        {
            get { return EditorSceneManager.GetActiveScene().isDirty; }
        }

        [Obsolete("Use EditorSceneManager to see which scenes are currently loaded")]
        public static string currentScene
        {
            get
            {
                Scene scene = EditorSceneManager.GetActiveScene();
                if (scene.IsValid())
                    return scene.path;

                return "";
            }
            set
            {
            }
        }
    }
}
