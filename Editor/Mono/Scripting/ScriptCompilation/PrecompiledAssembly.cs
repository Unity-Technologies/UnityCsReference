// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEditor.Compilation;

namespace UnityEditor.Scripting.ScriptCompilation
{
    [StructLayout(LayoutKind.Sequential)]
    struct PrecompiledAssembly
    {
        public string Path;
        public AssemblyFlags Flags;
        public OptionalUnityReferences OptionalUnityReferences;
    };
}
