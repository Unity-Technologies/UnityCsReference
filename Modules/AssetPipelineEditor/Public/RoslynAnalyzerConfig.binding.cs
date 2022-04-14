// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor.Utils;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/ScriptCompilation/RoslynAnalyzerConfig.h")]
    [ExcludeFromPreset]
    internal sealed class RoslynAnalyzerConfigFiles
    {
        [FreeFunction] internal static extern string[] GetAllAnalyzerConfigs();
        [FreeFunction("GetAnalyzerConfigRootFolder")] internal static extern string ConfigForRootFolder(string assemblyName);
        [FreeFunction] internal static extern string GetAnalyzerConfigForAssembly(string assemblyPath);

        internal static string GetAnalyzerConfigRootFolder(string assemblyName)
        {
            var ret = ConfigForRootFolder(assemblyName);
            return ret;
        }
    }
}
