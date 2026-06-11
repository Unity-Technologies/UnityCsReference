// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    /// <summary>
    /// Clears duplicate dictionary serialization cache when hosts are destroyed. Uses <see cref="ObjectChangeEvents"/>
    /// for undo-recorded destroys, <see cref="EditorSceneManager.sceneClosed"/> when a scene is closed in the Editor
    /// (including <see cref="EditorSceneManager.CloseScene"/> in Edit Mode), and <see cref="SceneManager.sceneUnloaded"/>
    /// for runtime unloads (e.g. Play Mode). Prunes map entries whose host is no longer in memory.
    /// </summary>
    // 'partial' is required by the [OnCodeLoaded] source generator (UAC0031); the
    // generated companion holds the registration that wires Initialize() into the
    // lifecycle pipeline.
    internal static partial class DictionarySerializationDuplicateEntriesCleanup
    {
        // [OnCodeLoaded] runs before SerializableManagedRefsUtilities::RestoreBackups, ensuring the
        // managed cache is non-null when restored duplicate dictionary entries are written into it.
        // The previous [InitializeOnLoad] hook ran *after* RestoreBackups and silently dropped those
        // entries on every script-recompile domain reload.
        [OnCodeLoaded]
        static void Initialize()
        {
            EnsureDuplicateEntriesCacheInitialized();
            ObjectChangeEvents.changesPublished += OnObjectChanges;
            EditorSceneManager.sceneClosed += OnEditorSceneClosed;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        /// <summary>
        /// Constructs the editor-only duplicate-entry storage and assigns it to the runtime field.
        /// Idempotent: re-entry leaves the existing instance untouched.
        /// </summary>
        internal static void EnsureDuplicateEntriesCacheInitialized()
        {
            if (DictionarySerialization.s_DuplicateEntriesForDictionaries != null)
                return;
            // Runtime does not construct storage (player stays null); wire the editor-only implementation here so edit mode and play mode in the Editor preserve duplicate dictionary rows.
            DictionarySerialization.s_DuplicateEntriesForDictionaries = new DuplicateEntriesForDictionaries();
        }

        static void OnObjectChanges(ref ObjectChangeEventStream stream)
        {
            bool sawDestroy = false;
            for (int i = 0; i < stream.length; ++i)
            {
                ObjectChangeKind kind = stream.GetEventType(i);
                if (kind == ObjectChangeKind.DestroyGameObjectHierarchy || kind == ObjectChangeKind.DestroyAssetObject)
                    sawDestroy = true;
            }

            if (!sawDestroy)
                return;

            PruneStaleDuplicateHosts();
        }

        static void OnEditorSceneClosed(Scene scene) => PruneAfterSceneRemovedFromHierarchyIfNeeded();

        static void OnSceneUnloaded(Scene scene) => PruneAfterSceneRemovedFromHierarchyIfNeeded();

        static void PruneAfterSceneRemovedFromHierarchyIfNeeded()
        {
            if (!DictionarySerialization.HasAnyCachedDuplicateDictionaryHosts())
                return;

            PruneStaleDuplicateHosts();
        }

        static void PruneStaleDuplicateHosts()
        {
            DictionarySerialization.PruneDuplicateDictionaryEntriesForUnloadedHosts();
        }
    }
}
