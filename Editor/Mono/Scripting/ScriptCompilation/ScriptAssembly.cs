// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil.Cil;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Compilation;
using UnityEditor.Modules;

namespace UnityEditor.Scripting.ScriptCompilation
{
    // Settings that would be common for a group of ScriptAssembly's created for the same build target.
    class ScriptAssemblySettings
    {
        public BuildTarget BuildTarget { get; set; }
        public BuildTargetGroup BuildTargetGroup { get; set; }
        public string OutputDirectory { get; set; }
        public EditorScriptCompilationOptions CompilationOptions { get; set; }
        public ScriptCompilerOptions PredefinedAssembliesCompilerOptions { get; set; }
        public string[] ExtraGeneralDefines { get; set; }
        public string[] AdditionalCompilerArguments { get; set; }
        public ICompilationExtension CompilationExtension { get; set; }
        public string ProjectRootNamespace { get; set; }
        public string ProjectDirectory { get; set; } = ".";

        public CodeOptimization EditorCodeOptimization { get; set; }

        public ScriptAssemblySettings()
        {
            BuildTarget = BuildTarget.NoTarget;
            BuildTargetGroup = BuildTargetGroup.Unknown;
            PredefinedAssembliesCompilerOptions = new ScriptCompilerOptions();
            ExtraGeneralDefines = new string[0];
            AdditionalCompilerArguments = new string[0];
        }

        public bool BuildingForEditor
        {
            get { return (CompilationOptions & EditorScriptCompilationOptions.BuildingForEditor) == EditorScriptCompilationOptions.BuildingForEditor; }
        }

        public bool BuildingDevelopmentBuild
        {
            get { return (CompilationOptions & EditorScriptCompilationOptions.BuildingDevelopmentBuild) == EditorScriptCompilationOptions.BuildingDevelopmentBuild; }
        }

        public CodeOptimization CodeOptimization
        {
            get { return BuildingForEditor ? EditorCodeOptimization : BuildingDevelopmentBuild? CodeOptimization.Debug : CodeOptimization.Release; }
        }

        public bool BuildingWithoutScriptUpdater
        {
            get { return (CompilationOptions & EditorScriptCompilationOptions.BuildingWithoutScriptUpdater) == EditorScriptCompilationOptions.BuildingWithoutScriptUpdater; }
        }
    }

    [DebuggerDisplay("{Filename}")]
    class ScriptAssembly
    {
        public string OriginPath { get; set; }
        public AssemblyFlags Flags { get; set; }
        public BuildTarget BuildTarget { get; set; }
        public string Filename { get; set; }
        public string OutputDirectory { get; set; }

        /// <summary>
        /// References to dependencies that will be built.
        /// </summary>
        public ScriptAssembly[] ScriptAssemblyReferences { get; set; }

        /// <summary>
        ///References to dependencies that that will *not* be built.
        /// </summary>
        public string[] References { get; set; }
        public string[] Defines { get; set; }
        public string[] Files { get; set; }
        public string RootNamespace { get; set; }
        public ScriptCompilerOptions CompilerOptions { get; set; }
        public string GeneratedResponseFile { get; set; }
        // Indicates whether the assembly had compile errors on last compilation
        public bool HasCompileErrors { get; set; }
        internal TargetAssemblyType TargetAssemblyType { get; set; }
        public string AsmDefPath { get; set; }

        public ScriptAssembly()
        {
            CompilerOptions = new ScriptCompilerOptions();
        }

        public string FullPath { get { return AssetPath.Combine(OutputDirectory, Filename); } }

        public string[] GetAllReferences()
        {
            return References.Concat(ScriptAssemblyReferences.Select(a => a.FullPath)).ToArray();
        }

        public IEnumerable<ScriptAssembly> AllRecursiveScripAssemblyReferencesIncludingSelf() =>
            ScriptAssemblyReferences
                .SelectMany(a => a.AllRecursiveScripAssemblyReferencesIncludingSelf())
                .Concat(new[] {this});

        public MonoIsland ToMonoIsland(EditorScriptCompilationOptions options, string buildOutputDirectory, string projectPath = null)
        {
            bool buildingForEditor = (options & EditorScriptCompilationOptions.BuildingForEditor) == EditorScriptCompilationOptions.BuildingForEditor;
            bool developmentBuild = (options & EditorScriptCompilationOptions.BuildingDevelopmentBuild) == EditorScriptCompilationOptions.BuildingDevelopmentBuild;

            var references = ScriptAssemblyReferences.Select(a => AssetPath.Combine(a.OutputDirectory, a.Filename));

            var referencesArray = references.Concat(References).ToArray();

            var responseFileProvider = new MicrosoftCSharpResponseFileProvider();
            if (!string.IsNullOrEmpty(projectPath))
            {
                responseFileProvider.ProjectPath = projectPath;
            }

            List<string> reposeFiles = responseFileProvider?.Get(OriginPath) ?? new List<string>();

            var outputPath = AssetPath.Combine(buildOutputDirectory, Filename);

            return new MonoIsland(BuildTarget,
                buildingForEditor,
                developmentBuild,
                CompilerOptions.AllowUnsafeCode,
                CompilerOptions.ApiCompatibilityLevel,
                Files,
                referencesArray,
                Defines,
                outputPath,
                reposeFiles.ToArray());
        }

        public string[] GetResponseFiles()
        {
            return new MicrosoftCSharpResponseFileProvider().Get(OriginPath).ToArray();
        }
    }
}
