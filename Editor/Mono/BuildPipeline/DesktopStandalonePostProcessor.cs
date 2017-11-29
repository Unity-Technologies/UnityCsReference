// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor;
using UnityEditor.BuildReporting;
using UnityEditor.Modules;
using UnityEditor.Utils;
using UnityEditorInternal;
using UnityEngine;

internal abstract class DesktopStandalonePostProcessor : DefaultBuildPostprocessor
{
    protected BuildPostProcessArgs m_PostProcessArgs;

    protected DesktopStandalonePostProcessor()
    {
    }

    protected DesktopStandalonePostProcessor(BuildPostProcessArgs postProcessArgs)
    {
        m_PostProcessArgs = postProcessArgs;
    }

    public void PostProcess()
    {
        SetupStagingArea();

        CopyStagingAreaIntoDestination();
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

    private void CopyNativePlugins()
    {
        string buildTargetName = BuildPipeline.GetBuildTargetName(m_PostProcessArgs.target);
        IPluginImporterExtension pluginImpExtension = new DesktopPluginImporterExtension();

        string pluginsFolder = StagingAreaPluginsFolder;
        string subDir32Bit = Path.Combine(pluginsFolder, "x86");
        string subDir64Bit = Path.Combine(pluginsFolder, "x86_64");

        bool haveCreatedPluginsFolder = false;
        bool haveCreatedSubDir32Bit = false;
        bool haveCreatedSubDir64Bit = false;

        foreach (PluginImporter imp in PluginImporter.GetImporters(m_PostProcessArgs.target))
        {
            BuildTarget t = m_PostProcessArgs.target;

            // Skip managed DLLs.
            if (!imp.isNativePlugin)
                continue;

            // HACK: This should never happen.
            if (string.IsNullOrEmpty(imp.assetPath))
            {
                UnityEngine.Debug.LogWarning("Got empty plugin importer path for " + m_PostProcessArgs.target.ToString());
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
        foreach (UnityEditorInternal.PluginDesc pluginDesc in PluginImporter.GetExtensionPlugins(m_PostProcessArgs.target))
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

    protected virtual void SetupStagingArea()
    {
        Directory.CreateDirectory(DataFolder);

        CopyNativePlugins();

        if (m_PostProcessArgs.target == BuildTarget.StandaloneWindows ||
            m_PostProcessArgs.target == BuildTarget.StandaloneWindows64)
        {
            CreateApplicationData();
        }

        PostprocessBuildPlayer.InstallStreamingAssets(DataFolder, m_PostProcessArgs.report);

        if (UseIl2Cpp)
        {
            CopyVariationFolderIntoStagingArea();
            var stagingAreaDataDirectory = Path.Combine(StagingArea, "Data");
            IL2CPPUtils.RunIl2Cpp(stagingAreaDataDirectory, GetPlatformProvider(m_PostProcessArgs.target), null, m_PostProcessArgs.usedClassRegistry);

            // Move GameAssembly next to game executable
            var il2cppOutputNativeDirectory = Path.Combine(stagingAreaDataDirectory, "Native");
            var gameAssemblyDirectory = GetDirectoryForGameAssembly();
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
                FileUtil.MoveFileOrDirectory(Paths.Combine(il2cppOutputNativeDirectory, "Data", "SymbolMap"), Path.Combine(StagingArea, "SymbolMap"));
            }

            // Move il2cpp data directory one directory up
            FileUtil.MoveFileOrDirectory(Path.Combine(il2cppOutputNativeDirectory, "Data"), Path.Combine(stagingAreaDataDirectory, "il2cpp_data"));

            // Native directory is supposed to be empty at this point
            FileUtil.DeleteFileOrDirectory(il2cppOutputNativeDirectory);

            var dataBackupFolder = Path.Combine(StagingArea, GetIl2CppDataBackupFolderName());
            FileUtil.CreateOrCleanDirectory(dataBackupFolder);

            // Move generated C++ code out of Data directory
            FileUtil.MoveFileOrDirectory(Path.Combine(stagingAreaDataDirectory, "il2cppOutput"), Path.Combine(dataBackupFolder, "il2cppOutput"));

            var managedDataDirectory = Path.Combine(DataFolder, "Managed");
            if (IL2CPPUtils.UseIl2CppCodegenWithMonoBackend(BuildPipeline.GetBuildTargetGroup(m_PostProcessArgs.target)))
            {
                // Are we using IL2CPP code generation with the Mono runtime? If so, strip the assemblies so we can use them for metadata.
                StripAssembliesToLeaveOnlyMetadata(m_PostProcessArgs.target, managedDataDirectory);
            }
            else
            {
                // Otherwise, move them to temp data directory as il2cpp does not need managed assemblies to run
                FileUtil.MoveFileOrDirectory(Path.Combine(stagingAreaDataDirectory, "Managed"), Path.Combine(dataBackupFolder, "Managed"));
            }

            ProcessPlatformSpecificIL2CPPOutput(StagingArea);
        }

        if (InstallingIntoBuildsFolder)
        {
            CopyDataForBuildsFolder();
            return;
        }

        if (!UseIl2Cpp)
            CopyVariationFolderIntoStagingArea();

        RenameFilesInStagingArea();
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
    protected void CreateApplicationData()
    {
        File.WriteAllText(Path.Combine(DataFolder, "app.info"),
            string.Join("\n", new[]
        {
            m_PostProcessArgs.companyName,
            m_PostProcessArgs.productName
        }));
        m_PostProcessArgs.report.RecordFileAdded(Path.Combine(DataFolder, "app.info"), CommonRoles.appInfo);
    }

    protected virtual bool CopyFilter(string path)
    {
        // Don't copy UnityEngine mdb files
        return Path.GetExtension(path) != ".mdb" || !Path.GetFileName(path).StartsWith("UnityEngine.");
    }

    protected virtual void CopyVariationFolderIntoStagingArea()
    {
        var playerFolder = m_PostProcessArgs.playerPackage + "/Variations/" + GetVariationName();
        FileUtil.CopyDirectoryFiltered(playerFolder, StagingArea, true, f => CopyFilter(f), true);

        // Mark all the managed DLLs in Data/Managed as engine API assemblies
        // Data/Managed may already contain managed DLLs in the UnityEngine.*.dll naming scheme from the extensions
        // So we find the files in the source Variations directory and mark the corresponding files in the output
        foreach (var file in Directory.GetFiles(Path.Combine(playerFolder, "Data/Managed"), "*.dll"))
        {
            var filename = Path.GetFileName(file);
            if (!filename.StartsWith("UnityEngine"))
                continue;

            var targetFilePath = Path.Combine(StagingArea, "Data/Managed/" + filename);
            m_PostProcessArgs.report.RecordFileAdded(targetFilePath, CommonRoles.managedEngineApi);
        }

        // Mark the default resources file
        m_PostProcessArgs.report.RecordFileAdded(Path.Combine(StagingArea, "Data/Resources/unity default resources"), CommonRoles.builtInResources);

        // Mark up each mono runtime
        foreach (var monoName in new[] { "Mono", "MonoBleedingEdge" })
        {
            m_PostProcessArgs.report.RecordFilesAddedRecursive(Path.Combine(StagingArea, "Data/" + monoName + "/EmbedRuntime"), CommonRoles.monoRuntime);
            m_PostProcessArgs.report.RecordFilesAddedRecursive(Path.Combine(StagingArea, "Data/" + monoName + "/etc"), CommonRoles.monoConfig);
        }
    }

    protected void CopyStagingAreaIntoDestination()
    {
        if (InstallingIntoBuildsFolder)
        {
            string dst = Unsupported.GetBaseUnityDeveloperFolder() + "/" + DestinationFolderForInstallingIntoBuildsFolder;

            if (!Directory.Exists(Path.GetDirectoryName(dst)))
            {
                throw new Exception("Installing in builds folder failed because the player has not been built (You most likely want to enable 'Development build').");
            }


            FileUtil.CopyDirectoryFiltered(DataFolder, dst, true, f => true, true);
            return;
        }

        DeleteDestination();

        //copy entire stagingarea over
        FileUtil.CopyDirectoryFiltered(StagingArea, DestinationFolder, true, f => true, true);

        m_PostProcessArgs.report.RecordFilesMoved(StagingArea, DestinationFolder);
    }

    protected abstract string StagingAreaPluginsFolder { get; }

    protected abstract void DeleteDestination();

    protected void DeleteUnusedMono(string dataFolder)
    {
        // Mono is built by the il2cpp builder, so we dont need the libs copied
        bool deleteBoth = IL2CPPUtils.UseIl2CppCodegenWithMonoBackend(BuildTargetGroup.Standalone);

        if (deleteBoth || EditorApplication.scriptingRuntimeVersion == ScriptingRuntimeVersion.Latest)
        {
            FileUtil.DeleteFileOrDirectory(Path.Combine(dataFolder, "Mono"));
            m_PostProcessArgs.report.RecordFilesDeletedRecursive(Path.Combine(dataFolder, "Mono"));
        }
        if (deleteBoth || EditorApplication.scriptingRuntimeVersion == ScriptingRuntimeVersion.Legacy)
        {
            FileUtil.DeleteFileOrDirectory(Path.Combine(dataFolder, "MonoBleedingEdge"));
            m_PostProcessArgs.report.RecordFilesDeletedRecursive(Path.Combine(dataFolder, "MonoBleedingEdge"));
        }
    }

    protected abstract string DestinationFolderForInstallingIntoBuildsFolder { get; }

    protected abstract void CopyDataForBuildsFolder();

    protected bool InstallingIntoBuildsFolder
    {
        get { return (m_PostProcessArgs.options & BuildOptions.InstallInBuildFolder) != 0; }
    }

    protected bool UseIl2Cpp
    {
        get
        {
            return PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone) == ScriptingImplementation.IL2CPP;
        }
    }

    protected string StagingArea { get { return m_PostProcessArgs.stagingArea; } }

    protected string InstallPath { get { return m_PostProcessArgs.installPath; } }

    protected string DataFolder { get { return StagingArea + "/Data"; } }

    protected BuildTarget Target { get { return m_PostProcessArgs.target; } }

    protected virtual string DestinationFolder
    {
        get { return FileUtil.UnityGetDirectoryName(m_PostProcessArgs.installPath); }
    }

    protected bool Development
    {
        get { return ((m_PostProcessArgs.options & BuildOptions.Development) != 0); }
    }

    protected virtual string GetVariationName()
    {
        return string.Format("{0}_{1}",
            PlatformStringFor(m_PostProcessArgs.target),
            (Development ? "development" : "nondevelopment"));
    }

    protected abstract string PlatformStringFor(BuildTarget target);
    protected abstract void RenameFilesInStagingArea();

    protected abstract UnityEditorInternal.IIl2CppPlatformProvider GetPlatformProvider(BuildTarget target);

    protected virtual void ProcessPlatformSpecificIL2CPPOutput(string stagingArea)
    {
    }

    protected string GetIl2CppDataBackupFolderName()
    {
        return Path.GetFileNameWithoutExtension(m_PostProcessArgs.installPath) + "_BackUpThisFolder_ButDontShipItWithYourGame";
    }

    protected virtual string GetDirectoryForGameAssembly()
    {
        return StagingArea;
    }

    protected virtual bool PlaceIL2CPPSymbolMapNextToExecutable()
    {
        return true;
    }

    internal class ScriptingImplementations : IScriptingImplementations
    {
        public ScriptingImplementation[] Supported()
        {
            return new[] { ScriptingImplementation.Mono2x, ScriptingImplementation.IL2CPP };
        }

        public ScriptingImplementation[] Enabled()
        {
            return new[] { ScriptingImplementation.Mono2x, ScriptingImplementation.IL2CPP };
        }
    }
}
