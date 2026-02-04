// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor.Build.Player;
using UnityEditor.Compilation;
using UnityEditor.MSBuild;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Scripting.ScriptCompilation.MsBuild;

class UnityEditorMSBuildPropsTargetsGeneration
{
    public static void UpdateGeneratedMSBuildFileIfNeeded(BuildTarget buildTarget, MSBuildCompilationOptions compilationOptions)
    {
        ProjectGenerator.Instance.GenerateEntryPointProjectIfMissing("Main");
        var unityNugetLocalFeed = Path.Combine(EditorApplication.applicationScriptingPath, "MSBuild/sdk-nugets");
        ProjectGenerator.Instance.MaintainGlobalJson("1.0.0", "global.json");
        ProjectGenerator.Instance.MaintainNugetConfig(unityNugetLocalFeed, "NuGet.config");

        UpdateUnityEditorVersion();
        UpdateDefinesProps(buildTarget, compilationOptions);
        UpdateReferencesProps(buildTarget);
        UpdatePluginsProps(buildTarget);
        UpdateSystemSearchPaths(buildTarget);

        var optimization = CompilationPipeline.codeOptimization;
        PropsGenerator.Instance.UpdateUnityContentLocation(EditorApplication.applicationScriptingPath, buildTarget.ToString(), GetCurrentDotNETRuntimeId(), optimization == CodeOptimization.Release);
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

        var assemblies = precompiledAssemblyProvider.GetPrecompiledAssemblies((isEditor ? EditorScriptCompilationOptions.BuildingForEditor : EditorScriptCompilationOptions.BuildingEmpty) | EditorScriptCompilationOptions.BuildingIncludingTestAssemblies, buildTarget, Array.Empty<string>());

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
            scriptCompilationOptions | EditorScriptCompilationOptions.BuildingForEditor;
        var editorApiCompatibility =
            PlayerSettings.EditorAssemblyCompatibilityToApiCompatibility(PlayerSettings
                .GetEditorAssembliesCompatibilityLevel());
        var editorOnlyCompatibleDefines = InternalEditorUtility.GetCompilationDefines(
            editorScriptCompilationOptions, buildTarget, subtarget, editorApiCompatibility);

        var playerAssembliesDefines = InternalEditorUtility.GetCompilationDefines(
            scriptCompilationOptions, buildTarget, subtarget, editorApiCompatibility);
        PropsGenerator.Instance.UpdateDefinesProps(editorOnlyCompatibleDefines, playerAssembliesDefines);
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

        return editorScriptCompilationOptions;
    }

    private static string[] GetModulesAssemblyPaths(bool isEditor, BuildTarget buildTarget)
    {
        var precompiledAssemblyProvider = new PrecompiledAssemblyProvider();
        var modulePaths = new List<string>();
        foreach (var assembly in precompiledAssemblyProvider.GetUnityAssemblies(isEditor, buildTarget))
        {
            if ((assembly.Flags & AssemblyFlags.UnityModule) == AssemblyFlags.UnityModule && !string.IsNullOrEmpty(assembly.Path))
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
