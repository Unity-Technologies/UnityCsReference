// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEditor.Multiplayer.Internal;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    internal class BuildReportData
    {
        public string outputPath;
        public string executablePath;
        public long buildEndedAtTicks;
    }

    [Serializable]
    class BuildPlayerNode : ExecutionNode
    {
        [SerializeReference] public NodeInput<string> BuildPath;
        [SerializeReference] public NodeInput<BuildProfile> Profile;
        [SerializeReference] public NodeInput<bool> ReuseExistingBuild;

        [SerializeReference] public NodeOutput<string> ExecutablePath;
        [SerializeReference] public NodeOutput<string> OutputPath;
        [SerializeReference] public NodeOutput<string> RelativeExecutablePath;
        [SerializeReference] public NodeOutput<Hash128> BuildHash;
        [SerializeReference] public NodeOutput<BuildReport> BuildReport;

        public BuildPlayerNode()
        {
            BuildPath = new(this);
            Profile = new(this);
            ReuseExistingBuild = new(this);

            OutputPath = new(this);
            ExecutablePath = new(this);
            RelativeExecutablePath = new(this);
            BuildHash = new(this);
            BuildReport = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var buildPath = GetInput(BuildPath);
            var buildProfile = GetInput(Profile);
            var reuseExistingBuild = GetInput(ReuseExistingBuild);

            if (EditorUtility.scriptCompilationFailed)
                throw new ApplicationException("Script compilation failed, aborting build.");

            if (reuseExistingBuild)
            {
                var buildOutputPath = InternalUtilities.AddBuildExtension(buildPath, buildProfile);
                var absoluteBuildOutputPath = Path.GetFullPath(buildOutputPath);

                if (File.Exists(absoluteBuildOutputPath) || Directory.Exists(absoluteBuildOutputPath))
                {
                    var outputPath = Path.GetDirectoryName(absoluteBuildOutputPath);
                    var reportPath = Path.Combine(outputPath, ".buildreport");

                    if (File.Exists(reportPath))
                    {
                        try
                        {
                            var reportData = JsonUtility.FromJson<BuildReportData>(File.ReadAllText(reportPath));
                            var executablePath = reportData.executablePath;
                            var relativeExecutablePath = Path.GetRelativePath(outputPath, executablePath);

                            SetOutput(OutputPath, outputPath);
                            SetOutput(ExecutablePath, executablePath);
                            SetOutput(RelativeExecutablePath, relativeExecutablePath);
                            SetOutput(BuildHash, ComputeBuildHash(outputPath));
                            SetOutput(BuildReport, null);
                            return;
                        }
                        catch (Exception)
                        {
                            Debug.LogWarning("Failed to load the executable path, cannot reuse existing build.");
                        }
                    }
                }
            }

            while (BuildPipeline.isBuildingPlayer)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }

            var buildCompleted = false;
            var exception = default(Exception);

            await Task.Yield();

            EditorApplication.delayCall += () =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                try
                {
                    ExecuteBuildCommand();
                    buildCompleted = true;
                }
                catch (Exception e)
                {
                    exception = e;
                }
            };

            while (!buildCompleted && exception == null)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (exception != null)
                throw exception;
        }

        void ExecuteBuildCommand()
        {
            var buildPath = GetInput(BuildPath);
            var buildProfile = GetInput(Profile);

            // Save the original product name to restore it later
            var originalProductName = PlayerSettings.productName;

            // Save the currently active build target, it's sub-target and the multiplayer role in case of they would be modified by the build.
            var previousProfile = InternalUtilities.BuildProfileState.FromActiveSettings();
            DebugUtils.Trace("Building");

            BuildReport report = null;
            try
            {
                var role = MultiplayerRolesSettings.instance.GetMultiplayerRoleForBuildProfile(buildProfile);
                var roleText = InternalUtilities.GetMultiplayerRoleDisplayText(role);
                PlayerSettings.productName = $"{originalProductName} ({roleText})";

                report = BuildPipeline.BuildPlayer(
                    new BuildPlayerWithProfileOptions
                    {
                        buildProfile = buildProfile,
                        locationPathName = InternalUtilities.AddBuildExtension(buildPath, buildProfile),
                    });

                if (report == null)
                    throw new Exception("BuildPipeline.BuildPlayer failed to generate a build report. The build artifact is likely corrupted.");
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new Exception(report.SummarizeErrors());
                }

                var outputPath = Path.GetDirectoryName(report.summary.outputPath);
                var executablePath = ExtractExecutablePath(report);
                var relativeExecutablePath = Path.GetRelativePath(outputPath, executablePath);

                SetOutput(OutputPath, outputPath);
                SetOutput(ExecutablePath, executablePath);
                SetOutput(RelativeExecutablePath, relativeExecutablePath);
                SetOutput(BuildHash, ComputeBuildHash(outputPath));
                SetOutput(BuildReport, report);

                var reportPath = Path.Combine(outputPath, ".buildreport");
                var localBuildTime = report.summary.buildEndedAt.ToLocalTime();
                File.WriteAllText(reportPath, JsonUtility.ToJson(new BuildReportData
                {
                    outputPath = report.summary.outputPath,
                    executablePath = executablePath,
                    buildEndedAtTicks = localBuildTime.Ticks
                }, prettyPrint: true));
            }
            catch (Exception e)
            {
                if (report != null && report.summary.result == BuildResult.Cancelled)
                    throw new OperationCanceledException("Build was cancelled.", e);

                Debug.LogException(e);
                throw;
            }
            finally
            {
                PlayerSettings.productName = originalProductName;
                InternalUtilities.BuildProfileState.Restore(previousProfile);
            }
        }

        private static readonly ProfilerMarker s_ComputeBuildHash = new("EditorBuildNode.ComputeBuildHash");
        private static Hash128 ComputeBuildHash(string buildPath)
        {
            // This provides a unique hash for the build based on its content.
            // If two builds share the same hash, it means they have equivalent content.
            // The current implementation is focused on Linux builds, that is because those are the
            // ones uploaded to the cloud. Mac builds, for instance, have a signing process that alter
            // this hash, and therefore extending the support to Mac builds will require more work and
            // potentially a different approach.

            using var _ = s_ComputeBuildHash.Auto();

            var files = GetAllBuildFilesForHash(buildPath);
            return HashUtils.ComputeForFiles(files);
        }

        private static string[] GetAllBuildFilesForHash(string buildPath)
        {
            var files = Directory.GetFiles(buildPath, "*", SearchOption.AllDirectories);
            var filteredFiles = new List<string>(files.Length);
            for (var i = files.Length - 1; i >= 0; i--)
            {
                var file = files[i];
                // boot.config contains a build guid which is unique to each build, so we should not
                // use it to compute the build hash.
                if (file.EndsWith(".DS_Store") || file.EndsWith("boot.config"))
                    continue;

                filteredFiles.Add(file);
            }

            return filteredFiles.ToArray();
        }

        private static string ExtractExecutablePath(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.StandaloneOSX)
            {
                if (report.summary.GetSubtarget<StandaloneBuildSubtarget>() == StandaloneBuildSubtarget.Server)
                    return $"{report.summary.outputPath}/{Application.productName}";
                return $"{report.summary.outputPath}/Contents/MacOS/{Application.productName}";
            }

            return report.summary.outputPath;
        }

        public static void BuildNow(BuildProfile buildProfile)
        {
            if (buildProfile == null)
            {
                Debug.LogError("Cannot build: No build profile provided");
                return;
            }

            var buildPath = ScenarioFactory.GenerateBuildPath(buildProfile);

            var tempNode = new BuildPlayerNode();
            tempNode.BuildPath = new NodeInput<string>(tempNode);
            tempNode.Profile = new NodeInput<BuildProfile>(tempNode);
            tempNode.ReuseExistingBuild = new NodeInput<bool>(tempNode);

            tempNode.BuildPath.SetValue(buildPath);
            tempNode.Profile.SetValue(buildProfile);
            tempNode.ReuseExistingBuild.SetValue(false);

            tempNode.ExecuteBuildCommand();
        }
    }
}
