// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using NiceIO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Utils;
using UnityEngine;
using UnityEngine.Scripting;
using Debug = UnityEngine.Debug;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor
{
    [MovedFrom("UnityEditor.LinuxStandalone")]
    public abstract class Sysroot
    {
        public abstract string Name { get; }
        public abstract bool Initialize();
        public abstract string HostPlatform { get; }
        public abstract string HostArch { get; }
        public abstract string TargetPlatform { get; }
        public abstract string TargetArch { get; }
        public abstract IEnumerable<string> GetIl2CppArguments();
        public abstract string GetSysrootPath();
        public abstract string GetToolchainPath();
        public abstract string GetIl2CppCompilerFlags();
        public abstract string GetIl2CppLinkerFlags();
        // The sysroot package does not currently contain an implemenation for this method, adding a default implementation to avoid breaking stuff
        public virtual string GetIl2CppLinkerFlagsFile() => null;
    }
}

namespace UnityEditorInternal
{
    internal static class SysrootManager
    {
        private static Dictionary<string, Sysroot> _knownSysroots = null;
        private static Dictionary<string, string> _archMap = null;
        private static string _hostPlatform = null;
        private static string _hostArch = null;

        private static string MakeKey(string hostPlatform, string hostArch, string targetPlatform, string targetArch)
        {
            return $"{hostPlatform.ToLower()},{hostArch.ToLower()},{targetPlatform.ToLower()},{targetArch.ToLower()}";
        }

        private static string MakeKey(string targetPlatform, string targetArch)
        {
            return MakeKey(_hostPlatform, _hostArch, targetPlatform, targetArch);
        }


        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            CreateArchMapping();
            RegisterSysroots();
        }

        private static void CreateArchMapping()
        {
            _archMap = new Dictionary<string, string>();
            _archMap.Add("amd64", "x86_64");
            _archMap.Add("i686", "x86");
        }

        private static string MapArch(string arch)
        {
            string mapped;
            if (_archMap.TryGetValue(arch.ToLower(), out mapped))
                return mapped;
            return arch.ToLower();
        }

        private static void RegisterSysroots()
        {
            _knownSysroots = new Dictionary<string, Sysroot>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<Sysroot>())
            {
                var sysroot = Activator.CreateInstance(type, new object[] {}, new object[] {}) as Sysroot;
                if (sysroot != null)
                {
                    if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("UNITY_SYSROOT_DEBUG")))
                        UnityEngine.Debug.Log($"Found sysroot: {sysroot.Name}, hp={sysroot.HostPlatform}, ha={sysroot.HostArch}, tp={sysroot.TargetPlatform}, ta={sysroot.TargetArch}");
                    _knownSysroots.Add(MakeKey(sysroot.HostPlatform, sysroot.HostArch, sysroot.TargetPlatform, sysroot.TargetArch), sysroot);
                }
            }
        }

        private static bool GetTargetPlatformAndArchFromBuildTarget(BuildTarget target, out string targetPlatform, out string targetArch)
        {
            switch (target)
            {
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.LinuxHeadlessSimulation:
                    targetPlatform = "linux";
                    targetArch = "x86_64";
                    return true;
                case BuildTarget.WebGL:
                    targetPlatform = "webgl";
                    targetArch = "";
                    return true;
                case BuildTarget.EmbeddedLinux:
                    targetPlatform = "embeddedlinux";
                    targetArch = "";
                    return true;
            }

            targetPlatform = null;
            targetArch = null;
            return false;
        }

        private static void GetPosixPlatformAndArch()
        {
            var p = new Process();
            p.StartInfo.FileName = "uname";
            p.StartInfo.Arguments = "-s -m";
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.Start();
            var parts = p.StandardOutput.ReadToEnd().Split(new char[] { ' ', '\r', '\n' });
            p.WaitForExit();
            if (parts.Length > 1)
            {
                _hostPlatform = parts[0].ToLower();
                _hostArch = MapArch(parts[1]);
            }
        }

        private static string AllowEnvironmentOverride(string origValue, string envVar)
        {
            string envValue = Environment.GetEnvironmentVariable(envVar);
            return envValue == null ? origValue : envValue;
        }

        private static bool GetHostPlatformAndArch()
        {
            if (_hostPlatform != null && _hostArch != null)
                return true;

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    _hostPlatform = "windows";
                    _hostArch = MapArch(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"));
                    break;
                case PlatformID.Unix:
                    GetPosixPlatformAndArch();
                    break;
            }

            _hostPlatform = AllowEnvironmentOverride(_hostPlatform, "UNITY_SYSROOT_HOST_PLATFORM");
            _hostArch = AllowEnvironmentOverride(_hostArch, "UNITY_SYSROOT_HOST_ARCH");

            return _hostPlatform != null && _hostArch != null;
        }

        public static Sysroot FindSysroot(BuildTarget target)
        {
            string targetPlatform, targetArch;
            if (!GetTargetPlatformAndArchFromBuildTarget(target, out targetPlatform, out targetArch))
                return null;

            return FindSysroot(targetPlatform, targetArch);
        }

        private static Sysroot FindSysroot(string targetPlatform, string targetArch)
        {
            if (!GetHostPlatformAndArch())
                return null;

            Sysroot sysroot;
            if (!_knownSysroots.TryGetValue(MakeKey(targetPlatform, targetArch), out sysroot))
                return null;

            if (!sysroot.Initialize())
            {
                UnityEngine.Debug.Log($"Failed to initialize sysroot {sysroot.Name}");
                return null;
            }

            return sysroot;
        }

        public static string HostTargetTuple(BuildTarget buildTarget)
        {
            if (GetHostPlatformAndArch())
            {
                string targetPlatform;
                string targetArch;
                if (GetTargetPlatformAndArchFromBuildTarget(buildTarget, out targetPlatform, out targetArch))
                {
                    string host;
                    switch (_hostPlatform)
                    {
                        case "darwin":
                            host = $"macos-{_hostArch}";
                            break;
                        case "windows":
                            host = $"win-{_hostArch}";
                            break;
                        default:
                            host = $"{_hostPlatform}-{_hostArch}";
                            break;
                    }
                    string target = String.IsNullOrEmpty(targetArch) ? targetPlatform : $"{targetPlatform}-{targetArch}";
                    return host == target ? target : $"{host}-{target}";
                }
            }
            return null;
        }

        public static IEnumerable<Sysroot> EnumerateSysroots()
        {
            foreach (Sysroot sysroot in _knownSysroots.Values)
            {
                yield return sysroot;
            }
        }
    }

    internal class IL2CPPUtils
    {
        private static readonly string[] BaseDefinesWindows = new[]
        {
            "_CRT_SECURE_NO_WARNINGS",
            "_WINSOCK_DEPRECATED_NO_WARNINGS",
            "WIN32",
            "WINDOWS",
            "_UNICODE",
            "UNICODE",
        };

        private static readonly string[] BaseDefines20 = new[]
        {
            "ALL_INTERIOR_POINTERS=1",
            "GC_GCJ_SUPPORT=1",
            "JAVA_FINALIZATION=1",
            "NO_EXECUTE_PERMISSION=1",
            "GC_NO_THREADS_DISCOVERY=1",
            "IGNORE_DYNAMIC_LOADING=1",
            "GC_DONT_REGISTER_MAIN_STATIC_DATA=1",
            "GC_VERSION_MAJOR=7",
            "GC_VERSION_MINOR=7",
            "GC_VERSION_MICRO=0",
            "GC_THREADS=1",
            "USE_MMAP=1",
            "USE_MUNMAP=1",
        }.ToArray();

        private static readonly string[] BaseDefines46 = BaseDefines20.Concat(new[]
        {
            "NET_4_0=1",
            "UNITY_AOT=1",
            "NET_STANDARD_2_0=1",
            "NET_UNITY_4_8=1",
            "NET_STANDARD=1",
        }).ToArray();

        internal static string ApiCompatibilityLevelToDotNetProfileArgument(ApiCompatibilityLevel compatibilityLevel, BuildTarget target)
        {
            switch (compatibilityLevel)
            {
                case ApiCompatibilityLevel.NET_2_0_Subset:
                    return "legacyunity";

                case ApiCompatibilityLevel.NET_2_0:
                    return "net20";

                case ApiCompatibilityLevel.NET_Unity_4_8:
                case ApiCompatibilityLevel.NET_Standard:
                    return "unityaot-" + BuildTargetDiscovery.GetPlatformProfileSuffix(target);

                default:
                    throw new NotSupportedException(string.Format("ApiCompatibilityLevel.{0} is not supported by IL2CPP!", compatibilityLevel));
            }
        }

        internal static string[] GetBuilderDefinedDefines(BuildTarget target, ApiCompatibilityLevel apiCompatibilityLevel, bool enableIl2CppDebugger)
        {
            List<string> defines = new List<string>();

            switch (apiCompatibilityLevel)
            {
                case ApiCompatibilityLevel.NET_2_0:
                case ApiCompatibilityLevel.NET_2_0_Subset:
                    defines.AddRange(BaseDefines20);
                    break;

                case ApiCompatibilityLevel.NET_Unity_4_8:
                case ApiCompatibilityLevel.NET_Standard:
                    defines.AddRange(BaseDefines46);
                    break;

                default:
                    throw new InvalidOperationException($"IL2CPP doesn't support building with {apiCompatibilityLevel} API compatibility level!");
            }

            if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64 ||
                target == BuildTarget.XboxOne || target == BuildTarget.WSAPlayer)
            {
                defines.AddRange(BaseDefinesWindows);

                if (target == BuildTarget.WSAPlayer)
                {
                    defines.Add("WINAPI_FAMILY=WINAPI_FAMILY_APP");
                }
                else
                {
                    defines.Add("WINAPI_FAMILY=WINAPI_FAMILY_DESKTOP_APP");
                }
            }

            if (enableIl2CppDebugger)
                defines.Add("IL2CPP_MONO_DEBUGGER=1");

            if (BuildPipeline.IsFeatureSupported("ENABLE_SCRIPTING_GC_WBARRIERS", target))
            {
                var hasGCBarrierValidation = PlayerSettings.gcWBarrierValidation;
                if (hasGCBarrierValidation)
                {
                    defines.Add("IL2CPP_ENABLE_STRICT_WRITE_BARRIERS=1");
                    defines.Add("IL2CPP_ENABLE_WRITE_BARRIER_VALIDATION=1");
                }

                var hasIncrementalGCTimeSlice = PlayerSettings.gcIncremental && (apiCompatibilityLevel == ApiCompatibilityLevel.NET_4_6 || apiCompatibilityLevel == ApiCompatibilityLevel.NET_Standard_2_0 ||
                    apiCompatibilityLevel == ApiCompatibilityLevel.NET_Unity_4_8 || apiCompatibilityLevel == ApiCompatibilityLevel.NET_Standard);

                if (hasGCBarrierValidation || hasIncrementalGCTimeSlice)
                {
                    var timeslice = hasIncrementalGCTimeSlice ? "3" : "0";
                    defines.Add("IL2CPP_ENABLE_WRITE_BARRIERS=1");
                    defines.Add($"IL2CPP_INCREMENTAL_TIME_SLICE={timeslice}");
                }
            }

            return defines.ToArray();
        }

        internal static string GetIl2CppFolder()
        {
            return GetIl2CppFolder(out var _);
        }

        internal static bool UsingDevelopmentBuild()
        {
            GetIl2CppFolder(out var isDevelopmentLocation);
            return isDevelopmentLocation;
        }

        static string GetIl2CppFolder(out bool isDevelopmentLocation)
        {
            var pathOverride = System.Environment.GetEnvironmentVariable("UNITY_IL2CPP_PATH");
            if (!string.IsNullOrEmpty(pathOverride))
            {
                isDevelopmentLocation = true;
                return pathOverride;
            }

            pathOverride = Debug.GetDiagnosticSwitch("VMIl2CppPath").value as string;
            if (!string.IsNullOrEmpty(pathOverride))
            {
                isDevelopmentLocation = true;
                return pathOverride;
            }

            isDevelopmentLocation = false;
            return Path.GetFullPath(Path.Combine(
                EditorApplication.applicationContentsPath,
                "il2cpp"));
        }

        internal static string GetBCLExtensionsFolder()
        {
            var il2CppFolder = GetIl2CppFolder(out var isDevelopmentLocation);
            if (isDevelopmentLocation)
                return Path.Combine(il2CppFolder, "build", "tests", "BCLExtensions", "net471");

            return Paths.Combine(il2CppFolder, "BCLExtensions");
        }

        internal static IEnumerable<string> GetBCLExtensionLibraries()
        {
            var bclExtensionsFolder = GetBCLExtensionsFolder();
            return new string[]
            {
                Path.Combine(bclExtensionsFolder, "System.Runtime.WindowsRuntime.dll"),
                Path.Combine(bclExtensionsFolder, "System.Runtime.WindowsRuntime.UI.Xaml.dll"),
            };
        }

        internal static string GetExePath(string toolName)
        {
            var platform = Application.platform;
            var il2CppFolder = GetIl2CppFolder(out var isDevelopmentLocation);
            var expectedToolExecutableName = $"{toolName}{(platform == RuntimePlatform.WindowsEditor ? ".exe" : "")}";

            if (isDevelopmentLocation)
            {
                // Locating the correct development build to use is a little tricky.  Complications come from
                // 1) We don't know if the Debug or Release build is desired.  To overcome this we will pick whichever was modified most recently
                // 2) We don't know if the published or non-published build is desired.  Again, we'll use whichever was modified most recently
                // 3) Published builds for all platforms may or may not be built.  We need to make sure not to pick a build for a different platform

                // Note that this logic will intentionally avoid checking for an expected TFM.  This is a dev build.  Using w/e is newest is probably
                // the most robust and maintainable approach.

                var toolBinDirectory = Path.Combine(il2CppFolder, toolName, "bin").ToNPath();
                var candidates = toolBinDirectory.Files($"*{expectedToolExecutableName}", recurse: true)
                    .OrderByDescending(f => f.GetLastWriteTimeUtc())
                    .ToArray();

                if (candidates.Length == 0)
                    throw new InvalidOperationException($"{toolName} does not appear to be built in {il2CppFolder}");

                var expectedPublishDirectoryName = BinaryDirectoryForPlatform(platform).ToNPath();

                foreach (var candidate in candidates)
                {
                    // Examples :
                    // 1)   il2cpp/bin/Release/<tfm>/il2cpp.exe
                    // 2)   il2cpp/bin/Debug/<tfm>/il2cpp.exe
                    if (candidate.Parent.Parent.Parent.FileName == "bin")
                    {
                        // Found a non-published build
                        return candidate.ToString();
                    }

                    // Examples :
                    // 1)   il2cpp/bin/Release/<tfm>/<platform dir>/publish/il2cpp.exe
                    // 2)   il2cpp/bin/Debug/<tfm>/<platform dir>/publish/il2cpp.exe
                    if (candidate.Parent.FileName == "publish" && candidate.Parent.Parent.FileName == expectedPublishDirectoryName)
                    {
                        // found a published build
                        return candidate.ToString();
                    }

                    // There is a 3rd path structure that we will ignore
                    // Examples :
                    // 1)   il2cpp/bin/Release/<tfm>/<platform dir>/il2cpp.exe
                    // 2)   il2cpp/bin/Debug/<tfm>/<platform dir>/il2cpp.exe
                }

                throw new InvalidOperationException($"Could not determine which of the {candidates.Length} of {toolName} to use.  The expected TFM or expected directory layout may have changed and this logic may need to be updated");
            }

            var deployDirectory = $"{il2CppFolder}/build/deploy";
            return $"{deployDirectory}/{expectedToolExecutableName}";
        }

        internal static string GetLibrarySearchPaths(string name, string tfm, string customRoot = null)
        {
            var il2CppFolder = GetIl2CppFolder(out var isDevelopmentLocation);
            var expectedToolExecutableName = $"{name}.dll";

            if (isDevelopmentLocation)
            {
                // Locating the correct development build to use is a little tricky.  Complications come from
                // 1) We don't know if the Debug or Release build is desired.  To overcome this we will pick whichever was modified most recently

                var topLevel = il2CppFolder;
                if (customRoot != null)
                    topLevel = Path.Combine(topLevel, customRoot);

                var toolBinDirectory = Path.Combine(topLevel, name, "bin").ToNPath();
                var candidates = toolBinDirectory.Files($"*{expectedToolExecutableName}", recurse: true)
                    .Where(f => f.Parent.FileName == tfm)
                    .OrderByDescending(f => f.GetLastWriteTimeUtc())
                    .ToArray();

                if (candidates.Length == 0)
                    throw new InvalidOperationException($"{name} does not appear to be built in {il2CppFolder}");

                return candidates[0].Parent.ToString();
            }

            throw new ArgumentException($"Could not locate assembly for {name}");
        }

        internal static string ConstructBeeLibrarySearchPath()
        {
            var projectBinDirs = new[]
            {
                GetLibrarySearchPaths("Unity.Options", "netstandard2.0", "repos/UnityOptions"),
                GetLibrarySearchPaths("Unity.Linker.Api", "netstandard2.0"),
                GetLibrarySearchPaths("Unity.IL2CPP.Api", "netstandard2.0"),
                GetLibrarySearchPaths("Unity.Api.Attributes", "netstandard2.0"),
                GetLibrarySearchPaths("Unity.IL2CPP.Bee.IL2CPPExeCompileCppBuildProgram.Data", "netstandard2.0"),
                GetLibrarySearchPaths("Unity.IL2CPP.Bee.BuildLogic", "net6.0"),
                // Now the quirky part.  We need to locate the platform build logic assemblies.
                // While il2cpp will have these during some build scenarios (IDE build or build.pl)
                // the project that will always have all of the is il2cpp-compile
                GetExePath("il2cpp-compile").ToNPath().Parent
            };

            return projectBinDirs.Aggregate(string.Empty, (accum, next) => $"{accum}{Path.PathSeparator}{next}");
        }

        internal static string GetAdditionalArguments()
        {
            var arguments = new List<string>();
            var additionalArgs = PlayerSettings.GetAdditionalIl2CppArgs();
            if (!string.IsNullOrEmpty(additionalArgs))
                arguments.Add(additionalArgs);

            additionalArgs = System.Environment.GetEnvironmentVariable("IL2CPP_ADDITIONAL_ARGS");
            if (!string.IsNullOrEmpty(additionalArgs))
            {
                arguments.Add(additionalArgs);
            }

            additionalArgs = Debug.GetDiagnosticSwitch("VMIl2CppAdditionalArgs").value as string;
            if (!string.IsNullOrEmpty(additionalArgs))
            {
                arguments.Add(additionalArgs.Trim('\''));
            }

            return arguments.Aggregate(String.Empty, (current, arg) => current + arg + " ");
        }

        private static string BinaryDirectoryForPlatform(RuntimePlatform platform)
        {
            if (platform == RuntimePlatform.WindowsEditor)
                return "win-x64";
            else if (platform == RuntimePlatform.LinuxEditor)
                return "linux-x64";

            var arch = RuntimeInformation.ProcessArchitecture.ToString().ToLower();
            if (platform == RuntimePlatform.OSXEditor && arch == "arm64")
                return "osx-arm64";
            return "osx-x64";
        }
    }

    internal class IL2CPPBuilder
    {
        [RequiredByNativeCode]
        public static string GetBuildAnalyticsSummaryCollectorExe()
        {
            return IL2CPPUtils.GetExePath("Analytics");
        }
    }
}
