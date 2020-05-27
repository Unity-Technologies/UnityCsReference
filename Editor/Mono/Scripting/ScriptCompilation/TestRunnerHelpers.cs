// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Modules;
using UnityEditor.Scripting.Compilers;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class TestRunnerHelpers
    {
        internal const string NunitAssemblyName = "nunit.framework.dll";
        private const string k_EditorTestRunnerAssemblyName = "UnityEditor.TestRunner.dll";
        private const string k_EngineTestRunnerAssemblyName = "UnityEngine.TestRunner.dll";

        public static bool ShouldAddTestRunnerReferences(TargetAssembly targetAssembly)
        {
            if (UnityCodeGenHelpers.IsCodeGen(targetAssembly.Filename))
            {
                return false;
            }

            bool referencesTestRunnerAssembly = false;
            foreach (var reference in targetAssembly.References)
            {
                referencesTestRunnerAssembly =
                    reference.Filename.Equals(k_EditorTestRunnerAssemblyName, StringComparison.Ordinal)
                    || reference.Filename.Equals(k_EngineTestRunnerAssemblyName, StringComparison.Ordinal);

                if (referencesTestRunnerAssembly)
                {
                    break;
                }
            }

            return !referencesTestRunnerAssembly
                && !targetAssembly.Filename.Equals(k_EditorTestRunnerAssemblyName, StringComparison.Ordinal)
                && !targetAssembly.Filename.Equals(k_EngineTestRunnerAssemblyName, StringComparison.Ordinal)
                && (PlayerSettings.playModeTestRunnerEnabled || (targetAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly);
        }

        public static IEnumerable<TargetAssembly> GetReferences(IEnumerable<TargetAssembly> assembliesCustomTargetAssemblies)
        {
            return assembliesCustomTargetAssemblies.Where(x =>
                x.Filename == k_EditorTestRunnerAssemblyName ||
                x.Filename == k_EngineTestRunnerAssemblyName);
        }

        public static bool ShouldAddNunitReferences(TargetAssembly targetAssembly)
        {
            bool referencesNUnit = false;

            if ((targetAssembly.Flags & AssemblyFlags.ExplicitReferences) == AssemblyFlags.ExplicitReferences)
            {
                foreach (var explicitPrecompiledReference in targetAssembly.ExplicitPrecompiledReferences)
                {
                    referencesNUnit = explicitPrecompiledReference.Equals(NunitAssemblyName, StringComparison.Ordinal);
                    if (referencesNUnit)
                    {
                        break;
                    }
                }
            }

            return !referencesNUnit
                && (PlayerSettings.playModeTestRunnerEnabled || (targetAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly);
        }

        public static void AddNunitReferences(Dictionary<string, PrecompiledAssembly> nameToPrecompiledAssemblies, ref List<PrecompiledAssembly> precompiledReferences)
        {
            PrecompiledAssembly nUnitAssembly;
            if (nameToPrecompiledAssemblies.TryGetValue(TestRunnerHelpers.NunitAssemblyName, out nUnitAssembly))
            {
                precompiledReferences.Add(nUnitAssembly);
            }
        }
    }
}
