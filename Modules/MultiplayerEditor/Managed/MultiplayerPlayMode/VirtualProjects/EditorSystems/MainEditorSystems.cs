// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Multiplayer.PlayMode.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class MainEditorSystems
    {
        internal AssetImportEvents AssetImportEvents { get; }
        internal SceneEvents SceneEvents { get; }

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

            EditorSceneManager.sceneSaved += scene =>
            {
                if (!EditorApplication.isPlaying)
                {
                    EditorApplication.delayCall += () => { SceneEvents.InvokeSceneSaved(scene.path); };
                }
            };
        }
    }
}
