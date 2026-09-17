// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Unity.AsmDefToCSProj;
using UnityEditor.Build.Player;
using UnityEditor.Compilation;
using UnityEditor.MSBuild;
using UnityEditorInternal;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Scripting.ScriptCompilation.MsBuild;

class UnityEditorMSBuildPropsTargetsGeneration
{
    // Session-scoped cache for PrecompiledAssemblyProvider results to avoid repeated queries
    [NoAutoStaticsCleanup] // session-scoped cache of plugin/module paths (string data only), safe to persist across reload
    private static PrecompiledAssemblyProviderCache s_assemblyProviderCache;
    [NoAutoStaticsCleanup] // value-type cache key paired with s_assemblyProviderCache, safe to persist across reload
    private static BuildTarget s_cachedBuildTarget;
    [NoAutoStaticsCleanup] // lock object, safe to persist across reload
    private static readonly object s_cacheLock = new object();

    private class PrecompiledAssemblyProviderCache
    {
        public List<string> EditorPluginPaths { get; set; }
        public List<string> PlayerPluginPaths { get; set; }
        public HashSet<string> AllPlugins { get; set; }
        public string[] EditorModulePaths { get; set; }
        public string[] PlayerModulePaths { get; set; }
    }

    private static PrecompiledAssemblyProviderCache GetOrCreateCache(BuildTarget buildTarget)
    {
        lock (s_cacheLock)
        {
            if (s_assemblyProviderCache != null && s_cachedBuildTarget == buildTarget)
            {
                return s_assemblyProviderCache;
            }

            var cache = new PrecompiledAssemblyProviderCache();

            // Cache plugin paths
            cache.EditorPluginPaths = GetPluginsAssemblyPaths(true, buildTarget);
            for (int i = 0; i < cache.EditorPluginPaths.Count; i++)
            {
                cache.EditorPluginPaths[i] = Path.GetFullPath(FileUtil.GetPhysicalPath(cache.EditorPluginPaths[i]));
            }

            cache.PlayerPluginPaths = GetPluginsAssemblyPaths(false, buildTarget);
            for (int i = 0; i < cache.PlayerPluginPaths.Count; i++)
            {
                cache.PlayerPluginPaths[i] = Path.GetFullPath(FileUtil.GetPhysicalPath(cache.PlayerPluginPaths[i]));
            }

            cache.AllPlugins = new HashSet<string>();
            foreach (var plugin in GetAllPlugins())
            {
                cache.AllPlugins.Add(Path.GetFullPath(FileUtil.GetPhysicalPath(plugin)));
            }

            // Cache module paths
            cache.EditorModulePaths = GetModulesAssemblyPaths(true, buildTarget);
            cache.PlayerModulePaths = GetModulesAssemblyPaths(false, buildTarget);

            s_assemblyProviderCache = cache;
            s_cachedBuildTarget = buildTarget;
            return cache;
        }
    }

    public static void InvalidateCache()
    {
        lock (s_cacheLock)
        {
            s_assemblyProviderCache = null;
        }
    }

    public static void UpdateGeneratedMSBuildFileIfNeeded(BuildTarget buildTarget, MSBuildCompilationOptions compilationOptions, string[] globalAnalyzers)
    {
        ProjectGenerator.Instance.GenerateEntryPointProjectIfMissing("Main");
        var unityNugetLocalFeed = Path.Combine(EditorApplication.applicationScriptingPath, "MSBuild/sdk-nugets");
        ProjectGenerator.Instance.MaintainGlobalJson(GetUnitySdkVersion(unityNugetLocalFeed), GetDotnetRuntimeVersion(), "global.json");
        ProjectGenerator.Instance.MaintainNugetConfig(unityNugetLocalFeed, "NuGet.config");

        UpdateUnityEditorVersion();
        UpdateDefinesProps(buildTarget, compilationOptions);
        UpdateReferencesProps(buildTarget);
        UpdatePluginsProps(buildTarget);
        UpdateSystemSearchPaths(buildTarget);
        UpdateRoslynAnalyzersProps(globalAnalyzers);

        PropsGenerator.Instance.UpdateUnityContentLocation(EditorApplication.applicationScriptingPath, EditorApplication.applicationBuildPipelinePath);
        PropsGenerator.Instance.UpdateBuildConfigurationProperties(buildTarget.ToString(), CompilationPipeline.codeOptimization == CodeOptimization.Release);
    }

    private static void UpdateRoslynAnalyzersProps(string[] globalAnalyzers)
    {
        PropsGenerator.Instance.UpdateRoslynAnalyzersProps(globalAnalyzers);
    }

    [NoAutoStaticsCleanup] // built-in analyzer paths resolved from the fixed editor install location, safe to persist across reload
    private static string[] s_builtinAnalyzerPaths;

    // Editor-shipped built-in analyzers/generators live under the editor install, outside the project, so
    // the converter can't discover them; read them from the native registry (cheap, not the asmdef graph)
    // and isolate the editor-shipped ones by their install location. They are fixed for the editor session,
    // so resolve once and cache.
    public static string[] GetBuiltinRoslynAnalyzerPaths()
    {
        if (s_builtinAnalyzerPaths != null)
            return s_builtinAnalyzerPaths;

        var contentsPath = Path.GetFullPath(EditorApplication.applicationContentsPath);
        var all = new PrecompiledAssemblyProvider().GetRoslynAnalyzerPaths();
        var builtins = new List<string>();
        foreach (var path in all)
        {
            if (string.IsNullOrEmpty(path))
                continue;
            if (Path.GetFullPath(path).StartsWith(contentsPath, StringComparison.OrdinalIgnoreCase))
                builtins.Add(path);
        }
        return s_builtinAnalyzerPaths = builtins.ToArray();
    }

    /// <summary>
    /// Updates only essential props required for initial graph evaluation.
    /// Defers expensive plugin/reference props generation to restore phase.
    /// </summary>
    public static void UpdateEssentialPropsOnly(BuildTarget buildTarget)
    {
        ProjectGenerator.Instance.GenerateEntryPointProjectIfMissing("Main");
        var unityNugetLocalFeed = Path.Combine(EditorApplication.applicationScriptingPath, "MSBuild/sdk-nugets");
        ProjectGenerator.Instance.MaintainGlobalJson(GetUnitySdkVersion(unityNugetLocalFeed), GetDotnetRuntimeVersion(), "global.json");
        ProjectGenerator.Instance.MaintainNugetConfig(unityNugetLocalFeed, "NuGet.config");

        var optimization = CompilationPipeline.codeOptimization;
        var editorVersion = Application.unityVersion;

        PropsGenerator.Instance.UpdateEssentialPropsOnly(
            EditorApplication.applicationScriptingPath,
            EditorApplication.applicationBuildPipelinePath,
            editorVersion);
    }

    /// <summary>
    /// Updates deferrable props (defines, references, plugins, search paths) in parallel.
    /// Uses cached PrecompiledAssemblyProvider results to avoid repeated queries.
    /// </summary>
    public static void UpdateDeferrablePropsInParallel(BuildTarget buildTarget, MSBuildCompilationOptions compilationOptions, string[] globalAnalyzers)
    {
        var cache = GetOrCreateCache(buildTarget);

        // Get defines
        var subtarget = EditorUserBuildSettings.GetActiveSubtargetFor(buildTarget);
        var scriptCompilationOptions = MapMSBuildCompilationOptions(compilationOptions);

        var editorScriptCompilationOptions =
            scriptCompilationOptions | EditorScriptCompilationOptions.BuildingForEditor;
        var editorApiCompatibility =
            PlayerSettings.EditorAssemblyCompatibilityToApiCompatibility(PlayerSettings
                .GetEditorAssembliesCompatibilityLevel());
        var editorOnlyCompatibleDefines = AppendCSharpVersionDefines(InternalEditorUtility.GetCompilationDefines(
            editorScriptCompilationOptions, buildTarget, subtarget, editorApiCompatibility));

        var playerAssembliesDefines = AppendCSharpVersionDefines(InternalEditorUtility.GetCompilationDefines(
            scriptCompilationOptions, buildTarget, subtarget, editorApiCompatibility));

        // Get search paths
        var searchPaths = BuildPlayerDataGenerator.GetStaticSearchPaths(buildTarget);

        // `globalAnalyzers` was already resolved by the converter during SetAllScripts; nothing to do here.
        PropsGenerator.Instance.UpdateDeferrablePropsInParallel(
            editorOnlyCompatibleDefines,
            playerAssembliesDefines,
            cache.EditorModulePaths,
            cache.PlayerModulePaths,
            cache.EditorPluginPaths,
            cache.PlayerPluginPaths,
            cache.AllPlugins,
            searchPaths,
            globalAnalyzers,
            buildTarget.ToString(),
            CompilationPipeline.codeOptimization == CodeOptimization.Release);
    }

    private static string GetCurrentDotNETRuntimeId()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.Arm64:
                    return "win-arm64";
                case Architecture.X64:
                    return "win-x64";
                default:
                    throw new NotSupportedException($"Unsupported architecture {RuntimeInformation.ProcessArchitecture} on Windows");
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux-x64";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.Arm64:
                    return "osx-arm64";
                case Architecture.X64:
                    return "osx-x64";
                default:
                    throw new NotSupportedException($"Unsupported architecture {RuntimeInformation.ProcessArchitecture} on macOS");
            }
        }

        throw new NotSupportedException($"Unsupported OS platform {RuntimeInformation.OSDescription}");
    }

    [NoAutoStaticsCleanup] // cached Unity SDK version string (fixed per install), safe to persist across reload
    private static string s_cachedUnitySdkVersion;

    // Reads the version from Unity.Sdk.<version>.nupkg so global.json tracks CI's bumped value
    // (in sdk/UnitySdksCommon.props) instead of a hardcoded literal.
    private static string GetUnitySdkVersion(string sdkNugetsPath)
    {
        if (s_cachedUnitySdkVersion != null)
            return s_cachedUnitySdkVersion;

        const string prefix = "Unity.Sdk.";
        const string suffix = ".nupkg";

        if (!Directory.Exists(sdkNugetsPath))
            throw new DirectoryNotFoundException($"Unity SDK nuget feed not found at {sdkNugetsPath}");

        foreach (var file in Directory.EnumerateFiles(sdkNugetsPath, $"{prefix}*{suffix}"))
        {
            var name = Path.GetFileName(file);
            if (name.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase))
                continue;

            var candidate = name.Substring(prefix.Length, name.Length - prefix.Length - suffix.Length);
            if (candidate.Length == 0 || !char.IsDigit(candidate[0]))
                continue;

            s_cachedUnitySdkVersion = candidate;
            return candidate;
        }

        throw new FileNotFoundException($"No {prefix}*{suffix} package found in {sdkNugetsPath}");
    }

    [NoAutoStaticsCleanup] // cached .NET SDK version string (fixed per install), safe to persist across reload
    private static string s_cachedDotnetRuntimeVersion;

    // The .NET SDK version written to the generated global.json (sdk.version) must match the .NET SDK
    // Unity ships. Read it from the SDK's authoritative ".version" metadata (the version is on the
    // second line) rather than trusting the folder name, so a .NET upgrade (8 -> 10 -> ...) needs no
    // change here. We return the full version (e.g. "10.0.102"), preserving minor/patch, and pick the
    // highest if more than one SDK is present.
    private static string GetDotnetRuntimeVersion()
    {
        if (s_cachedDotnetRuntimeVersion != null)
            return s_cachedDotnetRuntimeVersion;

        var sdkRoot = Path.Combine(EditorApplication.applicationScriptingPath, "DotNetSdk", "sdk");
        if (!Directory.Exists(sdkRoot))
            throw new DirectoryNotFoundException($"Bundled dotnet SDK not found at {sdkRoot}");

        string bestVersion = null;
        Version bestParsed = null;
        foreach (var dir in Directory.EnumerateDirectories(sdkRoot))
        {
            // Each SDK folder ships a ".version" file whose second line is the SDK version, e.g.
            //   <commit hash>
            //   8.0.318
            //   <rid>
            var versionFile = Path.Combine(dir, ".version");
            if (!File.Exists(versionFile))
                continue;
            var lines = File.ReadAllLines(versionFile);
            if (lines.Length < 2)
                continue;
            var version = lines[1].Trim();
            if (version.Length == 0)
                continue;
            // Strip any "-preview.x" suffix for comparison, but return the full version string.
            var numeric = version;
            var dash = numeric.IndexOf('-');
            if (dash >= 0)
                numeric = numeric.Substring(0, dash);
            if (!Version.TryParse(numeric, out var parsed))
                continue;
            if (bestParsed == null || parsed > bestParsed)
            {
                bestParsed = parsed;
                bestVersion = version;
            }
        }

        if (bestVersion == null)
            throw new FileNotFoundException($"No SDK with a valid .version file found under {sdkRoot}");

        return s_cachedDotnetRuntimeVersion = bestVersion;
    }

    public static void UpdateInstallPathFile()
    {
        Directory.CreateDirectory(Path.Combine("Library", "MSBuild"));

        //This is used by the host to find certain important files thats part of the unity install
        File.WriteAllText(Path.Combine("Library", "MSBuild", "unitylocation.txt"), EditorApplication.applicationContentsPath);
    }

    private static void UpdateReferencesProps(BuildTarget buildTarget)
    {
        var editorBuildReferences = GetModulesAssemblyPaths(true, buildTarget);
        var playerBuildReferences = GetModulesAssemblyPaths(false, buildTarget);
        PropsGenerator.Instance.UpdateReferencesProps(editorBuildReferences, playerBuildReferences);
    }

    private static void UpdatePluginsProps(BuildTarget buildTarget)
    {
        var editorBuildReferences = GetPluginsAssemblyPaths(true, buildTarget);
        for (int i = 0; i < editorBuildReferences.Count; i++)
        {
            editorBuildReferences[i] = Path.GetFullPath(FileUtil.GetPhysicalPath(editorBuildReferences[i]));
        }

        var playerBuildReferences = GetPluginsAssemblyPaths(false, buildTarget);
        for (int i = 0; i < playerBuildReferences.Count; i++)
        {
            playerBuildReferences[i] = Path.GetFullPath(FileUtil.GetPhysicalPath(playerBuildReferences[i]));
        }

        var allPlugins = new HashSet<string>();
        foreach (var plugin in GetAllPlugins())
        {
            allPlugins.Add(Path.GetFullPath(FileUtil.GetPhysicalPath(plugin)));
        }

        PropsGenerator.Instance.UpdatePluginsProps(editorBuildReferences, playerBuildReferences, allPlugins);
    }

    private static void UpdateSystemSearchPaths(BuildTarget buildTarget)
    {
        var searchPaths = BuildPlayerDataGenerator.GetStaticSearchPaths(buildTarget);
        PropsGenerator.Instance.UpdateSearchPaths(searchPaths);
    }

    private static void UpdateUnityEditorVersion()
    {
        var editorVersion = Application.unityVersion;
        PropsGenerator.Instance.UpdateVersionProps(editorVersion);
    }

    private static List<string> GetPluginsAssemblyPaths(bool isEditor, BuildTarget buildTarget)
    {
        var precompiledAssemblyProvider = new PrecompiledAssemblyProvider();

        var editorOptions = EditorScriptCompilationOptions.BuildingForEditor | EditorScriptCompilationOptions.BuildingWithAsserts | EditorScriptCompilationOptions.BuildingWithInstrumentation;
        var assemblies = precompiledAssemblyProvider.GetPrecompiledAssemblies((isEditor ? editorOptions : EditorScriptCompilationOptions.BuildingEmpty) | EditorScriptCompilationOptions.BuildingIncludingTestAssemblies, buildTarget, Array.Empty<string>());

        var pluginPaths = new List<string>();
        foreach (var assembly in assemblies)
        {
            if ((assembly.Flags & AssemblyFlags.UserAssembly) == AssemblyFlags.UserAssembly)
            {
                pluginPaths.Add(assembly.Path);
            }
        }

        return pluginPaths;
    }

    private static List<string> GetAllPlugins()
    {
        var precompiledAssemblyProvider = new PrecompiledAssemblyProvider();

        var plugins = precompiledAssemblyProvider.GetAllPrecompiledAssemblies();

        var pluginPaths = new List<string>();
        foreach (var assembly in plugins)
        {
            if ((assembly.Flags & AssemblyFlags.UserAssembly) != AssemblyFlags.UserAssembly)
            {
                continue;
            }

            pluginPaths.Add(assembly.Path);
        }

        return pluginPaths;
    }

    private static void UpdateDefinesProps(BuildTarget buildTarget, MSBuildCompilationOptions compilationOptions)
    {
        var subtarget = EditorUserBuildSettings.GetActiveSubtargetFor(buildTarget);
        var scriptCompilationOptions = MapMSBuildCompilationOptions(compilationOptions);

        var editorScriptCompilationOptions =
            scriptCompilationOptions | EditorScriptCompilationOptions.BuildingForEditor | EditorScriptCompilationOptions.BuildingWithAsserts | EditorScriptCompilationOptions.BuildingWithInstrumentation;
        var editorApiCompatibility =
            PlayerSettings.EditorAssemblyCompatibilityToApiCompatibility(PlayerSettings
                .GetEditorAssembliesCompatibilityLevel());
        var editorOnlyCompatibleDefines = AppendCSharpVersionDefines(InternalEditorUtility.GetCompilationDefines(
            editorScriptCompilationOptions, buildTarget, subtarget, editorApiCompatibility));

        var playerAssembliesDefines = AppendCSharpVersionDefines(InternalEditorUtility.GetCompilationDefines(
            scriptCompilationOptions, buildTarget, subtarget, editorApiCompatibility));
        PropsGenerator.Instance.UpdateDefinesProps(editorOnlyCompatibleDefines, playerAssembliesDefines);
    }

    // Mirrors EditorBuildRules.GetScriptAssemblies, which appends the C# language-version
    // defines to every assembly. The legacy pipeline adds these per-assembly; MSBU emits them
    // into the shared DefineConstants props so all generated projects compile with them.
    private static string[] AppendCSharpVersionDefines(string[] defines)
    {
        var versionDefines = EditorBuildRules.s_CSharpVersionDefines;
        var result = new string[defines.Length + versionDefines.Length];
        Array.Copy(defines, result, defines.Length);
        Array.Copy(versionDefines, 0, result, defines.Length, versionDefines.Length);
        return result;
    }

    private static EditorScriptCompilationOptions MapMSBuildCompilationOptions(
        MSBuildCompilationOptions compilationOptions)
    {
        EditorScriptCompilationOptions editorScriptCompilationOptions = EditorScriptCompilationOptions.BuildingEmpty;

        if ((compilationOptions & MSBuildCompilationOptions.BuildingForIl2Cpp) == MSBuildCompilationOptions.BuildingForIl2Cpp)
        {
            editorScriptCompilationOptions |= EditorScriptCompilationOptions.BuildingForIl2Cpp;
        }
        if ((compilationOptions & MSBuildCompilationOptions.BuildingWithAsserts) == MSBuildCompilationOptions.BuildingWithAsserts)
        {
            editorScriptCompilationOptions |= EditorScriptCompilationOptions.BuildingWithAsserts;
        }
        if ((compilationOptions & MSBuildCompilationOptions.BuildingWithInstrumentation) == MSBuildCompilationOptions.BuildingWithInstrumentation)
        {
            editorScriptCompilationOptions |= EditorScriptCompilationOptions.BuildingWithInstrumentation;
        }
        if ((compilationOptions & MSBuildCompilationOptions.BuildingWithDebug) == MSBuildCompilationOptions.BuildingWithDebug)
        {
            editorScriptCompilationOptions |= EditorScriptCompilationOptions.BuildingWithoutOptimization;
        }

        return editorScriptCompilationOptions;
    }

    private static string[] GetModulesAssemblyPaths(bool isEditor, BuildTarget buildTarget)
    {
        var precompiledAssemblyProvider = new PrecompiledAssemblyProvider();
        var modulePaths = new List<string>();
        foreach (var assembly in precompiledAssemblyProvider.GetUnityAssemblies(isEditor, buildTarget))
        {
            if (string.IsNullOrEmpty(assembly.Path))
                continue;

            // For editor builds, include every Unity assembly (modules plus the base
            // UnityEngine.dll / UnityEditor.dll). The base UnityEditor.dll is required
            // for editor packages that use types like UnityEditor.Editor or types in
            // UnityEditor.Web (e.g. com.unity.analytics' editor scripts) — these are
            // type-forwarded from UnityEditor.dll, so the C# compiler emits CS0012 if
            // the forwarder assembly isn't referenced even when the forwardee module
            // is. Player builds keep the legacy module-only filter to avoid pulling
            // editor-only assemblies into player compilation. See MSBU-698.
            if (isEditor || (assembly.Flags & AssemblyFlags.UnityModule) == AssemblyFlags.UnityModule)
            {
                modulePaths.Add(assembly.Path);
            }
        }

        // EditorModules are also in the PrecompiledAssemblies list, so lest fetch them.
        // It only contains Editor platform extensions
        if (isEditor)
        {
            foreach (var assembly in precompiledAssemblyProvider.GetAllPrecompiledAssemblies())
            {
                if ((assembly.Flags & AssemblyFlags.UserAssembly) != AssemblyFlags.UserAssembly && !string.IsNullOrEmpty(assembly.Path))
                {
                    modulePaths.Add(assembly.Path);
                }
            }
        }

        return modulePaths.ToArray();
    }

}
