// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Modules;
using UnityEditor.DeploymentTargets;
using UnityEngine.Scripting;

namespace UnityEditor
{
    class MissingBuildPropertiesException : Exception {}

    // Holds data needed to verify a target against a set of requirements
    internal abstract class DeploymentTargetRequirements
    {
    }

    // Holds data needed for operating (launching etc) on a build
    internal abstract class BuildProperties : ScriptableObject
    {
        public static BuildProperties GetFromBuildReport(BuildReport report)
        {
            var allData = report.GetAppendices<BuildProperties>();
            if (allData.Length > 0)
                return allData[0];

            throw new MissingBuildPropertiesException();
        }

        public abstract DeploymentTargetRequirements GetTargetRequirements();
    }

    internal static class PostprocessBuildPlayer
    {
        internal const string StreamingAssets = "Assets/StreamingAssets";

        internal static void AddProjectBootConfigKey(string key)
        {
            AddProjectBootConfigKeyValue(key, null);
        }

        internal static void AddProjectBootConfigKeyValue(string key, string value)
        {
            projectBootConfigEntries[key] = value;
        }

        internal static bool RemoveProjectBootConfigKey(string key)
        {
            return projectBootConfigEntries.Remove(key);
        }

        internal static bool GetProjectBootConfigKeyValue(string key, out string value)
        {
            return projectBootConfigEntries.TryGetValue(key, out value);
        }

        internal static void ClearProjectBootConfigEntries()
        {
            projectBootConfigEntries.Clear();
        }

        private static Dictionary<string, string> projectBootConfigEntries = new Dictionary<string, string>();

        internal static string GetStreamingAssetsBundleManifestPath()
        {
            string manifestPath = "";
            if (Directory.Exists(StreamingAssets))
            {
                var tmpPath = Path.Combine(StreamingAssets, "StreamingAssets.manifest");
                if (File.Exists(tmpPath))
                    manifestPath = tmpPath;
            }

            return manifestPath;
        }

        [RequiredByNativeCode]
        static public string PrepareForBuild(BuildPlayerOptions buildOptions)
        {
            var postprocessor = ModuleManager.GetBuildPostProcessor(buildOptions.target);
            if (postprocessor == null)
                return null;
            return postprocessor.PrepareForBuild(buildOptions);
        }

        static public string GetExtensionForBuildTarget(BuildTargetGroup targetGroup, BuildTarget target, BuildOptions options) =>
           GetExtensionForBuildTarget(target, EditorUserBuildSettings.GetActiveSubtargetFor(target), options);

        static public string GetExtensionForBuildTarget(BuildTarget target, int subtarget, BuildOptions options)
        {
            IBuildPostprocessor postprocessor = ModuleManager.GetBuildPostProcessor(target);
            if (postprocessor == null)
                return string.Empty;
            return postprocessor.GetExtension(target, subtarget, options);
        }

        static public string GetExtensionForBuildTarget(BuildTarget target, BuildOptions options) =>
            GetExtensionForBuildTarget(target, EditorUserBuildSettings.GetActiveSubtargetFor(target), options);

        static public bool SupportsInstallInBuildFolder(BuildTarget target)
        {
            IBuildPostprocessor postprocessor = ModuleManager.GetBuildPostProcessor(target);
            if (postprocessor != null)
            {
                return postprocessor.SupportsInstallInBuildFolder();
            }

            return false;
        }

        static public bool SupportsLz4Compression(BuildTargetGroup targetGroup, BuildTarget target) =>
            SupportsLz4Compression(target);

        static public bool SupportsLz4Compression(BuildTarget target)
        {
            IBuildPostprocessor postprocessor = ModuleManager.GetBuildPostProcessor(target);
            if (postprocessor != null)
                return postprocessor.SupportsLz4Compression();
            return false;
        }

        static public Compression GetDefaultCompression(BuildTarget target)
        {
            IBuildPostprocessor postprocessor = ModuleManager.GetBuildPostProcessor(target);
            if (postprocessor != null)
                return postprocessor.GetDefaultCompression();
            return Compression.None;
        }

        private class NoTargetsFoundException : Exception
        {
            public NoTargetsFoundException() : base() {}
            public NoTargetsFoundException(string message) : base(message) {}
        }

        [RequiredByNativeCode]
        static public void Launch(BuildTarget buildTarget, string path, string productName, BuildOptions options, BuildReport buildReport)
        {
            IBuildPostprocessor postprocessor = ModuleManager.GetBuildPostProcessor(buildTarget);
            if (postprocessor != null)
            {
                BuildLaunchPlayerArgs args;
                args.target = buildTarget;
                args.playerPackage = BuildPipeline.GetPlaybackEngineDirectory(buildTarget, options);
                args.installPath = path;
                args.productName = productName;
                args.options = options;
                args.report = buildReport;

                postprocessor.LaunchPlayer(args);
            }
            else
            {
                throw new UnityException(
                    $"Launching for build target {buildTarget} is not supported: There is no build post-processor available.");
            }
        }

        static public void LaunchOnTargets(BuildTarget buildTarget, BuildReport buildReport, List<DeploymentTargetId> launchTargets)
        {
            try
            {
                // Early out so as not to show/update progressbars unnecessarily
                if (buildReport == null)
                    throw new System.NotSupportedException();

                ProgressHandler progressHandler = new ProgressHandler("Deploying Player",
                    delegate(string title, string message, float globalProgress)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar(title, message, globalProgress))
                            throw new DeploymentOperationAbortedException();
                    }, 0.1f);     // BuildPlayer.cpp starts off at 0.1f for some reason

                var taskManager = new ProgressTaskManager(progressHandler);

                // Launch on all selected targets
                taskManager.AddTask(() =>
                {
                    int successfulLaunches = 0;
                    var exceptions = new List<DeploymentOperationFailedException>();
                    foreach (var target in launchTargets)
                    {
                        try
                        {
                            var manager = DeploymentTargetManager.CreateInstance(buildReport.summary.platform);
                            var buildProperties = BuildProperties.GetFromBuildReport(buildReport);
                            manager.LaunchBuildOnTarget(buildProperties, target, taskManager.SpawnProgressHandlerFromCurrentTask());
                            successfulLaunches++;
                        }
                        catch (DeploymentOperationFailedException e)
                        {
                            exceptions.Add(e);
                        }
                    }

                    foreach (var e in exceptions)
                        UnityEngine.Debug.LogException(e);

                    if (successfulLaunches == 0)
                    {
                        // TODO: Maybe more specifically no compatible targets?
                        throw new NoTargetsFoundException("Could not launch build");
                    }
                });

                taskManager.Run();
            }
            catch (DeploymentOperationFailedException e)
            {
                UnityEngine.Debug.LogException(e);
                EditorUtility.DisplayDialog(e.title, e.Message, "Ok");
            }
            catch (DeploymentOperationAbortedException)
            {
                System.Console.WriteLine("Deployment aborted");
            }
            catch (NoTargetsFoundException)
            {
                throw new UnityException(string.Format("Could not find any valid targets to launch on for {0}", buildTarget));
            }
        }

        [RequiredByNativeCode]
        static public void UpdateBootConfig(BuildTarget target, BootConfigData config, BuildOptions options)
        {
            IBuildPostprocessor postprocessor = ModuleManager.GetBuildPostProcessor(target);
            if (postprocessor != null)
                postprocessor.UpdateBootConfig(target, config, options);

            foreach (var keyValue in projectBootConfigEntries)
            {
                if ((keyValue.Value == null) || keyValue.Value.All(char.IsWhiteSpace))
                    config.AddKey(keyValue.Key);
                else
                    config.Set(keyValue.Key, keyValue.Value);
            }
        }

        [RequiredByNativeCode]
        static public void Postprocess(BuildTarget target, int subtarget, string installPath, string companyName, string productName,
            BuildOptions options,
            RuntimeClassRegistry usedClassRegistry, BuildReport report)
        {
            string stagingArea = "Temp/StagingArea";
            string stagingAreaData = "Temp/StagingArea/Data";
            string stagingAreaDataManaged = "Temp/StagingArea/Data/Managed";
            string playerPackage = BuildPipeline.GetPlaybackEngineDirectory(target, options);

            // Disallow providing an empty string as the installPath
            bool willInstallInBuildFolder = (options & BuildOptions.InstallInBuildFolder) != 0 && SupportsInstallInBuildFolder(target);
            if (installPath == String.Empty && !willInstallInBuildFolder)
                throw new Exception(installPath + " must not be an empty string");

            IBuildPostprocessor postprocessor = ModuleManager.GetBuildPostProcessor(target);
            if (postprocessor == null)
                // If postprocessor is not provided, build target is not supported
                throw new UnityException($"Build target '{target}' not supported");

            try
            {
                AddIconsArgs iconArgs;
                iconArgs.stagingArea = stagingArea;
                if (!postprocessor.AddIconsToBuild(iconArgs))
                    throw new BuildFailedException("Failed to add player icon");

                BuildPostProcessArgs args;
                args.target = target;
                args.subtarget = subtarget;
                args.stagingAreaData = stagingAreaData;
                args.stagingArea = stagingArea;
                args.stagingAreaDataManaged = stagingAreaDataManaged;
                args.playerPackage = playerPackage;
                args.installPath = installPath;
                args.companyName = companyName;
                args.productName = productName;
                args.productGUID = PlayerSettings.productGUID;
                args.options = options;
                args.usedClassRegistry = usedClassRegistry;
                args.report = report;

                BuildProperties props;
                postprocessor.PostProcess(args, out props);

                if (props != null)
                {
                    report.AddAppendix(props);
                }

                return;
            }
            catch (BuildFailedException)
            {
                throw;
            }
            catch (Exception e)
            {
                // Rethrow exceptions during build postprocessing as BuildFailedException, so we don't pretend the build was fine.
                throw new BuildFailedException(e);
            }
        }

        public static void PostProcessCompletedBuild(BuildPostProcessArgs args)
        {
            var postprocessor = ModuleManager.GetBuildPostProcessor(args.target);
            postprocessor.PostProcessCompletedBuild(args);
        }
    }
}
