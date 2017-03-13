// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.Scripting.ScriptCompilation;
using System.IO;

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
        public string FilenameSuffix { get; set; }

        public ScriptAssemblySettings()
        {
            BuildTarget = BuildTarget.NoTarget;
            BuildTargetGroup = BuildTargetGroup.Unknown;
        }
    }

    class ScriptAssembly
    {
        public BuildTarget BuildTarget { get; set; }
        public ApiCompatibilityLevel ApiCompatibilityLevel { get; set; }
        public string Filename { get; set; }
        public string OutputDirectory { get; set; }
        public ScriptAssembly[] ScriptAssemblyReferences { get; set; } // References to dependencies that will be built.
        public string[] References { get; set; } // References to dependencies that that will *not* be built.
        public string[] Defines { get; set; }
        public string[] Files { get; set; }
        public bool RunUpdater { get; set; }

        public string FullPath { get { return Path.Combine(OutputDirectory, Filename); } }

        public string GetExtensionOfSourceFiles()
        {
            return Files.Length > 0 ? Path.GetExtension(Files[0]).ToLower().Substring(1) : "NA";
        }

        public MonoIsland ToMonoIsland(BuildFlags buildFlags, string buildOutputDirectory)
        {
            bool buildingForEditor = (buildFlags & BuildFlags.BuildingForEditor) == BuildFlags.BuildingForEditor;
            bool developmentBuild = (buildFlags & BuildFlags.BuildingDevelopmentBuild) == BuildFlags.BuildingDevelopmentBuild;

            var references = ScriptAssemblyReferences.Select(a => Path.Combine(a.OutputDirectory, a.Filename));
            var referencesArray = references.Concat(References).ToArray();

            var outputPath = Path.Combine(buildOutputDirectory, Filename);

            return new MonoIsland(BuildTarget, buildingForEditor, developmentBuild, ApiCompatibilityLevel, Files, referencesArray, Defines, outputPath);
        }
    }
}
