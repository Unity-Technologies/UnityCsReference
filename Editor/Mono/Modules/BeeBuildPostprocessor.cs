// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Bee.BeeDriver;
using NiceIO;
using PlayerBuildProgramLibrary.Data;
using UnityEditor.Build;
using UnityEditor.CrashReporting;
using UnityEditor.Scripting;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditor.Utils;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.Modules
{
    internal abstract class BeeBuildPostprocessor : DefaultBuildPostprocessor
    {
        protected static bool isBuildingPlayer { get; set; }

        [RequiredByNativeCode]
        static bool IsBuildingPlayer()
        {
            return isBuildingPlayer;
        }

        [RequiredByNativeCode]
        static void BeginProfile()
        {
            UnityBeeDriverProfilerSession.Start($"{DagDirectory}/buildreport.json");
        }

        [RequiredByNativeCode]
        static void EndProfile()
        {
            UnityBeeDriverProfilerSession.Finish();
        }

        [RequiredByNativeCode]
        static void BeginBuildSection(string name)
        {
            UnityBeeDriverProfilerSession.BeginSection(name);
        }

        [RequiredByNativeCode]
        static void EndBuildSection()
        {
            UnityBeeDriverProfilerSession.EndSection();
        }

        protected BeeDriver Driver { get; private set; }
        protected virtual IPluginImporterExtension GetPluginImpExtension() => new EditorPluginImporterExtension();

        PlayerBuildProgressAPI progressAPI = null;


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

        private IEnumerable<Plugin> GetPluginsFor(BuildTarget target)
        {
            var buildTargetName = BuildPipeline.GetBuildTargetName(target);
            var pluginImpExtension = GetPluginImpExtension();
            foreach (PluginImporter imp in PluginImporter.GetImporters(target))
            {
                if (!IsPluginCompatibleWithCurrentBuild(target, imp))
                    continue;

                // Skip .cpp files. They get copied to il2cpp output folder just before code compilation
                if (DesktopPluginImporterExtension.IsCppPluginFile(imp.assetPath))
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

                yield return new Plugin()
                {
                    AssetPath = imp.assetPath,
                    DestinationPath = pluginImpExtension.CalculateFinalPluginPath(buildTargetName, imp)
                };
            }
        }

        LinkerConfig LinkerConfigFor(BuildPostProcessArgs args)
        {
            var namedBuildTarget = GetNamedBuildTarget(args);
            var strippingLevel = PlayerSettings.GetManagedStrippingLevel(namedBuildTarget);

            // IL2CPP does not support a managed stripping level of disabled. If the player settings
            // do try this (which should not be possible from the editor), use Low instead.
            if (GetUseIl2Cpp(args) && strippingLevel == ManagedStrippingLevel.Disabled)
                strippingLevel = ManagedStrippingLevel.Minimal;

            if (strippingLevel > ManagedStrippingLevel.Disabled)
            {
                var rcr = args.usedClassRegistry;

                NPath managedAssemblyFolderPath = $"{args.stagingAreaData}/Managed";

                var additionalArgs = new List<string>();

                var diagArgs = Debug.GetDiagnosticSwitch("VMUnityLinkerAdditionalArgs").value as string;
                if (!string.IsNullOrEmpty(diagArgs))
                    additionalArgs.Add(diagArgs.Trim('\''));

                var engineStrippingFlags = new List<string>();

                if (UnityEngine.Connect.UnityConnectSettings.enabled)
                    engineStrippingFlags.Add("EnableUnityConnect");
                if (UnityEngine.Analytics.PerformanceReporting.enabled)
                    engineStrippingFlags.Add("EnablePerformanceReporting");
                if (UnityEngine.Analytics.Analytics.enabled)
                    engineStrippingFlags.Add("EnableAnalytics");
                if (UnityEditor.CrashReporting.CrashReportingSettings.enabled)
                    engineStrippingFlags.Add("EnableCrashReporting");

                var linkerRunInformation = new UnityLinkerRunInformation(managedAssemblyFolderPath.MakeAbsolute().ToString(), null, args.target,
                    rcr, strippingLevel, null);
                AssemblyStripper.WriteEditorData(linkerRunInformation);

                return new LinkerConfig
                {
                    LinkXmlFiles = AssemblyStripper.GetLinkXmlFiles(linkerRunInformation).ToArray(),
                    EditorToLinkerData = linkerRunInformation.EditorToLinkerDataPath.ToNPath().MakeAbsolute().ToString(),
                    AssembliesToProcess = rcr.GetUserAssemblies()
                        .Where(s => rcr.IsDLLUsed(s))
                        .Select(s => $"{managedAssemblyFolderPath}/{s}")
                        .Concat(Directory.GetFiles(managedAssemblyFolderPath.ToString(), "I18N*.dll", SearchOption.TopDirectoryOnly))
                        .ToArray(),
                    SearchDirectories = new[] {managedAssemblyFolderPath.MakeAbsolute().ToString()},
                    Runtime = GetUseIl2Cpp(args) ? "il2cpp" : "mono",
                    Profile = IL2CPPUtils.ApiCompatibilityLevelToDotNetProfileArgument(
                        PlayerSettings.GetApiCompatibilityLevel(namedBuildTarget), args.target),
                    // *begin-nonstandard-formatting*
                    Ruleset = strippingLevel switch
                    {
                        ManagedStrippingLevel.Minimal => "Minimal",
                        ManagedStrippingLevel.Low => "Conservative",
                        ManagedStrippingLevel.Medium => "Aggressive",
                        ManagedStrippingLevel.High => "Experimental",
                        _ => throw new ArgumentException($"Unhandled {nameof(ManagedStrippingLevel)} value")
                    },
                    // *end-nonstandard-formatting*
                    AdditionalArgs = additionalArgs.ToArray(),
                    ModulesAssetPath = $"{BuildPipeline.GetPlaybackEngineDirectory(args.target, 0)}/modules.asset",
                    AllowDebugging = (args.report.summary.options & BuildOptions.AllowDebugging) == BuildOptions.AllowDebugging,
                    PerformEngineStripping = PlayerSettings.stripEngineCode,
                };
            }

            return null;
        }

        private static bool IsBuildOptionSet(BuildOptions options, BuildOptions flag) => (options & flag) != 0;

        protected virtual string Il2CppBuildConfigurationNameFor(BuildPostProcessArgs args)
        {
            return Il2CppNativeCodeBuilderUtils.GetConfigurationName(PlayerSettings.GetIl2CppCompilerConfiguration(GetNamedBuildTarget(args)));
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

        Il2CppConfig Il2CppConfigFor(BuildPostProcessArgs args)
        {
            if (!GetUseIl2Cpp(args))
                return null;

            var additionalArgs = new List<string>(AdditionalIl2CppArgsFor(args));

            var diagArgs = Debug.GetDiagnosticSwitch("VMIl2CppAdditionalArgs").value as string;
            if (!string.IsNullOrEmpty(diagArgs))
                additionalArgs.AddRange(SplitArgs(diagArgs.Trim('\'')));

            var playerSettingsArgs = PlayerSettings.GetAdditionalIl2CppArgs();
            if (!string.IsNullOrEmpty(playerSettingsArgs))
                additionalArgs.AddRange(SplitArgs(playerSettingsArgs));

            var namedBuildTarget = GetNamedBuildTarget(args);
            var apiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(namedBuildTarget);
            var platformHasIncrementalGC = BuildPipeline.IsFeatureSupported("ENABLE_SCRIPTING_GC_WBARRIERS", args.target);
            var allowDebugging = (args.report.summary.options & BuildOptions.AllowDebugging) == BuildOptions.AllowDebugging;

            return new Il2CppConfig
            {
                EnableDeepProfilingSupport = GetDevelopment(args) &&
                    IsBuildOptionSet(args.report.summary.options,
                    BuildOptions.EnableDeepProfilingSupport),
                EnableFullGenericSharing = EditorUserBuildSettings.il2CppCodeGeneration == Il2CppCodeGeneration.OptimizeSize,
                Profile = IL2CPPUtils.ApiCompatibilityLevelToDotNetProfileArgument(PlayerSettings.GetApiCompatibilityLevel(namedBuildTarget), args.target),
                Defines = string.Join(";", IL2CPPUtils.GetBuilderDefinedDefines(args.target, apiCompatibilityLevel, allowDebugging)),
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
            };
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
            Development = (args.report.summary.options & BuildOptions.Development) == BuildOptions.Development,
            UseIl2Cpp = GetUseIl2Cpp(args),
            Architecture = GetArchitecture(args),
            DataFolder = GetDataFolderFor(args),
            Services = new Services()
            {
                EnableAnalytics = UnityEngine.Analytics.Analytics.enabled,
                EnableCrashReporting = UnityEditor.CrashReporting.CrashReportingSettings.enabled,
                EnablePerformanceReporting = UnityEngine.Analytics.PerformanceReporting.enabled,
                EnableUnityConnect = UnityEngine.Connect.UnityConnectSettings.enabled,
            },
            StreamingAssetsFiles = BuildPlayerContext.ActiveInstance.StreamingAssets
                .Select(e => new StreamingAssetsFile { File = e.src.ToString(), RelativePath = e.dst.ToString() })
                .ToArray()
        };

        public override bool UsesBeeBuild() => true;
        protected virtual string GetInstallPathFor(BuildPostProcessArgs args)
        {
            // Try to minimize path lengths for windows
            NPath absoluteInstallationPath = args.installPath;
            return absoluteInstallationPath.IsChildOf(NPath.CurrentDirectory)
                ? absoluteInstallationPath.RelativeTo(NPath.CurrentDirectory).ToString()
                : absoluteInstallationPath.ToString();
        }

        protected string GetDataFolderFor(BuildPostProcessArgs args)
        {
            return $"Library/PlayerDataCache/{BuildPipeline.GetBuildTargetName(args.target)}/Data";
        }

        protected virtual string GetPlatformNameForBuildProgram(BuildPostProcessArgs args) => args.target.ToString();
        protected virtual string GetArchitecture(BuildPostProcessArgs args) => EditorUserBuildSettings.GetPlatformSettings(BuildPipeline.GetBuildTargetName(args.target), "Architecture");

        protected Dictionary<string, Action<NodeResult>> ResultProcessors { get; } = new Dictionary<string, Action<NodeResult>>();

        private SystemProcessRunnableProgram MakePlayerBuildProgram(BuildPostProcessArgs args)
        {
            var buildProgramAssembly = new NPath($"{args.playerPackage}/{GetPlatformNameForBuildProgram(args)}PlayerBuildProgram.exe");
            return new SystemProcessRunnableProgram(NetCoreRunProgram.NetCoreRunPath,
                new[]
                {
                    buildProgramAssembly.InQuotes(SlashMode.Native),
                    $"\"{EditorApplication.applicationContentsPath}/Tools/BuildPipeline\""
                });
        }

        class PlayerBuildProgressAPI : ProgressAPI
        {
            private string message;
            public string CurrentProgressInfo { get; protected set; }
            public float CurrentProgress { get; protected set; }

            public PlayerBuildProgressAPI(string _message)
            {
                message = _message;
            }

            public override ProgressToken Start() => new PlayerBuildProgressAPIToken(this);

            private class PlayerBuildProgressAPIToken : UnityProgressAPI.UnityProgressAPIToken
            {
                private PlayerBuildProgressAPI _progressAPI;
                public PlayerBuildProgressAPIToken(PlayerBuildProgressAPI progressAPI) :
                    base(progressAPI.message)
                {
                    _progressAPI = progressAPI;
                }

                public override void Report(string msg)
                {
                    _progressAPI.CurrentProgressInfo = msg;
                    base.Report(msg);
                }

                public override void Report(float progress)
                {
                    _progressAPI.CurrentProgress = progress;
                    base.Report(progress);
                }
            }
        }

        private static NPath DagDirectory => "Library/Bee";

        private string DagName(BuildPostProcessArgs args)
        {
            return $"Player{GetInstallPathFor(args).GetHashCode():x8}";
        }

        protected virtual IEnumerable<object> GetDataForBuildProgramFor(BuildPostProcessArgs args)
        {
            yield return PlayerBuildConfigFor(args);
            yield return PluginsDataFor(args);
            yield return LinkerConfigFor(args);
            yield return Il2CppConfigFor(args);
        }

        void SetupBeeDriver(BuildPostProcessArgs args)
        {
            RunnableProgram buildProgram = MakePlayerBuildProgram(args);
            progressAPI = new PlayerBuildProgressAPI($"Building {args.productName}");
            Driver = UnityBeeDriver.Make(buildProgram, DagName(args), DagDirectory.ToString(), false, "", progressAPI);

            foreach (var o in GetDataForBuildProgramFor(args))
            {
                if (o != null)
                    Driver.DataForBuildProgram.Add(o.GetType(), o);
            }
        }

        void Il2CPPResultProcessor(NodeResult node)
        {
            if (node.exitcode != 0)
                // IL2cpp reports nice errors, but the output file node will just point to the profiler output, which
                // is not useful error reporting. So just dump the output directly.
                Debug.LogError(node.stdout);
        }

        void UnityLinkerResultProcessor(NodeResult node)
        {
            if (node.exitcode != 0 && node.stdout.Contains("UnityEditor"))
                Debug.LogError($"UnityEditor.dll assembly is referenced by user code, but this is not allowed.");
            else
                DefaultResultProcessor(node);
        }

        public BeeBuildPostprocessor()
        {
            ResultProcessors["IL2CPP_CodeGen"] = Il2CPPResultProcessor;
            ResultProcessors["UnityLinker"] = UnityLinkerResultProcessor;
        }

        protected void DefaultResultProcessor(NodeResult node, bool printErrors = true, bool printWarnings = true)
        {
            var output = node.outputfile;
            if (string.IsNullOrEmpty(output))
                output = node.outputdirectory;

            var lines = (node.stdout ?? string.Empty).Split(new[] {'\r', '\n'},
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

            if (node.exitcode != 0)
                Debug.LogError($"Building {output} failed with output:\n{node.stdout}");
        }

        void ReportBuildResults(BeeDriverResult result)
        {
            foreach (var node in result.NodeResults)
            {
                var annotationAction = node.annotation.Split(' ')[0];
                if (ResultProcessors.TryGetValue(annotationAction, out var processor))
                    processor(node);
                else
                    DefaultResultProcessor(node);
            }

            foreach (var resultBeeDriverMessage in result.BeeDriverMessages)
            {
                if (resultBeeDriverMessage.Kind == BeeDriverResult.MessageKind.Warning)
                    Debug.LogWarning(resultBeeDriverMessage.Text);
                else
                    Debug.LogError(resultBeeDriverMessage.Text);
            }
        }

        void ReportBuildOutputFiles(BuildPostProcessArgs args)
        {
            var filesOutput = Driver.DataFromBuildProgram?.Get<BuiltFilesOutput>() ?? new BuiltFilesOutput();
            foreach (var outputfile in filesOutput.Files.ToNPaths().Where(f => f.FileExists()))
                args.report.RecordFileAdded(outputfile.ToString(), outputfile.Extension);
        }

        public override string PrepareForBuild(BuildOptions options, BuildTarget target)
        {
            // Clean the Bee folder in PrepareForBuild, so that it is also clean for script compilation.
            if ((options & BuildOptions.CleanBuildCache) == BuildOptions.CleanBuildCache)
                EditorCompilation.CleanCache();

            return base.PrepareForBuild(options, target);
        }

        protected virtual void CleanBuildOutput(BuildPostProcessArgs args)
        {
            new NPath(args.installPath).DeleteIfExists(DeleteMode.Soft);
            new NPath(GetIl2CppDataBackupFolderName(args)).DeleteIfExists(DeleteMode.Soft);
        }

        public override void PostProcess(BuildPostProcessArgs args)
        {
            try
            {
                // Remove any previous file entries in the build report.
                // We can track any file written by the backend ourselves.
                // Once all platforms use the Bee backend, we can remove a lot
                // of code to add file entries in the native build pipeline.
                args.report.DeleteAllFileEntries();

                if ((args.options & BuildOptions.CleanBuildCache) == BuildOptions.CleanBuildCache)
                    CleanBuildOutput(args);

                var buildStep = args.report.BeginBuildStep("Setup incremental player build");

                SetupBeeDriver(args);
                args.report.EndBuildStep(buildStep);

                buildStep = args.report.BeginBuildStep("Incremental player build");

                Driver.BuildAsync("Player");

                {
                    BeeDriverResult result = null;
                    while (result == null)
                    {
                        Thread.Sleep(1);
                        result = Driver.Tick();
                        if (EditorUtility.DisplayCancelableProgressBar("Incremental Player Build",
                            progressAPI.CurrentProgressInfo, progressAPI.CurrentProgress))
                        {
                            EditorUtility.DisplayCancelableProgressBar("Incremental Player Build",
                                "Canceling build", 1.0f);
                            Driver.CancelBuild();
                            Driver.WaitForResult();
                            throw new OperationCanceledException();
                        }
                    }
                    args.report.EndBuildStep(buildStep);

                    if (result.Success)
                        PostProcessCompletedBuild(args);
                    ReportBuildResults(result);

                    buildStep = args.report.BeginBuildStep("Report output files");
                    ReportBuildOutputFiles(args);
                    args.report.EndBuildStep(buildStep);

                    if (!result.Success)
                        throw new BuildFailedException("Incremental Player build failed!");
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

        protected virtual bool GetCreateSolution(BuildPostProcessArgs args) => false;

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

        protected virtual bool GetUseIl2Cpp(BuildPostProcessArgs args) =>
            PlayerSettings.GetScriptingBackend(GetNamedBuildTarget(args)) == ScriptingImplementation.IL2CPP;
    }
}
