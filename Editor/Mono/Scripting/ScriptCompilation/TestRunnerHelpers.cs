// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.Modules;
using UnityEditor.Scripting.Compilers;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class TestRunnerHelpers
    {
        private const string k_NunitAssemblyName = "nunit.framework.dll";
        private const string k_EditorTestRunnerAssemblyName = "UnityEditor.TestRunner.dll";
        private const string k_EngineTestRunnerAssemblyName = "UnityEngine.TestRunner.dll";

        public static bool ShouldAddTestRunnerReferences(EditorBuildRules.TargetAssembly targetAssembly)
        {
            return !targetAssembly.References.Any(x => x.Filename.Contains(k_EngineTestRunnerAssemblyName) || x.Filename.Contains(k_EditorTestRunnerAssemblyName))
                && !targetAssembly.Filename.Contains(k_EngineTestRunnerAssemblyName)
                && !targetAssembly.Filename.Contains(k_EditorTestRunnerAssemblyName)
                && (PlayerSettings.playModeTestRunnerEnabled || (targetAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly);
        }

        public static IEnumerable<EditorBuildRules.TargetAssembly> GetReferences(IEnumerable<EditorBuildRules.TargetAssembly> assembliesCustomTargetAssemblies)
        {
            return assembliesCustomTargetAssemblies.Where(x =>
                x.Filename == k_EditorTestRunnerAssemblyName ||
                x.Filename == k_EngineTestRunnerAssemblyName);
        }

        public static bool ShouldAddNunitReferences(EditorBuildRules.TargetAssembly targetAssembly)
        {
            return !targetAssembly.PrecompiledReferences.Any(x => AssetPath.GetFileName(x.Path) == k_NunitAssemblyName)
                && (PlayerSettings.playModeTestRunnerEnabled || (targetAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly);
        }

        public static bool IsPrecompiledAssemblyNunit(ref PrecompiledAssembly precompiledAssembly)
        {
            return AssetPath.GetFileName(precompiledAssembly.Path) == k_NunitAssemblyName;
        }
    }
}
