// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.DeploymentTargets;
using UnityEngine;
using Mono.Cecil;

namespace UnityEditor.Modules
{
    internal interface IPlatformSupportModule
    {
        /// Returns name identifying a target, for ex., Metro, note this name should match prefix
        /// for extension module UnityEditor.Metro.Extensions.dll, UnityEditor.Metro.Extensions.Native.dll
        string TargetName { get; }

        /// Returns the filename of jam which should be executed when you're recompiling extensions
        /// from Editor using CTRL + L shortcut, for ex., WP8EditorExtensions, MetroEditorExtensions, etc
        string JamTarget { get; }

        /// Returns an array of native libraries that are required by the extension and must be loaded
        /// by the editor.
        ///
        /// NOTE: If two different platform extensions return a native library with a same file name
        /// (regardless of the path), then only first one will be loaded. This is due to the fact that
        /// some platforms may require same native library, but we must ship a copy with both platforms,
        /// since our modularization and platform installers don't support shared stuff.
        string[] NativeLibraries { get; }

        /// Returns an array of assemblies that should be referenced by user's scripts. These will be
        /// referenced by editor scripts, and game scripts running in editor. Used to export additional
        /// platform specific editor API.
        string[] AssemblyReferencesForUserScripts { get; }

        // Returns an array of assemblies that should be included into C# project as references.
        // This is different from AssemblyReferencesForUserScripts by that most assembly references
        // are internal and not added to the C# project. On the other hand, certain assemblies
        // contain public API, and thus should be added to C# project.
        string[] AssemblyReferencesForEditorCsharpProject { get; }

        /// A human friendly version (eg. an incrementing number on each release) of the platform extension. Null/Empty if none available
        string ExtensionVersion { get; }

        // Names of displays to show in GameView and Camera inspector if the platform supports multiple displays. Return null if default names should be used.
        GUIContent[] GetDisplayNames();

        IBuildPostprocessor CreateBuildPostprocessor();

        // Returns an instance of IDeploymentTargetsExtension or null if not supported
        IDeploymentTargetsExtension CreateDeploymentTargetsExtension();

        // Returns an instance of IScriptingImplementations or null if only one scripting backend is supported
        IScriptingImplementations CreateScriptingImplementations();

        // Return an instance of ISettingEditorExtension or null if not used
        // See DefaultPlayerSettingsEditorExtension.cs for abstract implementation
        ISettingEditorExtension CreateSettingsEditorExtension();

        // Return an instance of IPreferenceWindowExtension or null if not used
        IPreferenceWindowExtension CreatePreferenceWindowExtension();

        // Return an instance of IBuildWindowExtension or null if not used
        IBuildWindowExtension CreateBuildWindowExtension();

        ICompilationExtension CreateCompilationExtension();

        // Rather than null above, this returns a default extension if not used
        ITextureImportSettingsExtension CreateTextureImportSettingsExtension();

        IPluginImporterExtension CreatePluginImporterExtension();

        IBuildAnalyzer CreateBuildAnalyzer();

        // Return an instance of IUserAssembliesValidator or null if not used
        IUserAssembliesValidator CreateUserAssembliesValidatorExtension();

        IProjectGeneratorExtension CreateProjectGeneratorExtension();

        // Register platform specific Unity extensions
        // For ex., Metro specifc UnityEngine.Networking.dll which is different from the generic UnityEngine.Networking.dll
        void RegisterAdditionalUnityExtensions();

        // return valid object for this device. This ensures that API for certain operations is
        // still available even if device was removed, for example stopping remote support.
        IDevice CreateDevice(string id);

        // Called when build target supplied by this module is activated in the editor.
        //
        // NOTE: Keep in mind that due domain reloads and the way unity builds, calls on OnActive
        //     and OnDeactivate will be forced even if current build target isn't being changed.
        //
        // PERFORMANCE: This method will be called each time user starts the game, so use this
        //     only for lightweight code, like registering to events, etc.
        //
        // Currently (de)activation happens when:
        //     * User switches build target.
        //     * User runs build for current target.
        //     * Build is run through scripting API.
        //     * Scripts are recompiled and reloaded (due user's change, forced reimport, etc).
        //     * User clicks play in editor.
        //     * ... and possibly more I'm not aware of.
        void OnActivate();

        // Called when build target supplied by this module is deactivated in the editor.
        //
        // NOTE: Keep in mind that due domain reloads and the way unity builds, calls on OnActive
        //     and OnDeactivate will be forced even if current build target isn't being changed.
        //
        // PERFORMANCE: This method will be called each time user starts the game, so use this
        //     only for lightweight code, like unregistering from events, etc.
        //
        // For more info see OnActivate().
        void OnDeactivate();

        // Called when extension is loaded, on editor start or domain reload.
        //
        // PERFORMANCE: This will be called for all available platform extensions during each
        //     domain reload, including each time user starts the game, so use this only for
        //     lightweight code.
        void OnLoad();

        // Called when extension is unloaded, when editor is exited or before domain reload.
        //
        // PERFORMANCE: This will be called for all available platform extensions during each
        //     domain reload, including each time user starts the game, so use this only for
        //     lightweight code.
        void OnUnload();
    }

    internal interface IBuildPostprocessor
    {
        void LaunchPlayer(BuildLaunchPlayerArgs args);

        void PostProcess(BuildPostProcessArgs args);

        bool SupportsInstallInBuildFolder();

        bool SupportsLz4Compression();

        void PostProcessScriptsOnly(BuildPostProcessArgs args);

        bool SupportsScriptsOnlyBuild();

        // This is the place to make sure platform has everything it needs for the build.
        // Use EditorUtility.Display(Cancelable)ProgressBar when running long tasks (e.g. downloading SDK from internet).
        // Return non-empty string indicating error message to stop the build.
        string PrepareForBuild(BuildOptions options, BuildTarget target);

        void UpdateBootConfig(BuildTarget target, BootConfigData config, BuildOptions options);

        // Return string.Empty if targeting a folder.
        string GetExtension(BuildTarget target, BuildOptions options);
    }

    internal interface IScriptingImplementations
    {
        // All supported scripting implementations. First is the default one.
        ScriptingImplementation[] Supported();

        // Scripting implementations exposed to the user.
        ScriptingImplementation[] Enabled();
    }

    // Extension point to add/alter the SettingsEditorWindow class
    // If you are creating a new extension you should probably inherit from DefaultPlayerSettingsEditorExtension
    internal interface ISettingEditorExtension
    {
        void OnEnable(PlayerSettingsEditor settingsEditor);

        bool HasPublishSection();

        // Leave blank if no contribution
        void PublishSectionGUI(float h, float midWidth, float maxWidth);

        bool HasIdentificationGUI();

        // Leave blank if no contribution
        void IdentificationSectionGUI();

        // Leave blank if no contribution
        void ConfigurationSectionGUI();

        bool SupportsOrientation();

        bool SupportsStaticBatching();
        bool SupportsDynamicBatching();
        bool SupportsHighDynamicRangeDisplays();
        bool SupportsGfxJobModes();

        bool CanShowUnitySplashScreen();

        void SplashSectionGUI();

        bool UsesStandardIcons();

        void IconSectionGUI();

        bool HasResolutionSection();

        void ResolutionSectionGUI(float h, float midWidth, float maxWidth);

        bool HasBundleIdentifier();

        bool SupportsMultithreadedRendering();

        void MultithreadedRenderingGUI(BuildTargetGroup targetGroup);

        bool SupportsCustomLightmapEncoding();
    }


    // Extension point to add preferences to the PreferenceWindow class
    internal interface IPreferenceWindowExtension
    {
        // Called from PreferenceWindow whenever preferences should be read
        void ReadPreferences();

        // Called from PreferenceWindow whenever preferences should be written
        void WritePreferences();

        // True is this extension contributes an external application/tool preference(s)
        bool HasExternalApplications();

        // Called from OnGui - this function should draw any contributing UI components
        void ShowExternalApplications();
    }

    // NOTE: You probably want to inherit from DefaultBuildWindowExtension class
    internal interface IBuildWindowExtension
    {
        void ShowPlatformBuildOptions();

        // Use this for "developer" Unity builds
        void ShowInternalPlatformBuildOptions();

        bool EnabledBuildButton();

        bool EnabledBuildAndRunButton();

        bool ShouldDrawScriptDebuggingCheckbox();

        bool ShouldDrawProfilerCheckbox();

        bool ShouldDrawDevelopmentPlayerCheckbox();

        bool ShouldDrawExplicitNullCheckbox();

        bool ShouldDrawExplicitDivideByZeroCheckbox();

        // Force full optimisations for script complilation in Development builds.
        // Useful for forcing optimized compiler for IL2CPP when profiling.
        bool ShouldDrawForceOptimizeScriptsCheckbox();
    }

    internal interface IBuildAnalyzer
    {
        void OnAddedExecutable(BuildReporting.BuildReport report, int fileIndex);
    }

    // Extension point to add platform-specific texture import settings.
    // You probably want to inherit from DefaultTextureImportSettingsExtension
    internal interface ITextureImportSettingsExtension
    {
        void ShowImportSettings(Editor baseEditor, TextureImportPlatformSettings platformSettings);
    }

    // Interface for target device related operations
    internal interface IDevice
    {
        // Start remote support for this device
        RemoteAddress StartRemoteSupport();

        // Stop remote support for this device
        void StopRemoteSupport();

        // Start player connection support for this device. This only sets up ability to connect,
        // like setting up TCP tunneling over USB, getting remote device's IP, but it doesn't
        // actually make the connection. Only available if SupportsPlayerConnection is true.
        // Otherwise throws NotSupportedException.
        RemoteAddress StartPlayerConnectionSupport();

        // Stop player connection support for this device. Only available if SupportsPlayerConnection
        // is true. Otherwise throws NotSupportedException.
        void StopPlayerConnectionSupport();
    }

    internal struct RemoteAddress
    {
        public string ip;
        public int port;

        public RemoteAddress(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }
    }

    internal interface IPluginImporterExtension
    {
        // Functions use by PluginImporterInspector
        void ResetValues(PluginImporterInspector inspector);
        bool HasModified(PluginImporterInspector inspector);
        void Apply(PluginImporterInspector inspector);
        void OnEnable(PluginImporterInspector inspector);
        void OnDisable(PluginImporterInspector inspector);
        void OnPlatformSettingsGUI(PluginImporterInspector inspector);

        // Called before building the player, checks if plugins don't overwrite each other
        string CalculateFinalPluginPath(string buildTargetName, PluginImporter imp);
        bool CheckFileCollisions(string buildTargetName);
    }

    internal struct BuildLaunchPlayerArgs
    {
        public BuildTarget target;
        public string playerPackage;
        public string installPath;
        public string productName;
        public BuildOptions options;
        public BuildReporting.BuildReport report;
    }

    internal struct BuildPostProcessArgs
    {
        public BuildTarget target;
        public string stagingArea;
        public string stagingAreaData;
        public string stagingAreaDataManaged;
        public string playerPackage;
        public string installPath;
        public string companyName;
        public string productName;
        public Guid productGUID;
        public BuildOptions options;
        public BuildReporting.BuildReport report;
        internal RuntimeClassRegistry usedClassRegistry;
    }

    // An 'IUserAssembliesValidator' is responsible of validating the assemblies
    // generated after a successful recompilation of the scripts.
    // For performance reason, and to keep the Editor responsive, the validation is
    // allowed to run in background.
    internal interface IUserAssembliesValidator
    {
        // If true, it allow the Editor to run the validation in a background Thread
        bool canRunInBackground { get; }

        // Invoked by the Editor after the user assemblies have been rebuilt, to allow the
        // validator to validate the generated assemblies.
        //
        // This method is invoked only in case rebuilding the scripts was successful.
        // The method might be run in a separate thread in case canRunInBackground was true.
        void Validate(string[] userAssemblies);

        // Invoked by the Editor in case the validation has just been killed, usually due to
        // script rebuilds being triggered when the Validate method has not been terminated yet.
        void Cleanup();
    }

    internal enum CSharpCompiler
    {
        Mono,
        Microsoft,
    }

    internal interface ICompilationExtension
    {
        CSharpCompiler GetCsCompiler(bool buildingForEditor, string assemblyName);
        string[] GetCompilerExtraAssemblyPaths(bool isEditor, string assemblyPathName);
        IAssemblyResolver GetAssemblyResolver(bool buildingForEditor, string assemblyPath, string[] searchDirectories);

        // Returns an array of windows metadata files (.winmd) that should be referenced when compiling scripts.
        // Only WinRT based platforms need these references.
        IEnumerable<string> GetWindowsMetadataReferences();

        // Returns an array of managed assemblies that should be referenced when compiling scripts
        // Currently, only .NET scripting backend uses it to include WinRTLegacy.dll into compilation
        IEnumerable<string> GetAdditionalAssemblyReferences();

        // Returns an array of defines that should be used when compiling scripts
        IEnumerable<string> GetAdditionalDefines();

        // Returns an array of C# source files that should be included into the assembly when compiling scripts
        IEnumerable<string> GetAdditionalSourceFiles();
    }


    internal class CSharpProject
    {
        public string Path { get; set; }
        public Guid Guid { get; set; }
    }

    /// <summary>
    /// Generates platform dependent projects
    /// For ex., Windows Store Apps would generate Assembly-CSharp* projects targeting .NET Core which.
    /// Currently this interface is used by Visual Studio Unity Tools project generator, which includes these projects into their main solution
    /// </summary>
    internal interface IProjectGeneratorExtension
    {
        void GenerateCSharpProject(CSharpProject project, string assemblyName, IEnumerable<string> sourceFiles, IEnumerable<string> defines, IEnumerable<CSharpProject> additionalProjectReferences);
    }
}
