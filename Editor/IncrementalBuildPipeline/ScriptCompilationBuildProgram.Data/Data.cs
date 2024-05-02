// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace ScriptCompilationBuildProgram.Data
{
    public static class Constants
    {
        public const string ScriptAssembliesTarget = "ScriptAssemblies";
        public const string MovedFromExtension = "mvfrm";
    }

    public class ScriptCompilationData
    {
        public AssemblyData[] Assemblies;
        public AssemblyData[] CodegenAssemblies;
        public string DotnetRuntimePath;
        public string DotnetRoslynPath;
        public string MovedFromExtractorPath;
        public string OutputDirectory;
        public bool Debug;
        public string BuildTarget;
        public string Localization;
        public bool EnableDiagnostics;
    }

    public class AssemblyData
    {
        public string Name;
        public string[] SourceFiles = new string[0];
        public string[] Defines = new string[0];
        public string[] PrebuiltReferences = new string[0];
        public int[] References = new int[0];
        public bool AllowUnsafeCode;
        public string RuleSet;
        public string AnalyzerConfigPath;
        public string LanguageVersion;
        public bool UseDeterministicCompilation;
        public string[] Analyzers = new string[0];
        public string[] AdditionalFiles = new string[0];
        public string Asmdef;
        public string[] BclDirectories = new string[0];
        public string[] CustomCompilerOptions = new string[0];
        public int DebugIndex;
        public bool SkipCodeGen;
        public string Path;
    }

    public class ScriptCompilationData_Out
    {
        public AssemblyData_Out[] Assemblies;
    }

    public class AssemblyData_Out
    {
        public string Path;
        public string ScriptUpdaterRsp;
        public string MovedFromExtractorFile;
    }
}
