// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bee.BeeDriver;
using BeeBuildProgramCommon.Data;
using NiceIO;
using UnityEditor.PackageManager;
using UnityEditorInternal;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class UnityBeeDriver
    {
        internal static readonly string BeeBackendExecutable = new NPath($"{EditorApplication.applicationContentsPath}/bee_backend{BeeScriptCompilation.ExecutableExtension}").ToString();
        internal static readonly string BeeCacheToolExecutable = $"{EditorApplication.applicationContentsPath}/Tools/BuildPipeline/BeeLocalCacheTool{BeeScriptCompilation.ExecutableExtension}";
        internal static readonly string BeeCacheDirEnvVar = "BEE_CACHE_DIRECTORY";
        internal static string BeeCacheDir => Environment.GetEnvironmentVariable(BeeCacheDirEnvVar) ?? new NPath($"{InternalEditorUtility.userAppDataFolder}/cache/bee").ToString(SlashMode.Native);

        [Serializable]
        internal class BeeBackendInfo
        {
            public string UnityVersion;
            public string BeeBackendHash;
        }

        internal static string BeeBackendHash
        {
            get
            {
                // Using SessionState, that way we won't need to rehash on domain reload.
                var hash = SessionState.GetString(nameof(BeeBackendHash), string.Empty);
                if (!string.IsNullOrEmpty(hash))
                    return hash;

                using var hasher = new SHA256Managed();
                using var stream = File.OpenRead(BeeBackendExecutable);
                var bytes = hasher.ComputeHash(stream);

                var sb = new StringBuilder();
                foreach (var b in bytes)
                    sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                hash = sb.ToString();

                SessionState.SetString(nameof(BeeBackendHash), hash);

                return hash;
            }
        }

        public static BeeBuildProgramCommon.Data.PackageInfo[] GetPackageInfos(string projectDirectory)
        {
            return PackageManager.PackageInfo.GetAllRegisteredPackages().Select(p =>
            {
                NPath resolvedPath = new NPath(p.resolvedPath);
                if (resolvedPath.IsChildOf(projectDirectory))
                    resolvedPath = resolvedPath.RelativeTo(projectDirectory);

                return new BeeBuildProgramCommon.Data.PackageInfo()
                {
                    Name = p.name,
                    ResolvedPath = resolvedPath.ToString(),
                };
            }).ToArray();
        }

        private static void RecreateDagDirectoryIfNeeded(NPath dagDirectory)
        {
            var beeBackendInfoPath = dagDirectory.Combine("bee_backend.info");
            var currentInfo = new BeeBackendInfo()
            {
                BeeBackendHash = BeeBackendHash,
                UnityVersion = Application.unityVersion
            };

            var diskInfo = new BeeBackendInfo();

            // Clear dag directory if it was produced with a different bee_backend, to avoid problem where bee_backend sometimes looses track of files.
            if (dagDirectory.Exists())
            {
                if (beeBackendInfoPath.Exists())
                {
                    var contents = beeBackendInfoPath.ReadAllText();
                    EditorJsonUtility.FromJsonOverwrite(contents, diskInfo);

                    // Note: We're clearing dag directory only when bee backend hash has changed, it's fine for Unity version to be different.
                    //       Unity version is used here for informational purposes, so we can clearly see from which Unity version the user was upgrading
                    if (string.IsNullOrEmpty(diskInfo.BeeBackendHash) ||
                        !diskInfo.BeeBackendHash.Equals(currentInfo.BeeBackendHash))
                    {
                        Console.WriteLine($"Clearing Bee directory '{dagDirectory}', since bee backend hash ('{beeBackendInfoPath}') is different, previous hash was {diskInfo.BeeBackendHash} (Unity version: {diskInfo.UnityVersion}), current hash is {currentInfo.BeeBackendHash} (Unity version: {currentInfo.UnityVersion}).");
                        dagDirectory.Delete();
                    }
                }
                else
                {
                    Console.WriteLine($"Clearing Bee directory '{dagDirectory}', since bee backend information ('{beeBackendInfoPath}') is missing.");
                    dagDirectory.Delete();
                }
            }

            dagDirectory.CreateDirectory();

            // Update info, if at least of one the fields is different
            if (string.IsNullOrEmpty(diskInfo.BeeBackendHash) ||
                string.IsNullOrEmpty(diskInfo.UnityVersion) ||
                !diskInfo.BeeBackendHash.Equals(currentInfo.BeeBackendHash) ||
                !diskInfo.UnityVersion.Equals(currentInfo.UnityVersion))
            {
                beeBackendInfoPath.WriteAllText(EditorJsonUtility.ToJson(currentInfo, true));
            }
        }

        public static BuildRequest BuildRequestFor(RunnableProgram buildProgram, EditorCompilation editorCompilation, string dagName, CacheMode cacheMode, string dagDirectory = null, bool useScriptUpdater = true)
        {
            return BuildRequestFor(buildProgram, dagName, dagDirectory, useScriptUpdater, editorCompilation.projectDirectory, new ILPostProcessingProgram(), cacheMode, StdOutModeForScriptCompilation);
        }

        public const StdOutMode StdOutModeForScriptCompilation =
            StdOutMode.LogStartArgumentsAndExitcode | StdOutMode.LogStdOutOnFinish;
        public const StdOutMode StdOutModeForPlayerBuilds =
            StdOutMode.LogStartArgumentsAndExitcode | StdOutMode.Stream;

        public static BuildRequest BuildRequestFor(
                RunnableProgram buildProgram,
                string dagName,
                string dagDirectory,
                bool useScriptUpdater,
                string projectDirectory,
                ILPostProcessingProgram ilpp,
                CacheMode cacheMode,
                StdOutMode stdoutMode,
                RunnableProgram beeBackendProgram = null)
        {
            NPath dagDir = dagDirectory ?? "Library/Bee";
            RecreateDagDirectoryIfNeeded(dagDir);
            var performingPlayerBuild = UnityBeeDriverProfilerSession.PerformingPlayerBuild;
            NPath profilerOutputFile =  performingPlayerBuild ? UnityBeeDriverProfilerSession.GetTraceEventsOutputForPlayerBuild() : $"{dagDir}/fullprofile.json";
            return new BuildRequest()
            {
                BuildProgram = buildProgram,
                BackendProgram = beeBackendProgram ?? UnityBeeBackendProgram(cacheMode, stdoutMode),
                ProjectRoot = projectDirectory,
                DagName = dagName,
                BuildStateDirectory = dagDir.EnsureDirectoryExists().ToString(),
                ProfilerOutputFile = profilerOutputFile.ToString(),
                // Use a null process name during a player build to avoid writing process metadata.  The player profiler will take care of writing the process metadata
                ProfilerProcessName = performingPlayerBuild ? null : "BeeDriver",
                SourceFileUpdaters = useScriptUpdater
                    ? new[] {new UnityScriptUpdater(projectDirectory)}
                    : Array.Empty<SourceFileUpdaterBase>(),
                ProcessSourceFileUpdatersResult = new UnitySourceFileUpdatersResultHandler(),

                DataForBuildProgram =
                {
                    () => new ConfigurationData
            {
                Il2CppDir = IL2CPPUtils.GetIl2CppFolder(),
                Il2CppPath = IL2CPPUtils.GetExePath("il2cpp"),
                UnityLinkerPath = IL2CPPUtils.GetExePath("UnityLinker"),
                NetCoreRunPath = NetCoreRunProgram.NetCoreRunPath,
                DotNetExe = NetCoreProgram.DotNetMuxerPath.ToString(),
                EditorContentsPath = EditorApplication.applicationContentsPath,
                Packages = GetPackageInfos(NPath.CurrentDirectory.ToString()),
                UnityVersion = Application.unityVersion,
                UnityVersionNumeric = new BeeBuildProgramCommon.Data.Version(Application.unityVersionVer, Application.unityVersionMaj, Application.unityVersionMin),
                UnitySourceCodePath = Unsupported.IsSourceBuild(false) ? Unsupported.GetBaseUnityDeveloperFolder() : null,
                AdvancedLicense = PlayerSettings.advancedLicense,
                Batchmode = InternalEditorUtility.inBatchMode,
                EmitDataForBeeWhy = (Debug.GetDiagnosticSwitch("EmitDataForBeeWhy").value as bool?)?? false,
                        NamedPipeOrUnixSocket = ilpp.NamedPipeOrUnixSocket,
                    }
                }
            };
        }

        public static void RunCleanBeeCache()
        {
            // Note that this size is spefied as the total number of used bytes for all the files in the cache
            // and not as the actual size of occupied blocks on the disk, which will add some overhead.
            long cacheSize = 256 * 1024 * 1024;
            var psi = new ProcessStartInfo()
            {
                FileName = BeeCacheToolExecutable,
                Arguments = $"clean {cacheSize}",
                UseShellExecute = false
            };
            psi.EnvironmentVariables[BeeCacheDirEnvVar] = BeeCacheDir;
            Process.Start(psi);
        }

        public enum CacheMode
        {
            Off,
            ReadOnly,
            WriteOnly,
            ReadWrite,
        }

        internal static RunnableProgram UnityBeeBackendProgram(CacheMode cacheMode, StdOutMode stdoutMode)
        {
            var env = new Dictionary<string, string>()
            {
                { "BEE_CACHE_BEHAVIOUR", cacheMode switch
                {
                    CacheMode.Off => "_",
                    CacheMode.ReadOnly => "R",
                    CacheMode.WriteOnly => "W",
                    CacheMode.ReadWrite => "RW",
                    _ => throw new ArgumentOutOfRangeException(nameof(cacheMode), cacheMode, null)
                }},
                { "CACHE_SERVER_ADDRESS", "none_but_bee_still_wants_us_to_set_it" },
                { "REAPI_CACHE_CLIENT", $"\"{BeeCacheToolExecutable}\"" },
                { BeeCacheDirEnvVar, BeeCacheDir },
                { "CHROMETRACE_TIMEOFFSET", "unixepoch" }
            };
            return new SystemProcessRunnableProgram(BeeBackendExecutable,
                alwaysEnvironmentVariables: env,
                stdOutMode: stdoutMode);
        }
    }
}
