// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.Multiplayer.PlayMode.Editor
{
    // Collects the scenes that clones need to reload from disk, so that one change only produces one reload per scene.
    //
    // A scene file can change on disk in two ways:
    //  - The main editor saves it. The save imports the scene asset and then raises EditorSceneManager.sceneSaved.
    //  - Something outside the editor (for example an external AI agent or a VCS update) rewrites the file. The main
    //    editor only notices this when the asset gets imported, and sceneSaved is never raised.
    // Both sources are queued here. A save raises both within the same editor update, so they are merged into a
    // single request when the queue is flushed on the next delayCall.
    class SceneReloadQueue
    {
        const string k_SceneExtension = ".unity";

        readonly List<string> m_PendingScenes = new();

        internal int PendingCount => m_PendingScenes.Count;

        // Returns true when the path was added. The caller re-arms the flush on every successful add, so a flush
        // that got dropped (for example because another delayCall handler threw) can never leave the queue stuck.
        internal bool Enqueue(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath) || ContainsPath(m_PendingScenes, scenePath))
            {
                return false;
            }

            m_PendingScenes.Add(scenePath);
            return true;
        }

        // Hands out the pending scenes and empties the queue, unless a flush has to wait: while Play Mode is active or
        // about to start, and while the editor compiles, the queue is kept and false is returned.
        // Scenes the main editor no longer has loaded are dropped. A scene loaded at runtime during Play Mode can be
        // queued, but after Play Mode the main editor is back on its edit mode scenes. Sending that scene would make a
        // clone with a single scene open it instead of the scene the main editor returned to.
        internal bool TryFlush(bool isPlayingOrWillChangePlaymode, bool isCompiling, Func<string, bool> isSceneLoaded, out List<string> scenes)
        {
            scenes = new List<string>();
            if (isPlayingOrWillChangePlaymode || isCompiling)
            {
                return false;
            }

            foreach (var scenePath in m_PendingScenes)
            {
                if (isSceneLoaded(scenePath))
                {
                    scenes.Add(scenePath);
                }
            }

            m_PendingScenes.Clear();
            return scenes.Count > 0;
        }

        // The pending scenes are kept in SessionState across domain reloads. Asset paths never contain new lines.
        internal string Serialize() => string.Join("\n", m_PendingScenes);

        internal void Restore(string serialized)
        {
            if (string.IsNullOrEmpty(serialized))
            {
                return;
            }

            foreach (var scenePath in serialized.Split('\n'))
            {
                Enqueue(scenePath);
            }
        }

        // Returns the imported assets that are scenes currently loaded in the editor, in import order.
        internal static List<string> GetImportedLoadedScenes(IEnumerable<string> importedAssets, Func<string, bool> isSceneLoaded)
        {
            var scenes = new List<string>();
            if (importedAssets == null || isSceneLoaded == null)
            {
                return scenes;
            }

            foreach (var assetPath in importedAssets)
            {
                if (string.IsNullOrEmpty(assetPath)
                    || !assetPath.EndsWith(k_SceneExtension, StringComparison.OrdinalIgnoreCase)
                    || ContainsPath(scenes, assetPath)
                    || !isSceneLoaded(assetPath))
                {
                    continue;
                }

                scenes.Add(assetPath);
            }

            return scenes;
        }

        static bool ContainsPath(IEnumerable<string> paths, string path)
        {
            foreach (var p in paths)
            {
                if (string.Equals(p, path, StringComparison.Ordinal)) return true;
            }

            return false;
        }
    }
}
