// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace PlayerBuildProgramLibrary.Data
{
    public class Plugin
    {
        public string AssetPath;
        public string DestinationPath;
        public string Architecture;
        public string CompileFlags;
        public bool AddToEmbeddedBinaries;
        public override string ToString()
        {
            return $"'{AssetPath} -> '{DestinationPath}' ({Architecture})";
        }
    }

    public class PluginsData
    {
        public Plugin[] Plugins = new Plugin[0];
    }

    public class GenerateNativePluginsForAssembliesArgs
    {
        public string PluginOutputFolder;
        public string SymbolOutputFolder;
        public string[] Assemblies;
    }

    public class GenerateNativePluginsForAssembliesSettings
    {
        public bool HasCallback;
        public string DisplayName;
        public string[] AdditionalInputFiles = new string[0];
    }

    public class PlayerBuildConfig
    {
        public string DestinationPath;
        public string StagingArea;
        public string DataFolder;
        public string CompanyName;
        public string ProductName;
        public string PlayerPackage;
        public string ApplicationIdentifier;
        public string Architecture;
        public ScriptingBackend ScriptingBackend;
        public bool NoGUID;
        public bool InstallIntoBuildsFolder;
        public bool GenerateIdeProject;
        public bool Development;
        public bool UseNewInputSystem;
        public GenerateNativePluginsForAssembliesSettings GenerateNativePluginsForAssembliesSettings;
        public Services Services;
        public string[] ManagedAssemblies;
        public StreamingAssetsFile[] StreamingAssetsFiles;
    }

    public class BuiltFilesOutput
    {
        public string[] Files = new string[0];
        public string BootConfigArtifact;
    }

    public class LinkerConfig
    {
        public string[] LinkXmlFiles = new string[0];
        public string[] AssembliesToProcess = new string[0];
        public string EditorToLinkerData;
        public string Runtime;
        public string Profile;
        public string Ruleset;
        public string ModulesAssetPath;
        public string[] AdditionalArgs = new string[0];
        public bool AllowDebugging;
        public bool PerformEngineStripping;
    }

    public class Il2CppConfig
    {
        public bool EnableDeepProfilingSupport;
        public bool EnableFullGenericSharing;
        public string Profile;
        public string[] IDEProjectDefines;

        public string ConfigurationName;
        public bool GcWBarrierValidation;
        public bool GcIncremental;

        public string[] AdditionalCppFiles = new string[0];
        public string[] AdditionalArgs = new string[0];
        public string CompilerFlags;
        public string[] AdditionalLibraries;
        public string[] AdditionalDefines;
        public string[] AdditionalIncludeDirectories;
        public string[] AdditionalLinkDirectories;
        public string LinkerFlags;
        public string LinkerFlagsFile;
        public string ExtraTypes;
        public bool CreateSymbolFiles;
        public bool AllowDebugging;
        public string SysRootPath;
        public string ToolChainPath;
        public string RelativeDataPath;
        public bool GenerateUsymFile;
        public string UsymtoolPath;
    }

    public class Services
    {
        public bool EnableUnityConnect;
        public bool EnablePerformanceReporting;
        public bool EnableAnalytics;
        public bool EnableCrashReporting;
    }

    public class StreamingAssetsFile
    {
        public string File;
        public string RelativePath;
    }

    public enum ScriptingBackend
    {
        Mono,
        IL2CPP,
        CoreCLR,
    }
}
