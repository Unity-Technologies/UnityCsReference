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
        public string dotnetRoslynPath;
        public string dotnetRuntimePath;
        public string movedFromExtractorPath;
        public string netcorerunPath;
        public string outputdirectory;
        public bool debug;
        public string buildTarget;
        public string localization;
        public PackageInfo[] packages;
    }

    public class AssemblyData
    {
        public string name;
        public string[] sourceFiles = new string[0];
        public string[] defines = new string[0];
        public string[] prebuiltReferences = new string[0];
        public int[] references = new int[0];
        public bool allowUnsafeCode;
        public string ruleSet;
        public string languageVersion;
        public bool useDeterministicCompilation;
        public string[] analyzers = new string[0];
        public string asmdef;
        public string[] bclDirectories = new string[0];
        public string[] customCompilerOptions = new string[0];
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
    }
}
