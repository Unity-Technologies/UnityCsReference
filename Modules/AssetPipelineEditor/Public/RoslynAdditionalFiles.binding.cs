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
    [NativeHeader("Editor/Src/ScriptCompilation/RoslynAnalyzers.bindings.h")]
    [ExcludeFromPreset]
    internal sealed class RoslynAdditionalFiles
    {
        [FreeFunction("GetAllCachedRoslynAdditionalFilePaths")] internal static extern string[] GetFilePaths();
        [FreeFunction("GetAllRoslynAdditionalFilePaths")] internal static extern string[] GetAnalyzerAdditionalFilesForTargetAssembly(string analyzerPath, string targetAssemblyPath);
        [FreeFunction("CacheRoslynAdditionalFiles")] internal static extern void AddAdditionalFiles(string[] additionalFiles);
    }
}
