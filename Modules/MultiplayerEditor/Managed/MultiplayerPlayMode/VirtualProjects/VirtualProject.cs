// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Multiplayer.PlayMode.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    enum EditorState
    {
        NotLaunched,
        Launching,
        Launched,
        UnexpectedlyStopped,
    }

    enum LaunchProjectError
    {
        None,
        ProjectNotFound,
        ProjectCurrentlyInUse,
    }

    enum CloseProjectError
    {
        None,
        ProjectNotFound,
    }

    class VirtualProject
    {
        public DateTime m_TimeSinceStartingLaunch;
        readonly SessionStateJsonRepository<VirtualProjectIdentifier, ProcessId> m_ProcessRepository;
        readonly SessionStateJsonRepository<VirtualProjectIdentifier, VirtualProjectStatePerProcessLifetime> m_StateRepository;
        readonly ProcessSystemDelegates m_ProcessSystemDelegates;
        readonly FileSystemDelegates m_FileSystemDelegates;
        readonly ParsingSystemDelegates m_ParsingSystemDelegates;
        readonly Dictionary<VirtualProjectIdentifier, double> m_ProcessLaunchTimes;

        internal VirtualProject(VirtualProjectIdentifier identifier,
            ProcessSystemDelegates processSystemDelegates,
            FileSystemDelegates fileSystemDelegates,
            ParsingSystemDelegates parsingSystemDelegates,
            SessionStateJsonRepository<VirtualProjectIdentifier, ProcessId> processRepository,
            SessionStateJsonRepository<VirtualProjectIdentifier, VirtualProjectStatePerProcessLifetime> stateRepository,
            Dictionary<VirtualProjectIdentifier, double> processLaunchTimes)
        {
            Identifier = identifier;

            m_ParsingSystemDelegates = parsingSystemDelegates;
            m_ProcessSystemDelegates = processSystemDelegates;
            m_FileSystemDelegates = fileSystemDelegates;
            m_ProcessRepository = processRepository;
            m_StateRepository = stateRepository;
            m_ProcessLaunchTimes = processLaunchTimes;
        }

        bool IsCreated
        {
            get
            {
                foreach (var project in VirtualProjectFileRepository.GetProjects(m_FileSystemDelegates))
                {
                    if (Equals(project, Identifier)) return true;
                }
                return false;
            }
        }

        bool IsRunning => VirtualProjectProcessRepository.TryGetProcessInfo(Identifier, m_ProcessRepository, out var processInfo) && processInfo != -1 && m_ProcessSystemDelegates.IsRunningFunc(processInfo);

        bool IsCommunicative => m_StateRepository.TryGetValue(Identifier, out var state) && state.IsCommunicative;

        internal int ProcessId => VirtualProjectProcessRepository.TryGetProcessInfo(Identifier, m_ProcessRepository, out var processInfo) ? processInfo : -1;

        public EditorState EditorState
        {
            get
            {
                return (IsRunning, IsCommunicative) switch
                {
                    (false, false) => EditorState.NotLaunched,
                    (false, true) => EditorState.UnexpectedlyStopped,
                    (true, false) => EditorState.Launching,
                    (true, true) => EditorState.Launched,
                };
            }
        }

        [NotNull] public VirtualProjectIdentifier Identifier { get; }

        [NotNull] public string Directory => PathsUtility.GetProjectPathByIdentifier(Identifier);

        public bool Launch(out LaunchProjectError errorState, out int processId, params string[] extraExternalArgs)
        {
            errorState = LaunchProjectError.None;
            processId = -1;
            if (!IsCreated)
            {
                errorState = LaunchProjectError.ProjectNotFound;
                return false;
            }

            if (IsRunning)
            {
                errorState = LaunchProjectError.ProjectCurrentlyInUse;
                return false;
            }

            if (!m_StateRepository.ContainsKey(Identifier))
            {
                m_StateRepository.Create(Identifier, new VirtualProjectStatePerProcessLifetime { LaunchArgs = extraExternalArgs });
            }
            else
            {
                m_StateRepository.Update(Identifier, state =>
                {
                    state.Retry = 0;
                    state.LaunchArgs = extraExternalArgs;
                }, out _);
            }

            m_TimeSinceStartingLaunch = DateTime.UtcNow;

            // Sync EditorPrefs to ensure virtual players can read main editor's preferences
            EditorPrefs.Sync();

            processId = VirtualProjectProcessRepository.Launch(m_ProcessSystemDelegates, Identifier, m_ProcessRepository, extraExternalArgs);
            if (!ProcessInterrupt.SubscribeToProcessExit(processId, (_, exitCode) => OnVirtualProjectExit(this, exitCode, m_StateRepository)))
            {
                MppmLog.Debug($"We failed to subscribe to detect when the process exits.");
            }

            m_ProcessLaunchTimes[Identifier] = EditorApplication.timeSinceStartup;
            return true;
        }

        public bool Close(out CloseProjectError errorState)
        {
            errorState = CloseProjectError.None;
            if (!IsCreated)
            {
                errorState = CloseProjectError.ProjectNotFound;
                return false;
            }

            // Clear out the existing state
            if (m_StateRepository.ContainsKey(Identifier))
            {
                m_StateRepository.Delete(Identifier);
            }
            m_TimeSinceStartingLaunch = default;

            VirtualProjectProcessRepository.Close(m_ProcessSystemDelegates, Identifier, m_ProcessRepository);
            return true;
        }

        static void OnVirtualProjectExit(VirtualProject project, int exitCode, SessionStateJsonRepository<VirtualProjectIdentifier, VirtualProjectStatePerProcessLifetime> sessionStateJsonRepository)
        {
            MppmLog.Debug($"The virtual project '{project.Identifier}' exited with {exitCode}!");

            if (exitCode != 199) return;

            // NOTE: This is on a separate thread. So certain Editor operations will fail (silently) like calling SessionState.SetString()
            // which is basically used everywhere inside Virtual Projects... So we can't call project.Close() here or you will fail to see
            // ------ UnityEngine.UnityException: SetString can only be called from the main thread.
            MppmLog.Warning($"The virtual project '{project.Identifier}' exited because of the License Server!");
        }
    }
}
