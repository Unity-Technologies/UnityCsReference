// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Modules;
using UnityEditor.Utils;
using UnityEditorInternal;
using UnityEngine;

internal abstract class DesktopStandalonePostProcessor : DefaultBuildPostprocessor
{
    protected DesktopStandalonePostProcessor()
    {
    }

    public override void PostProcess(BuildPostProcessArgs args)
    {
        SetupStagingArea(args);
        CopyStagingAreaIntoDestination(args);
        ProcessSymbolFiles(args);
    }

    public override bool SupportsLz4Compression()
    {
        return true;
    }

    public override void UpdateBootConfig(BuildTarget target, BootConfigData config, BuildOptions options)
    {
        base.UpdateBootConfig(target, config, options);

        if (PlayerSettings.forceSingleInstance)
            config.AddKey("single-instance");
        if (EditorApplication.scriptingRuntimeVersion == ScriptingRuntimeVersion.Latest)
            config.Set("scripting-runtime-version", "latest");
        if (IL2CPPUtils.UseIl2CppCodegenWithMonoBackend(BuildPipeline.GetBuildTargetGroup(target)))
            config.Set("mono-codegen", "il2cpp");
    }

    private void CopyNativePlugins(BuildPostProcessArgs args, out List<string> cppPlugins)
    {
        string buildTargetName = BuildPipeline.GetBuildTargetName(args.target);
        IPluginImporterExtension pluginImpExtension = new DesktopPluginImporterExtension();

        string pluginsFolder = GetStagingAreaPluginsFolder(args);
        string subDir32Bit = Path.Combine(pluginsFolder, "x86");
        string subDir64Bit = Path.Combine(pluginsFolder, "x86_64");

        bool haveCreatedPluginsFolder = false;
        bool haveCreatedSubDir32Bit = false;
        bool haveCreatedSubDir64Bit = false;
        cppPlugins = new List<string>();

        foreach (PluginImporter imp in PluginImporter.GetImporters(args.target))
        {
            BuildTarget t = args.target;

            // Skip .cpp files. They get copied to il2cpp output folder just before code compilation
            if (DesktopPluginImporterExtension.IsCppPluginFile(imp.assetPath))
            {
                cppPlugins.Add(imp.assetPath);
                continue;
            }

            // Skip managed DLLs.
            if (!imp.isNativePlugin)
                continue;

            // HACK: This should never happen.
            if (string.IsNullOrEmpty(imp.assetPath))
            {
                UnityEngine.Debug.LogWarning("Got empty plugin importer path for " + args.target.ToString());
                continue;
            }

            if (!haveCreatedPluginsFolder)
            {
                Directory.CreateDirectory(pluginsFolder);
                haveCreatedPluginsFolder = true;
            }

            bool isDirectory = Directory.Exists(imp.assetPath);
            string cpu = imp.GetPlatformData(t, "CPU");
            switch (cpu)
            {
                case "x86":
                    if (t == BuildTarget.StandaloneWindows64 ||
                        t == BuildTarget.StandaloneLinux64)
                    {
                        continue;
                    }
                    if (!haveCreatedSubDir32Bit)
                    {
                        Directory.CreateDirectory(subDir32Bit);
                        haveCreatedSubDir32Bit = true;
                    }
                    break;
                case "x86_64":
                    if (t != BuildTarget.StandaloneOSX &&
                        t != BuildTarget.StandaloneWindows64 &&
                        t != BuildTarget.StandaloneLinux64 &&
                        t != BuildTarget.StandaloneLinuxUniversal)
                    {
                        continue;
                    }
                    if (!haveCreatedSubDir64Bit)
                    {
                        Directory.CreateDirectory(subDir64Bit);
                        haveCreatedSubDir64Bit = true;
                    }
                    break;
                // This is a special case for CPU targets, means no valid CPU is selected
                case "None":
                    continue;
            }

            string destinationPath = pluginImpExtension.CalculateFinalPluginPath(buildTargetName, imp);
            if (string.IsNullOrEmpty(destinationPath))
                continue;

            string finalDestinationPath = Path.Combine(pluginsFolder, destinationPath);

            if (isDirectory)
            {
                FileUtil.CopyDirectoryRecursive(imp.assetPath, finalDestinationPath);
            }
            else
            {
                FileUtil.UnityFileCopy(imp.assetPath, finalDestinationPath);
            }
        }

        // TODO: Move all plugins using GetExtensionPlugins to GetImporters and remove GetExtensionPlugins
        foreach (UnityEditorInternal.PluginDesc pluginDesc in PluginImporter.GetExtensionPlugins(args.target))
        {
            if (!haveCreatedPluginsFolder)
            {
                Directory.CreateDirectory(pluginsFolder);
                haveCreatedPluginsFolder = true;
            }

            string pluginCopyPath = Path.Combine(pluginsFolder, Path.GetFileName(pluginDesc.pluginPath));

            // Plugins copied through GetImporters take priority, don't overwrite
            if (Directory.Exists(pluginCopyPath) || File.Exists(pluginCopyPath))
                continue;

            if (Directory.Exists(pluginDesc.pluginPath))
            {
                FileUtil.CopyDirectoryRecursive(pluginDesc.pluginPath, pluginCopyPath);
            }
            else
            {
                FileUtil.CopyFileIfExists(pluginDesc.pluginPath, pluginCopyPath, false);
            }
        }
    }

    private void CopyCppPlugins(string cppOutputDir, IEnumerable<string> cppPlugins)
    {
        foreach (var plugin in cppPlugins)
        {
            FileUtil.CopyFileOrDirectory(plugin, Path.Combine(cppOutputDir, Path.GetFileName(plugin)));
        }
    }

    private void SetupStagingArea(BuildPostProcessArgs args)
    {
        if (UseIl2Cpp && GetCreateSolution())
        {
            throw new Exception("CreateSolution is not supported with IL2CPP build");
        }

        Directory.CreateDirectory(args.stagingAreaData);

        List<string> cppPlugins;
        CopyNativePlugins(args, out cppPlugins);

        if (args.target == BuildTarget.StandaloneWindows ||
            args.target == BuildTarget.StandaloneWindows64)
        {
            CreateApplicationData(args);
        }

        PostprocessBuildPlayer.InstallStreamingAssets(args.stagingAreaData, args.report);

        if (UseIl2Cpp)
        {
            CopyVariationFolderIntoStagingArea(args);
            IL2CPPUtils.RunIl2Cpp(args.stagingAreaData, GetPlatformProvider(args), (cppOutputDir) => CopyCppPlugins(cppOutputDir, cppPlugins), args.usedClassRegistry);

            // Move GameAssembly next to game executable
            var il2cppOutputNativeDirectory = Path.Combine(args.stagingAreaData, "Native");
            var gameAssemblyDirectory = GetDirectoryForGameAssembly(args);
            foreach (var file in Directory.GetFiles(il2cppOutputNativeDirectory))
            {
                var fileName = Path.GetFileName(file);
                if (fileName.StartsWith("."))
                    continue; // Skip files starting with ., as they weren't output by our tools and potentially belong to the OS (like .DS_Store on macOS)

                FileUtil.MoveFileOrDirectory(file, Path.Combine(gameAssemblyDirectory, fileName));
            }

            if (PlaceIL2CPPSymbolMapNextToExecutable())
            {
                // Move symbol map to be next to game executable
                FileUtil.MoveFileOrDirectory(Paths.Combine(il2cppOutputNativeDirectory, "Data", "SymbolMap"), Path.Combine(args.stagingArea, "SymbolMap"));
            }

            // Move il2cpp data directory one directory up
            FileUtil.MoveFileOrDirectory(Path.Combine(il2cppOutputNativeDirectory, "Data"), Path.Combine(args.stagingAreaData, "il2cpp_data"));

            // Native directory is supposed to be empty at this point
            FileUtil.DeleteFileOrDirectory(il2cppOutputNativeDirectory);

            var dataBackupFolder = Path.Combine(args.stagingArea, GetIl2CppDataBackupFolderName(args));
            FileUtil.CreateOrCleanDirectory(dataBackupFolder);

            // Move generated C++ code out of Data directory
            FileUtil.MoveFileOrDirectory(Path.Combine(args.stagingAreaData, "il2cppOutput"), Path.Combine(dataBackupFolder, "il2cppOutput"));

            if (IL2CPPUtils.UseIl2CppCodegenWithMonoBackend(BuildPipeline.GetBuildTargetGroup(args.target)))
            {
                // Are we using IL2CPP code generation with the Mono runtime? If so, strip the assemblies so we can use them for metadata.
                StripAssembliesToLeaveOnlyMetadata(args.target, args.stagingAreaDataManaged);
            }
            else
            {
                // Otherwise, move them to temp data directory as il2cpp does not need managed assemblies to run
                FileUtil.MoveFileOrDirectory(args.stagingAreaDataManaged, Path.Combine(dataBackupFolder, "Managed"));
            }

            ProcessPlatformSpecificIL2CPPOutput(args);
        }

        if (GetInstallingIntoBuildsFolder(args))
        {
            CopyDataForBuildsFolder(args);
        }
        else
        {
            if (!UseIl2Cpp)
            {
                CopyVariationFolderIntoStagingArea(args);
            }

            if (GetCreateSolution())
            {
                CopyPlayerSolutionIntoStagingArea(args);
            }
            RenameFilesInStagingArea(args);
        }
    }

    static void StripAssembliesToLeaveOnlyMetadata(BuildTarget target, string stagingAreaDataManaged)
    {
        AssemblyReferenceChecker checker = new AssemblyReferenceChecker();
        checker.CollectReferences(stagingAreaDataManaged, true, 0.0f, false);

        EditorUtility.DisplayProgressBar("Removing bytecode from assemblies", "Stripping assemblies so that only metadata remains", 0.95F);
        MonoAssemblyStripping.MonoCilStrip(target, stagingAreaDataManaged, checker.GetAssemblyFileNames());
    }

    // Creates app.info which is used by Standalone player (when run in Low Integrity mode) when creating log file path at program start.
    // Log file path is created very early in the program execution, when none of the Unity managers are even created, that's why we can't get it from PlayerSettings
    // Note: In low integrity mode, we can only write to %USER PROFILE%\AppData\LocalLow on Windows
    protected void CreateApplicationData(BuildPostProcessArgs args)
    {
        File.WriteAllText(Path.Combine(args.stagingAreaData, "app.info"),
            string.Join("\n", new[]
        {
            args.companyName,
            args.productName
        }));
        args.report.RecordFileAdded(Path.Combine(args.stagingAreaData, "app.info"), CommonRoles.appInfo);
    }

    protected bool CopyPlayerFilter(string path, BuildPostProcessArgs args)
    {
        // Don't copy UnityEngine mdb files
        return Path.GetExtension(path) != ".mdb" || !Path.GetFileName(path).StartsWith("UnityEngine.");
    }

    protected virtual void CopyPlayerSolutionIntoStagingArea(BuildPostProcessArgs args)
    {
        throw new Exception("CreateSolution is not supported on " + BuildPipeline.GetBuildTargetName(args.target));
    }

    protected virtual void CopyVariationFolderIntoStagingArea(BuildPostProcessArgs args)
    {
        var playerFolder = args.playerPackage + "/Variations/" + GetVariationName(args);
        FileUtil.CopyDirectoryFiltered(playerFolder, args.stagingArea, true, f => CopyPlayerFilter(f, args), recursive: true);

        RecordCommonFiles(args, playerFolder, args.stagingAreaData);
    }

    protected static void RecordCommonFiles(BuildPostProcessArgs args, string variationSourceFolder, string monoFolderRoot)
    {
        // Mark all the managed DLLs in Data/Managed as engine API assemblies
        // Data/Managed may already contain managed DLLs in the UnityEngine.*.dll naming scheme from the extensions
        // So we find the files in the source Variations directory and mark the corresponding files in the output
        foreach (var file in Directory.GetFiles(Path.Combine(variationSourceFolder, "Data/Managed"), "*.dll"))
        {
            var filename = Path.GetFileName(file);
            if (!filename.StartsWith("UnityEngine"))
                continue;

            var targetFilePath = Path.Combine(args.stagingArea, "Data/Managed/" + filename);
            args.report.RecordFileAdded(targetFilePath, CommonRoles.managedEngineApi);
        }

        // Mark the default resources file
        args.report.RecordFileAdded(Path.Combine(args.stagingArea, "Data/Resources/unity default resources"),
            CommonRoles.builtInResources);

        // Mark up each mono runtime
        foreach (var monoName in new[] {"Mono", "MonoBleedingEdge"})
        {
            args.report.RecordFilesAddedRecursive(Path.Combine(monoFolderRoot, monoName + "/EmbedRuntime"),
                CommonRoles.monoRuntime);
            args.report.RecordFilesAddedRecursive(Path.Combine(monoFolderRoot, monoName + "/etc"),
                CommonRoles.monoConfig);
        }
    }

    private void CopyStagingAreaIntoDestination(BuildPostProcessArgs args)
    {
        if (GetInstallingIntoBuildsFolder(args))
        {
            string dst = Unsupported.GetBaseUnityDeveloperFolder() + "/" + GetDestinationFolderForInstallingIntoBuildsFolder(args);

            if (!Directory.Exists(Path.GetDirectoryName(dst)))
            {
                throw new Exception("Installing in builds folder failed because the player has not been built (You most likely want to enable 'Development build').");
            }

            FileUtil.CopyDirectoryFiltered(args.stagingAreaData, dst, true, f => true, recursive: true);
        }
        else
        {
            if (GetCreateSolution())
            {
                // TODO: smart overwrite
            }
            else
            {
                DeleteDestination(args);
            }

            // Copy entire stagingarea over
            FileUtil.CopyDirectoryFiltered(args.stagingArea, GetDestinationFolder(args), true, f => true, recursive: true);
            args.report.RecordFilesMoved(args.stagingArea, GetDestinationFolder(args));
        }
    }

    protected abstract string GetStagingAreaPluginsFolder(BuildPostProcessArgs args);

    protected abstract void DeleteDestination(BuildPostProcessArgs args);

    protected static string GetMonoFolderName(ScriptingRuntimeVersion scriptingRuntimeVersion)
    {
        switch (scriptingRuntimeVersion)
        {
            case ScriptingRuntimeVersion.Legacy:
                return "Mono";
            case ScriptingRuntimeVersion.Latest:
                return "MonoBleedingEdge";
            default:
                throw new ArgumentOutOfRangeException("scriptingRuntimeVersion", "Unknown scripting runtime version");
        }
    }

    protected void DeleteUnusedMono(string dataFolder, BuildReport report)
    {
        // Mono is built by the il2cpp builder, so we dont need the libs copied
        bool deleteBoth = IL2CPPUtils.UseIl2CppCodegenWithMonoBackend(BuildTargetGroup.Standalone);

        if (deleteBoth || EditorApplication.scriptingRuntimeVersion == ScriptingRuntimeVersion.Latest)
        {
            var monoPath = Path.Combine(dataFolder, GetMonoFolderName(ScriptingRuntimeVersion.Legacy));
            FileUtil.DeleteFileOrDirectory(monoPath);
            report.RecordFilesDeletedRecursive(monoPath);
        }
        if (deleteBoth || EditorApplication.scriptingRuntimeVersion == ScriptingRuntimeVersion.Legacy)
        {
            var monoPath = Path.Combine(dataFolder, GetMonoFolderName(ScriptingRuntimeVersion.Latest));
            FileUtil.DeleteFileOrDirectory(monoPath);
            report.RecordFilesDeletedRecursive(monoPath);
        }
    }

    protected abstract string GetDestinationFolderForInstallingIntoBuildsFolder(BuildPostProcessArgs args);

    protected abstract void CopyDataForBuildsFolder(BuildPostProcessArgs args);

    protected bool GetInstallingIntoBuildsFolder(BuildPostProcessArgs args)
    {
        return (args.options & BuildOptions.InstallInBuildFolder) != 0;
    }

    protected bool UseIl2Cpp
    {
        get
        {
            return PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone) == ScriptingImplementation.IL2CPP;
        }
    }

    protected virtual bool GetCreateSolution() { return false; }

    protected virtual string GetDestinationFolder(BuildPostProcessArgs args)
    {
        return FileUtil.UnityGetDirectoryName(args.installPath);
    }

    protected bool GetDevelopment(BuildPostProcessArgs args)
    {
        return ((args.options & BuildOptions.Development) != 0);
    }

    protected virtual string GetVariationName(BuildPostProcessArgs args)
    {
        return string.Format("{0}_{1}",
            PlatformStringFor(args.target),
            (GetDevelopment(args) ? "development" : "nondevelopment"));
    }

    protected abstract string PlatformStringFor(BuildTarget target);
    protected abstract void RenameFilesInStagingArea(BuildPostProcessArgs args);

    protected abstract IIl2CppPlatformProvider GetPlatformProvider(BuildPostProcessArgs args);

    protected virtual void ProcessPlatformSpecificIL2CPPOutput(BuildPostProcessArgs args)
    {
    }

    protected virtual void ProcessSymbolFiles(BuildPostProcessArgs args)
    {
    }

    protected string GetIl2CppDataBackupFolderName(BuildPostProcessArgs args)
    {
        return Path.GetFileNameWithoutExtension(args.installPath) + "_BackUpThisFolder_ButDontShipItWithYourGame";
    }

    protected virtual string GetDirectoryForGameAssembly(BuildPostProcessArgs args)
    {
        return args.stagingArea;
    }

    protected virtual bool PlaceIL2CPPSymbolMapNextToExecutable()
    {
        return true;
    }

    internal class ScriptingImplementations : DefaultScriptingImplementations
    {
    }
}
