// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Rendering;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class CommandLineParameters
    {
        public const string k_NoDownChainDependencies = "--no-down-chain-dependencies";
        // The only way to communicate with an editor to an editor quickly is commandline/ message/ or file.
        // This seems like the quickest method with minimal overhead considering 'test' editors have no down chain dependencies
        public const string k_RequestClonePlaymode = "--request-clone-playmode";

        public const string k_CloneProcess = "--virtual-project-clone";
        public const string k_ScenarioClone = "-scenarioClone";
        public const string k_ForgetProjectPath = "-forgetProjectPath";

        public const string k_NoUMP = "-noUpm";
        public const string k_UMPRestorePackages = "-upmRestorePackages";

        public const string k_DisableDirectoryMonitor = "-DisableDirectoryMonitor";
        public const string k_SuppressDefaultMenuEntries = "-suppressDefaultMenuEntries";
        public const string k_NoMainWindow = "-noMainWindow";
        public const string k_NoLaunchScreen = "-noLaunchScreen";
        public const string k_NoCloudProjectBindPopup = "-no-cloud-project-bind-popup";   // Note: this is for the UGS (Link to project) popup. If this popup changes or if they end up correctly checking for the -editor-mode flag then we can remove this
        public const string k_CloudEnvironment = "-cloudEnvironment";

        // This MUST be a __relative__ path from the virtual project folder to the library folder of the main project
        public const string k_VirtualLibraryFolder = "-library-redirect ../..";
        public const string k_AssetDatabaseReadOnly = "-readonly";
        public const string k_ActiveBuildProfile = "-activeBuildProfile";
        public const string k_BuildTarget = "-buildTarget";
        public const string k_StandaloneBuildSubtarget = "-standaloneBuildSubtarget";

        const string k_UMP = "-ump";
        const string k_ChannelServicePort = "-ump-channel-service-port";
        const string k_ChannelName = "-vp-channel-name";
        const string k_MainProcessId = "-mainProcessId"; // Note: various IDEs might use this to group processes
        const string k_VirtualProjectIdentifier = "-vpId";
        const string k_ProjectPath = "-projectPath";
        const string k_EditorMode = "-editor-mode";
        const string k_EditorDebuggingName = "-name";   // Note: this specific string is ONLY for the benefit of IDEs

        static readonly string[] k_ForwardedGraphicsSwitches =
        {
            "force-ugk",
            "force-ugk-validation",
            "force-d3d12-debug",
            "force-d3d12-debug-gbv",
            "force-d3d12-debug-as-errors",
        };

        static readonly string[] k_ForwardedGraphicsSwitchesWithValue =
        {
            "force-device-index",
        };

        static readonly char[] k_UnquotedValueForbiddenChars = { ' ', '\t', '"' };

        [NoAutoStaticsCleanup] // test seam; the default targets a framework method and pins nothing across reloads
        internal static Func<string[]> MainEditorCommandLineArgs { get; set; } = Environment.GetCommandLineArgs;

        public static string BuildLogFileArgument(VirtualProjectIdentifier identifier)
        {
            var commandLineArgs = Environment.GetCommandLineArgs();
            var index = -1;
            for (var i = 0; i < commandLineArgs.Length; i++)
            {
                if (commandLineArgs[i].Contains("logFile"))
                {
                    index = i;

                    break;
                }
            }

            if (index != -1)
            {
                var mainEditorLogFilePath = commandLineArgs[index + 1];
                mainEditorLogFilePath = mainEditorLogFilePath.Replace(".txt", string.Empty);
                mainEditorLogFilePath += $"-{identifier}.txt";

                // TODO: This should be using FileSystem instead of directly doing I/O
                if (!File.Exists(mainEditorLogFilePath))
                {
                    File.Create(mainEditorLogFilePath).Dispose();
                }

                return $"-logFile \"{mainEditorLogFilePath}\"";
            }

            return $"-logFile \"{PathsUtility.GetProjectPathByIdentifier(identifier, "Logs", "Editor.log")}\"";
        }

        public static string BuildChannelServicePortArgument
            => $"{k_ChannelServicePort} {UnityEditor.MPE.ChannelService.GetPort()}";

        public static string BuildChannelServiceChannelNameArgument(string channelName)
            => $"{k_ChannelName}={channelName}";

        public static string BuildMainProcessIdArgument(string processId)
            => $"{k_MainProcessId}={processId}";

        public static string BuildVirtualProjectIdentifierArgument(VirtualProjectIdentifier identifier)
            => $"{k_VirtualProjectIdentifier}={identifier}";

        public static string BuildProjectPathArgument(string path)
            => $"{k_ProjectPath} \"{path}\"";

        // A clone must render through the same graphics API as the main editor. Importers whose
        // dependencies include graphics device capabilities (for example .urtshader) otherwise hash
        // differently in the clone, which makes up-to-date assets look out of date. The clone's asset
        // database is read only, so it cannot reimport them and asserts instead. Returns an empty
        // string for devices with no force argument, leaving the clone to pick its own API as before.
        public static string BuildForceGraphicsApiArgument(GraphicsDeviceType deviceType)
            => deviceType switch
            {
                GraphicsDeviceType.Direct3D11 => "-force-d3d11",
                GraphicsDeviceType.Direct3D12 => "-force-d3d12",
                GraphicsDeviceType.Vulkan => "-force-vulkan",
                GraphicsDeviceType.Metal => "-force-metal",
                GraphicsDeviceType.OpenGLCore => "-force-glcore",
                _ => string.Empty
            };

        // A clone must run the same graphics device, adapter and debug layers as the main editor. The editor runs
        // UGK only when started with -force-ugk, and UGK numbers adapters differently from the legacy device, so
        // -force-device-index is only meaningful when forwarded together with it.
        public static List<string> BuildForwardedGraphicsArguments(string[] mainEditorCommandLineArgs)
        {
            var forwarded = new List<string>();
            if (mainEditorCommandLineArgs == null)
                return forwarded;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < mainEditorCommandLineArgs.Length; i++)
            {
                var switchName = GetSwitchName(mainEditorCommandLineArgs[i]);
                if (switchName == null || !seen.Add(switchName))
                    continue;

                var bareSwitch = FindSwitch(k_ForwardedGraphicsSwitches, switchName);
                if (bareSwitch != null)
                {
                    forwarded.Add($"-{bareSwitch}");
                    continue;
                }

                var valueSwitch = FindSwitch(k_ForwardedGraphicsSwitchesWithValue, switchName);
                var hasValue = i + 1 < mainEditorCommandLineArgs.Length && IsSwitchValue(mainEditorCommandLineArgs[i + 1]);
                if (valueSwitch != null && hasValue)
                {
                    forwarded.Add($"-{valueSwitch} {mainEditorCommandLineArgs[i + 1]}");
                    i++;
                }
            }

            return forwarded;
        }

        static string FindSwitch(string[] switches, string switchName)
            => Array.Find(switches, s => string.Equals(s, switchName, StringComparison.OrdinalIgnoreCase));

        static string GetSwitchName(string arg)
        {
            if (string.IsNullOrEmpty(arg))
                return null;
            if (arg.StartsWith("--", StringComparison.Ordinal))
                return arg.Substring(2);
            if (arg.StartsWith("-", StringComparison.Ordinal))
                return arg.Substring(1);
            return null;
        }

        // Mirrors IsArgValue in Runtime/Utilities/Argv.cpp, and also rejects values that would need quoting,
        // since the clone's arguments are joined without re-quoting.
        static bool IsSwitchValue(string arg)
        {
            if (string.IsNullOrEmpty(arg) || arg.IndexOfAny(k_UnquotedValueForbiddenChars) >= 0)
                return false;
            return arg[0] != '-' || arg.Length == 1 || char.IsDigit(arg[1]);
        }

        public static string BuildEditorModeArgument(string editorModeName)
            => $"{k_EditorMode} {editorModeName}";

        public static string BuildEditorDebuggingName(string projectName)
            => $"{k_EditorDebuggingName} \"{projectName}\"";

        public static string BuildCloudEnvironmentArgument(string cloudEnvironment)
            => $"{k_CloudEnvironment} {cloudEnvironment}";

        public static string ReadCurrentChannelName()
            => GetCommandLineArgumentValue(Environment.GetCommandLineArgs(), k_ChannelName);

        public static string ReadMainProcessId()
            => GetCommandLineArgumentValue(Environment.GetCommandLineArgs(), k_MainProcessId);

        public static string ReadCloudEnvironment()
            => GetCommandLineArgumentValue(Environment.GetCommandLineArgs(), k_CloudEnvironment);

        public static VirtualProjectIdentifier ReadVirtualProjectIdentifier()
        {
            var parameterValue = GetCommandLineArgumentValue(Environment.GetCommandLineArgs(), k_VirtualProjectIdentifier);

            VirtualProjectIdentifier.TryParse(parameterValue, out var identifier);
            return identifier;
        }

        public static bool IsUMPE() => HasCommandLineArgument(Environment.GetCommandLineArgs(), k_UMP);

        public static bool ReadIsClone()
            => HasCommandLineArgument(Environment.GetCommandLineArgs(), k_CloneProcess);
        public static bool ReadIsScenarioClone()
            => HasCommandLineArgument(Environment.GetCommandLineArgs(), k_ScenarioClone);
        public static bool ReadNoDownChainDependencies()
            => HasCommandLineArgument(Environment.GetCommandLineArgs(), k_NoDownChainDependencies);
        public static bool ReadRequestedClonePlaymode()
            => HasCommandLineArgument(Environment.GetCommandLineArgs(), k_RequestClonePlaymode);

        static string GetCommandLineArgumentValue(string[] commandLineArgs, string argumentName)
        {
            for (int i = 0; i < commandLineArgs.Length; i++)
            {
                var s = commandLineArgs[i];
                var withEqualSign = $"{argumentName}=";
                if (s.StartsWith(withEqualSign))
                {
                    return s.Replace(withEqualSign, string.Empty);
                }
                else if (s == argumentName && i + 1 < commandLineArgs.Length)
                {
                    return commandLineArgs[i + 1];
                }
            }

            return string.Empty;
        }

        static bool HasCommandLineArgument(string[] commandLineArgs, string argumentName)
        {
            foreach (var x in commandLineArgs)
            {
                if (x == argumentName) return true;
            }

            return false;
        }
    }
}
