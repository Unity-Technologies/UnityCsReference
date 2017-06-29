// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEditor.Scripting.Compilers;

namespace UnityEditor.Scripting.ScriptCompilation
{
    // Settings that would be common for a group of ScriptAssembly's created for the same build target.
    class ScriptAssemblySettings
    {
        public BuildTarget BuildTarget { get; set; }
        public BuildTargetGroup BuildTargetGroup { get; set; }
        public string OutputDirectory { get; set; }
        public string[] Defines { get; set; }
        public ApiCompatibilityLevel ApiCompatibilityLevel { get; set; }
        public EditorScriptCompilationOptions CompilationOptions { get; set; }
        public string FilenameSuffix { get; set; }

        public ScriptAssemblySettings()
        {
            BuildTarget = BuildTarget.NoTarget;
            BuildTargetGroup = BuildTargetGroup.Unknown;
        }

        public bool BuildingForEditor
        {
            get { return (CompilationOptions & EditorScriptCompilationOptions.BuildingForEditor) == EditorScriptCompilationOptions.BuildingForEditor; }
        }

        public bool BuildingDevelopmentBuild
        {
            get { return (CompilationOptions & EditorScriptCompilationOptions.BuildingDevelopmentBuild) == EditorScriptCompilationOptions.BuildingDevelopmentBuild; }
        }
    }

    class ScriptAssembly
    {
        public AssemblyFlags Flags { get; set; }
        public BuildTarget BuildTarget { get; set; }
        public SupportedLanguage Language { get; set; }
        public ApiCompatibilityLevel ApiCompatibilityLevel { get; set; }
        public string Filename { get; set; }
        public string OutputDirectory { get; set; }
        public ScriptAssembly[] ScriptAssemblyReferences { get; set; } // References to dependencies that will be built.
        public string[] References { get; set; } // References to dependencies that that will *not* be built.
        public string[] Defines { get; set; }
        public string[] Files { get; set; }
        public bool RunUpdater { get; set; }

        public string FullPath { get { return AssetPath.Combine(OutputDirectory, Filename); } }

        public string[] GetAllReferences()
        {
            return References.Concat(ScriptAssemblyReferences.Select(a => a.FullPath)).ToArray();
        }

        public MonoIsland ToMonoIsland(EditorScriptCompilationOptions options, string buildOutputDirectory)
        {
            bool buildingForEditor = (options & EditorScriptCompilationOptions.BuildingForEditor) == EditorScriptCompilationOptions.BuildingForEditor;
            bool developmentBuild = (options & EditorScriptCompilationOptions.BuildingDevelopmentBuild) == EditorScriptCompilationOptions.BuildingDevelopmentBuild;

            var references = ScriptAssemblyReferences.Select(a => AssetPath.Combine(a.OutputDirectory, a.Filename));
            var referencesArray = references.Concat(References).ToArray();

            var outputPath = AssetPath.Combine(buildOutputDirectory, Filename);

            return new MonoIsland(BuildTarget, buildingForEditor, developmentBuild, ApiCompatibilityLevel, Files, referencesArray, Defines, outputPath);
        }
    }
}
