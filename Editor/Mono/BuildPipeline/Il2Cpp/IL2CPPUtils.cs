// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Player;
using UnityEditor.Build.Reporting;
using UnityEditor.Il2Cpp;
using UnityEditor.Scripting;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Utils;
using UnityEngine;
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

        private static string MakeKey(Sysroot sysroot)
        {
            return MakeKey(sysroot.HostPlatform, sysroot.HostArch, sysroot.TargetPlatform, sysroot.TargetArch);
        }

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
                case BuildTarget.CloudRendering:
                    targetPlatform = "linux";
                    targetArch = "x86_64";
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
                    _hostPlatform = "Windows";
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
            "NET_STANDARD_2_0=1"
        }).ToArray();

        public const string BinaryMetadataSuffix = "-metadata.dat";

        internal static IIl2CppPlatformProvider PlatformProviderForNotModularPlatform(BuildTarget target, bool developmentBuild)
        {
            throw new Exception("Platform unsupported, or already modular.");
        }

        internal static IL2CPPBuilder RunIl2Cpp(string tempFolder, string stagingAreaData, IIl2CppPlatformProvider platformProvider, Action<string> modifyOutputBeforeCompile, RuntimeClassRegistry runtimeClassRegistry)
        {
            var builder = new IL2CPPBuilder(tempFolder, stagingAreaData, platformProvider, modifyOutputBeforeCompile, runtimeClassRegistry, IL2CPPUtils.UseIl2CppCodegenWithMonoBackend(BuildPipeline.GetBuildTargetGroup(platformProvider.target)));
            builder.Run();
            return builder;
        }

        internal static IL2CPPBuilder RunIl2Cpp(string stagingAreaData, IIl2CppPlatformProvider platformProvider, Action<string> modifyOutputBeforeCompile, RuntimeClassRegistry runtimeClassRegistry)
        {
            var builder = new IL2CPPBuilder(stagingAreaData, stagingAreaData, platformProvider, modifyOutputBeforeCompile, runtimeClassRegistry, IL2CPPUtils.UseIl2CppCodegenWithMonoBackend(BuildPipeline.GetBuildTargetGroup(platformProvider.target)));
            builder.Run();
            return builder;
        }

        internal static IL2CPPBuilder RunCompileAndLink(string tempFolder, string stagingAreaData, IIl2CppPlatformProvider platformProvider, Action<string> modifyOutputBeforeCompile, RuntimeClassRegistry runtimeClassRegistry)
        {
            var builder = new IL2CPPBuilder(tempFolder, stagingAreaData, platformProvider, modifyOutputBeforeCompile, runtimeClassRegistry, IL2CPPUtils.UseIl2CppCodegenWithMonoBackend(BuildPipeline.GetBuildTargetGroup(platformProvider.target)));
            builder.RunCompileAndLink();
            return builder;
        }

        internal static void CopyEmbeddedResourceFiles(string tempFolder, string destinationFolder)
        {
            foreach (var file in Directory.GetFiles(Paths.Combine(IL2CPPBuilder.GetCppOutputPath(tempFolder), "Data", "Resources")).Where(f => f.EndsWith("-resources.dat")))
                File.Copy(file, Paths.Combine(destinationFolder, Path.GetFileName(file)), true);
        }

        internal static void CopySymmapFile(string tempFolder, string destinationFolder)
        {
            CopySymmapFile(tempFolder, destinationFolder, string.Empty);
        }

        internal static void CopySymmapFile(string tempFolder, string destinationFolder, string destinationFileNameSuffix)
        {
            const string fileName = "SymbolMap";
            var file = Path.Combine(tempFolder, fileName);
            if (File.Exists(file))
                File.Copy(file, Path.Combine(destinationFolder, fileName + destinationFileNameSuffix), true);
        }

        internal static void CopyMetadataFiles(string tempFolder, string destinationFolder)
        {
            foreach (var file in Directory.GetFiles(Paths.Combine(IL2CPPBuilder.GetCppOutputPath(tempFolder), "Data", "Metadata")).Where(f => f.EndsWith(BinaryMetadataSuffix)))
                File.Copy(file, Paths.Combine(destinationFolder, Path.GetFileName(file)), true);
        }

        internal static void CopyConfigFiles(string tempFolder, string destinationFolder)
        {
            var sourceFolder = Paths.Combine(IL2CPPBuilder.GetCppOutputPath(tempFolder), "Data", "etc");
            FileUtil.CopyDirectoryRecursive(sourceFolder, destinationFolder);
        }

        internal static string ApiCompatibilityLevelToDotNetProfileArgument(ApiCompatibilityLevel compatibilityLevel)
        {
            switch (compatibilityLevel)
            {
                case ApiCompatibilityLevel.NET_2_0_Subset:
                    return "legacyunity";

                case ApiCompatibilityLevel.NET_2_0:
                    return "net20";

                case ApiCompatibilityLevel.NET_4_6:
                case ApiCompatibilityLevel.NET_Standard_2_0:
                    return "unityaot";

                default:
                    throw new NotSupportedException(string.Format("ApiCompatibilityLevel.{0} is not supported by IL2CPP!", compatibilityLevel));
            }
        }

        internal static bool UseIl2CppCodegenWithMonoBackend(BuildTargetGroup targetGroup)
        {
            return EditorApplication.useLibmonoBackendForIl2cpp &&
                PlayerSettings.GetScriptingBackend(targetGroup) == ScriptingImplementation.IL2CPP;
        }

        internal static bool EnableIL2CPPDebugger(IIl2CppPlatformProvider provider, BuildTargetGroup targetGroup)
        {
            if (!provider.allowDebugging || !provider.development)
                return false;

            switch (PlayerSettings.GetApiCompatibilityLevel(targetGroup))
            {
                case ApiCompatibilityLevel.NET_4_6:
                case ApiCompatibilityLevel.NET_Standard_2_0:
                    return true;

                default:
                    return false;
            }
        }

        internal static string[] GetBuilderDefinedDefines(IIl2CppPlatformProvider il2cppPlatformProvider, BuildTargetGroup buildTargetGroup)
        {
            List<string> defines = new List<string>();
            var apiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup);

            switch (apiCompatibilityLevel)
            {
                case ApiCompatibilityLevel.NET_2_0:
                case ApiCompatibilityLevel.NET_2_0_Subset:
                    defines.AddRange(BaseDefines20);
                    break;

                case ApiCompatibilityLevel.NET_4_6:
                case ApiCompatibilityLevel.NET_Standard_2_0:
                    defines.AddRange(BaseDefines46);
                    break;

                default:
                    throw new InvalidOperationException($"IL2CPP doesn't support building with {apiCompatibilityLevel} API compatibility level!");
            }


            var target = il2cppPlatformProvider.target;
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

            if (EnableIL2CPPDebugger(il2cppPlatformProvider, buildTargetGroup))
                defines.Add("IL2CPP_MONO_DEBUGGER=1");

            if (BuildPipeline.IsFeatureSupported("ENABLE_SCRIPTING_GC_WBARRIERS", target))
            {
                var hasGCBarrierValidation = PlayerSettings.gcWBarrierValidation;
                if (hasGCBarrierValidation)
                {
                    defines.Add("IL2CPP_ENABLE_STRICT_WRITE_BARRIERS=1");
                    defines.Add("IL2CPP_ENABLE_WRITE_BARRIER_VALIDATION=1");
                }

                var hasIncrementalGCTimeSlice = PlayerSettings.gcIncremental && (apiCompatibilityLevel == ApiCompatibilityLevel.NET_4_6 || apiCompatibilityLevel == ApiCompatibilityLevel.NET_Standard_2_0);

                if (hasGCBarrierValidation || hasIncrementalGCTimeSlice)
                {
                    var timeslice = hasIncrementalGCTimeSlice ? "3" : "0";
                    defines.Add("IL2CPP_ENABLE_WRITE_BARRIERS=1");
                    defines.Add($"IL2CPP_INCREMENTAL_TIME_SLICE={timeslice}");
                }
            }

            return defines.ToArray();
        }

        internal static string[] GetDebuggerIL2CPPArguments(IIl2CppPlatformProvider il2cppPlatformProvider, BuildTargetGroup buildTargetGroup)
        {
            var arguments = new List<string>();

            if (EnableIL2CPPDebugger(il2cppPlatformProvider, buildTargetGroup))
                arguments.Add("--enable-debugger");

            return arguments.ToArray();
        }

        internal static string[] GetBuildingIL2CPPArguments(IIl2CppPlatformProvider il2cppPlatformProvider, BuildTargetGroup buildTargetGroup)
        {
            // When changing this function, don't forget to change GetBuilderDefinedDefines!
            var arguments = new List<string>();
            var apiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup);

            if (BuildPipeline.IsFeatureSupported("ENABLE_SCRIPTING_GC_WBARRIERS", il2cppPlatformProvider.target))
            {
                var hasGCBarrierValidation = PlayerSettings.gcWBarrierValidation;
                if (hasGCBarrierValidation)
                    arguments.Add("--write-barrier-validation");

                var hasIncrementalGCTimeSlice = PlayerSettings.gcIncremental && (apiCompatibilityLevel == ApiCompatibilityLevel.NET_4_6 || apiCompatibilityLevel == ApiCompatibilityLevel.NET_Standard_2_0);
                if (hasIncrementalGCTimeSlice)
                    arguments.Add("--incremental-g-c-time-slice=3");
            }

            arguments.Add("--profiler-report");

            return arguments.ToArray();
        }

        internal static string GetIl2CppFolder()
        {
            var pathOverride = System.Environment.GetEnvironmentVariable("UNITY_IL2CPP_PATH");
            if (!string.IsNullOrEmpty(pathOverride))
                return pathOverride;

            pathOverride = Debug.GetDiagnosticSwitch("VMIl2CppPath") as string;
            if (!string.IsNullOrEmpty(pathOverride))
                return pathOverride;

            return Path.GetFullPath(Path.Combine(
                EditorApplication.applicationContentsPath,
                "il2cpp"));
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

            additionalArgs = Debug.GetDiagnosticSwitch("VMIl2CppAdditionalArgs") as string;
            if (!string.IsNullOrEmpty(additionalArgs))
            {
                arguments.Add(additionalArgs.Trim('\''));
            }

            return arguments.Aggregate(String.Empty, (current, arg) => current + arg + " ");
        }
    }

    internal class IL2CPPBuilder
    {
        private readonly string m_TempFolder;
        private readonly string m_StagingAreaData;
        private readonly IIl2CppPlatformProvider m_PlatformProvider;
        private readonly Action<string> m_ModifyOutputBeforeCompile;
        private readonly RuntimeClassRegistry m_RuntimeClassRegistry;
        private readonly bool m_BuildForMonoRuntime;

        public IL2CPPBuilder(string tempFolder, string stagingAreaData, IIl2CppPlatformProvider platformProvider, Action<string> modifyOutputBeforeCompile, RuntimeClassRegistry runtimeClassRegistry, bool buildForMonoRuntime)
        {
            m_TempFolder = tempFolder;
            m_StagingAreaData = stagingAreaData;
            m_PlatformProvider = platformProvider;
            m_ModifyOutputBeforeCompile = modifyOutputBeforeCompile;
            m_RuntimeClassRegistry = runtimeClassRegistry;
            m_BuildForMonoRuntime = buildForMonoRuntime;
        }

        public void Run()
        {
            var outputDirectory = GetCppOutputDirectoryInStagingArea();
            var managedDir = Path.GetFullPath(Path.Combine(m_StagingAreaData, "Managed"));

            // Make all assemblies in Staging/Managed writable for stripping.
            foreach (var file in Directory.GetFiles(managedDir))
            {
                var fileInfo = new FileInfo(file);
                fileInfo.IsReadOnly = false;
            }

            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(m_PlatformProvider.target);

            var managedStrippingLevel = PlayerSettings.GetManagedStrippingLevel(buildTargetGroup);

            // IL2CPP does not support a managed stripping level of disabled. If the player settings
            // do try this (which should not be possible from the editor), use Low instead.
            if (managedStrippingLevel == ManagedStrippingLevel.Disabled)
                managedStrippingLevel = ManagedStrippingLevel.Low;
            AssemblyStripper.StripAssemblies(managedDir, m_PlatformProvider.CreateUnityLinkerPlatformProvider(), m_PlatformProvider, m_RuntimeClassRegistry, managedStrippingLevel);

            // The IL2CPP editor integration here is responsible to give il2cpp.exe an empty directory to use.
            FileUtil.CreateOrCleanDirectory(outputDirectory);

            if (m_ModifyOutputBeforeCompile != null)
                m_ModifyOutputBeforeCompile(outputDirectory);

            var pipelineData = new Il2CppBuildPipelineData(m_PlatformProvider.target, managedDir);

            ConvertPlayerDlltoCpp(pipelineData, outputDirectory, managedDir, m_PlatformProvider.supportsManagedDebugging);

            var compiler = m_PlatformProvider.CreateNativeCompiler();
            if (compiler != null && m_PlatformProvider.CreateIl2CppNativeCodeBuilder() == null)
            {
                var nativeLibPath = OutputFileRelativePath();

                var includePaths = new List<string>(m_PlatformProvider.includePaths);
                includePaths.Add(outputDirectory);

                m_PlatformProvider.CreateNativeCompiler().CompileDynamicLibrary(
                    nativeLibPath,
                    NativeCompiler.AllSourceFilesIn(outputDirectory),
                    includePaths,
                    m_PlatformProvider.libraryPaths,
                    new string[0]);
            }
        }

        public void RunCompileAndLink()
        {
            var il2CppNativeCodeBuilder = m_PlatformProvider.CreateIl2CppNativeCodeBuilder();
            if (il2CppNativeCodeBuilder != null)
            {
                Il2CppNativeCodeBuilderUtils.ClearAndPrepareCacheDirectory(il2CppNativeCodeBuilder);

                var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(m_PlatformProvider.target);
                var compilerConfiguration = PlayerSettings.GetIl2CppCompilerConfiguration(buildTargetGroup);
                var arguments = Il2CppNativeCodeBuilderUtils.AddBuilderArguments(il2CppNativeCodeBuilder, OutputFileRelativePath(), m_PlatformProvider.includePaths, m_PlatformProvider.libraryPaths, compilerConfiguration).ToList();

                var additionalArgs = IL2CPPUtils.GetAdditionalArguments();
                if (!string.IsNullOrEmpty(additionalArgs))
                    arguments.Add(additionalArgs);

                arguments.Add($"--map-file-parser={CommandLineFormatter.PrepareFileName(GetMapFileParserPath())}");
                arguments.Add($"--generatedcppdir={CommandLineFormatter.PrepareFileName(Path.GetFullPath(GetCppOutputDirectoryInStagingArea()))}");
                arguments.Add(string.Format("--dotnetprofile=\"{0}\"", IL2CPPUtils.ApiCompatibilityLevelToDotNetProfileArgument(PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup))));
                arguments.AddRange(IL2CPPUtils.GetDebuggerIL2CPPArguments(m_PlatformProvider, buildTargetGroup));
                Action<ProcessStartInfo> setupStartInfo = il2CppNativeCodeBuilder.SetupStartInfo;
                var managedDir = Path.GetFullPath(Path.Combine(m_StagingAreaData, "Managed"));

                RunIl2CppWithArguments(arguments, setupStartInfo, managedDir);
            }
        }

        private string OutputFileRelativePath()
        {
            var nativeLibPath = Path.Combine(m_StagingAreaData, "Native");
            Directory.CreateDirectory(nativeLibPath);
            nativeLibPath = Path.Combine(nativeLibPath, m_PlatformProvider.nativeLibraryFileName);
            return nativeLibPath;
        }

        public string GetCppOutputDirectoryInStagingArea()
        {
            return GetCppOutputPath(m_TempFolder);
        }

        public static string GetCppOutputPath(string tempFolder)
        {
            return Path.Combine(tempFolder, "il2cppOutput");
        }

        public static string GetMapFileParserPath()
        {
            return Path.GetFullPath(
                Path.Combine(
                    EditorApplication.applicationContentsPath,
                    Application.platform == RuntimePlatform.WindowsEditor ? @"Tools\MapFileParser\MapFileParser.exe" : @"Tools/MapFileParser/MapFileParser"));
        }

        static void ProcessBuildPipelineOnBeforeConvertRun(BuildReport report, Il2CppBuildPipelineData data)
        {
            var processors = BuildPipelineInterfaces.processors.il2cppProcessors;
            if (processors == null)
                return;

            foreach (var processor in processors)
                processor.OnBeforeConvertRun(report, data);
        }

        private void ConvertPlayerDlltoCpp(Il2CppBuildPipelineData data, string outputDirectory, string workingDirectory, bool platformSupportsManagedDebugging)
        {
            ProcessBuildPipelineOnBeforeConvertRun(m_PlatformProvider.buildReport, data);

            var arguments = new List<string>();

            arguments.Add("--convert-to-cpp");

            if (m_PlatformProvider.emitNullChecks)
                arguments.Add("--emit-null-checks");

            if (m_PlatformProvider.enableStackTraces)
                arguments.Add("--enable-stacktrace");

            if (m_PlatformProvider.enableArrayBoundsCheck)
                arguments.Add("--enable-array-bounds-check");

            if (m_PlatformProvider.enableDivideByZeroCheck)
                arguments.Add("--enable-divide-by-zero-check");

            if (m_PlatformProvider.development && m_PlatformProvider.enableDeepProfilingSupport)
                arguments.Add("--enable-deep-profiler");

            if (m_BuildForMonoRuntime)
                arguments.Add("--mono-runtime");

            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(m_PlatformProvider.target);
            var apiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup);
            arguments.Add(string.Format("--dotnetprofile=\"{0}\"", IL2CPPUtils.ApiCompatibilityLevelToDotNetProfileArgument(apiCompatibilityLevel)));


            var il2CppNativeCodeBuilder = m_PlatformProvider.CreateIl2CppNativeCodeBuilder();
            if (il2CppNativeCodeBuilder != null)
            {
                var compilerConfiguration = PlayerSettings.GetIl2CppCompilerConfiguration(buildTargetGroup);
                Il2CppNativeCodeBuilderUtils.ClearAndPrepareCacheDirectory(il2CppNativeCodeBuilder);
                arguments.AddRange(Il2CppNativeCodeBuilderUtils.AddBuilderArguments(il2CppNativeCodeBuilder, OutputFileRelativePath(), m_PlatformProvider.includePaths, m_PlatformProvider.libraryPaths, compilerConfiguration));
            }

            arguments.AddRange(IL2CPPUtils.GetDebuggerIL2CPPArguments(m_PlatformProvider, buildTargetGroup));
            arguments.AddRange(IL2CPPUtils.GetBuildingIL2CPPArguments(m_PlatformProvider, buildTargetGroup));
            arguments.Add($"--map-file-parser={CommandLineFormatter.PrepareFileName(GetMapFileParserPath())}");

            var additionalArgs = IL2CPPUtils.GetAdditionalArguments();
            if (!string.IsNullOrEmpty(additionalArgs))
                arguments.Add(additionalArgs);

            arguments.Add($"--directory={CommandLineFormatter.PrepareFileName(Path.GetFullPath(data.inputDirectory))}");

            arguments.Add($"--generatedcppdir={CommandLineFormatter.PrepareFileName(Path.GetFullPath(outputDirectory))}");

            // NOTE: any arguments added here that affect how generated code is built need
            // to also be added to PlatformDependent\Win\Extensions\Managed\VisualStudioProjectHelpers.cs
            // as that file generated project files that invoke back into IL2CPP in order to build
            // generated code

            string progressMessage = "Converting managed assemblies to C++";
            if (il2CppNativeCodeBuilder != null)
            {
                progressMessage = "Building native binary with IL2CPP...";
            }

            if (EditorUtility.DisplayCancelableProgressBar("Building Player", progressMessage, 0.3f))
                throw new OperationCanceledException();

            Action<ProcessStartInfo> setupStartInfo = null;
            if (il2CppNativeCodeBuilder != null)
                setupStartInfo = il2CppNativeCodeBuilder.SetupStartInfo;

            if (PlayerBuildInterface.ExtraTypesProvider != null)
            {
                var extraTypes = new HashSet<string>();
                foreach (var extraType in PlayerBuildInterface.ExtraTypesProvider())
                {
                    extraTypes.Add(extraType);
                }

                var tempFile = Path.GetFullPath(Path.Combine(m_TempFolder, "extra-types.txt"));
                File.WriteAllLines(tempFile, extraTypes.ToArray());
                arguments.Add($"--extra-types-file={CommandLineFormatter.PrepareFileName(tempFile)}");
            }

            RunIl2CppWithArguments(arguments, setupStartInfo, workingDirectory);
        }

        private void RunIl2CppWithArguments(List<string> arguments, Action<ProcessStartInfo> setupStartInfo, string workingDirectory)
        {
            var args = arguments.Aggregate(String.Empty, (current, arg) => current + arg + " ");

            var useNetCore = ShouldUseIl2CppCore();
            string il2CppPath = useNetCore ? GetIl2CppCoreExe() : GetIl2CppExe();

            Console.WriteLine("Invoking il2cpp with arguments: " + args);

            CompilerOutputParserBase il2cppOutputParser = m_PlatformProvider.CreateIl2CppOutputParser();
            if (il2cppOutputParser == null)
                il2cppOutputParser = new Il2CppOutputParser();

            if (useNetCore)
                Runner.RunNetCoreProgram(il2CppPath, args, workingDirectory, il2cppOutputParser, setupStartInfo);
            else
                Runner.RunManagedProgram(il2CppPath, args, workingDirectory, il2cppOutputParser, setupStartInfo);
        }

        private string GetIl2CppExe()
        {
            return IL2CPPUtils.GetIl2CppFolder() + "/build/deploy/net471/il2cpp.exe";
        }

        private string GetIl2CppCoreExe()
        {
            return IL2CPPUtils.GetIl2CppFolder() + "/build/deploy/il2cppcore/il2cppcore.dll";
        }

        private bool ShouldUseIl2CppCore()
        {
            if (!m_PlatformProvider.supportsUsingIl2cppCore)
                return false;

            var disableIl2CppCoreEnv = System.Environment.GetEnvironmentVariable("UNITY_IL2CPP_DISABLE_NET_CORE");
            if (disableIl2CppCoreEnv == "1")
                return false;

            var disableIl2CppCoreDiag = (bool)(Debug.GetDiagnosticSwitch("VMIl2CppDisableNetCore") ?? false);
            if (disableIl2CppCoreDiag)
                return false;

            bool shouldUse = false;
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                // .Net Core 2.1 is only supported on MacOSX versions 10.12 and later
                if (SystemInfo.operatingSystem.StartsWith("Mac OS X 10."))
                {
                    var versionText = SystemInfo.operatingSystem.Substring(9);
                    var version = new Version(versionText);

                    if (version >= new Version(10, 12))
                        shouldUse = true;
                }
                else
                {
                    shouldUse = true;
                }
            }

            return shouldUse && NetCoreProgram.IsNetCoreAvailable();
        }
    }

    internal interface IIl2CppPlatformProvider
    {
        BuildTarget target { get; }
        bool emitNullChecks { get; }
        bool enableStackTraces { get; }
        bool enableArrayBoundsCheck { get; }
        bool enableDivideByZeroCheck { get; }
        bool enableDeepProfilingSupport { get; }
        string nativeLibraryFileName { get; }
        bool supportsManagedDebugging { get; }
        bool supportsUsingIl2cppCore { get; }
        bool development { get; }
        bool allowDebugging { get; }
        bool scriptsOnlyBuild { get; }

        BuildReport buildReport { get; }
        string[] includePaths { get; }
        string[] libraryPaths { get; }

        INativeCompiler CreateNativeCompiler();
        Il2CppNativeCodeBuilder CreateIl2CppNativeCodeBuilder();
        CompilerOutputParserBase CreateIl2CppOutputParser();

        BaseUnityLinkerPlatformProvider CreateUnityLinkerPlatformProvider();
    }

    internal abstract class BaseIl2CppPlatformProvider : IIl2CppPlatformProvider
    {
        public BaseIl2CppPlatformProvider(BuildTarget target, string libraryFolder, BuildReport buildReport)
        {
            this.target = target;
            this.libraryFolder = libraryFolder;
            this.buildReport = buildReport;
        }

        public virtual BuildTarget target { get; private set; }

        public virtual string libraryFolder { get; private set; }

        public virtual bool emitNullChecks
        {
            get { return true; }
        }

        // This is an opt-in setting, as most platforms will want to use native stacktrace mechanisms enabled by MapFileParser
        public virtual bool enableStackTraces
        {
            get { return false; }
        }

        public virtual bool enableArrayBoundsCheck
        {
            get { return true; }
        }

        public virtual bool enableDivideByZeroCheck
        {
            get { return false; }
        }

        public virtual bool enableDeepProfilingSupport
        {
            get
            {
                if (buildReport != null)
                    return (buildReport.summary.options & BuildOptions.EnableDeepProfilingSupport) == BuildOptions.EnableDeepProfilingSupport;
                return false;
            }
        }

        public virtual bool supportsManagedDebugging
        {
            get { return false; }
        }

        public virtual bool supportsUsingIl2cppCore
        {
            get { return true; }
        }

        public virtual bool development
        {
            get
            {
                if (buildReport != null)
                    return (buildReport.summary.options & BuildOptions.Development) == BuildOptions.Development;
                return false;
            }
        }

        public virtual bool allowDebugging
        {
            get
            {
                if (buildReport != null)
                    return (buildReport.summary.options & BuildOptions.AllowDebugging) == BuildOptions.AllowDebugging;
                return false;
            }
        }

        public virtual bool scriptsOnlyBuild
        {
            get
            {
                if (buildReport != null)
                    return (buildReport.summary.options & BuildOptions.BuildScriptsOnly) == BuildOptions.BuildScriptsOnly;
                return false;
            }
        }

        public BuildReport buildReport { get; private set; }

        public virtual string[] includePaths
        {
            get
            {
                return new[]
                {
                    Path.Combine(libraryFolder, "bdwgc/include"),
                    Path.Combine(libraryFolder, "libil2cpp/include")
                };
            }
        }

        public virtual string[] libraryPaths
        {
            get
            {
                return new string[0];
            }
        }

        public virtual string nativeLibraryFileName
        {
            get { return null; }
        }

        public virtual INativeCompiler CreateNativeCompiler()
        {
            return null;
        }

        public virtual Il2CppNativeCodeBuilder CreateIl2CppNativeCodeBuilder()
        {
            return null;
        }

        public virtual CompilerOutputParserBase CreateIl2CppOutputParser()
        {
            return null;
        }

        public abstract BaseUnityLinkerPlatformProvider CreateUnityLinkerPlatformProvider();
    }
}
