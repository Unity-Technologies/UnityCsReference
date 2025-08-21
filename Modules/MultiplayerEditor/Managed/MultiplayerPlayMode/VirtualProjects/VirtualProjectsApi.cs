// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Multiplayer.PlayMode.Editor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    struct GetAPIErrorInfo
    {
        public GetAPIError Error;
        public string[] Directories;
    }

    enum GetAPIError
    {
        None,
        ProjectNotFound,
        MissingRequiredDirectories,
    }

    struct CreateAPIErrorInfo
    {
        public CreateAPIError Error;
        public string ShellCommand;
        public string ShellError;
    }

    enum CreateAPIError
    {
        None,
        ProjectUnableToBeCreated,
        SymLinkUnableToBePerformed,
    }

    enum DeleteAPIError
    {
        None,
        ProjectNotFound,
        ProjectCurrentlyInUse,
    }

    struct VirtualProjectsApiDelegates
    {
        // This simply represents the static methods of a VirtualProjectsApi
        public delegate VirtualProject[] GetProjects(string prefix);
        public delegate bool TryGet(VirtualProjectIdentifier virtualProjectIdentifier, [NotNullWhen(true)] out VirtualProject virtualProject, out GetAPIErrorInfo errorState);
        public delegate bool IsNullOrInvalid(VirtualProject virtualProject);
        public delegate bool Create(string prefix, out VirtualProject project, out string projectDirectory, out CreateAPIErrorInfo errorState);
        public delegate bool Delete(VirtualProject virtualProject, out DeleteAPIError errorState);

        public GetProjects GetProjectsFunc;
        public TryGet TryGetFunc;
        public IsNullOrInvalid IsNullOrInvalidFunc;
        public Create CreateFunc;
        public Delete DeleteFunc;
    }

    struct InitData
    {
        public SessionStateJsonRepository<VirtualProjectIdentifier, ProcessId> ProcessRepository;
        public SessionStateJsonRepository<VirtualProjectIdentifier, VirtualProjectStatePerProcessLifetime> StateRepository;
        public Dictionary<VirtualProjectIdentifier, double> ProcessLaunchTimes;
        public VirtualProjectsApi.CreateVirtualProjectIdentifierFunc CreateVirtualProjectIdentifierFunc;
        public FileSystemDelegates FileSystemDelegates;
        public ProcessSystemDelegates ProcessSystemDelegates;
        public ParsingSystemDelegates ParsingSystemDelegates;
    }

    static class VirtualProjectsApi
    {
        internal const string k_FilterAll = "__all";

        internal delegate VirtualProjectIdentifier CreateVirtualProjectIdentifierFunc(string prefix);

        static InitData s_InitData;
        static bool s_Initialized;

        public static VirtualProjectsApiDelegates Delegates { get; } = new VirtualProjectsApiDelegates
        {
            GetProjectsFunc = GetProjects,
            TryGetFunc = TryGet,
            IsNullOrInvalidFunc = IsNullOrInvalid,
            CreateFunc = Create,
            DeleteFunc = Delete,
        };

        public static void Initialize(FileSystemDelegates fileSystemDelegates, ParsingSystemDelegates parsingSystemDelegates, ProcessSystemDelegates processSystemDelegates,
            SessionStateJsonRepository<VirtualProjectIdentifier, ProcessId> processRepository,
            SessionStateJsonRepository<VirtualProjectIdentifier, VirtualProjectStatePerProcessLifetime> stateRepository, Dictionary<VirtualProjectIdentifier, double> processLaunchTimes)
        {
            CreateVirtualProjectIdentifierFunc createVirtualProjectIdentifierFunc = VirtualProjectIdentifier.NewVirtualProjectIdentifier;
            s_InitData = new InitData
            {
                FileSystemDelegates = fileSystemDelegates,
                ProcessSystemDelegates = processSystemDelegates,
                ParsingSystemDelegates = parsingSystemDelegates,
                ProcessRepository = processRepository,
                StateRepository = stateRepository,
                CreateVirtualProjectIdentifierFunc = createVirtualProjectIdentifierFunc,
                ProcessLaunchTimes = processLaunchTimes,
            };
            s_Initialized = true;
        }

        public static VirtualProject[] GetProjects(string prefix)
        {
            Debug.Assert(s_Initialized);
            var virtualProjects = new List<VirtualProject>();
            foreach (var vpi in VirtualProjectFileRepository.GetProjects(s_InitData.FileSystemDelegates))
            {
                if (prefix == k_FilterAll || HasPrefix(vpi, prefix))
                {
                    virtualProjects.Add(new VirtualProject(vpi, s_InitData.ProcessSystemDelegates, s_InitData.FileSystemDelegates, s_InitData.ParsingSystemDelegates, s_InitData.ProcessRepository, s_InitData.StateRepository, s_InitData.ProcessLaunchTimes));
                }
            }

            return virtualProjects.ToArray();
        }

        public static bool TryGet(
            VirtualProjectIdentifier virtualProjectIdentifier,
            [NotNullWhen(true)] out VirtualProject virtualProject, out GetAPIErrorInfo errorState)
        {
            Debug.Assert(s_Initialized);
            var hasProject = VirtualProjectFileRepository.HasProject(s_InitData.FileSystemDelegates, virtualProjectIdentifier, out errorState);
            virtualProject = hasProject
                ? new VirtualProject(virtualProjectIdentifier, s_InitData.ProcessSystemDelegates, s_InitData.FileSystemDelegates, s_InitData.ParsingSystemDelegates, s_InitData.ProcessRepository, s_InitData.StateRepository, s_InitData.ProcessLaunchTimes)
                : null;

            return errorState.Error == GetAPIError.None;
        }

        public static bool IsNullOrInvalid(VirtualProject virtualProject)
        {
            Debug.Assert(s_Initialized);
            if (virtualProject == null) return true;
            if (!Contains(virtualProject.Identifier)) return true;
            return false;
        }

        public static bool Create(string prefix, out VirtualProject project, out string projectDirectory,
            out CreateAPIErrorInfo errorState)
        {
            Debug.Assert(s_Initialized);
            project = null;
            projectDirectory = string.Empty;
            errorState = default;

            for (var attempts = 0; attempts < 10; attempts++)
            {
                var identifier = s_InitData.CreateVirtualProjectIdentifierFunc(prefix);

                project = null;
                if (!Contains(identifier))
                {
                    var hasCreated = VirtualProjectFileRepository.CreateProject(s_InitData.FileSystemDelegates,
                        identifier, out projectDirectory, out errorState);
                    if (hasCreated)
                    {
                        project = new VirtualProject(identifier, s_InitData.ProcessSystemDelegates, s_InitData.FileSystemDelegates, s_InitData.ParsingSystemDelegates, s_InitData.ProcessRepository, s_InitData.StateRepository, s_InitData.ProcessLaunchTimes);
                    }
                    else if (errorState.Error == CreateAPIError.SymLinkUnableToBePerformed)
                    {
                        // Remove empty directory since we failed to create the directory
                        // No longer attempt to retry since we will never succeed
                        VirtualProjectFileRepository.DeleteProject(s_InitData.FileSystemDelegates, identifier);
                        return false;
                    }
                }

                if (project != null)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool Delete(VirtualProject virtualProject, out DeleteAPIError errorState)
        {
            Debug.Assert(s_Initialized);
            errorState = DeleteAPIError.None;
            var hasProject = Contains(virtualProject.Identifier);
            if (!hasProject)
            {
                errorState = DeleteAPIError.ProjectNotFound;
                return false;
            }

            if (virtualProject.EditorState != EditorState.NotLaunched)
            {
                errorState = DeleteAPIError.ProjectCurrentlyInUse;
                return false;
            }

            VirtualProjectFileRepository.DeleteProject(s_InitData.FileSystemDelegates, virtualProject.Identifier);
            return true;
        }

        static bool Contains(VirtualProjectIdentifier virtualProjectIdentifier)
        {
            Debug.Assert(s_Initialized);
            foreach (var vp in GetProjects(virtualProjectIdentifier.Prefix))
            {
                if (vp.Identifier == virtualProjectIdentifier) return true;
            }

            return false;
        }

        public static bool HasPrefix(VirtualProjectIdentifier virtualProjectIdentifier, string prefix)
        {
            Debug.Assert(s_Initialized);
            return string.IsNullOrWhiteSpace(prefix) && string.IsNullOrWhiteSpace(virtualProjectIdentifier.Prefix) ||
                   prefix?.Replace("-", string.Empty).Trim() ==
                   virtualProjectIdentifier.Prefix?.Replace("-", string.Empty).Trim();
        }
    }
}
