// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Multiplayer.Internal;

namespace Unity.Multiplayer.PlayMode.Editor
{
    // This represents the state of players which may or might not be an editor
    enum PlayerState
    {
        NotLaunched,
        Launching,
        Launched,
        UnexpectedlyStopped,
    }

    enum ActivationError
    {
        None,
        CompileErrors,
        CouldNotGetOrCreate,
    }

    enum TagError
    {
        None,
        InPlayMode,
        Duplicate,
        DoesNotExist,
        Empty,
    }

    class UnityPlayer
    {
        public DateTime m_TimeSinceStartingLaunch;
        readonly PlayerStateJson m_PlayerStateJson;
        readonly SystemDataStore m_SystemDataStore;
        internal readonly VirtualProjectsApiDelegates m_VirtualProjectsApi;
        readonly string[] m_WorkflowLaunchArgs;
        readonly string m_VPPrefix;

        internal UnityPlayer(PlayerStateJson playerStateJson, SystemDataStore systemDataStore,
                             VirtualProjectsApiDelegates virtualProjectsApi, string vpPrefix,
                             string[] workflowLaunchArgs)
        {
            m_WorkflowLaunchArgs = workflowLaunchArgs ?? Array.Empty<string>();
            m_PlayerStateJson = playerStateJson ?? throw new ArgumentNullException(nameof(playerStateJson));
            m_SystemDataStore = systemDataStore ?? throw new ArgumentNullException(nameof(systemDataStore));
            m_VirtualProjectsApi = virtualProjectsApi;
            m_VPPrefix = !string.IsNullOrWhiteSpace(vpPrefix)
                ? vpPrefix
                : throw new ArgumentNullException(nameof(vpPrefix));    // Note: We pass in the prefix so that 'tests' can use a different one from 'prod'
        }

        public event Action OnPlayerCommunicative;

        public string[] Tags => m_PlayerStateJson.Tags.ToArray();

        public bool Activate(out ActivationError error, List<string> additionalArgs = null)
        {
            error = ActivationError.None;
            if (MultiplayerPlaymodeEditorUtility.IsPlayerActivateProhibited)
            {
                error = ActivationError.CompileErrors;
                return error == ActivationError.None;
            }
            if (m_PlayerStateJson.Type == PlayerType.Main)
            {
                m_PlayerStateJson.Active = true;
                return error == ActivationError.None;
            }
            if (m_PlayerStateJson.Active)
            {
                return error == ActivationError.None;
            }

            // Use the previous project associated with this player
            // or use the first available MPPM scoped Virtual Project (and update the player data)
            // or create a new MPPM scope Virtual Project (and update the player data)
            var availableProject = GetVirtualProject(m_SystemDataStore, m_PlayerStateJson, m_VirtualProjectsApi, m_VPPrefix);

            // Return the found project or we need to create new vp when we don't have enough created
            if (availableProject == null)
            {
                if (!m_VirtualProjectsApi.CreateFunc(m_VPPrefix, out availableProject, out var projectDirectory, out var e))
                {
                    switch (e.Error)
                    {
                        case CreateAPIError.ProjectUnableToBeCreated:
                            MppmLog.Error($"Could not activate player since the directory '{projectDirectory}' has reached over the maximum supported path length for Windows.");
                            break;
                        case CreateAPIError.SymLinkUnableToBePerformed:
                            // The ShellError we expect is "Local NTFS volumes are required to complete the operation" in order to print the clearer message
                            if (e.ShellError.Contains("NTFS volumes"))
                            {
                                MppmLog.Error($"Multiplayer Playmode only works on NTFS volumes. USB/Jump Drives (FAT32) and other storage mediums do not support the appropriate operations.");
                            }
                            else
                            {
                                MppmLog.Error($"Command Failed: {e.ShellCommand}{Environment.NewLine}Error:{Environment.NewLine}{e.ShellError}");
                                MppmLog.Error($"Could not activate player since the directory '{projectDirectory}' was unable to be symlinked.");
                            }

                            break;
                        case CreateAPIError.None:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (availableProject == null)
                    {
                        error = ActivationError.CouldNotGetOrCreate;
                        return error == ActivationError.None;
                    }
                }
            }

            var isInvalidIdentifier = m_PlayerStateJson.TypeDependentPlayerInfo.VirtualProjectIdentifier == null;
            var isDifferingIdentifier = !Equals(m_PlayerStateJson.TypeDependentPlayerInfo, TypeDependentPlayerInfo.NewClone(availableProject.Identifier));
            if (isInvalidIdentifier || isDifferingIdentifier)
            {
                m_PlayerStateJson.TypeDependentPlayerInfo = TypeDependentPlayerInfo.NewClone(availableProject.Identifier);
            }

            if (additionalArgs == null)
                additionalArgs = new List<string>();

            additionalArgs.AddRange(m_WorkflowLaunchArgs);
            availableProject.Launch(out _, out _, additionalArgs.ToArray());

            m_TimeSinceStartingLaunch = availableProject.m_TimeSinceStartingLaunch;

            m_PlayerStateJson.Active = true;
            m_SystemDataStore.SavePlayerJson(m_PlayerStateJson.Index, m_PlayerStateJson);

            return error == ActivationError.None;
        }

        public bool Deactivate(out ActivationError error)
        {
            error = ActivationError.None;
            if (m_PlayerStateJson.Type == PlayerType.Main)
            {
                m_PlayerStateJson.Active = true;
                return error == ActivationError.None;
            }
            if (!m_PlayerStateJson.Active)
            {
                return error == ActivationError.None;
            }

            // Use the previous project associated with this player
            // or use the first available MPPM scoped Virtual Project (and update the player data)
            var project = GetVirtualProject(m_SystemDataStore, m_PlayerStateJson, m_VirtualProjectsApi, m_VPPrefix);
            if (project == null)
            {
                error = ActivationError.CouldNotGetOrCreate;
                return error == ActivationError.None;
            }

            var isInvalidIdentifier = m_PlayerStateJson.TypeDependentPlayerInfo.VirtualProjectIdentifier == null;
            var isDifferingIdentifier = !Equals(m_PlayerStateJson.TypeDependentPlayerInfo, TypeDependentPlayerInfo.NewClone(project.Identifier));
            if (isInvalidIdentifier || isDifferingIdentifier)
            {
                m_PlayerStateJson.TypeDependentPlayerInfo = TypeDependentPlayerInfo.NewClone(project.Identifier);
            }

            if (project.EditorState is EditorState.Launched or EditorState.Launching)
            {
                project.Close(out _);
            }

            m_PlayerStateJson.Active = false;
            m_SystemDataStore.SavePlayerJson(m_PlayerStateJson.Index, m_PlayerStateJson);

            return error == ActivationError.None;
        }

        public bool AddTag(string tag, out TagError error)
        {
            error = TagError.None;
            if (m_PlayerStateJson.Tags.Contains(tag))
            {
                error = TagError.Duplicate;
                return error == TagError.None;
            }

            m_PlayerStateJson.Tags.Add(tag);
            m_SystemDataStore.SavePlayerJson(m_PlayerStateJson.Index, m_PlayerStateJson);

            return error == TagError.None;
        }

        public bool RemoveTag(string tag, out TagError error)
        {
            error = TagError.None;
            if (!m_PlayerStateJson.Tags.Contains(tag))
            {
                error = TagError.DoesNotExist;
                return error == TagError.None;
            }

            m_PlayerStateJson.Tags.Remove(tag);
            m_SystemDataStore.SavePlayerJson(m_PlayerStateJson.Index, m_PlayerStateJson);

            return error == TagError.None;
        }

        public bool ClearTags(out TagError error)
        {
            error = TagError.None;

            m_PlayerStateJson.Tags.Clear();
            m_SystemDataStore.SavePlayerJson(m_PlayerStateJson.Index, m_PlayerStateJson);
            return error == TagError.None;
        }

        public PlayerState PlayerState
        {
            get
            {
                EditorState editorState;
                if (m_PlayerStateJson.Type == PlayerType.Main)
                {
                    editorState = EditorState.Launched;
                }
                else
                {
                    var project = GetVirtualProject(m_SystemDataStore, m_PlayerStateJson, m_VirtualProjectsApi, m_VPPrefix);
                    editorState = project?.EditorState ?? EditorState.NotLaunched;
                }
                return editorState switch
                {
                    EditorState.NotLaunched => PlayerState.NotLaunched,
                    EditorState.Launching => PlayerState.Launching,
                    EditorState.Launched => PlayerState.Launched,
                    EditorState.UnexpectedlyStopped => PlayerState.UnexpectedlyStopped,
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }
        }

        public MultiplayerRoleFlags Role
        {
            get => (MultiplayerRoleFlags)m_PlayerStateJson.MultiplayerRole;
            set
            {
                m_PlayerStateJson.MultiplayerRole = (int)value;
                m_SystemDataStore.SavePlayerJson(m_PlayerStateJson.Index, m_PlayerStateJson);
            }
        }

        public string Name => m_PlayerStateJson.Name;
        public PlayerType Type => m_PlayerStateJson.Type;
        public PlayerIdentifier PlayerIdentifier => m_PlayerStateJson.PlayerIdentifier;
        public TypeDependentPlayerInfo TypeDependentPlayerInfo => m_PlayerStateJson.TypeDependentPlayerInfo;

        internal void InvokeOnPlayerCommunicative()
        {
            OnPlayerCommunicative?.Invoke();
        }

        public static PlayerStateJson[] GetPlayers(SystemDataStore systemDataStore)
        {
            return new List<PlayerStateJson>(systemDataStore.LoadAllPlayerJson().Values).ToArray();
        }

        static VirtualProject GetVirtualProject(SystemDataStore systemDataStore, PlayerStateJson playerStateJson, VirtualProjectsApiDelegates virtualProjectsApi, string prefix)
        {
            Debug.Assert(playerStateJson.Type != PlayerType.Main);

            if (playerStateJson.TypeDependentPlayerInfo.VirtualProjectIdentifier != null)
            {
                if (virtualProjectsApi.TryGetFunc(playerStateJson.TypeDependentPlayerInfo.VirtualProjectIdentifier, out var foundProject, out var eTryGet))
                {
                    return foundProject;
                }

                // At this stage TryGetFunc has failed - The File structure / directory is currently in an
                // unknown or erroneous state and the PlayerJSON state is out of sync with the current
                // Filesystem - autocorrect by clearing it from the PlayerJson state.
                var projectID = playerStateJson.TypeDependentPlayerInfo.VirtualProjectIdentifier.ToString();
                playerStateJson.TypeDependentPlayerInfo = TypeDependentPlayerInfo.NewEmpty();

                // If we couldn't find the project then update the data store so that it isn't held as 'Active'
                playerStateJson.Active = false;
                systemDataStore.SavePlayerJson(playerStateJson.Index, playerStateJson);

                if (eTryGet.Error == GetAPIError.MissingRequiredDirectories)
                {
                    MppmLog.Warning($"The project directory structure for [{projectID}] was not complete. " +
                                  $"Missing directories [{string.Join(" | ", eTryGet.Directories)}]. " +
                                  $"If possible, inform the developers the repro steps and you may delete [{playerStateJson.TypeDependentPlayerInfo.VirtualProjectIdentifier}] if you wish to continue (a new project should be safely created).");
                    return null;
                }

                if (eTryGet.Error == GetAPIError.ProjectNotFound)
                {
                    MppmLog.Warning($"The project directory for [{playerStateJson.TypeDependentPlayerInfo.VirtualProjectIdentifier}] could not be found.");
                }
            }

            var vpIdentifiersFromDataStore = new List<VirtualProjectIdentifier>();
            foreach (var p in GetPlayers(systemDataStore))
            {
                if (p.TypeDependentPlayerInfo.VirtualProjectIdentifier != null)
                {
                    vpIdentifiersFromDataStore.Add(p.TypeDependentPlayerInfo.VirtualProjectIdentifier);
                }
            }

            // Don't have a virtual project associated with this player yet, associate one.
            var projectsOnDisk = virtualProjectsApi.GetProjectsFunc(prefix);
            var hasAnyExistingVirtualProjectOnDisk = projectsOnDisk.Length > 0;
            if (hasAnyExistingVirtualProjectOnDisk)
            {
                // Grab any VP on disk that is NOT listed in the datastore
                foreach (var p in projectsOnDisk)
                {
                    var isProjectInUse = false;
                    foreach (var identifier in vpIdentifiersFromDataStore.ToArray())
                    {
                        if (Equals(identifier, p.Identifier))
                        {
                            isProjectInUse = true;
                            break;
                        }
                    }

                    if (!isProjectInUse)
                    {
                        return p;
                    }
                }
            }

            return null;
        }
    }
}
