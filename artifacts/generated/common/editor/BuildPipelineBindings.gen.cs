// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.BuildReporting;
using Object = UnityEngine.Object;
using Mono.Cecil;


namespace UnityEditor
{



[System.Flags]
public enum BuildOptions
{
    
    None = 0,
    
    Development = 1 << 0,
    
    AutoRunPlayer = 1 << 2,
    
    ShowBuiltPlayer = 1 << 3,
    
    BuildAdditionalStreamedScenes = 1 << 4,
    
    AcceptExternalModificationsToPlayer = 1 << 5,
    
    InstallInBuildFolder = 1 << 6,
    
    [System.Obsolete ("WebPlayer has been removed in 5.4", true)]
    WebPlayerOfflineDeployment = 1 << 7,
    
    ConnectWithProfiler = 1 << 8,
    
    AllowDebugging = 1 << 9,
    
    SymlinkLibraries = 1 << 10,
    
    UncompressedAssetBundle = 1 << 11,
    [System.Obsolete ("Use BuildOptions.Development instead")]
    StripDebugSymbols = 0,
    [System.Obsolete ("Texture Compression is now always enabled")]
    CompressTextures = 0,
    
    ConnectToHost = 1 << 12,
    
    EnableHeadlessMode = 1 << 14,
    
    BuildScriptsOnly = 1 << 15,
    
    
    Il2CPP = 1 << 16,
    
    ForceEnableAssertions = 1 << 17,
    
    CompressWithLz4 = 1 << 18,
    CompressWithLz4HC = 1 << 19,
    
    [System.Obsolete ("Specify IL2CPP optimization level in Player Settings.")]
    ForceOptimizeScriptCompilation = 0,
    
    ComputeCRC = 1 << 20,
    
    StrictMode = 1 << 21,
    
    NoUniqueIdentifier = 1 << 23
}

[System.Flags]
public enum BuildAssetBundleOptions
{
    
    None = 0,
    
    UncompressedAssetBundle = 1,
    
    [System.Obsolete ("This has been made obsolete. It is always enabled in the new AssetBundle build system introduced in 5.0.")]
    CollectDependencies = 2,
    
    [System.Obsolete ("This has been made obsolete. It is always enabled in the new AssetBundle build system introduced in 5.0.")]
    CompleteAssets = 4,
    
    DisableWriteTypeTree = 8,
    
    DeterministicAssetBundle = 16,
    
    ForceRebuildAssetBundle = 32,
    
    IgnoreTypeTreeChanges = 64,
    
    AppendHashToAssetBundleName = 128,
    
    ChunkBasedCompression = 256,
    
    StrictMode = 512,
    
    DryRunBuild = 1024,
    
    DisableLoadAssetByFileName = 4096,
    
    DisableLoadAssetByFileNameWithExtension = 8192
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct AssetBundleBuild
{
    public string   assetBundleName;
    public string   assetBundleVariant;
    public string[] assetNames;
    public string[] addressableNames;
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct BuildPlayerOptions
{
    public string[] scenes {get; set; }
    public string locationPathName {get; set; }
    public string assetBundleManifestPath {get; set; }
    public BuildTargetGroup targetGroup {get; set; }
    public BuildTarget target {get; set; }
    public BuildOptions options {get; set; }
}

public sealed partial class BuildPipeline
{
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  BuildTargetGroup GetBuildTargetGroup (BuildTarget platform) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  BuildTargetGroup GetBuildTargetGroupByName (string platform) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  BuildTarget GetBuildTargetByName (string platform) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetBuildTargetGroupDisplayName (BuildTargetGroup targetPlatformGroup) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetBuildTargetName (BuildTarget targetPlatform) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetEditorTargetName () ;

    [System.Obsolete ("PushAssetDependencies has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void PushAssetDependencies () ;

    [System.Obsolete ("PopAssetDependencies has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void PopAssetDependencies () ;

    private static string[] InvokeCalculateBuildTags(BuildTarget target, BuildTargetGroup group)
        {

            return new string[0];
        }
    
    
    private static void LogBuildExceptionAndExit(string buildFunctionName, System.Exception exception)
        {
            Debug.LogErrorFormat("Internal Error in {0}:", buildFunctionName);
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    
    
    public static string BuildPlayer(EditorBuildSettingsScene[] levels, string locationPathName, BuildTarget target, BuildOptions options)
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = EditorBuildSettingsScene.GetActiveSceneList(levels);
            buildPlayerOptions.locationPathName = locationPathName;
            buildPlayerOptions.target = target;
            buildPlayerOptions.options = options;
            return BuildPlayer(buildPlayerOptions);
        }
    
    
    public static string BuildPlayer(string[] levels, string locationPathName, BuildTarget target, BuildOptions options)
        {
            BuildTargetGroup buildTargetGroup = GetBuildTargetGroup(target);
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = levels;
            buildPlayerOptions.locationPathName = locationPathName;
            buildPlayerOptions.targetGroup = buildTargetGroup;
            buildPlayerOptions.target = target;
            buildPlayerOptions.options = options;
            return BuildPlayer(buildPlayerOptions);
        }
    
    
    public static string BuildPlayer(BuildPlayerOptions buildPlayerOptions)
        {
            return BuildPlayer(buildPlayerOptions.scenes, buildPlayerOptions.locationPathName, buildPlayerOptions.assetBundleManifestPath, buildPlayerOptions.targetGroup, buildPlayerOptions.target, buildPlayerOptions.options);
        }
    
    
    private static string BuildPlayer(string[] scenes, string locationPathName, string assetBundleManifestPath, BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options)
        {
            if (isBuildingPlayer)
            {
                return "Cannot start a new build because there is already a build in progress.";
            }

            if (buildTargetGroup == BuildTargetGroup.Unknown)
                buildTargetGroup = GetBuildTargetGroup(target);

            string locationPathNameError;
            if (!ValidateLocationPathNameForBuildTargetGroup(locationPathName, buildTargetGroup, target, options, out locationPathNameError))
            {
                return locationPathNameError;
            }

            try
            {
                return BuildPlayerInternal(scenes, locationPathName, assetBundleManifestPath, buildTargetGroup, target, options).SummarizeErrors();
            }
            catch (System.Exception exception)
            {
                LogBuildExceptionAndExit("BuildPipeline.BuildPlayer", exception);
                return "";
            }
        }
    
    
    internal static bool ValidateLocationPathNameForBuildTargetGroup(string locationPathName, BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options, out string errorMessage)
        {
            if (string.IsNullOrEmpty(locationPathName))
            {
                var willInstallInBuildFolder = (options & BuildOptions.InstallInBuildFolder) != 0 &&
                    PostprocessBuildPlayer.SupportsInstallInBuildFolder(buildTargetGroup, target);
                if (!willInstallInBuildFolder)
                {
                    errorMessage =
                        "The 'locationPathName' parameter for BuildPipeline.BuildPlayer should not be null or empty.";
                    return false;
                }
            }
            else if (string.IsNullOrEmpty(Path.GetFileName(locationPathName)))
            {
                var extensionForBuildTarget = PostprocessBuildPlayer.GetExtensionForBuildTarget(buildTargetGroup, target, options);

                if (!string.IsNullOrEmpty(extensionForBuildTarget))
                {
                    errorMessage = string.Format(
                            "For the '{0}' target the 'locationPathName' parameter for BuildPipeline.BuildPlayer should not end with a directory separator.\n" +
                            "Provided path: '{1}', expected a path with the extension '.{2}'.", target, locationPathName,
                            extensionForBuildTarget);
                    return false;
                }
            }

            errorMessage = "";

            return true;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsFeatureSupported (string define, BuildTarget platform) ;

    [System.Obsolete ("BuildStreamedSceneAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
public static string BuildStreamedSceneAssetBundle(string[] levels, string locationPath, BuildTarget target, BuildOptions options)
        {
            return BuildPlayer(levels, locationPath, target, options | BuildOptions.BuildAdditionalStreamedScenes);
        }
    
    
    [System.Obsolete ("BuildStreamedSceneAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
public static string BuildStreamedSceneAssetBundle(string[] levels, string locationPath, BuildTarget target)
        {
            return BuildPlayer(levels, locationPath, target, BuildOptions.BuildAdditionalStreamedScenes);
        }
    
    
    [System.Obsolete ("BuildStreamedSceneAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
public static string BuildStreamedSceneAssetBundle(string[] levels, string locationPath, BuildTarget target, out uint crc, BuildOptions options)
        {
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
            return BuildStreamedSceneAssetBundle(levels, locationPath, buildTargetGroup, target, out crc, options);
        }
    
    
    [System.Obsolete ("BuildStreamedSceneAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
internal static string BuildStreamedSceneAssetBundle(string[] levels, string locationPath, BuildTargetGroup buildTargetGroup, BuildTarget target, out uint crc, BuildOptions options)
        {
            crc = 0;
            try
            {
                var report = BuildPlayerInternal(levels, locationPath, null, buildTargetGroup, target, options | BuildOptions.BuildAdditionalStreamedScenes | BuildOptions.ComputeCRC);
                crc = report.crc;

                var summary = report.SummarizeErrors();
                Object.DestroyImmediate(report, true);
                return summary;
            }
            catch (System.Exception exception)
            {
                LogBuildExceptionAndExit("BuildPipeline.BuildStreamedSceneAssetBundle", exception);
                return "";
            }
        }
    
    
    [System.Obsolete ("BuildStreamedSceneAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
public static string BuildStreamedSceneAssetBundle(string[] levels, string locationPath, BuildTarget target, out uint crc)
        {
            return BuildStreamedSceneAssetBundle(levels, locationPath, target, out crc, 0);
        }
    
    
    private static BuildReport BuildPlayerInternal(string[] levels, string locationPathName, string assetBundleManifestPath, BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options)
        {
            if (0 != (BuildOptions.EnableHeadlessMode & options) &&
                0 != (BuildOptions.Development & options))
                throw new Exception("Unsupported build setting: cannot build headless development player");

            return BuildPlayerInternalNoCheck(levels, locationPathName, assetBundleManifestPath, buildTargetGroup, target, options, false);
        }
    
    
    public extern static bool isBuildingPlayer
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  BuildReport BuildPlayerInternalNoCheck (string[] levels, string locationPathName, string assetBundleManifestPath, BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options, bool delayToAfterScriptReload) ;

    
    #pragma warning disable 618
    
    
    [System.Obsolete ("BuildAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
public static bool BuildAssetBundle(Object mainAsset, Object[] assets, string pathName, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            uint crc;
            return BuildAssetBundle(mainAsset, assets, pathName, out crc, assetBundleOptions, targetPlatform);
        }
    
    
    [System.Obsolete ("BuildAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
public static bool BuildAssetBundle(Object mainAsset, Object[] assets, string pathName, out uint crc, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            BuildTargetGroup targetPlatformGroup = BuildPipeline.GetBuildTargetGroup(targetPlatform);
            return BuildAssetBundle(mainAsset, assets, pathName, out crc, assetBundleOptions, targetPlatformGroup, targetPlatform);
        }
    
    
    internal static bool BuildAssetBundle(Object mainAsset, Object[] assets, string pathName, out uint crc, BuildAssetBundleOptions assetBundleOptions, BuildTargetGroup targetPlatformGroup, BuildTarget targetPlatform)
        {
            crc = 0;
            try
            {
                return BuildAssetBundleInternal(mainAsset, assets, null, pathName, assetBundleOptions, targetPlatformGroup, targetPlatform, out crc);
            }
            catch (System.Exception exception)
            {
                LogBuildExceptionAndExit("BuildPipeline.BuildAssetBundle", exception);
                return false;
            }
        }
    
    
    [System.Obsolete ("BuildAssetBundleExplicitAssetNames has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
public static bool BuildAssetBundleExplicitAssetNames(Object[] assets, string[] assetNames, string pathName, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            uint crc;
            return BuildAssetBundleExplicitAssetNames(assets, assetNames, pathName, out crc, assetBundleOptions, targetPlatform);
        }
    
    
    [System.Obsolete ("BuildAssetBundleExplicitAssetNames has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
public static bool BuildAssetBundleExplicitAssetNames(Object[] assets, string[] assetNames, string pathName, out uint crc, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            BuildTargetGroup targetPlatformGroup = BuildPipeline.GetBuildTargetGroup(targetPlatform);
            return BuildAssetBundleExplicitAssetNames(assets, assetNames, pathName, out crc, assetBundleOptions, targetPlatformGroup, targetPlatform);
        }
    
    
    internal static bool BuildAssetBundleExplicitAssetNames(Object[] assets, string[] assetNames, string pathName, out uint crc, BuildAssetBundleOptions assetBundleOptions, BuildTargetGroup targetPlatformGroup, BuildTarget targetPlatform)
        {
            crc = 0;
            try
            {
                return BuildAssetBundleInternal(null, assets, assetNames, pathName, assetBundleOptions, targetPlatformGroup, targetPlatform, out crc);
            }
            catch (System.Exception exception)
            {
                LogBuildExceptionAndExit("BuildPipeline.BuildAssetBundleExplicitAssetNames", exception);
                return false;
            }
        }
    
    #pragma warning restore 618
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool BuildAssetBundleInternal (Object mainAsset, Object[] assets, string[] assetNames, string pathName, BuildAssetBundleOptions assetBundleOptions, BuildTargetGroup targetPlatformGroup, BuildTarget targetPlatform, out uint crc) ;

    public static AssetBundleManifest BuildAssetBundles(string outputPath, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            BuildTargetGroup targetPlatformGroup = BuildPipeline.GetBuildTargetGroup(targetPlatform);
            return BuildAssetBundles(outputPath, assetBundleOptions, targetPlatformGroup, targetPlatform);
        }
    
    
    internal static AssetBundleManifest BuildAssetBundles(string outputPath, BuildAssetBundleOptions assetBundleOptions, BuildTargetGroup targetPlatformGroup, BuildTarget targetPlatform)
        {
            if (isBuildingPlayer)
                throw new InvalidOperationException("Cannot build asset bundles while a build is in progress.");

            if (!System.IO.Directory.Exists(outputPath))
                throw new ArgumentException("The output path \"" + outputPath + "\" doesn't exist");

            return BuildAssetBundlesInternal(outputPath, assetBundleOptions, targetPlatformGroup, targetPlatform);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  AssetBundleManifest BuildAssetBundlesInternal (string outputPath, BuildAssetBundleOptions assetBundleOptions, BuildTargetGroup targetPlatformGroup, BuildTarget targetPlatform) ;

    public static AssetBundleManifest BuildAssetBundles(string outputPath, AssetBundleBuild[] builds, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            BuildTargetGroup targetPlatformGroup = BuildPipeline.GetBuildTargetGroup(targetPlatform);
            return BuildAssetBundles(outputPath, builds, assetBundleOptions, targetPlatformGroup, targetPlatform);
        }
    
    
    internal static AssetBundleManifest BuildAssetBundles(string outputPath, AssetBundleBuild[] builds, BuildAssetBundleOptions assetBundleOptions, BuildTargetGroup targetPlatformGroup, BuildTarget targetPlatform)
        {
            if (isBuildingPlayer)
                throw new InvalidOperationException("Cannot build asset bundles while a build is in progress.");

            if (!System.IO.Directory.Exists(outputPath))
                throw new ArgumentException("The output path \"" + outputPath + "\" doesn't exist");

            if (builds == null)
                throw new ArgumentException("AssetBundleBuild cannot be null.");

            return BuildAssetBundlesWithInfoInternal(outputPath, builds, assetBundleOptions, targetPlatformGroup, targetPlatform);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  AssetBundleManifest BuildAssetBundlesWithInfoInternal (string outputPath, AssetBundleBuild[] builds, BuildAssetBundleOptions assetBundleOptions, BuildTargetGroup targetPlatformGroup, BuildTarget targetPlatform) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool GetCRCForAssetBundle (string targetPath, out uint crc) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool GetHashForAssetBundle (string targetPath, out Hash128 hash) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool LicenseCheck (BuildTarget target) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsBuildTargetSupported (BuildTargetGroup buildTargetGroup, BuildTarget target) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsBuildTargetCompatibleWithOS (BuildTarget target) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetBuildTargetAdvancedLicenseName (BuildTarget target) ;

    internal static string GetPlaybackEngineDirectory(BuildTarget target, BuildOptions options)
        {
            BuildTargetGroup buildTargetGroup = GetBuildTargetGroup(target);
            return GetPlaybackEngineDirectory(buildTargetGroup, target, options);
        }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetPlaybackEngineDirectory (BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetPlaybackEngineExtensionDirectory (BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetPlaybackEngineDirectory (BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options, string playbackEngineDirectory) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetBuildToolsDirectory (BuildTarget target) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetMonoBinDirectory (BuildTarget target) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetMonoLibDirectory (BuildTarget target) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string CompatibilityProfileToClassLibFolder (ApiCompatibilityLevel compatibilityLevel) ;

    internal static string GetBuildTargetGroupName(BuildTarget target)
        {
            return GetBuildTargetGroupName(GetBuildTargetGroup(target));
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetBuildTargetGroupName (BuildTargetGroup buildTargetGroup) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsUnityScriptEvalSupported (BuildTarget target) ;

    internal static string[] GetReferencingPlayerAssembliesForDLL(string dllPath)
        {
            DefaultAssemblyResolver resolverRoot = new DefaultAssemblyResolver();
            resolverRoot.AddSearchDirectory(Path.GetDirectoryName(dllPath));
            AssemblyDefinition assemblyRoot = AssemblyDefinition.ReadAssembly(dllPath, new ReaderParameters { AssemblyResolver = resolverRoot });

            string[] assemblyPaths = BuildPipeline.GetManagedPlayerDllPaths();
            List<string> referencingAssemblies = new List<string>();

            foreach (string assemblyPath in assemblyPaths)
            {
                DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();
                resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));
                AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { AssemblyResolver = resolver });

                foreach (AssemblyNameReference anr in assembly.MainModule.AssemblyReferences)
                {
                    if (anr.FullName == assemblyRoot.Name.FullName)
                    {
                        referencingAssemblies.Add(assemblyPath);
                    }
                }
            }

            return referencingAssemblies.ToArray();
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string[] GetManagedPlayerDllPaths () ;

}

}
