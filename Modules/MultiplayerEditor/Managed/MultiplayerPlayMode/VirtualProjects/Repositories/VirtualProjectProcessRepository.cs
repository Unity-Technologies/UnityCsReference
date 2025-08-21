// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Multiplayer.PlayMode.Editor;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class VirtualProjectProcessRepository
    {
        internal static int Launch(ProcessSystemDelegates processSystemDelegates, VirtualProjectIdentifier identifier,
            SessionStateJsonRepository<VirtualProjectIdentifier, ProcessId> sessionStateJsonRepository, string[] extraExternalArgs)
        {
            var defaultVirtualProjectLaunchArgs = new[]
            {
                CommandLineParameters.BuildProjectPathArgument(PathsUtility.GetProjectPathByIdentifier(identifier)),
                CommandLineParameters.k_CloneProcess,
                CommandLineParameters.k_ForgetProjectPath,
                CommandLineParameters.k_VirtualLibraryFolder,
                CommandLineParameters.k_AssetDatabaseReadOnly,
                CommandLineParameters.k_DisableDirectoryMonitor,
                CommandLineParameters.k_NoUMP,
                CommandLineParameters.k_UMPRestorePackages,
                CommandLineParameters.BuildLogFileArgument(identifier),
                CommandLineParameters.BuildChannelServiceChannelNameArgument(MessagingService.k_DefaultChannelName),
                CommandLineParameters.BuildChannelServicePortArgument,
                CommandLineParameters.BuildMainProcessIdArgument(processSystemDelegates.OurIdFunc().ToString()),
                CommandLineParameters.BuildVirtualProjectIdentifierArgument(identifier),
            };

            // If MPPM (for example) specific args get passed in we just add them to ours
            var allArgs = extraExternalArgs == null ? new List<string>() : new List<string>(extraExternalArgs);

            if (!MultiplayerPlayModeSettings.ForceMainWindow)
            {
                allArgs.Add(CommandLineParameters.k_NoMainWindow);
                allArgs.Add(CommandLineParameters.k_SuppressDefaultMenuEntries);
            }

            allArgs.AddRange(defaultVirtualProjectLaunchArgs);
            if (BuildProfile.GetActiveBuildProfile() != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(BuildProfile.GetActiveBuildProfile());
                allArgs.Add(CommandLineParameters.k_ActiveBuildProfile + $" \"{assetPath}\"");
            }
            else
            {
                allArgs.Add(CommandLineParameters.k_BuildTarget + $" {EditorUserBuildSettings.activeBuildTarget}");
                allArgs.Add(CommandLineParameters.k_StandaloneBuildSubtarget + $" {EditorUserBuildSettings.standaloneBuildSubtarget}");

            }
            var arguments = string.Join(" ", allArgs);
            var executablePath = Paths.GetApplicationPath(Application.platform == RuntimePlatform.OSXEditor ? "Contents/MacOS/Unity" : "");

            if (!processSystemDelegates.TryRunFunc(executablePath, arguments, out var p, out var error))
            {
                Debug.LogError($"Launch Failed: {executablePath}{Environment.NewLine}Error:{Environment.NewLine}{error}");
                return -1;
            }

            var processId = new ProcessId(p);

            sessionStateJsonRepository.Create(identifier, processId);

            return processId.Value;
        }

        internal static bool TryGetProcessInfo(VirtualProjectIdentifier identifier, SessionStateJsonRepository<VirtualProjectIdentifier, ProcessId> sessionStateJsonRepository, out int processId)
        {
            processId = -1;
            if (sessionStateJsonRepository.TryGetValue(identifier, out var boxedProcessId))
            {
                processId = boxedProcessId.Value;
            }
            return processId != -1;
        }

        internal static void Close(ProcessSystemDelegates processSystemDelegates, VirtualProjectIdentifier identifier, SessionStateJsonRepository<VirtualProjectIdentifier, ProcessId> sessionStateJsonRepository)
        {
            if (!sessionStateJsonRepository.TryGetValue(identifier, out var processId)) return;
            if (processId.Value == -1) return;

            processSystemDelegates.KillFunc(processId.Value);
            sessionStateJsonRepository.Delete(identifier);
        }
    }
}
