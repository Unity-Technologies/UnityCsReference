// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using UnityEditor.SceneManagement;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class SceneHierarchy
    {
        internal string ActiveScene { get; private set; }
        readonly List<string> m_LoadedScenes = new();
        readonly List<string> m_UnloadedScenes = new();
        internal IReadOnlyCollection<string> LoadedScenes => m_LoadedScenes;
        internal IReadOnlyCollection<string> UnloadedScenes => m_UnloadedScenes;

        internal static SceneHierarchy FromCurrentEditorSceneManager()
        {
            var sceneHierarchy = new SceneHierarchy { ActiveScene = EditorSceneManager.GetActiveScene().path };

            for (var i = 0; i < EditorSceneManager.sceneCount; ++i)
            {
                var scene = EditorSceneManager.GetSceneAt(i);

                if (scene.isLoaded)
                {
                    sceneHierarchy.m_LoadedScenes.Add(scene.path);
                }
                else
                {
                    sceneHierarchy.m_UnloadedScenes.Add(scene.path);
                }
            }

            return sceneHierarchy;
        }

        internal static void Serialize(BinaryWriter writer, in SceneHierarchy sceneHierarchy)
        {
            writer.Write(sceneHierarchy.ActiveScene);

            //Loaded Scenes
            writer.Write(sceneHierarchy.LoadedScenes.Count);
            foreach (var scene in sceneHierarchy.LoadedScenes)
            {
                writer.Write(scene);
            }

            //Unloaded Scenes
            writer.Write(sceneHierarchy.UnloadedScenes.Count);
            foreach (var scene in sceneHierarchy.UnloadedScenes)
            {
                writer.Write(scene);
            }
        }

        internal static bool TryParse(BinaryReader reader, out SceneHierarchy sceneHierarchy)
        {
            sceneHierarchy = new SceneHierarchy { ActiveScene = reader.ReadString() };

            //Loaded Scenes
            var loadedScenesCount = reader.ReadInt32();
            for (var i = 0; i < loadedScenesCount; ++i)
            {
                sceneHierarchy.m_LoadedScenes.Add(reader.ReadString());
            }

            //Unloaded Scenes
            var unloadedScenesCount = reader.ReadInt32();
            for (var i = 0; i < unloadedScenesCount; ++i)
            {
                sceneHierarchy.m_UnloadedScenes.Add(reader.ReadString());
            }

            return !string.IsNullOrWhiteSpace(sceneHierarchy.ActiveScene) && sceneHierarchy.LoadedScenes.Count >= 1;
        }
    }
}
