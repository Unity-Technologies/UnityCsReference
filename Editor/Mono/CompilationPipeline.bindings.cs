// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.Compilation
{
    [NativeHeader("Editor/Src/ScriptCompilation/ScriptCompilationPipeline.h")]
    public static partial class CompilationPipeline
    {
        [FreeFunction]
        extern internal static void ClearEditorCompilationErrors();
        [FreeFunction]
        extern internal static void LogEditorCompilationError(string message, int instanceID);

        // Internal helper method to detect double domain reload
        // when codegen assemblies are recompiled.
        internal static bool IsCodegenComplete()
        {
            return !EditorApplication.isCompiling &&
                !EditorCompilationInterface.ShouldRecompileNonCodeGenAssembliesAfterReload() &&
                !ShouldRecompileNonCodeGenAssembliesAfterReload();
        }

        [FreeFunction]
        internal static extern bool ShouldRecompileNonCodeGenAssembliesAfterReload();
    }
}
