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

        protected BeeDriver Driver { get; private set; }
        PlayerBuildProgressAPI progressAPI = null;


        void AddPluginsDataToDriver(BuildPostProcessArgs args)
        {
            var pluginsList = new List<Plugin>();
            string buildTargetName = BuildPipeline.GetBuildTargetName(args.target);
            IPluginImporterExtension pluginImpExtension = new DesktopPluginImporterExtension();

            foreach (PluginImporter imp in PluginImporter.GetImporters(args.target))
            {
                if (!IsPluginCompatibleWithCurrentBuild(args.target, imp))
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
                    UnityEngine.Debug.LogWarning("Got empty plugin importer path for " + args.target);
                    continue;
                }

                var destinationPath = pluginImpExtension.CalculateFinalPluginPath(buildTargetName, imp);
                if (string.IsNullOrEmpty(destinationPath))
                    continue;

                pluginsList.Add(new Plugin()
                {
                    AssetPath = imp.assetPath,
                    DestinationPath = pluginImpExtension.CalculateFinalPluginPath(buildTargetName, imp)
                });
            }

            Driver.DataForBuildProgram.Add(new PluginsData()
            {
                Plugins = pluginsList.ToArray()
            });
        }

        void AddLinkerConfigToDriver(BuildPostProcessArgs args)
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(args.target);
            var strippingLevel = PlayerSettings.GetManagedStrippingLevel(buildTargetGroup);

            // IL2CPP does not support a managed stripping level of disabled. If the player settings
            // do try this (which should not be possible from the editor), use Low instead.
            if (GetUseIl2Cpp(args) && strippingLevel == ManagedStrippingLevel.Disabled)
                strippingLevel = ManagedStrippingLevel.Low;

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

                Driver.DataForBuildProgram.Add(new LinkerConfig()
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
                        PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup)),
                    // *begin-nonstandard-formatting*
                    Ruleset = strippingLevel switch
                    {
                        ManagedStrippingLevel.Low => "Conservative",
                        ManagedStrippingLevel.Medium => "Aggressive",
                        ManagedStrippingLevel.High => "Experimental",
                        _ => throw new ArgumentException($"Unhandled {nameof(ManagedStrippingLevel)} value")
                    },
                    // *end-nonstandard-formatting*
                    AdditionalArgs = additionalArgs.ToArray(),
                    ModulesAssetPath = $"{BuildPipeline.GetPlaybackEngineDirectory(args.target, 0)}/modules.asset",
                    AllowDebugging = (args.report.summary.options & BuildOptions.AllowDebugging) ==
                        BuildOptions.AllowDebugging,
                    PerformEngineStripping = PlayerSettings.stripEngineCode,
                    EngineStrippingFlags = engineStrippingFlags.ToArray(),
                });
            }
        }

        private static bool IsBuildOptionSet(BuildOptions options, BuildOptions flag) => (options & flag) != 0;

        void AddIl2CppConfigToDriver(BuildPostProcessArgs args)
        {
            if (!GetUseIl2Cpp(args))
                return;

            var additionalArgs = new List<string>();

            var diagArgs = Debug.GetDiagnosticSwitch("VMIl2CppAdditionalArgs").value as string;
            if (!string.IsNullOrEmpty(diagArgs))
                additionalArgs.Add(diagArgs.Trim('\''));

            var playerSettingsArgs = PlayerSettings.GetAdditionalIl2CppArgs();
            if (!string.IsNullOrEmpty(playerSettingsArgs))
                additionalArgs.Add(playerSettingsArgs);

            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(args.target);
            var apiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup);
            var platformHasIncrementalGC = BuildPipeline.IsFeatureSupported("ENABLE_SCRIPTING_GC_WBARRIERS", args.target);
            Driver.DataForBuildProgram.Add(new Il2CppConfig()
            {
                EnableDeepProfilingSupport = GetDevelopment(args) &&
                    IsBuildOptionSet(args.report.summary.options,
                    BuildOptions.EnableDeepProfilingSupport),
                Profile = IL2CPPUtils.ApiCompatibilityLevelToDotNetProfileArgument(PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup)),
                ConfigurationName = PlayerSettings.GetIl2CppCompilerConfiguration(buildTargetGroup).ToString(), //Todo: "ReleasePlus"
                GcWBarrierValidation = platformHasIncrementalGC && PlayerSettings.gcWBarrierValidation,
                GcIncremental = platformHasIncrementalGC && PlayerSettings.gcIncremental &&
                    (apiCompatibilityLevel == ApiCompatibilityLevel.NET_4_6 ||
                        apiCompatibilityLevel == ApiCompatibilityLevel.NET_Standard_2_0),
                CreateSymbolFiles = !GetDevelopment(args) || CrashReportingSettings.enabled,
                AdditionalCppFiles = PluginImporter.GetImporters(args.target)
                    .Where(imp => DesktopPluginImporterExtension.IsCppPluginFile(imp.assetPath))
                    .Select(imp => imp.assetPath)
                    .ToArray(),
                AdditionalArgs = additionalArgs.ToArray()
            });
        }

        void AddPlayerBuildConfigToDriver(BuildPostProcessArgs args)
        {
            Driver.DataForBuildProgram.Add(new PlayerBuildConfig()
            {
                DestinationPath = GetInstallPathFor(args),
                StagingArea = args.stagingArea,
                CompanyName = args.companyName,
                ProductName = Paths.MakeValidFileName(args.productName),
                PlayerPackage = args.playerPackage,
                ApplicationIdentifier = PlayerSettings.GetApplicationIdentifier(BuildPipeline.GetBuildTargetGroup(args.target)),
                InstallIntoBuildsFolder = GetInstallingIntoBuildsFolder(args),
                GenerateIdeProject = GetCreateSolution(args),
                Development = (args.report.summary.options & BuildOptions.Development) == BuildOptions.Development,
                UseIl2Cpp = GetUseIl2Cpp(args),
                Architecture = GetArchitecture(args),
                DataFolder = $"Library/PlayerDataCache/{BuildPipeline.GetBuildTargetName(args.target)}/Data"
            });
        }

        public override bool UsesBeeBuild() => true;
        protected virtual string GetInstallPathFor(BuildPostProcessArgs args) => args.installPath;
        protected virtual string GetPlatformNameForBuildProgram(BuildPostProcessArgs args) => args.target.ToString();
        protected virtual string GetArchitecture(BuildPostProcessArgs args) => EditorUserBuildSettings.GetPlatformSettings(BuildPipeline.GetBuildTargetName(args.target), "Architecture");
        private SystemProcessRunnableProgram MakePlayerBuildProgram(BuildPostProcessArgs args)
        {
            var buildProgramAssembly = new NPath($"{BuildPipeline.GetBuildToolsDirectory(args.target)}/{GetPlatformNameForBuildProgram(args)}PlayerBuildProgram.exe");
            return new SystemProcessRunnableProgram(NetCoreRunProgram.NetCoreRunPath, buildProgramAssembly.InQuotes(SlashMode.Native));
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

        protected virtual void SetupBeeDriver(BuildPostProcessArgs args)
        {
            RunnableProgram buildProgram = MakePlayerBuildProgram(args);
            progressAPI = new PlayerBuildProgressAPI($"Building {args.productName}");
            Driver = UnityBeeDriver.Make(buildProgram, DagName(args), DagDirectory.ToString(), false, NPath.CurrentDirectory.ToString(), progressAPI);

            AddPlayerBuildConfigToDriver(args);
            AddPluginsDataToDriver(args);
            AddLinkerConfigToDriver(args);
            AddIl2CppConfigToDriver(args);
        }

        void ReportBuildResults(BeeDriverResult result)
        {
            foreach (var node in result.NodeResults)
            {
                if (node.exitcode != 0)
                {
                    var output = node.outputfile;
                    if (string.IsNullOrEmpty(output))
                        output = node.outputdirectory;

                    if (node.annotation.StartsWith("UnityLinker ") && node.stdout.Contains("UnityEditor"))
                        Debug.LogError($"UnityEditor.dll assembly is referenced by user code, but this is not allowed.");
                    else if (node.annotation.StartsWith("IL2CPP_CodeGen"))
                    {
                        // IL2cpp reports nice errors, but the output file node will just point to the profiler output, which
                        // is not useful error reporting. So just dump the output directly.
                        Debug.LogError(node.stdout);
                    }
                    else
                        Debug.LogError($"Building {output} failed with output:\n{node.stdout}");
                }
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
            var filesOutput = Driver.DataFromBuildProgram.Get<BuiltFilesOutput>();
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

        protected static bool GetDevelopment(BuildPostProcessArgs args) =>
            IsBuildOptionSet(args.options, BuildOptions.Development);

        protected static bool GetInstallingIntoBuildsFolder(BuildPostProcessArgs args) =>
            IsBuildOptionSet(args.options, BuildOptions.InstallInBuildFolder);

        protected static bool GetUseIl2Cpp(BuildPostProcessArgs args) =>
            PlayerSettings.GetScriptingBackend(BuildPipeline.GetBuildTargetGroup(args.target)) == ScriptingImplementation.IL2CPP;
    }
}
