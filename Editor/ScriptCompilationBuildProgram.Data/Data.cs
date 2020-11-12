// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace ScriptCompilationBuildProgram.Data
{
    public static class Constants
    {
        public const string ScriptAssembliesTarget = "ScriptAssemblies";
        public const string MovedFromExtension = "movedfrom";
    }

    public class PackageInfo
    {
        public string name;
        public string resolvedpath;
        public bool immutable;
    }

    public class ScriptCompilationData
    {
        public AssemblyData[] assemblies;
        public AssemblyData[] codegenAssemblies;
        public string cscPath;
        public string movedFromExtractorPath;
        public string netcorerunPath;
        public string outputdirectory;
        public bool debug;
        public string buildTarget;
        public PackageInfo[] packages;
    }

    public class AssemblyData
    {
        public string name;
        public string[] sourceFiles;
        public string[] defines;
        public string[] prebuiltReferences;
        public int[] references;
        public bool allowUnsafeCode;
        public string ruleSet;
        public string languageVersion;
        public bool useDeterministicCompilation;
        public string[] analyzers;
        public string asmdef;
        public string[] bclDirectories;
        public string[] customCompilerOptions;
        public int debugIndex;
    }

    public class ConfigurationData
    {
        public string editorContentsPath;
    }

    public class ScriptCompilationData_Out
    {
        public AssemblyData_Out[] assemblies;
    }

    public class AssemblyData_Out
    {
        public string path;
        public string scriptUpdaterRsp;
        public string movedFromExtractorFile;
        public bool sourcesAreInsideProjectFolder;
    }
}
