// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Scripting.ScriptCompilation.MsBuild;

namespace Unity.ProjectAuditor.Editor.Utils
{
    // TEMP: this is for tests that have intentionally different behaviours. delete once trunk migrates to MSBuild.
    internal static class MsBuildPipeline
    {
        public const string NoIlAnalysis = "IL analysis does not run with the MSBuild compilation pipeline";
        public const string NoAssemblyBuilder = "AssemblyBuilder compilation is not used with the MSBuild compilation pipeline";
        public const string NoCompilationPipelineAssemblies = "CompilationPipeline.GetAssemblies is not populated with the MSBuild compilation pipeline";
        public const string NoEmptyDefaultAssembly = "Assembly-CSharp is not built by the MSBuild compilation pipeline when the project has no scripts outside an asmdef";

        public static bool IsEnabled => MsBuildCompilationInterface.IsEnabled();
    }
}
