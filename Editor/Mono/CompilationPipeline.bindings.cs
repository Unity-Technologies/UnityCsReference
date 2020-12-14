// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.Compilation
{
    [Flags]
    internal enum CompilationSetupErrorFlags    // Keep in sync with enum CompilationSetupErrorFlags::Flags in ScriptCompilationPipeline.h
    {
        none = 0,
        cyclicReferences = (1 << 0),            // set when CyclicAssemblyReferenceException is thrown
        loadError = (1 << 1),                   // set when AssemblyDefinitionException is thrown
        precompiledAssemblyError = (1 << 2),    // set when PrecompiledAssemblyException is thrown
        all = cyclicReferences | loadError | precompiledAssemblyError,
    };

    [NativeHeader("Editor/Src/ScriptCompilation/ScriptCompilationPipeline.h")]
    public static partial class CompilationPipeline
    {
        [FreeFunction]
        internal static extern void DisableScriptDebugInfo();

        [FreeFunction]
        internal static extern void EnableScriptDebugInfo();

        [FreeFunction]
        internal static extern bool IsScriptDebugInfoEnabled();
    }
}
