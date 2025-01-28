// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Reporting
{
    ///<summary>The BuildReport API gives you information about the Unity build process.</summary>
    ///<remarks>A BuildReport object is returned by <see cref="BuildPipeline.BuildPlayer" /> and can be used to discover information about the files output, the build steps taken, and other platform-specific information such as native code stripping.
    ///
    ///For AssetBundle builds the BuildReport is available by calling <see cref="GetLatestReport" /> immediately after calling <see cref="BuildPipeline.BuildAssetBundles" />.</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using System.IO;
    ///using System.Linq;
    ///using System.Text;
    ///using UnityEditor;
    ///using UnityEditor.Build;
    ///using UnityEditor.Build.Reporting;
    ///using UnityEngine;
    ///
    ///public class BuildReportExample
    ///{
    ///    [MenuItem("Example/Build AssetBundle")]
    ///    static public void BuildBundles()
    ///    {
    ///        string buildOutputDirectory = "BuildOutput";
    ///        if (!Directory.Exists(buildOutputDirectory))
    ///            Directory.CreateDirectory(buildOutputDirectory);
    ///
    ///        var bundleDefinitions = new AssetBundleBuild[]
    ///        {
    ///            new AssetBundleBuild()
    ///            {
    ///                assetBundleName = "MyBundle",
    ///                assetNames = new string[] { "Assets/Scenes/SampleScene.unity" }
    ///            }
    ///        };
    ///
    ///        BuildPipeline.BuildAssetBundles(
    ///            buildOutputDirectory,
    ///            bundleDefinitions,
    ///            BuildAssetBundleOptions.ForceRebuildAssetBundle,
    ///            EditorUserBuildSettings.activeBuildTarget);
    ///
    ///        BuildReport report = BuildReport.GetLatestReport();
    ///        if (report != null)
    ///        {
    ///            var sb = new StringBuilder();
    ///            sb.AppendLine("Build result   : " + report.summary.result);
    ///            sb.AppendLine("Build size     : " + report.summary.totalSize + " bytes");
    ///            sb.AppendLine("Build time     : " + report.summary.totalTime);
    ///            sb.AppendLine("Error summary  : " + report.SummarizeErrors());
    ///            sb.Append(LogBuildReportSteps(report));
    ///            sb.AppendLine(LogBuildMessages(report));
    ///            Debug.Log(sb.ToString());
    ///        }
    ///        else
    ///        {
    ///            // Certain errors like invalid input can fail the build immediately, with no BuildReport written
    ///            Debug.Log("AssetBundle build failed");
    ///        }
    ///    }
    ///
    ///    public static string LogBuildReportSteps(BuildReport buildReport)
    ///    {
    ///        var sb = new StringBuilder();
    ///
    ///        sb.AppendLine($"Build steps: {buildReport.steps.Length}");
    ///        int maxWidth = buildReport.steps.Max(s => s.name.Length + s.depth) + 3;
    ///        foreach (var step in buildReport.steps)
    ///        {
    ///            string rawStepOutput = new string('-', step.depth + 1) + ' ' + step.name;
    ///            sb.AppendLine($"{rawStepOutput.PadRight(maxWidth)}: {step.duration:g}");
    ///        }
    ///        return sb.ToString();
    ///    }
    ///
    ///    public static string LogBuildMessages(BuildReport buildReport)
    ///    {
    ///        var sb = new StringBuilder();
    ///        foreach (var step in buildReport.steps)
    ///        {
    ///            foreach (var message in step.messages)
    ///                // If desired, this logic could ignore any Info or Warning messages to focus on more serious messages
    ///                sb.AppendLine($"[{message.type}] {message.content}");
    ///        }
    ///
    ///        string messages = sb.ToString();
    ///        if (messages.Length > 0)
    ///            return "Messages logged during Build:\n" + messages;
    ///        else
    ///            return "";
    ///    }
    ///}
    ///
    /// // For the purpose of demonstration, this callback logs different types of errors and forces a build failure
    ///[BuildCallbackVersion(1)]
    ///class MyTroublesomeBuildCallback : IProcessSceneWithReport
    ///{
    ///    public int callbackOrder { get { return 0; } }
    ///    public void OnProcessScene(UnityEngine.SceneManagement.Scene scene, BuildReport report)
    ///    {
    ///        Debug.Log("MyTroublesomeBuildCallback called for " + scene.name);
    ///        Debug.LogError("Logging an error");
    ///
    ///        throw new BuildFailedException("Forcing the build to stop");
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [NativeHeader("Runtime/Utilities/DateTime.h")]
    [NativeType(Header = "Modules/BuildReportingEditor/Public/BuildReport.h")]
    [NativeClass("BuildReporting::BuildReport")]
    public sealed class BuildReport : Object
    {
        private BuildReport()
        {
        }

        [System.Obsolete("Use GetFiles() method instead (UnityUpgradable) -> GetFiles()", true)]
        public BuildFile[] files => throw new NotSupportedException();

        ///<summary>Returns an array of all the files output by the build process.</summary>
        ///<remarks>The returned array is a copy and this method execution length scales linearly with number of files.</remarks>
        ///<returns>An array of all the files output by the build process.</returns>
        public extern BuildFile[] GetFiles();

        ///<summary>An array of all the <see cref="BuildStep" />s that took place during the build process.</summary>
        [NativeName("BuildSteps")]
        public extern BuildStep[] steps { get; }

        ///<summary>A <see cref="BuildSummary" /> containing overall statistics and data about the build process.</summary>
        public extern BuildSummary summary { get; }

        ///<summary>The <see cref="StrippingInfo" /> object for the build.</summary>
        ///<remarks>The StrippingInfo object contains information about which native code modules in the engine are still present in the build, and the reasons why they are still present.
        ///
        ///This is only available when building for platforms that support code stripping. When building for other platforms, this property will be null.</remarks>
        public StrippingInfo strippingInfo
        {
            get { return GetAppendices<StrippingInfo>().SingleOrDefault(); }
        }

        ///<summary>An array of all the <see cref="PackedAssets" /> generated by the build process.</summary>
        public PackedAssets[] packedAssets
        {
            get { return GetAppendicesByType<PackedAssets>(); }
        }

        ///<summary>An optional array of <see cref="ScenesUsingAssets" /> generated by the build process if <see cref="BuildOptions.DetailedBuildReport" /> was used during the build.</summary>
        public ScenesUsingAssets[] scenesUsingAssets
        {
            get { return GetAppendicesByType<ScenesUsingAssets>(); }
        }

        [NativeMethod("RelocateFiles")]
        internal extern void RecordFilesMoved(string originalPathPrefix, string newPathPrefix);

        [NativeMethod("AddFile")]
        internal extern void RecordFileAdded(string path, string role);

        [NativeMethod("AddFilesRecursive")]
        internal extern void RecordFilesAddedRecursive(string rootDir, string role);

        [NativeMethod("DeleteFile")]
        internal extern void RecordFileDeleted(string path);

        [NativeMethod("DeleteFilesRecursive")]
        internal extern void RecordFilesDeletedRecursive(string rootDir);

        [NativeMethod("DeleteAllFiles")]
        internal extern void DeleteAllFileEntries();

        ///<summary>Returns a string summarizing any errors that occurred during the build</summary>
        ///<remarks>Convenience method for summarizing errors (or exceptions) that occurred during a build into a single line of text.
        ///If no error was logged this returns an empty string.  If a single error was logged this reports the error messages.  Otherwise it reports the number of errors, for example "5 errors".
        ///
        ///Note: To examine all errors, warnings and other messages recorded during a build you can enumerating through the build <see cref="steps" /> and check <see cref="Build.Reporting.BuildStep.messages" />.
        ///And to retrieve the count of errors call <see cref="Build.Reporting.BuildSummary.totalErrors" />.</remarks>
        [FreeFunction("BuildReporting::SummarizeErrors", HasExplicitThis = true)]
        public extern string SummarizeErrors();

        internal extern void AddMessage(LogType messageType, string message, string exceptionType);

        internal extern void SetBuildResult(BuildResult result);

        internal extern int BeginBuildStep(string stepName);
        internal extern void ResumeBuildStep(int depth);
        internal extern void EndBuildStep(int depth);

        internal extern void AddAppendix([NotNull] Object obj);

        internal TAppendix[] GetAppendices<TAppendix>() where TAppendix : Object
        {
            return GetAppendices(typeof(TAppendix)).Cast<TAppendix>().ToArray();
        }

        internal extern Object[] GetAppendices([NotNull] Type type);

        internal TAppendix[] GetAppendicesByType<TAppendix>() where TAppendix : Object
        {
            return GetAppendicesByType(typeof(TAppendix)).Cast<TAppendix>().ToArray();
        }

        [NativeThrows]
        internal extern Object[] GetAppendicesByType([NotNull] Type type);

        internal extern Object[] GetAllAppendices();

        ///<summary>Return the build report generated by the most recent Player or AssetBundle build</summary>
        ///<remarks>A BuildReport is generated when a Player build runs, or when <see cref="BuildPipeline.BuildAssetBundles" /> is called.
        ///The BuildReport is automatically saved to **Library/LastBuild.buildreport** and can be reloaded using this static method.</remarks>
        [FreeFunction("BuildReporting::GetLatestReport")]
        public static extern BuildReport GetLatestReport();

        [FreeFunction("BuildReporting::GetReport")]
        internal static extern BuildReport GetReport(GUID guid);

        internal extern void SetBuildGUID(GUID guid);
    }
}
