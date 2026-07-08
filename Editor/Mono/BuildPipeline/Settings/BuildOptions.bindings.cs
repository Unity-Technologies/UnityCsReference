// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using UnityEngine.Internal;

namespace UnityEditor
{
    // Building options. Multiple options can be combined together.
    ///<summary>Options to configure a build. You can combine multiple build options together.</summary>
    ///<example>
    ///  <code source="../../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/Settings/BuildOptions_BuildOptions.cs"/>
    ///</example>
    ///<seealso cref="BuildPipeline.BuildPlayer" />
    [Flags]
    public enum BuildOptions
    {
        // Perform the specified build without any special settings or extra tasks.
        ///<summary>Perform the specified build without any special settings or extra tasks.</summary>
        None = 0,

        // Build a development version of the standalone player.
        ///<summary>Build a development version of the Player.</summary>
        ///<remarks>A development build includes debug symbols and enables the <see cref="Profiler" />.</remarks>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        Development = 1 << 0,

        // Run the built player.
        ///<summary>Run the built Player.</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        AutoRunPlayer = 1 << 2,

        // Show the built player.
        ///<summary>Show the built Player.</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        ShowBuiltPlayer = 1 << 3,

        // For internal use. Used when BuildAssetBundles implementation triggers part of the player build code path when packaging Scenes into AssetBundles.
        ///<summary>For internal use</summary>
        ///<remarks>This flag is for internal use only.  To create an AssetBundles containing Scenes use <see cref="BuildPipeline.BuildAssetBundles" />.</remarks>
        BuildAdditionalStreamedScenes = 1 << 4,

        // Do not overwrite player directory, but accept user's modifications.
        ///<summary>Appends to an existing Xcode (iOS) project during the build process.</summary>
        ///<remarks>This preserves any changes made to the existing Xcode project settings. With the IL2CPP scripting backend, this setting also allows incremental builds of the generated C++ code to work in Xcode. Appending to Xcode projects is supported only on macOS and Windows platforms. This option applies to iOS builds; for similar behavior on Android, use <see cref="EditorUserBuildSettings.exportAsGoogleAndroidProject" /> instead.</remarks>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        AcceptExternalModificationsToPlayer = 1 << 5,

        [ExcludeFromDocs]
        InstallInBuildFolder = 1 << 6,

        // Do a non-incremental, clean cache build
        ///<summary>Clear all cached build results, resulting in a full rebuild of all scripts and all player data.</summary>
        CleanBuildCache = 1 << 7,

        // automatically connects the profiler when the build is ran
        ///<summary>Start the Player with a connection to the Profiler in the Editor.</summary>
        ///<remarks>When the build starts, an open Profiler window automatically connects to the Player and starts profiling. When you build the Player by enabling the <see cref="BuildOptions.AutoRunPlayer" /> option, the Editor automatically opens the Profiler window.</remarks>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        ConnectWithProfiler = 1 << 8,

        // Allow script debuggers to attach to the player remotely.
        ///<summary>Allow script debuggers to attach to the Player remotely. You can debug your scripts only if you use <see cref="BuildOptions.Development" />.</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        AllowDebugging = 1 << 9,

        // Symlink runtime libraries when generating iOS XCode project. (Faster iteration time).
        ///<summary>Symlink runtime libraries when generating iOS Xcode project. (Faster iteration time).</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        ///<seealso cref="EditorUserBuildSettings.symlinkSources" />
        [Obsolete("BuildOptions.SymlinkLibraries is obsolete. Use BuildOptions.SymlinkSources instead (UnityUpgradable) -> [UnityEditor] BuildOptions.SymlinkSources", false)]

        SymlinkLibraries = 1 << 10,

        // Symlink runtime libraries and reference externally .m, .mm, .c, .cpp, .swift files from Unity project when generating iOS XCode project.(Faster iteration time).
        // Reference externally .java, .kt files when generating Android gradle project
        ///<summary>Symlink sources when generating the project. This is useful if you're changing source files inside the generated project and want to bring the changes back into your Unity project or a package.</summary>
        ///<remarks>This option affects sources in both Unity projects and packages. Only the following platforms support this option:
        ///
        ///**iOS**: When <c>symlinkSources</c> is enabled, Unity creates symlinks for libraries (libil2cpp.a, libiPhone-lib.a, etc.). This means you don't need to copy the libraries. Sources with .mm, .m, .cpp, .c, .h, .swift, and .xib extensions are referenced externally from Xcode project.
        ///
        ///**Android**: When <c>symlinkSources</c> is enabled, Gradle project references .java, .kt and .androidlib plug-in sources externally from Unity project rather than copying the source files directly into the Gradle project. In case of .androidlib plug-in, the plug-in folder must include <c>build.gradle</c> file for the <c>symlinkSources</c> option to work.</remarks>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        ///<seealso cref="EditorUserBuildSettings.symlinkSources" />
        SymlinkSources = 1 << 10,

        // Don't compress the data when creating the asset bundle.
        ///<summary>Don't compress the data when creating the asset bundle.</summary>
        ///<remarks>This makes it faster to build and load, but since it is much bigger it will take longer to download.</remarks>
        UncompressedAssetBundle = 1 << 11,

        [Obsolete("Use BuildOptions.Development instead")]
        [ExcludeFromDocs]
        StripDebugSymbols = 0,
        [Obsolete("Texture Compression is now always enabled")]
        [ExcludeFromDocs]
        CompressTextures = 0,

        //Set the player to try to connect to the host
        ///<summary>Sets the Player to connect to the Editor.</summary>
        ///<remarks>Sets the Player to connect to the Editor and it requires <see cref="BuildOptions.Development" /> to be set. Please note that you should only use this in development.</remarks>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        ConnectToHost = 1 << 12,

        //custom connection id
        ///<summary>Determines if the player should be using the custom connection ID.</summary>
        ///<remarks>If a Custom Connection ID is set in the Analysis Preferences, then this is set. If no Custom Connection ID is set then it reverts to using the default host name for the device and this flag will not be set.</remarks>
        CustomConnectionID = 1 << 13,

        // Headless Mode
        ///<summary>Options for building the standalone player in headless mode.</summary>
        ///<remarks>The application output compiles the standalone player based on the following target platforms:
        ///
        ///**Mac player:** Compiled as a standard console application.
        ///
        ///**Win Player:** Compiled with /System:Console and runs as a standard windows console application.
        ///
        ///**Linux player:** Compiled as a standard console application.
        ///
        ///UNITY_SERVER will be defined when building managed assemblies.</remarks>
        [Obsolete("Use StandaloneBuildSubtarget.Server instead")]
        EnableHeadlessMode = 1 << 14,

        // Build scripts only
        ///<summary>Only build the scripts in a project.</summary>
        ///<remarks>
        ///  <para>Before you can use <c>BuildScriptsOnly</c>, you need to build the whole Project. Then you can run builds that only have script changes. Rebuilding the player data will be skipped for faster iteration speed.
        ///
        ///Platforms which support the incremental build pipeline will automatically run scripts only builds if Unity detects that the data files have not changed, even if <c>BuildScriptsOnly</c> is not used. You can still use <c>BuildScriptsOnly</c> to force a script only build and ignore any pending player data changes.
        ///
        ///The following script example uses <c>BuildScriptsOnly</c>. The script builds the entire Project initially. After you've run the script for the first time, you can use the script to only compile any changes you make to the script. To try this out, add the following Editor script and the game script to your project.</para>
        ///  <para>Attach the following simple script to an empty GameObject in the scene:</para>
        ///  <para>Now run the <c>Build/Build scripts</c> example. This builds an executable.  Run that executable and a dark blue window with the label appears. Next add some cubes and spheres to the Project. Make the following script changes:</para>
        ///  <para>Finally, swap the commented lines in the <c>EditorExample</c> script:</para>
        ///  <para>Use the <c>Build/Build scripts</c> to regenerate the application and then launch it. The application will now show random changes to the background color. However the added cubes and spheres are not visible.</para>
        ///</remarks>
        ///<example>
        ///  <code source="../../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/Settings/BuildOptions_BuildOptions.cs"/>
        ///</example>
        ///<example>
        ///  <code source="../../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/Settings/BuildOptions_BuildScriptsOnlyExampleClass1.cs"/>
        ///</example>
        ///<example>
        ///  <code source="../../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/Settings/BuildOptions_BuildScriptsOnlyExampleClass2.cs"/>
        ///</example>
        BuildScriptsOnly = 1 << 15,

        ///<summary>Patch a <see cref="Development" /> app package rather than completely rebuilding it.
        ///
        ///Supported only on Android platform.</summary>
        ///<remarks>Use the <c>PatchPackage</c> option in your build script to rebuild and deploy project changes. Patching an existing deployment can be significantly faster than rebuilding the entire application. Only <see cref="Development" /> builds can be patched.
        ///
        ///To patch a package, build the project using the following options in your build script: <c>BuildOptions.PatchPackage</c> or <see cref="BuildOptions.Development" />. Alternatively, you can click the **Patch** or **Patch And Run** buttons on the **Build Settings** window.
        ///
        ///This option will implicitly change some options to enhance performance of deployment. The application will not be split using expansion files (such as OBB).</remarks>
        PatchPackage = 1 << 16,

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("BuildOptions.IL2CPP is deprecated and has no effect. Use PlayerSettings.SetScriptingBackend() instead.", true)]
        [ExcludeFromDocs]
        Il2CPP = 0,

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("BuildOptions.ForceEnableAssertions is deprecated. Use PlayerSettings.SetManagedCodeVariant(NamedBuildTarget, ManagedCodeVariant.Checked) instead.")]
        [ExcludeFromDocs]
        ForceEnableAssertions = 1 << 17,

        // Forces chunk-based LZ4 compression for the asset bundle. Such asset bundles can be decompressed on the fly.
        ///<summary>Use chunk-based LZ4 compression when building the Player.</summary>
        ///<remarks>This value allows content to be stored in a compressed form when the Player is deployed to a device. Decompression is performed in real time when the Player reads the data. Scene or Asset loading might be faster or slower depending on disk read speed when compared to using uncompressed data.
        ///
        ///When this flag is passed the player content is stored inside a Unity Archive file named **data.unity3d**.  The build process splits the data into 128KB chunks and applies LZ4 compression to each chunk.
        ///For higher compression the <see cref="BuildOptions.CompressWithLz4HC" /> flag can be used instead.
        ///
        ///This archive file contains the following content:
        ///
        ///1. Player settings - globalgamemanagers and globalgamemanagers.assets* files.
        ///
        ///2. Scenes and referenced Assets - level* and sharedassets*.asset files.
        ///
        ///3. Assets from Resources folders - resources.assets files.
        ///
        ///4. Global Illumination data - GI/level* files.
        ///
        ///5. Built-in resources - Resources/unity_builtin_extra file.
        ///
        ///This archive file does not contain the Resources/unity default resources file.
        ///
        ///This feature is supported for **Standalone**, **Android** and **iOS** build targets and is default for **WebGL** target.
        ///
        ///Enabling CompressWithLz4 in **Android** might be a significant performance boost when loading data, as LZ4 decompression is faster than the default Zip decompression.
        ///
        ///**Note:**
        ///
        ///Using chunk-based compression for player data will reduce the size of the player on the device, while still allowing efficient loading.
        ///However chunk-based compression is typically not as small as full-file compression, and it will not compress much further if another layer of compression is applied at packaging time.
        ///Hence the game installer might end up a bit larger when using this flag.
        ///
        ///LZ4 compression can also be applied to AssetBundles. For more information, refer to <see cref="BuildAssetBundleOptions.ChunkBasedCompression" /> and <see href="xref:um-asset-bundles-cache" />.</remarks>
        ///<seealso href="xref:um-reducing-filesize" />
        ///<seealso cref="Unity.IO.Archive.ArchiveHandle.Compression" />
        CompressWithLz4 = 1 << 18,

        ///<summary>Use chunk-based LZ4 high-compression when building the Player.</summary>
        ///<remarks>This property functions the same way as <see cref="BuildOptions.CompressWithLz4" />, but uses a slower and higher quality compression algorithm.</remarks>
        CompressWithLz4HC = 1 << 19,


        ///<summary>Force full optimizations for script compilation in Development builds.</summary>
        ///<remarks>Forces full optimization for IL2CPP code compilation. Useful when profiling Development builds to see how optimizations affect performance.</remarks>
        [Obsolete("Specify IL2CPP optimization level in Player Settings.")]
        [ExcludeFromDocs]
        ForceOptimizeScriptCompilation = 0,

        // Request that the CRC of the built output be computed and included in the build report
        [ExcludeFromDocs]
        ComputeCRC = 1 << 20,

        // Force the build to fail when any errors are encountered
        ///<summary>Prevent the build from succeeding if any errors are reported during the build process.</summary>
        ///<remarks>Without this flag, non-fatal errors, such as shader compilation issues on a particular platform, won't cause the build to fail, but might lead to incorrect behavior at runtime.
        /// When this flag is set, errors logged from build callbacks also
        /// fail the build, including these build callbacks:
        /// <see cref="Build.IPreprocessBuildWithContext.OnPreprocessBuild"/>,
        /// <see cref="Build.IPostprocessBuildWithContext.OnPostprocessBuild"/>,
        /// <see cref="Build.IProcessSceneWithReport.OnProcessScene"/>,
        /// <see cref="Build.IPreprocessShaders.OnProcessShader"/>, and
        /// <see cref="Build.IPreprocessComputeShaders.OnProcessComputeShader"/>.
        /// </remarks>
        StrictMode = 1 << 21,

        ///<summary>Build will include Assemblies for testing.</summary>
        IncludeTestAssemblies = 1 << 22,

        // Will forces the buildGUID to all zeros
        ///<summary>Will force the buildGUID to all zeros.</summary>
        NoUniqueIdentifier = 1 << 23,

        ///<summary>Suppress the error reported when a LoadableObjectId or LoadableSceneId is encountered during a Player build.</summary>
        ///<remarks>Use this when migrating between build pipeline backends when assets legitimately have Loadable references, but the same content is also included in the Player. The references still resolve to null in the resulting build; this flag only silences the error log to keep the build usable.</remarks>
        SuppressLoadableErrors = 1 << 24,

        // Wait for player connection on start
        ///<summary>Sets the Player to wait for player connection on player start.</summary>
        ///<seealso cref="Networking.PlayerConnection.PlayerConnection" />
        WaitForPlayerConnection = 1 << 25,

        // Enables Code Coverage. Can be used as a complimentary way of enabling code coverage on platforms
        // that do not support command line arguments
        ///<summary>Enables code coverage. You can use this as a complimentary way of enabling code coverage on platforms that do not support command line arguments.</summary>
        EnableCodeCoverage = 1 << 26,

        // Only needed internally for AssetBundleStripUnityVersion
        //StripUnityVersion = 1 << 27

        // Enable C# code instrumentation for the player.
        ///<summary>Enables Deep Profiling support in the Player.</summary>
        ///<remarks>Deep profiling allows to instrument all C# method calls.
        ///
        ///**Note:** Enabling the <c>EnableDeepProfilingSupport</c> option might significantly slow down the Player, compared to one built with this option disabled. When enabled, additional checks are inserted at the beginning and end of every C# method. These checks continually test if the Player is currently profiled in Deep Profile mode or not, which adds some overhead to their execution time. If the Player is indeed profiled in Deep Profile mode, the method execution time is recorded, which adds some additional overhead. The overhead will not be fully attributed to the method that has been instrumented like this, but will affect the recorded execution time of the calling method as well.</remarks>
        EnableDeepProfilingSupport = 1 << 28,

        // The BuildReport object returned by BuildPipeline.BuildPlayer will contain more details (about build times and contents), at the cost of a slightly (typically, a few percents) longer build time
        ///<summary>Generates detailed information in the <see cref="UnityEditor.Build.Reporting.BuildReport" />.</summary>
        ///<remarks>
        ///  <para>The BuildReport object returned by <see cref="BuildPipeline.BuildPlayer" /> will contain additional data about build times and contents. This might lead to slightly longer build time, typically by a few percents.
        ///
        ///The following script example illustrates how to use <c>DetailedBuildReport</c> when building a Player. Create a project and add the script under Assets/Editor.</para>
        ///  <para>1. Run the <c>Build/DetailedBuildReport</c> scripts example.
        ///2. Access the information about the build process in the <c>buildReport</c> variable which you can process using the <see cref="UnityEditor.Build.Reporting.BuildReport" /> API.
        ///3. Refer to the <see href="https://github.com/Unity-Technologies/BuildReportInspector">Build Report Inspector source script </see> to find illustrations on how to query the <see cref="UnityEditor.Build.Reporting.BuildReport" /> API.</para>
        ///</remarks>
        ///<example>
        ///  <code source="../../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/Settings/BuildOptions_BuildOptions.cs"/>
        ///</example>
        DetailedBuildReport = 1 << 29,

        [Obsolete("Shader LiveLink is no longer supported.", true)]
        [ExcludeFromDocs]
        ShaderLivelinkSupport = 0,

    }
}
