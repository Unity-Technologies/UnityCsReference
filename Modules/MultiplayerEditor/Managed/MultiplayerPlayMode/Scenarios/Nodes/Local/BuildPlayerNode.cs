// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEditor.Build.Profile;
using UnityEditor.Multiplayer.Internal;
using System.Collections.Generic;
using Unity.Profiling;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class BuildPlayerNode : ExecutionNode
    {
        [SerializeReference] public NodeInput<string> BuildPath;
        [SerializeReference] public NodeInput<BuildProfile> Profile;

        [SerializeReference] public NodeOutput<string> ExecutablePath;
        [SerializeReference] public NodeOutput<string> OutputPath;
        [SerializeReference] public NodeOutput<string> RelativeExecutablePath;
        [SerializeReference] public NodeOutput<Hash128> BuildHash;
        [SerializeReference] public NodeOutput<BuildReport> BuildReport;

        public BuildPlayerNode()
        {
            BuildPath = new(this);
            Profile = new(this);

            OutputPath = new(this);
            ExecutablePath = new(this);
            RelativeExecutablePath = new(this);
            BuildHash = new(this);
            BuildReport = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                DebugUtils.Trace("Can't build when there are compile errors, aborting build");
                throw new ApplicationException("Script compilation failed, aborting build.");
            }

            // TODO: Make sure we can reuse builds when possible.

            // Build pipeline does not support parallel builds.
            while (BuildPipeline.isBuildingPlayer)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }

            // Make sure the build is executed in the editor loop.
            var buildCompleted = false;
            var exception = default(Exception);

            // This is important because it gives the UI extra time to set things up properly before it starts building, preventing it from blocking the Editor process.
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

            BuildReport report;
            try
            {
                // Modify product name to include multiplayer role for window title
                var role = MultiplayerRolesSettings.instance.GetMultiplayerRoleForBuildProfile(buildProfile);
                var roleText = InternalUtilities.GetMultiplayerRoleDisplayText(role);
                PlayerSettings.productName = $"{originalProductName} ({roleText})";

                // Build the player with the specified build profile.
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

                // If the build is successful, set the node outputs
                var outputPath = Path.GetDirectoryName(report.summary.outputPath);
                var executablePath = ExtractExecutablePath(report);
                SetOutput(OutputPath, outputPath);
                SetOutput(ExecutablePath, executablePath);
                SetOutput(RelativeExecutablePath, Path.GetRelativePath(outputPath, executablePath));
                SetOutput(BuildHash, ComputeBuildHash(outputPath));
                SetOutput(BuildReport, report);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
            finally
            {
                // Restore original product name and build profile state
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
    }
}
