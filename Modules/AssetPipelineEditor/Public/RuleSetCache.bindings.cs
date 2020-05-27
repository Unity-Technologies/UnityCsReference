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
    [NativeHeader("Editor/Src/ScriptCompilation/RuleSetFiles.h")]
    [ExcludeFromPreset]
    internal sealed class RuleSetFileCache
    {
        [FreeFunction("GetAllRuleSetFilePaths")]
        internal static extern string[] GetAllPaths();

        internal static string GetPathForAssembly(string scriptAssemblyOriginPath)
        {
            if (Path.IsPathRooted(scriptAssemblyOriginPath)
                || !scriptAssemblyOriginPath.StartsWith("assets\\", StringComparison.InvariantCultureIgnoreCase)
                && !scriptAssemblyOriginPath.StartsWith("assets/", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException($"{nameof(scriptAssemblyOriginPath)} must be relative to the project directory and inside the Assets folder.");
            }
            scriptAssemblyOriginPath = scriptAssemblyOriginPath.ConvertSeparatorsToUnity().TrimTrailingSlashes();
            return GetPathForScriptAssembly(scriptAssemblyOriginPath);
        }

        [FreeFunction("GetRuleSetFilePath")]
        internal static extern string GetPathForScriptAssembly(string scriptAssemblyOriginPath);
    }
}
