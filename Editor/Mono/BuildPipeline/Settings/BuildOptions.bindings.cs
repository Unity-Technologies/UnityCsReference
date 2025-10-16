// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    // Building options. Multiple options can be combined together.
    [Flags]
    public enum BuildOptions
    {
        // Perform the specified build without any special settings or extra tasks.
        None = 0,

        // Build a development version of the standalone player.
        Development = 1 << 0,

        // Run the built player.
        AutoRunPlayer = 1 << 2,

        // Show the built player.
        ShowBuiltPlayer = 1 << 3,

        // For internal use. Used when BuildAssetBundles implementation triggers part of the player build code path when packaging Scenes into AssetBundles.
        BuildAdditionalStreamedScenes = 1 << 4,

        // Do not overwrite player directory, but accept user's modifications.
        AcceptExternalModificationsToPlayer = 1 << 5,

        //*undocumented*
        InstallInBuildFolder = 1 << 6,

        // Do a non-incremental, clean cache build
        CleanBuildCache = 1 << 7,

        // automatically connects the profiler when the build is ran
        ConnectWithProfiler = 1 << 8,

        // Allow script debuggers to attach to the player remotely.
        AllowDebugging = 1 << 9,

        // Symlink runtime libraries when generating iOS XCode project. (Faster iteration time).
        [Obsolete("BuildOptions.SymlinkLibraries is obsolete. Use BuildOptions.SymlinkSources instead (UnityUpgradable) -> [UnityEditor] BuildOptions.SymlinkSources", false)]

        SymlinkLibraries = 1 << 10,

        // Symlink runtime libraries and reference externally .m, .mm, .c, .cpp, .swift files from Unity project when generating iOS XCode project.(Faster iteration time).
        // Reference externally .java, .kt files when generating Android gradle project
        SymlinkSources = 1 << 10,

        // Don't compress the data when creating the asset bundle.
        UncompressedAssetBundle = 1 << 11,

        [Obsolete("Use BuildOptions.Development instead")]
        StripDebugSymbols = 0,
        [Obsolete("Texture Compression is now always enabled")]
        CompressTextures = 0,

        //Set the player to try to connect to the host
        ConnectToHost = 1 << 12,

        //custom connection id
        CustomConnectionID = 1 << 13,

        // Headless Mode
        [Obsolete("Use StandaloneBuildSubtarget.Server instead")]
        EnableHeadlessMode = 1 << 14,

        // Build scripts only
        BuildScriptsOnly = 1 << 15,

        PatchPackage = 1 << 16,

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("BuildOptions.IL2CPP is deprecated and has no effect. Use PlayerSettings.SetScriptingBackend() instead.", true)]
        Il2CPP = 0,

        // Include assertions in non development builds
        ForceEnableAssertions = 1 << 17,

        // Forces chunk-based LZ4 compression for the asset bundle. Such asset bundles can be decompressed on the fly.
        CompressWithLz4 = 1 << 18,

        CompressWithLz4HC = 1 << 19,

        //*undocumented*
        [Obsolete("Specify IL2CPP optimization level in Player Settings.")]
        ForceOptimizeScriptCompilation = 0,

        // Request that the CRC of the built output be computed and included in the build report
        ComputeCRC = 1 << 20,

        // Force the build to fail when any errors are encountered
        StrictMode = 1 << 21,

        IncludeTestAssemblies = 1 << 22,

        // Will forces the buildGUID to all zeros
        NoUniqueIdentifier = 1 << 23,

        // Wait for player connection on start
        WaitForPlayerConnection = 1 << 25,

        // Enables Code Coverage. Can be used as a complimentary way of enabling code coverage on platforms
        // that do not support command line arguments
        EnableCodeCoverage = 1 << 26,

        // Only needed internally for AssetBundleStripUnityVersion
        //StripUnityVersion = 1 << 27

        // Enable C# code instrumentation for the player.
        EnableDeepProfilingSupport = 1 << 28,

        // The BuildReport object returned by BuildPipeline.BuildPlayer will contain more details (about build times and contents), at the cost of a slightly (typically, a few percents) longer build time
        DetailedBuildReport = 1 << 29,

        [Obsolete("Shader LiveLink is no longer supported.", true)]
        ShaderLivelinkSupport = 0,

    }
}
