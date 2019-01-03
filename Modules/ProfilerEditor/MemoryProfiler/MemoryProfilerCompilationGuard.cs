// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Compilation;
using ExperimentalMemoryProfiler = UnityEngine.Profiling.Memory.Experimental.MemoryProfiler;

namespace UnityEditor.Profiling.Memory.Experimental
{
    /// <summary>
    /// Memory profiler compilation guard to prevent starting of captures during the compilation process.
    /// </summary>
    internal static class MemoryProfilerCompilationGuard
    {
        [InitializeOnLoadMethod]
        public static void InjectCompileGuard()
        {
            CompilationPipeline.compilationStarted +=  ExperimentalMemoryProfiler.StartedCompilationCallback;
        }
    }
}
