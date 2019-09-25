// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor.Compilation
{
    [NativeHeader("Editor/Src/ScriptCompilation/ScriptCompilationPipeline.h")]
    public static partial class CompilationPipeline
    {
        [FreeFunction]
        internal static extern void ClearEditorCompilationErrors();
        [FreeFunction]
        internal static extern void LogEditorCompilationError(string message, int instanceID);

        [FreeFunction]
        internal static extern void DisableScriptDebugInfo();

        [FreeFunction]
        internal static extern void EnableScriptDebugInfo();

        [FreeFunction]
        internal static extern bool IsScriptDebugInfoEnabled();
    }
}
