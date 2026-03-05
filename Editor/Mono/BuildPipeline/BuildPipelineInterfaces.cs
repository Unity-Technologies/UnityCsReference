// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEditor.Profiling;
using UnityEditor.Rendering;
using UnityEngine.Scripting;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Content;
using UnityEngine.Rendering;
using UnityEngine.Bindings;
using Unity.Collections;

namespace UnityEditor.Build
{
    ///<summary>Interface that provides control over callback order.</summary>
    ///<remarks>This is the base class for build callback interfaces, for example <see cref="Build.IPreprocessBuildWithContext" />, <see cref="Build.IPreprocessShaders" />, <see cref="Build.IProcessSceneWithReport" />, <see cref="Build.IPostprocessBuildWithContext" />, and <see cref="Build.IUnityLinkerProcessor" />.
    ///
    ///Every class that implements these interfaces must define the callbackOrder property with a "get" accessor.</remarks>
    public interface IOrderedCallback
    {
        ///<summary>Returns a numeric value that determines the order in which the build callback is invoked.</summary>
        ///<remarks>Callbacks with lower values are called before those with higher values, allowing you to control the execution order of build callbacks.
        ///This mechanism is particularly useful for resolving conflicts between different callback implementations by specifying their relative order.
        ///
        ///It is important to note that complete control over the callback order may not always be feasible, due to the use of callbacks by code that may be outside
        ///your control, for example inside packages and inside the implementation of the Unity Editor.  For instance, even if you assign a large numeric value to
        ///ensure your callback is the last to run, other implementations might specify the same or an even higher value.</remarks>
        ///<example>
        ///  <code source="../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/IOrderedCallback_IOrderedCallback.cs"/>
        ///</example>
        int callbackOrder { get; }
    }

    ///<summary>This interface is obsolete. Use <see cref="Build.IPreprocessBuildWithContext" /> instead.</summary>
    [Obsolete("Use IPreprocessBuildWithReport instead")]
    public interface IPreprocessBuild : IOrderedCallback
    {
        ///<summary>This method is obsolete. Use <see cref="Build.IPreprocessBuildWithContext.OnPreprocessBuild" /> instead.</summary>
        void OnPreprocessBuild(BuildTarget target, string path);
    }

    ///<summary>Extend BuildPlayerProcessor to receive callbacks during a player build.</summary>
    ///<remarks>Add files and perform custom setup before the build starts. For more information, refer to [Use build callbacks](xref:build-callbacks)</remarks>
    ///<seealso cref="IFilterBuildAssemblies" />
    ///<seealso cref="IPostBuildPlayerScriptDLLs" />
    ///<seealso cref="IUnityLinkerProcessor" />
    ///<seealso cref="IPreprocessBuildWithContext" />
    ///<seealso cref="IPostprocessBuildWithContext" />
    public abstract class BuildPlayerProcessor : IOrderedCallback
    {
        ///<summary>Returns the relative callback order for callbacks.  Callbacks with lower values are called before ones with higher values.</summary>
        public virtual int callbackOrder => 0;
        ///<summary>Implement this function to receive a callback before a Player build starts.</summary>
        ///<remarks>You can use this function to customize the build before Unity starts building the Player. For example, the following code example demonstrates how to include streaming assets in the Player build without placing them in your project's <c>StreamingAssets</c> folder.</remarks>
        ///<param name="buildPlayerContext">The context for the scheduled Player build.</param>
        ///<example>
        ///  <code><![CDATA[
        ///class PrepareBuild : UnityEditor.Build.BuildPlayerProcessor
        ///{
        ///    public override void PrepareForBuild(UnityEditor.Build.BuildPlayerContext buildPlayerContext)
        ///    {
        ///        // Add data files to the Player build's StreamingAssets folder
        ///        // Works for files located both inside and outside the Unity project
        ///
        ///        buildPlayerContext.AddAdditionalPathToStreamingAssets("Assets/dataFromUnityProject.txt", "dataFromUnityProject.txt");
        ///
        ///        buildPlayerContext.AddAdditionalPathToStreamingAssets("C:/Temp/dataOutsideUnityProject.txt", "dataOutsideUnityProject.txt");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="BuildPlayerContext.AddAdditionalPathToStreamingAssets" />
        public abstract void PrepareForBuild(BuildPlayerContext buildPlayerContext);
    }

    ///<summary>Implement this interface to execute code at the start of the Player build process.</summary>
    ///<remarks>This interface is replaced by <see cref="Build.IPreprocessBuildWithContext" />, which works for AssetBundle builds as well.
    ///
    ///At the start of a Player build, Unity uses the <see cref="IOrderedCallback.callbackOrder" /> property on each implementation to determine the order in which to invoke the callbacks.
    ///
    ///This callback can be useful for automated tasks and ensuring your build environment is correctly configured.
    ///
    ///Example usages include:
    ///
    ///* For validation checks, e.g. confirming required build settings, environmental variables, content or other project-specific conditions.  When possible you can automatically fix problems by changing settings. Or you can fail the build, by throwing a BuildFailedException along with a clear error message.
    ///* To make sure required Assets are included in the build.  See <see cref="PlayerSettings.SetPreloadedAssets" />.
    ///* To generate version numbers, change logs, link.xml files or other content that should be regenerated prior to each Player build.
    ///* For logging, reporting or sending analytics.
    ///
    ///Note: Build callbacks are a powerful feature, but it is strongly recommended that their implementations maintain deterministic build outputs.
    ///The result of a build should be predictable and reproducible, based on the project’s content, the Unity version, and installed packages.
    ///Introducing environment-specific behavior, external dependencies, randomness, or other non-deterministic elements can lead to outcomes
    ///that are challenging to debug or reproduce. This unpredictability may also compromise the efficiency and accuracy of incremental builds or incremental upgrades.</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using System;
    ///using UnityEditor.Build;
    ///using UnityEditor.Build.Reporting;
    ///
    ///class BuildScheduleEnforcer : IPreprocessBuildWithReport
    ///{
    ///    public int callbackOrder { get { return 100; } }
    ///    public void OnPreprocessBuild(BuildReport report)
    ///    {
    ///        if (DateTime.Now.DayOfWeek == DayOfWeek.Thursday)
    ///            // Force the build to fail. This message will appear in the console and Editor log.
    ///            throw new BuildFailedException("No builds are allowed on Thursdays");
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<seealso cref="Build.BuildPlayerProcessor.PrepareForBuild" />
    ///<seealso cref="Build.IPostprocessBuildWithReport" />
    ///<seealso cref="Build.BuildPlayerProcessor" />
    ///<seealso cref="BuildPipeline.BuildPlayer" />
    public interface IPreprocessBuildWithReport : IOrderedCallback
    {
        ///<summary>Implement this method to receive a callback before the build is started.</summary>
        ///<remarks>This method is replaced by <see cref="Build.IPreprocessBuildWithContext.OnPreprocessBuild" />, which works for AssetBundle builds as well.
        ///                    This callback is invoked during Player builds, but not during AssetBundle builds.</remarks>
        ///<param name="report">A report containing information about the build, such as its target platform and output path.</param>
        ///<example>
        ///  <code source="../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/IPreprocessBuildWithReport_OnPreprocessBuild2.cs"/>
        ///</example>
        ///<seealso cref="Build.IPostprocessBuildWithReport" />
        ///<seealso cref="Build.BuildPlayerProcessor" />
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        void OnPreprocessBuild(BuildReport report);
    }
    ///<summary>Implement this interface to execute code at the start of the Player build or AssetBundle build process.</summary>
    ///<remarks>At the start of a Player build or AssetBundle build, Unity uses the <see cref="IOrderedCallback.callbackOrder" /> property on each implementation to determine the order in which to invoke the callbacks.
    ///
    ///This callback can be useful for automated tasks and ensuring your build environment is correctly configured.
    ///
    ///You can't invoke an additional build from inside this callback.  To invoke an AssetBundle build at the start of a Player build you should use <see cref="Build.BuildPlayerProcessor.PrepareForBuild" /> instead.
    ///
    ///Example usages include:
    ///
    ///* For validation checks, e.g. confirming required build settings, environmental variables, content or other project-specific conditions.  When possible you can automatically fix problems by changing settings. Or you can fail the build, by throwing a BuildFailedException along with a clear error message.
    ///* To make sure required Assets are included in the build.  See <see cref="PlayerSettings.SetPreloadedAssets" />.
    ///* To generate version numbers, change logs, link.xml files or other content that should be regenerated prior to each Player build or AssetBundle build.
    ///* For logging, reporting or sending analytics.
    ///
    ///Note: Build callbacks are a powerful feature, but it is strongly recommended that their implementations maintain deterministic build outputs.
    ///The result of a build should be predictable and reproducible, based on the project’s content, the Unity version, and installed packages.
    ///Introducing environment-specific behavior, external dependencies, randomness, or other non-deterministic elements can lead to outcomes
    ///that are challenging to debug or reproduce. This unpredictability might compromise the efficiency and accuracy of incremental builds or incremental upgrades.
    ///
    ///The main difference between this interface and <see cref="Build.IPreprocessBuildWithReport" /> or <see cref="Build.IPreprocessBuild" /> is that this callback gets called on AssetBundle builds and Player builds.
    ///For more information about build callbacks, refer to [Use build callbacks](xref:build-callbacks)</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using System;
    ///using UnityEditor.Build;
    ///
    ///class BuildScheduleEnforcer : IPreprocessBuildWithContext
    ///{
    ///    public int callbackOrder { get { return 100; } }
    ///    public void OnPreprocessBuild(BuildCallbackContext ctx)
    ///    {
    ///        if (DateTime.Now.DayOfWeek == DayOfWeek.Thursday)
    ///            // Force the build to fail. This message will appear in the console and Editor log.
    ///            throw new BuildFailedException("No builds are allowed on Thursdays");
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<seealso cref="Build.BuildPlayerProcessor.PrepareForBuild" />
    ///<seealso cref="Build.IPostprocessBuildWithContext" />
    ///<seealso cref="Build.BuildPlayerProcessor" />
    ///<seealso cref="BuildPipeline.BuildPlayer" />
    ///<seealso cref="BuildPipeline.BuildAssetBundles" />
    public interface IPreprocessBuildWithContext : IOrderedCallback
    {
        ///<summary>Implement this method to receive a callback before the build is started.</summary>
        ///<remarks>This callback is invoked during Player builds and AssetBundle builds.</remarks>
        ///<param name="ctx">A context containing information about the build, such as its build report.</param>
        ///<example>
        ///  <code source="../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/IPreprocessBuildWithContext_OnPreprocessBuild2.cs"/>
        ///</example>
        ///<seealso cref="Build.IPostprocessBuildWithContext" />
        ///<seealso cref="Build.BuildPlayerProcessor" />
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        ///<seealso cref="BuildPipeline.BuildAssetBundles" />
        void OnPreprocessBuild(BuildCallbackContext ctx);
    }

    ///<summary>Implement this interface to receive a callback to filter assemblies away from the build.</summary>
    ///<remarks>For more information about build callbacks, refer to [Use build callbacks](xref:build-callbacks)</remarks>
    public interface IFilterBuildAssemblies : IOrderedCallback
    {
        ///<summary>Will be called after building script assemblies, but makes it possible to filter away unwanted scripts to be included.</summary>
        ///<remarks>Each implementation will be called in the order sorted by callbackOrder. The result of each invocation is piped through on the next call to OnFilterAssemblies.
        ///You are not allowed to add new assemblies.</remarks>
        ///<param name="buildOptions">The current build options.</param>
        ///<param name="assemblies">The list of assemblies that will be included.</param>
        ///<returns>Returns the filtered list of assemblies that are included in the build.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEditor;
        ///using UnityEditor.Build;
        ///using UnityEditor.Build.Reporting;
        ///using UnityEngine;
        ///using System.Linq;
        ///
        ///class MyCustomFilter : IFilterBuildAssemblies
        ///{
        ///    public int callbackOrder { get { return 0; } }
        ///    public string[] OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies)
        ///    {
        ///        return assemblies.Where(x => x == "some.dll").ToArray();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Build.BuildPlayerProcessor" />
        ///<seealso cref="IPostBuildPlayerScriptDLLs" />
        ///<seealso cref="IUnityLinkerProcessor" />
        string[] OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies);
    }

    ///<summary>This interface is obsolete. Use <see cref="Build.IPostprocessBuildWithContext" /> instead.</summary>
    [Obsolete("Use IPostprocessBuildWithReport instead")]
    public interface IPostprocessBuild : IOrderedCallback
    {
        ///<summary>This method is obsolete. Use <see cref="Build.IPostprocessBuildWithContext.OnPostprocessBuild" /> instead.</summary>
        void OnPostprocessBuild(BuildTarget target, string path);
    }

    ///<summary>Implement this interface to execute code immediately after the Player build process is completed.</summary>
    ///<remarks>This interface is replaced by <see cref="Build.IPostprocessBuildWithContext" />, which works for AssetBundle builds as well.
    ///This is useful for tasks that need to be performed as the last step of building, such as cleaning up assets, generating analytics or reports, or customizing build outputs.
    ///
    ///As a final step of a Player build, Unity uses the <see cref="IOrderedCallback.callbackOrder" /> property on each implementation to determine the order in which to invoke the callbacks.</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using System.Linq;
    ///using System.Text;
    ///using UnityEditor.Build;
    ///using UnityEditor.Build.Reporting;
    ///using UnityEngine;
    ///
    /// // To try this example add this code into an Editor-only assembly,
    /// // run a Player build, and then look for the message in the console.
    /// // Note: if the build fails or is cancelled then the code will not run.
    ///class BuildPostProcessor : IPostprocessBuildWithReport
    ///{
    ///    public int callbackOrder { get { return 100; } }
    ///    public void OnPostprocessBuild(BuildReport report)
    ///    {
    ///        // Log some information from the BuildReport
    ///        // Note: OnPostprocessBuild callbacks are invoked before the build is complete.
    ///        // So the content of the BuildReport is not completely finalized.
    ///        // For example, the summary.buildEndedAt has not been be determined,
    ///        // and the incomplete "parent" BuildSteps still report 0 for their durations.
    ///        var summary = report.summary;
    ///
    ///        var files = report.GetFiles();
    ///        ulong size = 0;
    ///        foreach (var file in files)
    ///            size += file.size;
    ///
    ///        var sb = new StringBuilder();
    ///        sb.AppendLine("Build completed");
    ///        sb.AppendLine($"  Target: {summary.platform}");
    ///        sb.AppendLine($"  Output Location: {summary.outputPath}");
    ///        sb.AppendLine($"  Number of output files: {files.Length}");
    ///        sb.AppendLine($"  Total size in bytes: {size}");
    ///        sb.AppendLine($"  Starting time: {summary.buildStartedAt.ToLocalTime().ToShortTimeString()}");
    ///        sb.AppendLine();
    ///
    ///        var buildSteps = report.steps;
    ///        sb.AppendLine($"Build steps: {buildSteps.Length}");
    ///        int maxWidth = buildSteps.Max(s => s.name.Length + s.depth) + 2;
    ///        foreach (var step in buildSteps)
    ///        {
    ///            string rawStepOutput = new string('-', step.depth) + ' ' + step.name;
    ///            sb.AppendLine($"{rawStepOutput.PadRight(maxWidth)}: {step.duration:g}");
    ///        }
    ///
    ///        Debug.Log(sb.ToString());
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<seealso cref="Build.IPreprocessBuildWithReport" />
    ///<seealso cref="BuildPipeline.BuildPlayer" />
    public interface IPostprocessBuildWithReport : IOrderedCallback
    {
        ///<summary>Implement this function to receive a callback after the build is complete.</summary>
        ///<remarks>This method is replaced by <see cref="Build.IPostprocessBuildWithContext.OnPostprocessBuild" />, which works for AssetBundle builds as well.
        ///                    This callback is invoked during Player builds, but not during AssetBundle builds.
        ///                    If the build stops early, due to a failure or cancellation, then the callback is not invoked.</remarks>
        ///<param name="report">A BuildReport containing information about the build, such as the target platform and output path.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEditor;
        ///using UnityEditor.Build;
        ///using UnityEditor.Build.Reporting;
        ///using UnityEngine;
        ///
        ///class MyCustomBuildProcessor : IPostprocessBuildWithReport
        ///{
        ///    public int callbackOrder { get { return 0; } }
        ///    public void OnPostprocessBuild(BuildReport report)
        ///    {
        ///        Debug.Log("MyCustomBuildProcessor.OnPostprocessBuild for target " + report.summary.platform + " at path " + report.summary.outputPath);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        ///<seealso cref="Build.IPreprocessBuildWithReport" />
        void OnPostprocessBuild(BuildReport report);
    }
    ///<summary>Implement this interface to execute code immediately after the Player build or AssetBundle build process is completed.</summary>
    ///<remarks>This is useful for tasks that need to run after a build completes, even if the build failed or was cancelled. For example, you might want to clean up assets, generate analytics or reports, or customize build outputs. The postprocess callback runs whether the build succeeds, fails, or is cancelled, as long as the corresponding <see cref="Build.IPreprocessBuildWithContext" /> callback ran. It's only skipped if early validation prevents the build from starting.
    ///
    ///As a final step of a Player build or AssetBundle build, Unity uses the <see cref="IOrderedCallback.callbackOrder" /> property on each implementation to determine the order in which to invoke the callbacks.
    ///
    ///Note: The main difference between this interface and <see cref="Build.IPostprocessBuildWithReport" /> or <see cref="Build.IPostprocessBuild" /> is that this callback gets called on AssetBundle builds as well as Player builds.</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using System.Linq;
    ///using System.Text;
    ///using UnityEditor.Build;
    ///using UnityEditor.Build.Reporting;
    ///using UnityEngine;
    ///
    /// // To try this example add this code into an Editor-only assembly,
    /// // run a Player build or AssetBundle build, and then look for the message in the console.
    ///class BuildPostProcessor : IPostprocessBuildWithContext
    ///{
    ///    public int callbackOrder { get { return 100; } }
    ///    public void OnPostprocessBuild(BuildCallbackContext ctx)
    ///    {
    ///        // Log some information from the BuildCallbackContext
    ///        // Note: OnPostprocessBuild callbacks are invoked after the build process completes,
    ///        // regardless of whether it succeeded, failed, or was cancelled.
    ///        // However, the content of the BuildCallbackContext.Report may not be completely finalized.
    ///        // For example, the summary.buildEndedAt has not been be determined,
    ///        // and the incomplete "parent" BuildSteps still report 0 for their durations.
    ///        var summary = ctx.Report.summary;
    ///
    ///        // This callback runs whether the build succeeded, failed, or was cancelled
    ///        if (summary.result != BuildResult.Succeeded)
    ///        {
    ///            Debug.LogWarning($"Build completed with result: {summary.result}");
    ///        }
    ///
    ///        var files = ctx.Report.GetFiles();
    ///        ulong size = 0;
    ///        foreach (var file in files)
    ///            size += file.size;
    ///
    ///        var sb = new StringBuilder();
    ///        sb.AppendLine("Build completed");
    ///        sb.AppendLine($"  Target: {summary.platform}");
    ///        sb.AppendLine($"  Output Location: {summary.outputPath}");
    ///        sb.AppendLine($"  Number of output files: {files.Length}");
    ///        sb.AppendLine($"  Total size in bytes: {size}");
    ///        sb.AppendLine($"  Starting time: {summary.buildStartedAt.ToLocalTime().ToShortTimeString()}");
    ///        sb.AppendLine();
    ///
    ///        var buildSteps = ctx.Report.steps;
    ///        sb.AppendLine($"Build steps: {buildSteps.Length}");
    ///        int maxWidth = buildSteps.Max(s => s.name.Length + s.depth) + 2;
    ///        foreach (var step in buildSteps)
    ///        {
    ///            string rawStepOutput = new string('-', step.depth) + ' ' + step.name;
    ///            sb.AppendLine($"{rawStepOutput.PadRight(maxWidth)}: {step.duration:g}");
    ///        }
    ///
    ///        Debug.Log(sb.ToString());
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<seealso cref="Build.IPreprocessBuildWithContext" />
    ///<seealso cref="BuildPipeline.BuildPlayer" />
    ///<seealso cref="BuildPipeline.BuildAssetBundles" />
    public interface IPostprocessBuildWithContext : IOrderedCallback
    {
        ///<summary>Implement this function to receive a callback after the build is complete.</summary>
        ///<remarks>This callback is invoked during Player builds or AssetBundle builds.
        ///                    This callback is invoked even when the build stops early due to a failure or cancellation. However it will not be invoked if initial validation checks prevent the build from starting.</remarks>
        ///<param name="ctx">A context containing information about the build, such as its build report.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEditor;
        ///using UnityEditor.Build;
        ///using UnityEngine;
        ///using UnityEditor.Build.Reporting;
        ///
        ///class MyCustomBuildProcessor : IPostprocessBuildWithContext
        ///{
        ///    public int callbackOrder { get { return 0; } }
        ///    public void OnPostprocessBuild(BuildCallbackContext ctx)
        ///    {
        ///        if (ctx.IsContentOnlyBuild)
        ///            Debug.Log("AssetBundle build: MyCustomBuildProcessor.OnPostprocessBuild for target " + ctx.Report.summary.platform + " at path " + ctx.Report.summary.outputPath);
        ///        else if (ctx.IsPlayerBuild)
        ///            Debug.Log("Player build: MyCustomBuildProcessor.OnPostprocessBuild for target " + ctx.Report.summary.platform + " at path " + ctx.Report.summary.outputPath);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        ///<seealso cref="BuildPipeline.BuildAssetBundles" />
        ///<seealso cref="Build.IPreprocessBuildWithContext" />
        void OnPostprocessBuild(BuildCallbackContext ctx);
    }

    ///<summary>Implement this interface to receive a callback after the player scripts have been compiled.</summary>
    ///<remarks>For more information about build callbacks, refer to [Use build callbacks](xref:build-callbacks)</remarks>
    public interface IPostBuildPlayerScriptDLLs : IOrderedCallback
    {
        ///<summary>Implement this interface to receive a callback just after the player scripts have been compiled.</summary>
        ///<remarks>You can implement this if you need to read or patch managed Assemblies for players being built. You can get assembly locations from the <see cref="BuildReport.files">files</see> property of the <c>report</c> parameter. Note that implementing this callback will cause builds to run slower, as assemblies need to be copied to an intermediate location, and is not recommended for best performance.</remarks>
        ///<param name="report">A report containing information about the build, such as its target platform and output path.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEditor;
        ///using UnityEditor.Build;
        ///using UnityEditor.Build.Reporting;
        ///using UnityEngine;
        ///
        ///class MyCustomBuildProcessor : IPostBuildPlayerScriptDLLs
        ///{
        ///    public int callbackOrder { get { return 0; } }
        ///    public void OnPostBuildPlayerScriptDLLs(BuildReport report)
        ///    {
        ///        Debug.Log("MyCustomBuildProcessor.OnPostBuildPlayerScriptDLLs for target " + report.summary.platform + " at path " + report.summary.outputPath);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Build.BuildPlayerProcessor" />
        ///<seealso cref="IFilterBuildAssemblies" />
        ///<seealso cref="IUnityLinkerProcessor" />
        ///<seealso cref="IPreprocessBuildWithContext" />
        void OnPostBuildPlayerScriptDLLs(BuildReport report);
    }

    ///<summary>Implement this interface to receive a callback for each Scene during the build.</summary>
    ///<remarks>This interface is obsolete. Use <see cref="Build.IProcessSceneWithReport" /> instead.</remarks>
    [Obsolete("Use IProcessSceneWithReport instead")]
    public interface IProcessScene : IOrderedCallback
    {
        ///<summary>Implement this function to receive a callback for each Scene during the build.</summary>
        /// <param name="scene">The current Scene being processed.</param>
        void OnProcessScene(UnityEngine.SceneManagement.Scene scene);
    }

    ///<summary>Implement this interface to receive a callback for each Scene during the build.</summary>
    ///<remarks>If the scene or related content in the project is unchanged from the previous Player build, Unity doesn't build the scene and instead uses cached Player build data will be used. In this case the callback isn't called. For more information about build callbacks, refer to [Use build callbacks](xref:build-callbacks)</remarks>
    public interface IProcessSceneWithReport : IOrderedCallback
    {
        /// <summary>
        /// Implement this function to receive a callback for each Scene during the build.
        /// </summary>
        /// <remarks>This callback is invoked during Player and AssetBundle builds, and also as a scene is reloaded while entering Editor playmode. <see cref="BuildPipeline.isBuildingPlayer" /> can be used to determine in which context the callback is being called</remarks>
        /// <param name="scene">The current Scene being processed.</param>
        /// <param name="report">A report containing information about the current build. When this callback is invoked for Scene loading during Editor playmode, this parameter will be null.</param>
        ///<example>
        ///  <code><![CDATA[
        /// using UnityEditor;
        /// using UnityEditor.Build;
        /// using UnityEditor.Build.Reporting;
        /// using UnityEngine;
        /// [BuildCallbackVersion(1)]
        /// class MyCustomBuildProcessor : IProcessSceneWithReport
        /// {
        ///     public int callbackOrder { get { return 0; } }
        ///     public void OnProcessScene(UnityEngine.SceneManagement.Scene scene, BuildReport report) { Debug.Log("MyCustomBuildProcessor.OnProcessScene " + scene.name); }
        /// }
        ///]]></code>
        ///</example>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        ///<seealso cref="BuildPipeline.BuildAssetBundles" />
        ///<seealso cref="Build.BuildCallbackVersionAttribute" />
        void OnProcessScene(UnityEngine.SceneManagement.Scene scene, BuildReport report);
    }

    ///<summary>Implement this interface to receive a callback after the active build platform has changed.</summary>
    ///<seealso cref="IActiveBuildTargetChanged.OnActiveBuildTargetChanged">OnActiveBuildTargetChanged</seealso>
    public interface IActiveBuildTargetChanged : IOrderedCallback
    {
        ///<summary>This function is called automatically when the active build platform has changed.</summary>
        ///<param name="previousTarget">The build target before the change.</param>
        ///<param name="newTarget">The new active build target.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEditor;
        ///using UnityEditor.Build;
        ///
        ///public class ActiveBuildTargetListener : IActiveBuildTargetChanged
        ///{
        ///    public int callbackOrder { get { return 0; } }
        ///    public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        ///    {
        ///        Debug.Log("Switched build target to " + newTarget);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="EditorUserBuildSettings.SwitchActiveBuildTarget">SwitchActiveBuildTarget</seealso>
        void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget);
    }

    ///<summary>Implement this interface to receive a callback before a shader is compiled.</summary>
    public interface IPreprocessShaders : IOrderedCallback
    {
        ///<summary>Implement this interface to receive a callback before a shader snippet is compiled.</summary>
        ///<remarks>
        ///  <para>When you build your application, Unity compiles each shader source file into multiple [shader variants](xref:shader-variants). Unity creates variants for some or all of the possible combinations of keywords you define in the shader source file.
        ///
        ///                    You can use <c>OnProcessShader</c> to iterate through each shader and variant Unity is about to compile, and exclude ('strip') variants that use keywords or keyword combinations you don't need. If you strip variants, you can greatly reduce build size, build times, and how much runtime memory Unity uses.
        ///
        ///                    For example you can use <c>OnProcessShader</c> to remove variants that use the following:
        ///
        ///- Keywords that aren't needed for the current target platform.
        ///- Combinations of keywords that are never used.
        ///- Keywords you only use in your debug builds.
        ///
        ///                    Unity invokes the <c>OnProcessShader</c> callback in both Player and AssetBundle builds.
        ///
        ///                    You can [check what shader variants you have in your project](xref:shader-how-many-variants) to help you identify keywords and variants to strip.
        ///
        ///                    For example if you [declare a keyword](xref:SL-MultipleProgramVariants) called <c>DEBUG</c> in your shader code using <c>#pragma multi_compile _ DEBUG</c>, the following [Editor script](xref:SpecialFolders) finds and strips shader variants that use the keyword.
        ///
        ///                    The script does the following when you build your application:
        ///
        ///1. Creates a class that implements the <c>IPreprocessShaders</c> interface.
        ///2. Creates an instance of <c>ShaderKeyword</c> with the name of the keyword.
        ///3. Implements the <c>OnProcessShader</c> callback function and iterates over the <c>data</c> list, which contains every variant in the shader.
        ///4. Uses <c>data.shaderKeywordSet.IsEnabled()</c> to check if each variant uses the keyword.
        ///5. Uses <c>data.removeAt()</c> to strip a shader variant if it contains the keyword and you've disabled **Development build** in [Build Settings](xref:Build Settings).</para>
        ///  <para>You can also find local keywords. You must create the <c>ShaderKeyword</c> instance inside the implementation of <c>OnProcessShader</c>, so you can use the callback's <c>shader</c> variable in the <c>ShaderKeyword</c> constructor.
        ///
        ///                    For example if you declare a local keyword called <c>RED</c> in your shader code using <c>#pragma multi_compile_local _ RED</c>, the following script finds and strips shader variants that use the keyword.</para>
        ///  <para>If you strip a variant that a Material needs at runtime, Unity chooses an available shader variant that matches as closely as possible.
        ///
        ///                    Find out about other ways you can [strip shader variants](xref:shader-variant-stripping).
        ///
        ///</para>
        ///</remarks>
        ///<param name="shader">The shader that Unity is about to compile.</param>
        ///<param name="snippet">Details about the specific shader code being compiled.</param>
        ///<param name="data">List of variants that Unity is about to compile for the shader.</param>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Modules/ShaderCompilationEditor/Tests/UTFTests/ShaderCompilation/Playmode/Assets/DocumentationExamples/Editor/Build/IPreprocessShaders/ShaderDebugBuildPreprocessor.cs}]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using System.Collections.Generic;
        ///using UnityEditor.Build;
        ///using UnityEditor.Rendering;
        ///using UnityEngine;
        ///using UnityEngine.Rendering;
        ///
        ///class MyCustomBuildProcessor : IPreprocessShaders
        ///{
        ///
        ///    public int callbackOrder { get { return 0; } }
        ///
        ///    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        ///    {
        ///
        ///        // Create an instance of ShaderKeyword using the constructor that takes a Shader argument
        ///        ShaderKeyword localKeywordToStrip = new ShaderKeyword(shader, "RED");
        ///
        ///        for (int i = 0; i < data.Count; ++i)
        ///        {
        ///            if (data[i].shaderKeywordSet.IsEnabled(localKeywordToStrip))
        ///            {
        ///                data.RemoveAt(i);
        ///                --i;
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        ///<seealso cref="BuildPipeline.BuildAssetBundles" />
        ///<seealso cref="Build.IPreprocessComputeShaders" />
        void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data);
    }

    ///<summary>Implement this interface to receive a callback before a compute shader is compiled.</summary>
    ///<remarks>For more information about build callbacks, refer to [Use build callbacks](xref:build-callbacks)</remarks>
    public interface IPreprocessComputeShaders : IOrderedCallback
    {
        ///<summary>Implement this interface to receive a callback before Unity compiles a compute shader kernel into a build.</summary>
        ///<remarks>Use this callback to examine the compute shader variants that Unity is about to compile into your build, and exclude any variant that you do not want. Excluding unwanted shader variants can reduce build size and build times.
        ///
        ///Variants are represented by <see cref="UnityEditor.Rendering.ShaderCompilerData" /> structs. For each variant, you can check whether given global or local keywords are enabled using <see cref="UnityEditor.Rendering.ShaderKeywordSet.IsEnabled" />.
        ///
        ///To check whether a variant has a global keyword enabled, create a <see cref="ShaderKeyword" /> instance with the name of the global keyword. To check whether a variant has a local keyword enabled, create a <see cref="ShaderKeyword" /> instance with the name of the local keyword and an additional parameter that specifies the compute shader that uses the local keyword.
        ///
        ///To exclude a shader variant from the build, directly remove the elements from <c>data</c> . Note that removing elements individually in a for loop can be slow; if you are removing a lot of elements, consider moving the unwanted elements to the end of the List and then removing them all in a single operation.
        ///
        ///Note that this callback only provides details of compute shaders. To see regular shaders that Unity is about to compile into the build, see <see cref="Build.IPreprocessShaders" /> .
        ///
        ///This callback is invoked for both Player and AssetBundle builds.</remarks>
        ///<param name="shader">The compute shader that Unity is about to compile.</param>
        ///<param name="kernelName">The name of the kernel that Unity is about to compile.</param>
        ///<param name="data">The list of shader variants that Unity is about to compile.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using System.Collections.Generic;
        ///using UnityEditor.Build;
        ///using UnityEditor.Rendering;
        ///using UnityEngine;
        ///using UnityEngine.Rendering;
        ///
        ///class MyCustomBuildProcessor : IPreprocessComputeShaders
        ///{
        ///    ShaderKeyword m_GlobalKeywordBlue;
        ///
        ///    public MyCustomBuildProcessor()
        ///    {
        ///        // Global keywords are shader agnostic so they can be initialized early
        ///        m_GlobalKeywordBlue = new ShaderKeyword("_BLUE");
        ///    }
        ///
        ///    public int callbackOrder { get { return 0; } }
        ///
        ///    public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
        ///    {
        ///        // Local keywords are initialized here as their constructor needs to specify the shader
        ///        ShaderKeyword localKeywordRed = new ShaderKeyword(shader, "_RED");
        ///        for (int i = data.Count - 1; i >= 0; --i)
        ///        {
        ///            // Variants with global keyword _BLUE disabled are included in the build
        ///            if (!data[i].shaderKeywordSet.IsEnabled(m_GlobalKeywordBlue))
        ///                continue;
        ///
        ///            // Variants with local keyword _RED disabled are included in the build
        ///            if (!data[i].shaderKeywordSet.IsEnabled(localKeywordRed))
        ///                continue;
        ///
        ///            // Any variants that do not meet the criteria above are stripped from the build.
        ///            // In this example, Unity strips variants that have both _BLUE and _RED keywords enabled.
        ///            data.RemoveAt(i);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso href="xref:shader-variants-and-keywords">Shader variants and keywords</seealso>
        ///<seealso href="xref:SL-MultipleProgramVariants">Declaring and using shader keywords in HLSL</seealso>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        ///<seealso cref="BuildPipeline.BuildAssetBundles" />
        void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data);
    }

    [VisibleToOtherModules("UnityEditor.BurstModule")]
    // This API lets you generate native plugins to be integrated into the player build,
    // during the incremental player build. The incremental player build platform implementations will know
    // how to consume these plugins and link them into the build.
    internal interface IGenerateNativePluginsForAssemblies : IOrderedCallback
    {
        // Arguments to the PrepareOnMainThread method
        public struct PrepareArgs
        {
            // The currently active build report.
            public BuildReport report { get; set; }
        }

        // Return value of the PrepareOnMainThread method
        public struct PrepareResult
        {
            // Any pathname in here will be considered an input file to the generated plugins;
            // Any changes to any of these files will trigger a rebuild of the plugins.
            public string[] additionalInputFiles { get; set; }

            // Message to be shown in the progress bar when the GenerateNativePluginsForAssemblies method is run.
            public string displayName { get; set; }
        }

        // Prepare method which is called on the main thread before the incremental player build starts.
        // Use this to do any work which must happen on the main thread, and to set up dependencies which must trigger a
        // rebuild of the generated plugins.
        public PrepareResult PrepareOnMainThread(PrepareArgs args);

        // Arguments to the GenerateNativePluginsForAssemblies method

        public struct GenerateArgs
        {
            // Path names to the managed assembly files on disk as built for the currently active player target
            public string[] assemblyFiles { get; set; }
        }

        // Return value of the GenerateNativePluginsForAssemblies method
        public struct GenerateResult
        {
            // Any pathname returned in this array will be treated as a plugin to be linked into the player
            public string[] generatedPlugins { get; set; }
            public string[] generatedDebugFiles { get; set; }
        }

        // Method to generate native plugins during the player build. This will be called on a thread by the incremental
        // player build pipeline to allow generating native plugins from editor code which will be linked into the player.
        // If the plugins have already be generated in a previous build, this will only be called if any of the input
        // files have changed. Input files are all assemblies (as specified in args.assemblyFiles) and all input files
        // returned by `PrepareOnMainThread` in `additionalInputFiles`.
        public GenerateResult GenerateNativePluginsForAssemblies(GenerateArgs args);
    }

    ///<summary>Implement this interface to receive callbacks related to the running of UnityLinker.</summary>
    ///<remarks>For more information about build callbacks, refer to [Use build callbacks](xref:build-callbacks)</remarks>
    ///<seealso cref="Build.BuildPlayerProcessor" />
    ///<seealso cref="IFilterBuildAssemblies" />
    ///<seealso cref="IPostBuildPlayerScriptDLLs" />
    ///<seealso cref="IPreprocessBuildWithContext" />
    public interface IUnityLinkerProcessor : IOrderedCallback
    {
        /// <summary>
        /// Generates additional link.xml files for preserving additional types and their members.
        /// </summary>
        /// <param name="report">The current built report.</param>
        /// <param name="data">Information about the current run of UnityLinker.</param>
        /// <returns>The file path to the generated link.xml file. If the path is relative, GenerateAdditionalLinkXmlFile combines it with the working directory to make an absolute path.</returns>
        string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinker.UnityLinkerBuildPipelineData data);
    }

    ///<summary>Implement this interface to receive callbacks related to the running of IL2CPP.</summary>
    [Obsolete("The IIl2CppProcessor interface has been removed from Unity. Use IPostBuildPlayerScriptDLLs if you need to access player assemblies before il2cpp runs.", true)]
    public interface IIl2CppProcessor : IOrderedCallback
    {
    }

    internal static class BuildPipelineInterfaces
    {
        internal class Processors
        {
#pragma warning disable 618
            public List<IPreprocessBuild> buildPreprocessors;
            public List<IPostprocessBuild> buildPostprocessors;
            public List<IProcessScene> sceneProcessors;
#pragma warning restore 618

            public List<BuildPlayerProcessor> buildPlayerProcessors;

            public List<IPreprocessBuildWithReport> buildPreprocessorsWithReport;
            public List<IPostprocessBuildWithReport> buildPostprocessorsWithReport;
            public List<IPreprocessBuildWithContext> buildPreprocessorsWithContext;
            public List<IPostprocessBuildWithContext> buildPostprocessorsWithContext;
            public List<IPostprocessLaunch> launchPostprocessors;
            public List<IProcessSceneWithReport> sceneProcessorsWithReport;

            public List<IFilterBuildAssemblies> filterBuildAssembliesProcessor;
            public List<IActiveBuildTargetChanged> buildTargetProcessors;
            public List<IPreprocessShaders> shaderProcessors;
            public List<IPreprocessComputeShaders> computeShaderProcessors;
            public List<IPostBuildPlayerScriptDLLs> buildPlayerScriptDLLProcessors;

            public List<IUnityLinkerProcessor> unityLinkerProcessors;
            public List<IGenerateNativePluginsForAssemblies> generateNativePluginsForAssembliesProcessors;
        }

        private static Processors m_Processors;
        internal static Processors processors
        {
            get
            {
                m_Processors = m_Processors ?? new Processors();
                return m_Processors;
            }
            set { m_Processors = value; }
        }

        [Flags]
        internal enum BuildCallbacks
        {
            None = 0,
            BuildProcessors = 1,
            SceneProcessors = 2,
            BuildTargetProcessors = 4,
            FilterAssembliesProcessors = 8,
            ShaderProcessors = 16,
            BuildPlayerScriptDLLProcessors = 32,
            UnityLinkerProcessors = 64,
            GenerateNativePluginsForAssembliesProcessors = 128,
            ComputeShaderProcessors = 256
        }

        //common comparer for all callback types
        internal static int CompareICallbackOrder(IOrderedCallback a, IOrderedCallback b)
        {
            return a.callbackOrder.CompareTo(b.callbackOrder);
        }

        static void AddToList<T>(object o, ref List<T> list) where T : class
        {
            T inst = o as T;
            if (inst == null)
                return;
            if (list == null)
                list = new List<T>();
            list.Add(inst);
        }

        static void AddToListIfTypeImplementsInterface<T>(Type t, ref object o, ref List<T> list) where T : class
        {
            if (!ValidateType<T>(t))
                return;

            if (o == null)
                o = Activator.CreateInstance(t);
            AddToList(o, ref list);
        }

        private class AttributeCallbackWrapper : IPostprocessBuildWithReport, IProcessSceneWithReport, IActiveBuildTargetChanged
        {
            internal int m_callbackOrder;
            internal MethodInfo m_method;

            public int callbackOrder { get { return m_callbackOrder; } }

            public AttributeCallbackWrapper(MethodInfo m)
            {
                m_callbackOrder = ((CallbackOrderAttribute)Attribute.GetCustomAttribute(m, typeof(CallbackOrderAttribute))).callbackOrder;
                m_method = m;
            }

            public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
            {
                m_method.Invoke(null, new object[] { previousTarget, newTarget });
            }

            public void OnPostprocessBuild(BuildReport report)
            {
                m_method.Invoke(null, new object[] { report.summary.platform, report.summary.outputPath });
            }

            public void OnProcessScene(UnityEngine.SceneManagement.Scene scene, BuildReport report)
            {
                m_method.Invoke(null, null);
            }
        }

        //this variable is reinitialized on domain reload so any calls to Init after a domain reload will set things up correctly
        static BuildCallbacks previousFlags = BuildCallbacks.None;
        static BuildTarget previousTargetPlatform = BuildTarget.NoTarget;

        [RequiredByNativeCode]
        internal static void InitializeBuildCallbacks(BuildCallbacks findFlags)
        {
            using var _ = new ScopeTraceProfileBlock($"InitializeBuildCallbacks:{findFlags}");

            if (findFlags == previousFlags
                //Fix for UUM-109242 Some callback implementations may cache data tied to the active target in their constructor.
                //This ensures that we rebuild the callback cache if we did not have a domain reload but the targets are different.
                && EditorUserBuildSettings.activeBuildTarget == previousTargetPlatform)
                return;

            CleanupBuildCallbacks();
            previousFlags = findFlags;
            previousTargetPlatform = EditorUserBuildSettings.activeBuildTarget;

            bool findBuildProcessors = (findFlags & BuildCallbacks.BuildProcessors) == BuildCallbacks.BuildProcessors;
            bool findSceneProcessors = (findFlags & BuildCallbacks.SceneProcessors) == BuildCallbacks.SceneProcessors;
            bool findTargetProcessors = (findFlags & BuildCallbacks.BuildTargetProcessors) == BuildCallbacks.BuildTargetProcessors;
            bool findFilterProcessors = (findFlags & BuildCallbacks.FilterAssembliesProcessors) == BuildCallbacks.FilterAssembliesProcessors;
            bool findShaderProcessors = (findFlags & BuildCallbacks.ShaderProcessors) == BuildCallbacks.ShaderProcessors;
            bool findComputeShaderProcessors = (findFlags & BuildCallbacks.ComputeShaderProcessors) == BuildCallbacks.ComputeShaderProcessors;
            bool findBuildPlayerScriptDLLsProcessors = (findFlags & BuildCallbacks.BuildPlayerScriptDLLProcessors) == BuildCallbacks.BuildPlayerScriptDLLProcessors;
            bool findUnityLinkerProcessors = (findFlags & BuildCallbacks.UnityLinkerProcessors) == BuildCallbacks.UnityLinkerProcessors;
            bool findGenerateNativePluginsForAssembliesProcessors = (findFlags & BuildCallbacks.GenerateNativePluginsForAssembliesProcessors) == BuildCallbacks.GenerateNativePluginsForAssembliesProcessors;

            var postProcessBuildAttributeParams = new Type[] { typeof(BuildTarget), typeof(string) };
            foreach (var t in TypeCache.GetTypesDerivedFrom<IOrderedCallback>())
            {
                if (t.IsAbstract || t.IsInterface)
                    continue;

                // Defer creating the instance until we actually add it to one of the lists
                object instance = null;

                if (findBuildProcessors)
                {
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.buildPlayerProcessors);
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.buildPreprocessors);
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.buildPreprocessorsWithReport);
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.buildPreprocessorsWithContext);
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.buildPostprocessors);
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.buildPostprocessorsWithReport);
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.buildPostprocessorsWithContext);
                }

                if (findSceneProcessors)
                {
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.sceneProcessors);
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.sceneProcessorsWithReport);
                }

                if (findTargetProcessors)
                {
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.buildTargetProcessors);
                }

                if (findFilterProcessors)
                {
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.filterBuildAssembliesProcessor);
                }

                if (findUnityLinkerProcessors)
                {
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.unityLinkerProcessors);
                }

                if (findGenerateNativePluginsForAssembliesProcessors)
                {
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.generateNativePluginsForAssembliesProcessors);
                }

                if (findShaderProcessors)
                {
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.shaderProcessors);
                }

                if (findComputeShaderProcessors)
                {
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.computeShaderProcessors);
                }

                if (findBuildPlayerScriptDLLsProcessors)
                {
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.buildPlayerScriptDLLProcessors);
                }
            }

            if (findBuildProcessors)
            {
                foreach (var m in EditorAssemblies.GetAllMethodsWithAttribute<Callbacks.PostProcessBuildAttribute>())
                    if (ValidateMethod<Callbacks.PostProcessBuildAttribute>(m, postProcessBuildAttributeParams))
                        AddToList(new AttributeCallbackWrapper(m), ref processors.buildPostprocessorsWithReport);
            }

            if (findSceneProcessors)
            {
                foreach (var m in EditorAssemblies.GetAllMethodsWithAttribute<Callbacks.PostProcessSceneAttribute>())
                    if (ValidateMethod<Callbacks.PostProcessSceneAttribute>(m, Type.EmptyTypes))
                        AddToList(new AttributeCallbackWrapper(m), ref processors.sceneProcessorsWithReport);
            }

            processors.buildPlayerProcessors?.Sort(CompareICallbackOrder);
            if (processors.buildPreprocessors != null)
                processors.buildPreprocessors.Sort(CompareICallbackOrder);
            if (processors.buildPreprocessorsWithReport != null)
                processors.buildPreprocessorsWithReport.Sort(CompareICallbackOrder);
            if (processors.buildPreprocessorsWithContext != null)
                processors.buildPreprocessorsWithContext.Sort(CompareICallbackOrder);
            if (processors.buildPostprocessors != null)
                processors.buildPostprocessors.Sort(CompareICallbackOrder);
            if (processors.buildPostprocessorsWithReport != null)
                processors.buildPostprocessorsWithReport.Sort(CompareICallbackOrder);
            if (processors.buildPostprocessorsWithContext != null)
                processors.buildPostprocessorsWithContext.Sort(CompareICallbackOrder);
            if (processors.buildTargetProcessors != null)
                processors.buildTargetProcessors.Sort(CompareICallbackOrder);
            if (processors.sceneProcessors != null)
                processors.sceneProcessors.Sort(CompareICallbackOrder);
            if (processors.sceneProcessorsWithReport != null)
                processors.sceneProcessorsWithReport.Sort(CompareICallbackOrder);
            if (processors.filterBuildAssembliesProcessor != null)
                processors.filterBuildAssembliesProcessor.Sort(CompareICallbackOrder);
            if (processors.unityLinkerProcessors != null)
                processors.unityLinkerProcessors.Sort(CompareICallbackOrder);
            if (processors.generateNativePluginsForAssembliesProcessors != null)
                processors.generateNativePluginsForAssembliesProcessors.Sort(CompareICallbackOrder);
            if (processors.shaderProcessors != null)
                processors.shaderProcessors.Sort(CompareICallbackOrder);
            if (processors.computeShaderProcessors != null)
                processors.computeShaderProcessors.Sort(CompareICallbackOrder);
            if (processors.buildPlayerScriptDLLProcessors != null)
                processors.buildPlayerScriptDLLProcessors.Sort(CompareICallbackOrder);
        }

        internal static bool ValidateType<T>(Type t)
        {
            return (typeof(T).IsAssignableFrom(t) && t != typeof(AttributeCallbackWrapper));
        }

        static bool ValidateMethod<T>(MethodInfo method, Type[] expectedArguments)
        {
            Type attribute = typeof(T);
            if (method.IsDefined(attribute, false))
            {
                // Remove the `Attribute` from the name.
                if (!method.IsStatic)
                {
                    string atributeName = attribute.Name.Replace("Attribute", "");
                    Debug.LogErrorFormat("Method {0} with {1} attribute must be static.", method.Name, atributeName);
                    return false;
                }

                if (method.IsGenericMethod || method.IsGenericMethodDefinition)
                {
                    string atributeName = attribute.Name.Replace("Attribute", "");
                    Debug.LogErrorFormat("Method {0} with {1} attribute cannot be generic.", method.Name, atributeName);
                    return false;
                }

                var parameters = method.GetParameters();
                bool signatureCorrect = parameters.Length == expectedArguments.Length;
                if (signatureCorrect)
                {
                    // Check types match
                    for (int i = 0; i < parameters.Length; ++i)
                    {
                        if (parameters[i].ParameterType != expectedArguments[i])
                        {
                            signatureCorrect = false;
                            break;
                        }
                    }
                }

                if (!signatureCorrect)
                {
                    string atributeName = attribute.Name.Replace("Attribute", "");
                    string expectedArgumentsString = "static void " + method.Name + "(";

                    for (int i = 0; i < expectedArguments.Length; ++i)
                    {
                        expectedArgumentsString += expectedArguments[i].Name;
                        if (i != expectedArguments.Length - 1)
                            expectedArgumentsString += ", ";
                    }
                    expectedArgumentsString += ")";

                    Debug.LogErrorFormat("Method {0} with {1} attribute does not have the correct signature, expected: {2}.", method.Name, atributeName, expectedArgumentsString);
                    return false;
                }
                return true;
            }
            return false;
        }

        private static bool InvokeCallbackInterfacesPair<T1, T2>(List<T1> oneInterfaces, Action<T1> invocationOne, List<T2> twoInterfaces, Action<T2> invocationTwo, bool exitOnFailure) where T1 : IOrderedCallback where T2 : IOrderedCallback
        {
            if (oneInterfaces == null && twoInterfaces == null)
                return true;

            // We want to walk both interface lists and invoke the callbacks, but if we just did the whole of list 1 followed by the whole of list 2, the ordering would be wrong.
            // So, we have to walk both lists simultaneously, calling whichever callback has the lower ordering value
            IEnumerator<T1> e1 = (oneInterfaces != null) ? (IEnumerator<T1>)oneInterfaces.GetEnumerator() : null;
            IEnumerator<T2> e2 = (twoInterfaces != null) ? (IEnumerator<T2>)twoInterfaces.GetEnumerator() : null;
            if (e1 != null && !e1.MoveNext())
                e1 = null;
            if (e2 != null && !e2.MoveNext())
                e2 = null;

            while (e1 != null || e2 != null)
            {
                try
                {
                    if (e1 != null && (e2 == null || e1.Current.callbackOrder < e2.Current.callbackOrder))
                    {
                        var callback = e1.Current;
                        if (!e1.MoveNext())
                            e1 = null;
                        invocationOne(callback);
                    }
                    else if (e2 != null)
                    {
                        var callback = e2.Current;
                        if (!e2.MoveNext())
                            e2 = null;
                        invocationTwo(callback);
                    }
                }
                catch (TargetInvocationException e)
                {
                    // Note: Attribute based callbacks are called via reflection.
                    // Exceptions in those calls are wrapped in TargetInvocationException
                    Debug.LogException(e.InnerException);
                    if (exitOnFailure)
                        return false;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    if (exitOnFailure)
                        return false;
                }
            }

            return true;
        }

        internal static void PreparePlayerBuild(BuildPlayerContext context)
        {
            foreach (var p in processors.buildPlayerProcessors ?? new List<BuildPlayerProcessor>())
                p.PrepareForBuild(context);
        }

        [RequiredByNativeCode]
        internal static void OnBuildPreProcess(BuildReport report)
        {
#pragma warning disable 618
            InvokeCallbackInterfacesPair(
                processors.buildPreprocessors, bpp =>
                {
                    using var _ = new ScopeTraceProfileBlock($"{bpp.GetType().Name}.OnPreprocessBuild");
                    bpp.OnPreprocessBuild(report.summary.platform, report.summary.outputPath);
                },
                processors.buildPreprocessorsWithReport, bpp =>
                {
                    using var _ = new ScopeTraceProfileBlock($"{bpp.GetType().Name}.OnPreprocessBuild:WithReport");
                    bpp.OnPreprocessBuild(report);
                },
                (report.summary.options & BuildOptions.StrictMode) != 0 || (report.summary.assetBundleOptions & BuildAssetBundleOptions.StrictMode) != 0);
#pragma warning restore 618

            // NOTE: This is a workaround for PLAT-11795.
            // Sometimes, when a player settings override is modified in one of the callbacks, its internal
            // serialized version is not updated prior to the build. As a result it will be restored to the
            // serialized values. To avoid that situation we force the update here.
            var profile = BuildProfile.GetActiveBuildProfile();
            if (profile != null)
                profile.SerializePlayerSettings();
        }

        [RequiredByNativeCode]
        internal static void OnBuildPreProcessWithContext(BuildCallbackContext context)
        {
            if (processors.buildPreprocessorsWithContext != null)
            {
                foreach (var processor in processors.buildPreprocessorsWithContext)
                {
                    using var _ = new ScopeTraceProfileBlock($"{processor.GetType().Name}.OnPreprocessBuild:WithContext");
                    try
                    {
                        processor.OnPreprocessBuild(context);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        if (context.Report != null && ((context.Report.summary.options & BuildOptions.StrictMode) != 0 || (context.Report.summary.assetBundleOptions & BuildAssetBundleOptions.StrictMode) != 0))
                            return;
                    }
                }
            }
        }

        [RequiredByNativeCode]
        internal static void OnSceneProcess(UnityEngine.SceneManagement.Scene scene, BuildReport report)
        {
#pragma warning disable 618
            InvokeCallbackInterfacesPair(
                processors.sceneProcessors, spp =>
                {
                    using var _ = new ScopeTraceProfileBlock($"{spp.GetType().Name}.OnProcessScene:{scene.name}");
                    using (new EditorPerformanceMarker($"{spp.GetType().Name}.{nameof(spp.OnProcessScene)}", spp.GetType()).Auto())
                        spp.OnProcessScene(scene);
                },
                processors.sceneProcessorsWithReport, spp =>
                {
                    using var _ = new ScopeTraceProfileBlock($"{spp.GetType().Name}.OnProcessScene:WithReport:{scene.name}");
                    using (new EditorPerformanceMarker($"{spp.GetType().Name}.{nameof(spp.OnProcessScene)}", spp.GetType()).Auto())
                        spp.OnProcessScene(scene, report);
                },
                report && ((report.summary.options & BuildOptions.StrictMode) != 0 || (report.summary.assetBundleOptions & BuildAssetBundleOptions.StrictMode) != 0));
#pragma warning restore 618
        }

        [RequiredByNativeCode]
        internal static void OnSceneProcess_HashVersion(out Hash128 hashVersion)
        {
            hashVersion = new Hash128();

            Type versionAttrribute = typeof(BuildCallbackVersionAttribute);

            if (processors.sceneProcessorsWithReport != null)
            {
                foreach (IProcessSceneWithReport processor in processors.sceneProcessorsWithReport)
                {
                    Type processorType = processor.GetType();
                    hashVersion.Append(processorType.AssemblyQualifiedName);

                    BuildCallbackVersionAttribute attribute = Attribute.GetCustomAttribute(processorType, versionAttrribute, false) as BuildCallbackVersionAttribute;
                    hashVersion.Append(attribute != null ? attribute.Version : 1);
                }
            }
        }

        [RequiredByNativeCode]
        internal static void OnBuildPostProcess(BuildReport report)
        {
#pragma warning disable 618
            InvokeCallbackInterfacesPair(
                processors.buildPostprocessors, bpp =>
                {
                    using var _ = new ScopeTraceProfileBlock($"{bpp.GetType().Name}.OnPostprocessBuild");
                    bpp.OnPostprocessBuild(report.summary.platform, report.summary.outputPath);
                },
                processors.buildPostprocessorsWithReport, bpp =>
                {
                    using var _ = new ScopeTraceProfileBlock($"{bpp.GetType().Name}.OnPostprocessBuild:WithReport");
                    bpp.OnPostprocessBuild(report);
                },
                (report.summary.options & BuildOptions.StrictMode) != 0 || (report.summary.assetBundleOptions & BuildAssetBundleOptions.StrictMode) != 0);
#pragma warning restore 618
        }

        [RequiredByNativeCode]
        internal static void OnBuildPostProcessWithContext(BuildCallbackContext context)
        {
            if (processors.buildPostprocessorsWithContext != null)
            {
                foreach (var processor in processors.buildPostprocessorsWithContext)
                {
                    using var _ = new ScopeTraceProfileBlock($"{processor.GetType().Name}.OnPostprocessBuild:WithContext");
                    try
                    {
                        processor.OnPostprocessBuild(context);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        if (context.Report != null && ((context.Report.summary.options & BuildOptions.StrictMode) != 0 || (context.Report.summary.assetBundleOptions & BuildAssetBundleOptions.StrictMode) != 0))
                            return;
                    }
                }
            }
        }


        // Some platforms like Desktop, instead of launching the app via C#, perform their launch in C++
        // See BuildPlayer.cpp LaunchPlayerIfSupported, which calls native LaunchApplication
        // Internal_OnPostprocessLaunch is used by this code path
        [RequiredByNativeCode]
        internal static void Internal_OnPostprocessLaunch(BuildTarget buildTarget, bool success)
        {
            OnPostprocessLaunch(new DefaultLaunchReport(NamedBuildTarget.FromActiveSettings(buildTarget), success ? LaunchResult.Succeeded : LaunchResult.Failed));
        }

        internal static void OnPostprocessLaunch(ILaunchReport launchReport)
        {
            // Domain reload happens after player build, so anything collected in InitializeBuildCallbacks gets invalidated
            // Thus collect callbacks here as necessary
            if (processors.launchPostprocessors == null)
            {
                foreach (var t in TypeCache.GetTypesDerivedFrom<IPostprocessLaunch>())
                {
                    if (t.IsAbstract || t.IsInterface)
                        continue;

                    object instance = null;
                    AddToListIfTypeImplementsInterface(t, ref instance, ref processors.launchPostprocessors);
                }

                processors?.launchPostprocessors?.Sort(CompareICallbackOrder);
            }

            if (processors.launchPostprocessors == null)
                return;

            foreach (var run in processors.launchPostprocessors)
            {
                try
                {
                    run.OnPostprocessLaunch(launchReport);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        [RequiredByNativeCode]
        internal static void OnActiveBuildTargetChanged(BuildTarget previousPlatform, BuildTarget newPlatform)
        {
            if (processors.buildTargetProcessors != null)
            {
                foreach (IActiveBuildTargetChanged abtc in processors.buildTargetProcessors)
                {
                    try
                    {
                        abtc.OnActiveBuildTargetChanged(previousPlatform, newPlatform);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        [RequiredByNativeCode]
        internal static ShaderCompilerData[] OnPreprocessShaders(Shader shader, ShaderType shaderType, PassType passType, string passName, PassIdentifier passIdentifier, ShaderCompilerData[] data)
        {
            var snippet = new ShaderSnippetData(shaderType, passType, passName, passIdentifier);

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var dataList = data.ToList();
#pragma warning restore UA2001
            if (processors.shaderProcessors != null)
            {
                foreach (IPreprocessShaders abtc in processors.shaderProcessors)
                {
                    abtc.OnProcessShader(shader, snippet, dataList);
                }
            }
            return dataList.ToArray();
        }

        [RequiredByNativeCode]
        internal static ShaderCompilerData[] OnPreprocessComputeShaders(ComputeShader shader, string kernelName, ShaderCompilerData[] data)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var dataList = data.ToList();
#pragma warning restore UA2001
            if (processors.computeShaderProcessors != null)
            {
                foreach (IPreprocessComputeShaders abtc in processors.computeShaderProcessors)
                {
                    try
                    {
                        abtc.OnProcessComputeShader(shader, kernelName, dataList);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            return dataList.ToArray();
        }

        [RequiredByNativeCode]
        internal static bool HasOnPostBuildPlayerScriptDLLs()
        {
            return (processors.buildPlayerScriptDLLProcessors != null && processors.buildPlayerScriptDLLProcessors.Count > 0);
        }

        [RequiredByNativeCode]
        internal static void OnPostBuildPlayerScriptDLLs(BuildReport report)
        {
            if (processors.buildPlayerScriptDLLProcessors != null)
            {
                foreach (var step in processors.buildPlayerScriptDLLProcessors)
                {
                    using var _ = new ScopeTraceProfileBlock($"{step.GetType().Name}.OnPostBuildPlayerScriptDLLs");
                    try
                    {
                        step.OnPostBuildPlayerScriptDLLs(report);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        if ((report.summary.options & BuildOptions.StrictMode) != 0 || (report.summary.assetBundleOptions & BuildAssetBundleOptions.StrictMode) != 0)
                            return;
                    }
                }
            }
        }

        [RequiredByNativeCode]
        internal static string[] FilterAssembliesIncludedInBuild(BuildOptions buildOptions, string[] assemblies)
        {
            if (processors.filterBuildAssembliesProcessor == null)
            {
                return assemblies;
            }

            string[] startAssemblies = assemblies;
            string[] filteredAssemblies = assemblies;

            foreach (var filteredAssembly in processors.filterBuildAssembliesProcessor)
            {
                using var _ = new ScopeTraceProfileBlock($"{filteredAssembly.GetType().Name}.OnFilterAssemblies");
                int assemblyCount = filteredAssemblies.Length;
                filteredAssemblies = filteredAssembly.OnFilterAssemblies(buildOptions, filteredAssemblies);
                if (filteredAssemblies.Length > assemblyCount)
                {
                    throw new Exception("More Assemblies in the list than delivered. Only filtering, not adding extra assemblies");
                }
            }

            if (!Array.TrueForAll(filteredAssemblies, x => startAssemblies.Contains(x)))
            {
                throw new Exception("New Assembly names are in the list. Only filtering are allowed");
            }

            return filteredAssemblies;
        }

        [RequiredByNativeCode]
        internal static void CleanupBuildCallbacks()
        {
            processors.buildTargetProcessors = null;
            processors.buildPlayerProcessors = null;
            processors.buildPreprocessors = null;
            processors.buildPostprocessors = null;
            processors.sceneProcessors = null;
            processors.buildPreprocessorsWithReport = null;
            processors.buildPreprocessorsWithContext = null;
            processors.buildPostprocessorsWithReport = null;
            processors.buildPostprocessorsWithContext = null;
            processors.sceneProcessorsWithReport = null;
            processors.filterBuildAssembliesProcessor = null;
            processors.unityLinkerProcessors = null;
            processors.generateNativePluginsForAssembliesProcessors = null;
            processors.shaderProcessors = null;
            processors.computeShaderProcessors = null;
            processors.buildPlayerScriptDLLProcessors = null;
            previousFlags = BuildCallbacks.None;
        }
    }
}
