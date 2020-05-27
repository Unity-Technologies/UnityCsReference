// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    // This class will be needed by the Roslyn analysis runner
    [NativeHeader("Editor/Src/ScriptCompilation/CompiledAssemblies.bindings.h")]
    [ExcludeFromPreset]
    internal sealed class CompiledAssemblyCache
    {
        [FreeFunction] internal static extern string[] GetAllPaths();
        [FreeFunction] internal static extern void AddPaths(string[] paths);
        [FreeFunction] internal static extern void AddPath(string path);
        [FreeFunction] internal static extern void RemovePath(string path);
        [FreeFunction("RemoveAllPaths")] internal static extern void Clear();
    }
}
