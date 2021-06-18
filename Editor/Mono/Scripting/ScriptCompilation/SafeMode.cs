// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal interface ISafeModeInfo
    {
        string[] GetWhiteListAssemblyNames();
    }

    [NativeHeader("Editor/Src/ScriptCompilation/ScriptCompilationPipeline.h")]
    internal class SafeModeInfo : ISafeModeInfo
    {
        public string[] GetWhiteListAssemblyNames()
        {
            return GetWhiteListAssemblyNamesInternal();
        }

        [FreeFunction("GetSafeModeWhiteListTargetAssemblies")]
        public static extern string[] GetWhiteListAssemblyNamesInternal();
    }
}
