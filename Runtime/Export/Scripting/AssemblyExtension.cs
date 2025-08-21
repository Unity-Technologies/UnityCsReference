// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Export/Scripting/ScriptingRuntime.h")]
    internal static class AssemblyExtension
    {
        /// <summary>
        /// The Assembly.Location property is not available when an assembly is loaded from a Stream. In many cases we do
        /// however know the actual location. This will extract that information.
        /// </summary>
        /// <param name="assembly">The assembly to lookup the location for. Will throw ArgumentNullException if null.</param>
        /// <returns>Returns the path to the assembly if known or empty if not</returns>
        public static string GetLoadedAssemblyPath(this Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

// Need to use Location in this helper extension for il2cpp backend.
#pragma warning disable UAC0007
            return assembly.Location;
#pragma warning restore UAC0007
        }

    }
}
