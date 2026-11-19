// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Collections.Generic;
using Unity.Multiplayer.PlayMode.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class MainEditorSystems
    {
        internal AssetImportEvents AssetImportEvents { get; }
        internal SceneEvents SceneEvents { get; }

        // Compilation usually ends in a domain reload, which creates a new MainEditorSystems and loses the queue.
        // The pending scenes are therefore kept in SessionState and restored in Listen.
        const string k_PendingSceneReloadsKey = "MPPM.MainEditorSystems.PendingSceneReloads";

        readonly SceneReloadQueue m_SceneReloadQueue = new();

        internal MainEditorSystems()
        {
            AssetImportEvents = new AssetImportEvents();
            SceneEvents = new SceneEvents();
        }

        internal void Listen(MainEditorContext vpContext)
        {
            /*
             * These system classes are simply an aggregation of logic and other events
             *
             * Its only purpose is to forward events to the Internal Runtimes, Workflows, and MultiplayerPlaymode (UI)
             */
            EditorApplication.update += () =>
            {
                vpContext.MessagingService.HandleUpdate();
            };
            EditorApplication.quitting += () =>
            {
                // :ApplicationEvent :Quit
                // The Main Editor fundamentally closes all its child editors
                // since it coordinates Asset Syncing at the API level
                foreach (var project in vpContext.VirtualProjectsApi.GetProjectsFunc(VirtualProjectsApi.k_FilterAll))
                {
                    project.Close(out _);
                }
            };

            AssetDatabaseCallbacks.OnPostprocessAllAssetsCallback += (didDomainReload, numAssetsChanged) =>
            {
                // If the main editor is still compiling, there will be another refresh/domain reload when it’s complete.
                // So we won’t bother getting the clone to do an intermediate refresh that is likely to have out of date assets that will trigger the assert message.
                if (!EditorApplication.isCompiling)
                    AssetImportEvents.InvokeRequestImport(didDomainReload, numAssetsChanged);
            };

            vpContext.MessagingService.Receive<PlayerInitializedMessage>(message =>
            {
                if (!vpContext.StateRepository.ContainsKey(message.Identifier))
                {
                    var state = new VirtualProjectStatePerProcessLifetime
                    {
                        IsCommunicative = true,
                    };
                    vpContext.StateRepository.Create(message.Identifier, state);
                }
                else
                {
                    vpContext.StateRepository.Update(
                        message.Identifier,
                        state =>
                        {
                            state.Retry = 0;
                            state.IsCommunicative = true;
                        },
                        out _);
                }
            });

            EditorSceneManager.activeSceneChangedInEditMode += (_, _) =>
            {
                EditorApplication.delayCall += () =>
                {
                    SceneEvents.InvokeSceneHierarchyChanged(SceneHierarchy.FromCurrentEditorSceneManager());
                };
            };

            EditorSceneManager.sceneClosed += _ =>
            {
                if (!EditorApplication.isPlaying)
                {
                    EditorApplication.delayCall += () =>
                    {
                        SceneEvents.InvokeSceneHierarchyChanged(SceneHierarchy.FromCurrentEditorSceneManager());
                    };
                }
            };

            EditorSceneManager.sceneOpened += (_, _) =>
            {
                if (!EditorApplication.isPlaying)
                {
                    EditorApplication.delayCall += () =>
                    {
                        SceneEvents.InvokeSceneHierarchyChanged(SceneHierarchy.FromCurrentEditorSceneManager());
                    };
                }
            };

            EditorSceneManager.sceneSaved += scene => QueueSceneReload(scene.path);

            // A scene can also be changed on disk without the main editor saving it, for example when an external
            // AI agent rewrites the scene file. sceneSaved is not raised in that case, so the clones would keep showing
            // the old scene. Any loaded scene that gets reimported is sent to the clones the same way as a save.
            AssetDatabaseCallbacks.OnImportedAssetsCallback += importedAssets =>
            {
                // Auto refresh also imports during Play Mode and while compiling. The flush waits for both to end.
                var loadedScenes = new HashSet<string>(SceneHierarchy.FromCurrentEditorSceneManager().LoadedScenes, StringComparer.Ordinal);
                foreach (var scenePath in SceneReloadQueue.GetImportedLoadedScenes(importedAssets, loadedScenes.Contains))
                {
                    QueueSceneReload(scenePath);
                }
            };

            // A flush deferred by Play Mode is retried when Play Mode ends. EditorApplication is static, but a domain
            // reload drops this handler together with this instance, and Listen subscribes it again for the new one.
            // Compilation is handled in FlushSceneReloads instead: CompilationPipeline.compilationFinished is cleaned up
            // on code reload, so subscribing to it from here is not allowed (UAL0015).
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    ArmFlush();
                }
            };

            // Pending reloads that survived a domain reload.
            m_SceneReloadQueue.Restore(SessionState.GetString(k_PendingSceneReloadsKey, string.Empty));
            if (m_SceneReloadQueue.PendingCount > 0)
            {
                ArmFlush();
            }
        }

        void QueueSceneReload(string scenePath)
        {
            if (m_SceneReloadQueue.Enqueue(scenePath))
            {
                SessionState.SetString(k_PendingSceneReloadsKey, m_SceneReloadQueue.Serialize());
                ArmFlush();
            }
        }

        // Re-armed on every enqueue: if another delayCall handler throws earlier in the same tick, the flush
        // is dropped, and the next enqueue has to schedule it again. Unsubscribing first keeps it to one call.
        void ArmFlush()
        {
            EditorApplication.delayCall -= FlushSceneReloads;
            EditorApplication.delayCall += FlushSceneReloads;
        }

        void FlushSceneReloads()
        {
            // While deferred the queue is kept. playModeStateChanged arms the flush again after Play Mode.
            var loadedScenes = new HashSet<string>(SceneHierarchy.FromCurrentEditorSceneManager().LoadedScenes, StringComparer.Ordinal);
            if (!m_SceneReloadQueue.TryFlush(EditorApplication.isPlayingOrWillChangePlaymode, EditorApplication.isCompiling, loadedScenes.Contains, out var scenes))
            {
                // While compiling, check again on the next editor update. If the compilation ends in a domain reload,
                // the pending scenes are restored from SessionState in Listen instead.
                if (EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode && m_SceneReloadQueue.PendingCount > 0)
                {
                    ArmFlush();
                }

                // Everything pending may have been dropped because it is no longer loaded.
                if (m_SceneReloadQueue.PendingCount == 0)
                {
                    SessionState.EraseString(k_PendingSceneReloadsKey);
                }

                return;
            }

            SessionState.EraseString(k_PendingSceneReloadsKey);
            foreach (var scenePath in scenes)
            {
                SceneEvents.InvokeSceneSaved(scenePath);
            }
        }
    }
}
