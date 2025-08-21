// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Multiplayer.PlayMode.Editor;
using UnityEditor;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class CloneSystems
    {
        const string k_InitializeMessageSent = "vp_InitializeMessageSent";

        internal AssetImportEvents AssetImportEvents { get; }
        internal SceneEvents SceneEvents { get; }

        internal CloneSystems()
        {
            AssetImportEvents = new AssetImportEvents();
            SceneEvents = new SceneEvents();
        }

        internal void Listen(CloneContext vpContext)
        {
            /*
             * These system classes are simply an aggregation of logic and other events
             *
             * Its only purpose is to forward events to the Internal Runtimes, Workflows, and MultiplayerPlaymode (UI)
             */
            if (!SessionState.GetBool(k_InitializeMessageSent, false))
            {
                SessionState.SetBool(k_InitializeMessageSent, true);
                vpContext.MessagingService.Broadcast(
                    new CloneInitializedMessage(VirtualProjectsEditor.CloneIdentifier));
            }


            const int durationUntilKillCloneOneSecond = 1;
            const int durationUntilPlaymodeThreeSeconds = 3;
            var playmodeStartTime = DateTime.UtcNow;
            var killCloneStartTime = DateTime.UtcNow;
            var hasParsedMainEditorId = int.TryParse(VirtualProjectsEditor.MainEditorProcessId, out var mainProcessId);
            if (!hasParsedMainEditorId)
            {
                MppmLog.Warning("Unable to parse the main editors process id.");
            }

            EditorApplication.update += () =>
            {
                vpContext.MessagingService.HandleUpdate();

                // Watch the main editor and close ourselves if it is closed
                // The Main Editor fundamentally closes all its child editors
                // since it coordinates Asset Syncing at the API level
                var hasExceededKillTime = (DateTime.UtcNow - killCloneStartTime).TotalSeconds >= (float)durationUntilKillCloneOneSecond;
                if (hasParsedMainEditorId && hasExceededKillTime)
                {
                    killCloneStartTime = DateTime.UtcNow;
                    if (!vpContext.ProcessSystemDelegates.IsRunningFunc(mainProcessId))
                    {
                        // :ApplicationEvent :Quit
                        MppmLog.Debug($"Closing clone '{VirtualProjectsEditor.CloneIdentifier}' due to main editor being closed");
                        EditorApplication.Exit(0);
                    }
                }

                // This is specifically for the Request Clone Info system
                // Unlike MPPM, there are no other consumers of this 'event' in the system... yet
                var hasExceeded = (DateTime.UtcNow - playmodeStartTime).TotalSeconds >= (float)durationUntilPlaymodeThreeSeconds;
                if (CommandLineParameters.ReadRequestedClonePlaymode() && hasExceeded)
                {
                    EditorApplication.isPlaying = true;
                }
            };

            vpContext.MessagingService.Receive<TriggerCloneRefreshMessage>(message =>
            {
                AssetImportEvents.InvokeRequestImport(message.DidDomainReload, message.NumAssetsChanged);
            });
            vpContext.MessagingService.Receive<SceneHierarchyChangedMessage>(message => SceneEvents.InvokeSceneHierarchyChanged(message.SceneHierarchy));
            vpContext.MessagingService.Receive<SceneSavedMessage>(message => SceneEvents.InvokeSceneSaved(message.SceneSaved));
        }
    }
}
