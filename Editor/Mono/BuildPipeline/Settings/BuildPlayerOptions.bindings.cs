// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine;
using System;

namespace UnityEditor
{
    // NB! Keep in sync with BuildPlayerOptionsManagedStruct in Editor/Src/BuildPipeline/BuildPlayerOptions.h
    ///<summary>Provide various options to control the behavior of <see cref="BuildPipeline.BuildPlayer" />.</summary>
    ///<example>
    ///  <code source="../../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/Settings/BuildPlayerOptions_BuildPlayerOptions.cs"/>
    ///</example>
    ///<seealso cref="EditorBuildSettings" />
    ///<seealso cref="BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions" />
    [StructLayout(LayoutKind.Sequential)]
    public struct BuildPlayerOptions
    {
        ///<summary>The Scenes to be included in the build.</summary>
        ///<remarks>If empty, the currently open Scene will be built. Paths are relative to the project folder, for example "Assets/MyLevels/MyScene.unity". If any scene is currently open and has unsaved changes, the editor will ask the user to save these changes before building - or save changes automatically, if <see cref="Application.isBatchMode" /> is true.</remarks>
        ///<seealso cref="EditorBuildSettings.scenes" />
        public string[] scenes { get; set; }
        ///<summary>Specifies the path for the application to be built.</summary>
        ///<remarks>The value of <c>locationPathName</c> determines both the location and the file name of the built application.
        ///
        ///                This path can be either relative to the project directory or an absolute path.
        ///
        ///                Unity does not modify this path automatically, so it must include the correct file name and extension based on the target platform.
        ///
        ///                If the directory doesn't exist, Unity will create the directory automatically during the build process.
        ///
        ///**Guidelines**:
        ///
        ///* The path must include your chosen file name. For example, <c>Builds/MyApp.apk</c> for Android, <c>Builds/MyApp.exe</c> for Windows. Unity does not append extensions automatically.
        ///* For Android Package, use the <c>.apk</c> extension and for Android App Bundle, use the <c>.aab</c> extension.
        ///* For Windows, use the <c>.exe</c> extension.
        ///* For Mac, use the <c>.app</c> extension.
        ///* If the path is relative (e.g., <c>Builds/MyApp</c>), Unity will treat it as relative to the project root directory.
        ///* To place the build outside the project directory,  specify an absolute path such as <c>C:/MyBuilds/MyApp.exe</c>.
        ///
        ///Don't use the following project directories for <c>locationPathName</c>:
        ///
        ///* Assets/
        ///* ProjectSettings/
        ///* Library/
        ///* Packages/
        ///* UserSettings/
        ///
        ///**Note:** Paths pointing directly to the user desktop are not allowed.</remarks>
        [NativeName("path")]
        public string locationPathName { get; set; }
        ///<summary>The path to an manifest file describing all of the asset bundles used in the build (optional).</summary>
        ///<remarks>When you call <see cref="BuildPipeline.BuildAssetBundles" /> to create your AssetBundles, Unity will also generate a manifest file with a filename matching the parent directory name and ".manifest" as its extension.
        ///You can assign the path to this manifest file to <c>assetBundleManifestPath</c> to ensure that a player build does not strip any types used in the AssetBundles that you built.
        ///
        ///You do not need to set this property when you use /link.xml/ files, or if you generate AssetBundles using the &lt;a href="https://docs.unity3d.com/Packages/com.unity.addressables@latest"&gt;Addressables&lt;/a&gt; package.
        ///
        ///See [Managed code stripping](xref:um-managed-code-stripping) for more information about code stripping.</remarks>
        public string assetBundleManifestPath { get; set; }
        ///<summary>The <see cref="BuildTargetGroup" /> to build.</summary>
        [NativeName("platformGroup")]
        public BuildTargetGroup targetGroup { get; set; }
        ///<summary>The <see cref="BuildTarget" /> to build.</summary>
        ///<remarks>For best results, leave this property unset so that the build uses the target defined in the active <see cref="BuildProfile" />. If you do set it, set it to match the active build target.
        ///
        ///Building a target that doesn't match the active build target is unreliable. Changing the active build target requires recompiling Editor scripts for the new platform and a domain reload, which can't happen while a build script is running. As a result, build callbacks and platform-dependent code might compile for the wrong platform, and some platforms fail to build at all.
        ///
        ///To select the target, set the active build profile first: use the Build Profiles window in the Editor, or the <c>-activeBuildProfile</c> or <c>-buildTarget</c> argument on the [command line](xref:um-build-command-line). For more information, refer to [Create a custom build script](xref:um-build-script-build).</remarks>
        ///<seealso cref="EditorUserBuildSettings.activeBuildTarget" />
        [NativeName("platform")]
        public BuildTarget target { get; set; }
        ///<summary>The Subtarget to build.</summary>
        ///<remarks> If you only set <see cref="BuildPlayerOptions.target"/> and not <see cref="BuildPlayerOptions.subtarget"/>, this defaults to the platform's default subtarget.
        ///
 /// Setting this property persists the value in <see cref="EditorUserBuildSettings"/>, which overwrites the current Editor subtarget. This behavior also affects an active <see cref="BuildProfile"/>. When using a build profile, use <see cref="BuildPlayerWithProfileOptions"/> instead.
        ///
        ///Both this property and <see cref="BuildPlayerOptions.target" /> should remain unset to automatically use the target and subtarget as defined in the current Build Profile.
        ///
        ///The valid values for this property correspond to target-specific enum types, which are stored as integers. Examples of these enum types include <see cref="StandaloneBuildSubtarget" />, <see cref="MobileTextureSubtarget" />, <see cref="WebGLTextureSubtarget" /> and <see cref="XboxBuildSubtarget" />.
        ///
        ///Usage examples:
        ///
        ///* For building a headless server, when building for <see cref="BuildTarget.StandaloneWindows" />, <see cref="BuildTarget.StandaloneOSX" /> or <see cref="BuildTarget.StandaloneLinux64" />, specify <see cref="StandaloneBuildSubtarget.Server" /> (numeric value 1).
        ///* For ETC2 texture compression, when building for <see cref="BuildTarget.Android" />, or another mobile platform, specify <see cref="MobileTextureSubtarget.ETC2" /> (numeric value 5).</remarks>
        ///<seealso cref="BuildAssetBundlesParameters.subtarget" />
        ///<seealso cref="EditorUserBuildSettings.standaloneBuildSubtarget" />
        ///<seealso cref="EditorUserBuildSettings.androidBuildSubtarget" />
        ///<seealso cref="EditorUserBuildSettings.webGLBuildSubtarget" />
        ///<seealso cref="EditorUserBuildSettings.ps4BuildSubtarget" />
        public int subtarget { get; set; }
        ///<summary>The <see cref="BuildOptions" /> flags to apply when building the Player.</summary>
        ///<remarks>Set this property to a bitwise combination of <see cref="BuildOptions" /> values before you pass <see cref="BuildPlayerOptions" /> to <see cref="BuildPipeline.BuildPlayer" />. For example, set <c>options = BuildOptions.Development | BuildOptions.AutoRunPlayer</c> to create a development build and run it automatically after the build completes.</remarks>
        public BuildOptions options { get; set; }
        ///<summary>The additional preprocessor defines you can specify while compiling assemblies for the Player. These defines are appended to the existing Scripting Define Symbols list configured in the Player settings.</summary>
        public string[] extraScriptingDefines { get; set; }
        
        ///<summary>Use this property to reference the output folders or build report directories from one or more <see cref="BuildPipeline.BuildContentDirectory"/> builds.</summary>
        ///<remarks>The types used in those builds are included in the information provided to UnityLinker.
        ///This ensures that the Player can load all the content from those additional builds.
        ///
        ///During a build you can also add directories from a build callback with <see cref="Build.BuildPlayerContext.AddPreviousBuildReportDirectory"/>.</remarks>
        ///<seealso cref="BuildPipeline.BuildContentDirectory"/>
        ///<seealso cref="Build.BuildPlayerContext.AddPreviousBuildReportDirectory"/>
        public string[] previousBuildReportDirectories { get; set; }

        [NativeHeader("Editor/Src/BuildPipeline/BuildPlayerOptions.h")]
        [FreeFunction("BuildPipeline::GetBuildPlayerSetup")]
        internal static extern BuildPlayerOptions GetBuildPlayerOptions(IntPtr dataPtr);
    }
}
