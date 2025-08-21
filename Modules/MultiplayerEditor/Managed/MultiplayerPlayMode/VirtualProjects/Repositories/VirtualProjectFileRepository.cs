// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Unity.Multiplayer.PlayMode.Editor;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class VirtualProjectFileRepository
    {
        internal static readonly string[] PathsRequiredForProject = {
            "Assets",
            "ProjectSettings",
            "Packages",
            "Temp",
        };

        static readonly IReadOnlyCollection<string> PathsRequiringSymlink = new[]
        {
            "Assets",
            "ProjectSettings",
        };

        public static bool CreateProject(FileSystemDelegates fileSystemDelegates, VirtualProjectIdentifier identifier,
            out string cloneProjectPath, out CreateAPIErrorInfo errorState)
        {
            errorState = default;
            cloneProjectPath = PathsUtility.GetProjectPathByIdentifier(identifier);

            // This is to verify if the directory on Windows respects the supported path length
            if (!fileSystemDelegates.IsPathValidFunc(cloneProjectPath))
            {
                errorState = new CreateAPIErrorInfo { Error = CreateAPIError.ProjectUnableToBeCreated };
                return false;
            }

            fileSystemDelegates.CreateDirectoryFunc(cloneProjectPath);

            var mainProjectDirectory = fileSystemDelegates.GetParentPathFunc(Paths.GetCurrentProjectDataPath());
            var cloneProjectPathCopyForLambda = cloneProjectPath;
            foreach (var path in PathsRequiringSymlink)
            {
                var source = Path.Combine(mainProjectDirectory, path);
                var destination = Path.Combine(cloneProjectPathCopyForLambda, path);
                if (!fileSystemDelegates.SymlinkFileFunc(source, destination, out errorState))
                {
                    return false;
                }
            }

            // Normally Unity creates this automatically, but there was a short period of time when
            // that was not the case. Once we depend on >=2023.3.0b8 we'll be able to remove this.
            fileSystemDelegates.CreateDirectoryFunc(Path.Combine(cloneProjectPath, "Temp"));

            fileSystemDelegates.CreateDirectoryFunc(Path.Combine(cloneProjectPath, "Packages"));

            return true;
        }

        public static void DeleteProject(FileSystemDelegates fileSystemDelegates, VirtualProjectIdentifier identifier)
        {
            var cloneProjectPath = PathsUtility.GetProjectPathByIdentifier(identifier);
            fileSystemDelegates.DeleteDirectoryFunc(cloneProjectPath);
        }

        public static bool HasProject(FileSystemDelegates fileSystemDelegates, VirtualProjectIdentifier identifier, out GetAPIErrorInfo errorState)
        {
            foreach (var projectDirectoryName in fileSystemDelegates.GetDirectoryNamesFunc(Paths.CurrentProjectVirtualProjectsFolder))
            {
                var hasParse = VirtualProjectIdentifier.TryParse(projectDirectoryName, out var identifierFromDirectory);
                var isSpecifiedDirectory = hasParse && identifierFromDirectory == identifier;
                if (isSpecifiedDirectory)
                {
                    if (!HasRequiredDirectoriesForClone(fileSystemDelegates, identifier, out var missingDirectories))
                    {
                        errorState = new GetAPIErrorInfo
                        {
                            Error = GetAPIError.MissingRequiredDirectories,
                            Directories = missingDirectories,
                        };
                        return false;
                    }

                    errorState = default;
                    return true;
                }
            }

            errorState = new GetAPIErrorInfo { Error = GetAPIError.ProjectNotFound };
            return false;
        }

        public static VirtualProjectIdentifier[] GetProjects(FileSystemDelegates fileSystemDelegates)
        {
            var results = new List<VirtualProjectIdentifier>();
            var paths = fileSystemDelegates.GetDirectoryNamesFunc(Paths.CurrentProjectVirtualProjectsFolder);
            foreach (var name in paths)
            {
                var hasParse = VirtualProjectIdentifier.TryParse(name, out var identifier);
                var hasFiles = hasParse && HasRequiredDirectoriesForClone(fileSystemDelegates, identifier, out _);

                if (hasFiles)
                {
                    results.Add(identifier);
                }
            }

            return results.ToArray();
        }

        static bool HasRequiredDirectoriesForClone(FileSystemDelegates fileSystemDelegates, VirtualProjectIdentifier identifier, out string[] missingDirectories)
        {
            var projectPath = PathsUtility.GetProjectPathByIdentifier(identifier);
            var folderNames = fileSystemDelegates.GetDirectoryNamesFunc(projectPath);
            var resultMissingDirectories = new List<string>(PathsRequiredForProject);
            for (var index = resultMissingDirectories.Count - 1; index >= 0; index--)
            {
                var path = resultMissingDirectories[index];
                foreach (var info in folderNames)
                {
                    if (path == info)
                    {
                        resultMissingDirectories.Remove(path);
                    }
                }
            }

            // TODO Remove this when our minimum editor version is >=2023.3.0b8.
            // It's fine for the 'Temp' directory to be missing. Just create it in this case.
            if (resultMissingDirectories.Contains("Temp"))
            {
                var cloneProjectPath = PathsUtility.GetProjectPathByIdentifier(identifier);
                fileSystemDelegates.CreateDirectoryFunc(Path.Combine(cloneProjectPath, "Temp"));
                resultMissingDirectories.Remove("Temp");
            }

            missingDirectories = resultMissingDirectories.ToArray();
            return resultMissingDirectories.Count == 0;
        }

    }
}
