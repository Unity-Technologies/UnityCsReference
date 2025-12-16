// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor.Scripting
{
    [NativeHeader("Editor/Src/Scripting/BCLExtensions.h")]
    internal static class BCLExtensions
    {
        [FreeFunction(Name = "BCLExtensions::NetstandardRuntimeDirectory", IsThreadSafe = true)]
        extern internal static string NetstandardRuntimeDirectory();
        [FreeFunction(Name = "BCLExtensions::CoreCLRRuntimeDirectory", IsThreadSafe = true)]
        extern internal static string CoreCLRRuntimeDirectory();
        [FreeFunction(Name = "BCLExtensions::NetstandardTargetingPackDirectory", IsThreadSafe = true)]
        extern internal static string NetstandardTargetingPackDirectory();
        [FreeFunction(Name = "BCLExtensions::CoreCLRTargetingPackDirectory", IsThreadSafe = true)]
        extern internal static string CoreCLRTargetingPackDirectory();
        [FreeFunction(Name = "BCLExtensions::IsFilenameConflictingWithBCLExtensions", IsThreadSafe = true)]
        extern internal static bool IsFilenameConflictingWithBCLExtensions(string assemblyFileName);

    }
}
