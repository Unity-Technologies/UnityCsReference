// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Bee.BeeDriver;
using Bee.BinLog;
using NiceIO;
using PlayerBuildProgramLibrary.Data;
using UnityEditor.Build;
using UnityEditor.Build.Player;
using UnityEditor.Build.Reporting;
using UnityEditor.CrashReporting;
using UnityEditor.Scripting;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditor.Utils;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.Modules
{
    internal abstract class BeeBuildPostprocessor : IBuildPostprocessor
    {
        protected BeeDriverResult BeeDriverResult { get; set; }
        protected static bool isBuildingPlayer { get; set; }

        static readonly string kXrBootSettingsKey = "xr-boot-settings";
        public virtual void LaunchPlayer(BuildLaunchPlayerArgs args) => throw new NotSupportedException();

        public virtual void PostProcess(BuildPostProcessArgs args, out BuildProperties outProperties)
        {
            PostProcess(args);

            // NOTE: For some reason, calling PostProcess seems like it can trigger this object to be GC'd
            //  so create is just before returning
            outProperties = ScriptableObject.CreateInstance<DefaultBuildProperties>();
        }

        public virtual bool SupportsInstallInBuildFolder() => false;

        public virtual bool SupportsLz4Compression() => false;

        public virtual Compression GetDefaultCompression() => Compression.None;

        public virtual void UpdateBootConfig(BuildTarget target, BootConfigData config, BuildOptions options)
        {
            config.Set("wait-for-native-debugger", "0");
            if (config.Get("player-connection-debug") == "1")
            {
                config.Set("wait-for-managed-debugger", EditorUserBuildSettings.waitForManagedDebugger ? "1" : "0");
                config.Set("managed-debugger-fixed-port", EditorUserBuildSettings.managedDebuggerFixedPort.ToString());
            }

            config.Set("hdr-display-enabled", PlayerSettings.useHDRDisplay ? "1" : "0");
            if (BuildPipeline.IsFeatureSupported("ENABLE_SCRIPTING_GC_WBARRIERS", target))
            {
                if (PlayerSettings.gcWBarrierValidation)
                    config.AddKey("validate-write-barriers");
                if (PlayerSettings.gcIncremental)
                    config.Set("gc-max-time-slice", "3");
            }

            string xrBootSettings = UnityEditor.EditorUserBuildSettings.GetPlatformSettings(BuildPipeline.GetBuildTargetName(target), kXrBootSettingsKey);
            if (!String.IsNullOrEmpty(xrBootSettings))
            {
                var bootSettings = xrBootSettings.Split(';');
                foreach (var bootSetting in bootSettings)
                {
                    var setting = bootSetting.Split(':');
                    if (setting.Length == 2 && !String.IsNullOrEmpty(setting[0]) && !String.IsNullOrEmpty(setting[1]))
                    {
                        config.Set(setting[0], setting[1]);
                    }
                }
            }


            if ((options & BuildOptions.Development) != 0)
            {
                if ((options & BuildOptions.EnableDeepProfilingSupport) != 0)
                {
                    config.Set("profiler-enable-deep-profiling-support", "1");
                }
            }
        }

        protected virtual string GetIl2CppDataBackupFolderName(BuildPostProcessArgs args) => $"{args.installPath.ToNPath().FileNameWithoutExtension}_BackUpThisFolder_ButDontShipItWithYourGame";

        public virtual string GetExtension(BuildTarget target, int subtarget, BuildOptions options) => string.Empty;

        [RequiredByNativeCode]
        static bool IsBuildingPlayer() => isBuildingPlayer;

        [RequiredByNativeCode]
        static void BeginProfile() => UnityBeeDriverProfilerSession.Start($"{DagDirectory}/buildreport.json");

        [RequiredByNativeCode]
        static void EndProfile() => UnityBeeDriverProfilerSession.Finish();

        [RequiredByNativeCode]
        static void BeginBuildSection(string name) => UnityBeeDriverProfilerSession.BeginSection(name);

        [RequiredByNativeCode]
        static void EndBuildSection() => UnityBeeDriverProfilerSession.EndSection();

        protected virtual IPluginImporterExtension GetPluginImpExtension() => new EditorPluginImporterExtension();



        protected virtual PluginsData PluginsDataFor(BuildPostProcessArgs args)
        {
            return new PluginsData
            {
                Plugins = GetPluginBuildTargetsFor(args).SelectMany(GetPluginsFor).ToArray()
            };
        }

        protected virtual IEnumerable<BuildTarget> GetPluginBuildTargetsFor(BuildPostProcessArgs args)
        {
            yield return args.target;
        }

        protected virtual Plugin GetPluginFor(PluginImporter imp, BuildTarget target, string destinationPath)
        {
            // Skip .cpp files. They get copied to il2cpp output folder just before code compilation
            if (DesktopPluginImporterExtension.IsCppPluginFile(imp.assetPath))
                return null;

            return new Plugin
            {
                AssetPath = imp.assetPath,
                DestinationPath = destinationPath,
            };
        }

        IEnumerable<Plugin> GetPluginsFor(BuildTarget target)
        {
            var buildTargetName = BuildPipeline.GetBuildTargetName(target);
            var pluginImpExtension = GetPluginImpExtension();
            foreach (PluginImporter imp in PluginImporter.GetImporters(target))
            {
                if (!IsPluginCompatibleWithCurrentBuild(target, imp))
                    continue;

                // Skip managed DLLs.
                if (!imp.isNativePlugin)
                    continue;

                // HACK: This should never happen.
                if (string.IsNullOrEmpty(imp.assetPath))
                {
                    UnityEngine.Debug.LogWarning("Got empty plugin importer path for " + target);
                    continue;
                }

                var destinationPath = pluginImpExtension.CalculateFinalPluginPath(buildTargetName, imp);
                if (string.IsNullOrEmpty(destinationPath))
                    continue;

                var plugin = GetPluginFor(imp, target, destinationPath);
                if (plugin != null)
                    yield return plugin;
            }
        }

        static IEnumerable<NPath> GetFilesWithRoleFromBuildReport(BuildReport report, params string[] roles) =>
            report.GetFiles()
                .Where(file => roles.Contains(file.role))
                .Select(file => file.path.ToNPath())
                .GroupBy(file => file.FileName)
                .Select(group => group.First());
        
        LinkerConfig LinkerConfigFor(BuildPostProcessArgs args)
        {
            var namedBuildTarget = GetNamedBuildTarget(args);
            var strippingLevel = PlayerSettings.GetManagedStrippingLevel(namedBuildTarget);

            // IL2CPP does not support a managed stripping level of disabled. If the player settings
            // do try this (which should not be possible from the editor), use Low instead.
            if (GetScriptingBackend(args) == ScriptingBackend.IL2CPP && strippingLevel == ManagedStrippingLevel.Disabled)
                strippingLevel = ManagedStrippingLevel.Minimal;

            var additionalArgs = new List<string>();

            var diagArgs = Debug.GetDiagnosticSwitch("VMUnityLinkerAdditionalArgs").value as string;
            if (!string.IsNullOrEmpty(diagArgs))
                additionalArgs.Add(diagArgs.Trim('\''));

            var linkerInputDirectory = DagDirectory.Combine($"artifacts/UnityLinkerInputs").CreateDirectory();
            
            // In Disabled mode, we pass all generated and engine assemblies to the linker as roots, as the linker
            // will only perform a simple assembly reference traversal, ignoring link.xml files and attributes which 
            // would otherwise find dependent assemblies to preserve.
            // In other modes (when stripping is desired), we pass only a smaller set of user assemblies (assemblies from
            // packages if used in any scenes, as well as any assembly from the Assets folder) as roots.
            var assembliesToProcess = strippingLevel == ManagedStrippingLevel.Disabled
                ? GetFilesWithRoleFromBuildReport(args.report, "ManagedLibrary", "ManagedEngineAPI").Select(f => f.FileName)
                : args.usedClassRegistry.GetUserAssemblies();
            
            return new LinkerConfig
            {
                LinkXmlFiles = AssemblyStripper.GetLinkXmlFiles(args, linkerInputDirectory),
                EditorToLinkerData = AssemblyStripper.WriteEditorData(args, linkerInputDirectory),
                AssembliesToProcess = assembliesToProcess.ToArray(),
                Runtime = GetScriptingBackend(args).ToString().ToLower(),
                Profile = IL2CPPUtils.ApiCompatibilityLevelToDotNetProfileArgument(
                    PlayerSettings.GetApiCompatibilityLevel(namedBuildTarget), args.target),
                Ruleset = strippingLevel switch
                {
                    ManagedStrippingLevel.Disabled => "Copy",
                    ManagedStrippingLevel.Minimal => "Minimal",
                    ManagedStrippingLevel.Low => "Conservative",
                    ManagedStrippingLevel.Medium => "Aggressive",
                    ManagedStrippingLevel.High => "Experimental",
                    _ => throw new ArgumentException($"Unhandled {nameof(ManagedStrippingLevel)} value")
                },
                AdditionalArgs = additionalArgs.ToArray(),
                ModulesAssetPath = $"{BuildPipeline.GetPlaybackEngineDirectory(args.target, 0)}/modules.asset",
                AllowDebugging = GetAllowDebugging(args),
                PerformEngineStripping = PlayerSettings.stripEngineCode,
            };
        }

        static bool IsBuildOptionSet(BuildOptions options, BuildOptions flag) => (options & flag) != 0;

        static string GetConfigurationName(Il2CppCompilerConfiguration compilerConfiguration)
        {
            // In IL2CPP, Master config is called "ReleasePlus"
            return compilerConfiguration != Il2CppCompilerConfiguration.Master ? compilerConfiguration.ToString() : "ReleasePlus";
        }

        protected virtual string Il2CppBuildConfigurationNameFor(BuildPostProcessArgs args)
        {
            return GetConfigurationName(PlayerSettings.GetIl2CppCompilerConfiguration(GetNamedBuildTarget(args)));
        }

        protected virtual IEnumerable<string> AdditionalIl2CppArgsFor(BuildPostProcessArgs args)
        {
            yield break;
        }

        IEnumerable<string> SplitArgs(string args)
        {
            int startIndex = 0;
            bool inQuotes = false;
            int i = 0;
            for (; i < args.Length; i++)
            {
                if (args[i] == '"')
                    inQuotes = !inQuotes;
                if (args[i] == ' ' && !inQuotes)
                {
                    if (i - startIndex > 0)
                        yield return args.Substring(startIndex, i - startIndex);
                    startIndex = i + 1;
                }
            }
            if (i - startIndex > 0)
                yield return args.Substring(startIndex, i - startIndex);
        }


        protected virtual string Il2CppSysrootPathFor(BuildPostProcessArgs args) => null;

        protected virtual string Il2CppToolchainPathFor(BuildPostProcessArgs args) => null;

        protected virtual string Il2CppCompilerFlagsFor(BuildPostProcessArgs args) => null;

        protected virtual string Il2CppLinkerFlagsFor(BuildPostProcessArgs args) => null;

        protected virtual string Il2CppDataRelativePath(BuildPostProcessArgs args) => "Data";

        Il2CppConfig Il2CppConfigFor(BuildPostProcessArgs args)
        {
            if (GetScriptingBackend(args) != ScriptingBackend.IL2CPP)
                return null;

            var additionalArgs = new List<string>(AdditionalIl2CppArgsFor(args));

            var diagArgs = Debug.GetDiagnosticSwitch("VMIl2CppAdditionalArgs").value as string;
            if (!string.IsNullOrEmpty(diagArgs))
                additionalArgs.AddRange(SplitArgs(diagArgs.Trim('\'')));

            var playerSettingsArgs = PlayerSettings.GetAdditionalIl2CppArgs();
            if (!string.IsNullOrEmpty(playerSettingsArgs))
                additionalArgs.AddRange(SplitArgs(playerSettingsArgs));
            var sysrootPath = Il2CppSysrootPathFor(args);
            var toolchainPath = Il2CppToolchainPathFor(args);
            var compilerFlags = Il2CppCompilerFlagsFor(args);
            var linkerFlags = Il2CppLinkerFlagsFor(args);
            var relativeDataPath = Il2CppDataRelativePath(args);

            if (CrashReportingSettings.enabled)
                additionalArgs.Add("--emit-source-mapping");

            var namedBuildTarget = GetNamedBuildTarget(args);
            var apiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(namedBuildTarget);
            var il2cppCodeGeneration = PlayerSettings.GetIl2CppCodeGeneration(namedBuildTarget);
            var platformHasIncrementalGC = BuildPipeline.IsFeatureSupported("ENABLE_SCRIPTING_GC_WBARRIERS", args.target);
            var allowDebugging = GetAllowDebugging(args);

            NPath extraTypesFile = null;
            if (PlayerBuildInterface.ExtraTypesProvider != null)
            {
                var extraTypes = new HashSet<string>();
                foreach (var extraType in PlayerBuildInterface.ExtraTypesProvider())
                {
                    extraTypes.Add(extraType);
                }

                extraTypesFile = "Temp/extra-types.txt";
                extraTypesFile.WriteAllLines(extraTypes.ToArray());
            }

            return new Il2CppConfig
            {
                EnableDeepProfilingSupport = GetDevelopment(args) &&
                    IsBuildOptionSet(args.report.summary.options,
                    BuildOptions.EnableDeepProfilingSupport),
                EnableFullGenericSharing = il2cppCodeGeneration == Il2CppCodeGeneration.OptimizeSize,
                Profile = IL2CPPUtils.ApiCompatibilityLevelToDotNetProfileArgument(PlayerSettings.GetApiCompatibilityLevel(namedBuildTarget), args.target),
                IDEProjectDefines = IL2CPPUtils.GetBuilderDefinedDefines(args.target, apiCompatibilityLevel, allowDebugging),
                ConfigurationName = Il2CppBuildConfigurationNameFor(args),
                GcWBarrierValidation = platformHasIncrementalGC && PlayerSettings.gcWBarrierValidation,
                GcIncremental = platformHasIncrementalGC && PlayerSettings.gcIncremental &&
                    (apiCompatibilityLevel == ApiCompatibilityLevel.NET_4_6 ||
                        apiCompatibilityLevel == ApiCompatibilityLevel.NET_Standard_2_0 ||
                        apiCompatibilityLevel == ApiCompatibilityLevel.NET_Unity_4_8 ||
                        apiCompatibilityLevel == ApiCompatibilityLevel.NET_Standard),
                CreateSymbolFiles = !GetDevelopment(args) || CrashReportingSettings.enabled,
                AdditionalCppFiles = PluginImporter.GetImporters(args.target)
                    .Where(imp => DesktopPluginImporterExtension.IsCppPluginFile(imp.assetPath))
                    .Select(imp => imp.assetPath)
                    .ToArray(),
                AdditionalArgs = additionalArgs.ToArray(),
                AllowDebugging = allowDebugging,
                CompilerFlags = compilerFlags,
                LinkerFlags = linkerFlags,
                SysRootPath = sysrootPath,
                ToolChainPath = toolchainPath,
                RelativeDataPath = relativeDataPath,
                ExtraTypes = extraTypesFile?.ToString(),
            };
        }

        static bool IsNewInputSystemEnabled()
        {
            var propName = "activeInputHandler";
            var ps = PlayerSettings.GetSerializedObject();
            var newInputEnabledProp = ps.FindProperty(propName);
            if (newInputEnabledProp == null)
                throw new Exception($"Failed to find {propName}");
            return newInputEnabledProp.intValue != 0;
        }

        static GenerateNativePluginsForAssembliesSettings GetGenerateNativePluginsForAssembliesSettings(BuildPostProcessArgs args)
        {
            var settings = new GenerateNativePluginsForAssembliesSettings();
            settings.DisplayName = "Generating Native Plugins";
            if (BuildPipelineInterfaces.processors.generateNativePluginsForAssembliesProcessors != null)
            {
                foreach (var processor in BuildPipelineInterfaces.processors.generateNativePluginsForAssembliesProcessors)
                {
                    var setupResult = processor.PrepareOnMainThread(new () { report = args.report });
                    if (setupResult.additionalInputFiles != null)
                        settings.AdditionalInputFiles = settings.AdditionalInputFiles.Concat(setupResult.additionalInputFiles).ToArray();
                    if (setupResult.displayName != null)
                        settings.DisplayName = setupResult.displayName;
                    settings.HasCallback = true;
                }
            }

            return settings;
        }

        PlayerBuildConfig PlayerBuildConfigFor(BuildPostProcessArgs args) => new PlayerBuildConfig
        {
            DestinationPath = GetInstallPathFor(args),
            StagingArea = args.stagingArea,
            CompanyName = args.companyName,
            ProductName = Paths.MakeValidFileName(args.productName),
            PlayerPackage = args.playerPackage,
            ApplicationIdentifier = PlayerSettings.GetApplicationIdentifier(GetNamedBuildTarget(args)),
            InstallIntoBuildsFolder = GetInstallingIntoBuildsFolder(args),
            GenerateIdeProject = GetCreateSolution(args),
            Development = (args.options & BuildOptions.Development) == BuildOptions.Development,
            NoGUID = (args.options & BuildOptions.NoUniqueIdentifier) == BuildOptions.NoUniqueIdentifier,
            ScriptingBackend = GetScriptingBackend(args),
            Architecture = GetArchitecture(args),
            DataFolder = GetDataFolderFor(args),
            GenerateNativePluginsForAssembliesSettings = GetGenerateNativePluginsForAssembliesSettings(args),
            Services = new ()
            {
                EnableAnalytics = UnityEngine.Analytics.Analytics.enabled,
                EnableCrashReporting = UnityEditor.CrashReporting.CrashReportingSettings.enabled,
                EnablePerformanceReporting = UnityEngine.Analytics.PerformanceReporting.enabled,
                EnableUnityConnect = UnityEngine.Connect.UnityConnectSettings.enabled,
            },
            StreamingAssetsFiles = BuildPlayerContext.ActiveInstance.StreamingAssets
                .Select(e => new StreamingAssetsFile { File = e.src.ToString(), RelativePath = e.dst.ToString() })
                .ToArray(),
            UseNewInputSystem = IsNewInputSystemEnabled(),
            ManagedAssemblies = GetFilesWithRoleFromBuildReport(args.report, "ManagedLibrary", "DependentManagedLibrary", "ManagedEngineAPI")
                .Select(p => p.ToString())
                .ToArray()
        };

        protected virtual string GetInstallPathFor(BuildPostProcessArgs args)
        {
            // Try to minimize path lengths for windows
            NPath absoluteInstallationPath = args.installPath;
            return absoluteInstallationPath.IsChildOf(NPath.CurrentDirectory)
                ? absoluteInstallationPath.RelativeTo(NPath.CurrentDirectory).ToString()
                : absoluteInstallationPath.ToString();
        }

        protected string GetDataFolderFor(BuildPostProcessArgs args) => $"Library/PlayerDataCache/{BuildPipeline.GetSessionIdForBuildTarget(args.target, args.subtarget)}/Data";

        protected ScriptingBackend GetScriptingBackend(BuildPostProcessArgs args)
        {
            var scriptingBackend = PlayerSettings.GetScriptingBackend(GetNamedBuildTarget(args));
            switch (scriptingBackend)
            {
                case ScriptingImplementation.Mono2x:
                    return ScriptingBackend.Mono;

                case ScriptingImplementation.IL2CPP:
                    return ScriptingBackend.IL2CPP;

#pragma warning disable 618
                case ScriptingImplementation.CoreCLR:
                    return ScriptingBackend.CoreCLR;
#pragma warning restore 618

                default:
                    throw new NotSupportedException("Unknown scripting backend:" + scriptingBackend);
            }
        }

        protected virtual string GetPlatformNameForBuildProgram(BuildPostProcessArgs args) => args.target.ToString();
        protected virtual string GetArchitecture(BuildPostProcessArgs args) => EditorUserBuildSettings.GetPlatformSettings(BuildPipeline.GetBuildTargetName(args.target), "Architecture");
        protected Dictionary<string, Action<NodeFinishedMessage>> ResultProcessors { get; } = new ();

        RunnableProgram MakePlayerBuildProgram(BuildPostProcessArgs args)
        {
            var buildProgramAssembly = new NPath($"{args.playerPackage}/{GetPlatformNameForBuildProgram(args)}PlayerBuildProgram.exe");
            NPath buildPipelineFolder = $"{EditorApplication.applicationContentsPath}/Tools/BuildPipeline";
            NPath beePlatformFolder = $"{args.playerPackage}/Bee";
            var searchPaths = $"{beePlatformFolder}{Path.PathSeparator}";
            if (IL2CPPUtils.UsingDevelopmentBuild())
            {
                NPath il2cppPath = IL2CPPUtils.GetExePath("il2cpp").ToNPath().Parent;
                searchPaths = $"{il2cppPath}{Path.PathSeparator}";
            }

            return new SystemProcessRunnableProgram(NetCoreRunProgram.NetCoreRunPath,
                new[]
                {
                    buildProgramAssembly.InQuotes(SlashMode.Native),
                    $"\"{searchPaths}{buildPipelineFolder}\""
                },
                new () {{ "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1" }});
        }

        static NPath DagDirectory => "Library/Bee";

        string DagName(BuildPostProcessArgs args) => $"Player{GetInstallPathFor(args).GetHashCode():x8}";

        protected virtual IEnumerable<object> GetDataForBuildProgramFor(BuildPostProcessArgs args)
        {
            yield return PlayerBuildConfigFor(args);
            yield return PluginsDataFor(args);
            yield return LinkerConfigFor(args);
            yield return Il2CppConfigFor(args);
        }
        protected virtual RunnableProgram BeeBackendProgram(BuildPostProcessArgs args) => null;

        protected virtual BuildRequest SetupBuildRequest(BuildPostProcessArgs args, ILPostProcessingProgram ilpp)
        {
            RunnableProgram buildProgram = MakePlayerBuildProgram(args);
            var cacheMode = ((args.options & BuildOptions.CleanBuildCache) == BuildOptions.CleanBuildCache)
                ? UnityBeeDriver.CacheMode.WriteOnly
                : UnityBeeDriver.CacheMode.ReadWrite;

            var buildRequest = UnityBeeDriver.BuildRequestFor(buildProgram, DagName(args), DagDirectory.ToString(), false, "",ilpp, cacheMode, UnityBeeDriver.StdOutModeForPlayerBuilds, BeeBackendProgram(args));
            buildRequest.DataForBuildProgram.Add(() => GetDataForBuildProgramFor(args).Where(o=> o is not null));

            return buildRequest;
        }

        // Some node types produce meaningful, human readable error messages,
        // but the output files names are Unity internals, not helpful to users.
        // For such nodes, directly print the output if the action fails.
        void PrintStdoutOnErrorProcessor(NodeFinishedMessage node)
        {
            if (node.ExitCode != 0)
                Debug.LogError(node.Output);
            else
                DefaultResultProcessor(node);
        }

        void UnityLinkerResultProcessor(NodeFinishedMessage node)
        {
            if (node.ExitCode != 0 && node.Output.Contains("UnityEditor"))
                Debug.LogError($"UnityEditor.dll assembly is referenced by user code, but this is not allowed.");
            else
                DefaultResultProcessor(node);
        }

        public BeeBuildPostprocessor()
        {
            ResultProcessors["IL2CPP_CodeGen"] = PrintStdoutOnErrorProcessor;
            ResultProcessors["UnityLinker"] = UnityLinkerResultProcessor;
            ResultProcessors["ExtractUsedFeatures"] = PrintStdoutOnErrorProcessor;
        }

        protected void DefaultResultProcessor(NodeFinishedMessage node, bool printErrors = true, bool printWarnings = true)
        {
            var output = node.Node.OutputDirectory;
            if (string.IsNullOrEmpty(output))
                output = node.Node.OutputFile;

            var lines = (node.Output ?? string.Empty).Split(new[] {'\r', '\n'},
                StringSplitOptions.RemoveEmptyEntries);

            if (printErrors)
            {
                var errorKey = "error:";
                foreach (var error in lines.Where(l =>
                    l.StartsWith(errorKey, StringComparison.InvariantCultureIgnoreCase)))
                    Debug.LogError($"{output}: {error.Substring(errorKey.Length).TrimStart()}");
            }

            if (printWarnings)
            {
                var warningKey = "warning:";
                foreach (var warning in lines.Where(l =>
                    l.StartsWith(warningKey, StringComparison.InvariantCultureIgnoreCase)))
                    Debug.LogWarning($"{output}: {warning.Substring(warningKey.Length).TrimStart()}");
            }

            if (node.ExitCode != 0)
                Debug.LogError($"Building {output} failed with output:\n{node.Output}");
        }

        void ReportBuildResults()
        {
            foreach (var node in BeeDriverResult.NodeFinishedMessages)
            {
                var annotationAction = node.Node.Annotation.Split(' ')[0];
                if (ResultProcessors.TryGetValue(annotationAction, out var processor))
                    processor(node);
                else
                    DefaultResultProcessor(node);
            }

            foreach (var resultBeeDriverMessage in BeeDriverResult.BeeDriverMessages)
            {
                if (resultBeeDriverMessage.Kind == BeeDriverResult.MessageKind.Warning)
                    Debug.LogWarning(resultBeeDriverMessage.Text);
                else
                    Debug.LogError(resultBeeDriverMessage.Text);
            }
        }

        void ReportBuildOutputFiles(BuildPostProcessArgs args)
        {
            // Remove any previous file entries in the build report.
            // We can track any file written by the backend ourselves.
            // Once all platforms use the Bee backend, we can remove a lot
            // of code to add file entries in the native build pipeline.
            args.report.DeleteAllFileEntries();

            var filesOutput = BeeDriverResult.DataFromBuildProgram.Get<BuiltFilesOutput>();
            foreach (var outputfile in filesOutput.Files.ToNPaths().Where(f => f.FileExists() && !f.IsSymbolicLink))
                args.report.RecordFileAdded(outputfile.ToString(), outputfile.Extension);

            var config = filesOutput.BootConfigArtifact.ToNPath().ReadAllLines();
            var guidKey = "build-guid=";
            var guidLine = config.FirstOrDefault(l => l.StartsWith(guidKey));
            if (guidLine != null)
            {
                var guid = guidLine.Substring(guidKey.Length);
                args.report.SetBuildGUID(new GUID(guid));
            }
            else
            {
                args.report.SetBuildGUID(new GUID("00000000000000000000000000000000"));
            }
        }

        public virtual string PrepareForBuild(BuildOptions options, BuildTarget target)
        {
            // Clean the Bee folder in PrepareForBuild, so that it is also clean for script compilation.
            if ((options & BuildOptions.CleanBuildCache) == BuildOptions.CleanBuildCache)
                EditorCompilation.ClearBeeBuildArtifacts();

            return null;
        }

        protected virtual void CleanBuildOutput(BuildPostProcessArgs args)
        {
            if (!GetInstallingIntoBuildsFolder(args))
            {
                new NPath(GetInstallPathFor(args)).DeleteIfExists(DeleteMode.Soft);
                new NPath(GetInstallPathFor(args)).Parent.Combine(GetIl2CppDataBackupFolderName(args)).DeleteIfExists(DeleteMode.Soft);
            }
        }

        static void GenerateNativePluginsForAssemblies(GenerateNativePluginsForAssembliesArgs args)
        {
            using var section = UnityBeeDriverProfilerSession.ProfilerInstance.Section(nameof(GenerateNativePluginsForAssembliesArgs));
            var generateArgs = new IGenerateNativePluginsForAssemblies.GenerateArgs { assemblyFiles = args.Assemblies };
            bool wrotePlugins = false;
            bool wroteSymbols = false;
            foreach (var processor in BuildPipelineInterfaces.processors.generateNativePluginsForAssembliesProcessors)
            {
                var result = processor.GenerateNativePluginsForAssemblies(generateArgs);
                if (result.generatedPlugins?.Length > 0)
                {
                    wrotePlugins = true;
                    foreach (var file in result.generatedPlugins.ToNPaths())
                        file.Copy($"{args.PluginOutputFolder}/{file.FileName}");
                }
                if (result.generatedSymbols?.Length > 0)
                {
                    wroteSymbols = true;
                    foreach (var file in result.generatedSymbols.ToNPaths())
                        file.Copy($"{args.SymbolOutputFolder}/{file.FileName}");
                }
            }

            if (!wrotePlugins)
            {
                // We need to produce a file, so Bee will not be upset when we use the `FilesOrDummy` mechanism.
                new NPath(args.PluginOutputFolder)
                    .Combine("no_plugins_were_generated.txt")
                    .WriteAllText("GenerateNativePluginsForAssemblies did not produce any output");
            }

            if (!wroteSymbols)
            {
                // We need to produce a file, so Bee will not be upset when we use the `FilesOrDummy` mechanism.
                new NPath(args.SymbolOutputFolder)
                    .Combine("no_symbols_were_generated.txt")
                    .WriteAllText("GenerateNativePluginsForAssemblies did not produce any output");
            }
        }

        public virtual void PostProcess(BuildPostProcessArgs args)
        {
            try
            {
                if ((args.options & BuildOptions.CleanBuildCache) == BuildOptions.CleanBuildCache)
                    CleanBuildOutput(args);

                var buildStep = args.report.BeginBuildStep("Setup incremental player build");

                var buildRequest = SetupBuildRequest(args,new ILPostProcessingProgram());
                args.report.EndBuildStep(buildStep);

                buildStep = args.report.BeginBuildStep("Incremental player build");

                var cancellationTokenSource = new CancellationTokenSource();

                buildRequest.Target = "Player";
                buildRequest.RegisterRPCCallback<GenerateNativePluginsForAssembliesArgs>(nameof(GenerateNativePluginsForAssemblies), GenerateNativePluginsForAssemblies);
                var activeBuild = BeeDriver.BuildAsync(buildRequest, cancellationToken: cancellationTokenSource.Token);

                {
                    while (!activeBuild.TaskObject.IsCompleted)
                    {
                        activeBuild.TaskObject.Wait(100);

                        //important to keep on pumping the execution context here, as there might be async tasks being kicked off by the bee driver build that have to run on the main thread.
                        ((UnitySynchronizationContext) SynchronizationContext.Current).Exec();

                        var activeBuildStatus = activeBuild.Status;
                        float progress = activeBuildStatus.Progress.HasValue
                            ? activeBuildStatus.Progress.Value.nodesFinishedOrUpToDate / (float) activeBuildStatus.Progress.Value.totalNodesQeueued
                            : 0f;
                        if (EditorUtility.DisplayCancelableProgressBar("Incremental Player Build", activeBuildStatus.Description, progress))
                        {
                            EditorUtility.DisplayCancelableProgressBar("Incremental Player Build", "Canceling build", 1.0f);
                            cancellationTokenSource.Cancel();
                            throw new OperationCanceledException();
                        }
                    }
                    args.report.EndBuildStep(buildStep);

                    BeeDriverResult = activeBuild.TaskObject.Result;
                    
                    UnityBeeDriverProfilerSession.AddTaskToWaitForBeforeFinishing(BeeDriverResult.ProfileOutputWritingTask);
                    
                    if (BeeDriverResult.Success)
                    {
                        PostProcessCompletedBuild(args);
                    }
                    ReportBuildResults();

                    UnityBeeDriver.RunCleanBeeCache();

                    if (BeeDriverResult.Success)
                    {
                        buildStep = args.report.BeginBuildStep("Report output files");
                        ReportBuildOutputFiles(args);
                        args.report.EndBuildStep(buildStep);
                    } else
                        throw new BuildFailedException($"Player build failed: {args.report.SummarizeErrors()}", silent: true);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (BuildFailedException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new BuildFailedException(e);
            }
        }

        public virtual void PostProcessCompletedBuild(BuildPostProcessArgs args)
        {
            if (PlayerSettings.GetManagedStrippingLevel(GetNamedBuildTarget(args)) == ManagedStrippingLevel.Disabled)
                return;

            var strippingInfo = GetStrippingInfoFromBuild(args);
            if (strippingInfo != null && EditorBuildOutputPathFor(args) != null)
            {
                args.report.AddAppendix(strippingInfo);
                var linkerToEditorData = AssemblyStripper.ReadLinkerToEditorData(EditorBuildOutputPathFor(args).ToString());
                AssemblyStripper.UpdateBuildReport(linkerToEditorData, strippingInfo);
            }
        }

        protected virtual bool GetCreateSolution(BuildPostProcessArgs args) => false;

        protected virtual NPath EditorBuildOutputPathFor(BuildPostProcessArgs buildPostProcessArgs) => null;

        protected virtual StrippingInfo GetStrippingInfoFromBuild(BuildPostProcessArgs args) => null;

        protected virtual bool IsPluginCompatibleWithCurrentBuild(BuildTarget buildTarget, PluginImporter imp)
        {
            var cpu = imp.GetPlatformData(buildTarget, "CPU");
            return !string.Equals(cpu, "None", StringComparison.OrdinalIgnoreCase);
        }

        protected NamedBuildTarget GetNamedBuildTarget(BuildPostProcessArgs args)
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(args.target);

            if (buildTargetGroup == BuildTargetGroup.Standalone)
            {
                return (StandaloneBuildSubtarget)args.subtarget == StandaloneBuildSubtarget.Server
                    ? NamedBuildTarget.Server : NamedBuildTarget.Standalone;
            }

            return NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
        }

        protected bool GetDevelopment(BuildPostProcessArgs args) =>
            IsBuildOptionSet(args.options, BuildOptions.Development);

        protected bool GetInstallingIntoBuildsFolder(BuildPostProcessArgs args) =>
            IsBuildOptionSet(args.options, BuildOptions.InstallInBuildFolder);

        protected bool ShouldAppendBuild(BuildPostProcessArgs args) =>
            IsBuildOptionSet(args.options, BuildOptions.AcceptExternalModificationsToPlayer);

        protected virtual bool GetAllowDebugging(BuildPostProcessArgs args) => (args.report.summary.options & BuildOptions.AllowDebugging) == BuildOptions.AllowDebugging;

    }

    internal class DefaultBuildProperties : BuildProperties
    {
        public override DeploymentTargetRequirements GetTargetRequirements() { return null; }
    }
}
