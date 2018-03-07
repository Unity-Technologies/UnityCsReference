// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEditor.Compilation;
using UnityEngine.Bindings;

namespace UnityEditor.Scripting.ScriptCompilation
{
    [NativeHeader("Editor/Src/ScriptCompilation/ScriptCompilationPipeline.h")]
    [StructLayout(LayoutKind.Sequential)]
    struct PrecompiledAssembly
    {
        [NativeName("path")]
        public string Path;
        [NativeName("flags")]
        public AssemblyFlags Flags;
        [NativeName("optionalUnityReferences")]
        public OptionalUnityReferences OptionalUnityReferences;
    };
}
