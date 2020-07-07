// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/ScriptCompilation/CompiledAssemblies.bindings.h")]
    [ExcludeFromPreset]
    internal sealed class CompiledAssemblyCache
    {
        [StaticAccessor("CompiledAssembliesBindings", StaticAccessorType.DoubleColon)] internal static extern string[] GetAllPaths();
        [StaticAccessor("CompiledAssembliesBindings", StaticAccessorType.DoubleColon)] internal static extern bool Contains(string path);

        // The path gets added even if it already exists in the cache
        [StaticAccessor("CompiledAssembliesBindings", StaticAccessorType.DoubleColon)] internal static extern void AddPath(string path);
        [StaticAccessor("CompiledAssembliesBindings", StaticAccessorType.DoubleColon)] internal static extern void RemovePath(string path);
        [StaticAccessor("CompiledAssembliesBindings", StaticAccessorType.DoubleColon)] internal static extern void RemoveAllPaths();
    }
}
