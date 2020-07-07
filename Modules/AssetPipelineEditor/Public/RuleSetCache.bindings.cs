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
        [FreeFunction("GetAllRuleSetFilePaths")] internal static extern string[] GetAllPaths();
        [FreeFunction] internal static extern string GetRuleSetFilePathInRootFolder(string ruleSetFileNameWithoutExtension);
        [FreeFunction] internal static extern string GetRuleSetFilePathForScriptAssembly(string scriptAssemblyOriginPath);

        internal static string GetPathForAssembly(string scriptAssemblyOriginPath)
        {
            if (String.IsNullOrEmpty(scriptAssemblyOriginPath))
            {
                return default;
            }

            if (Path.IsPathRooted(scriptAssemblyOriginPath))
            {
                scriptAssemblyOriginPath = Paths.GetPathRelativeToProjectDirectory(scriptAssemblyOriginPath);

                if (String.IsNullOrEmpty(scriptAssemblyOriginPath))
                {
                    throw new ArgumentException($"'{nameof(scriptAssemblyOriginPath)}' must be either the Assets folder itself, or nested within it. " +
                        "You may provide either the full path, or the path relative to the project directory, as an argument to this method.");
                }
            }

            string formattedPath = scriptAssemblyOriginPath.ConvertSeparatorsToUnity().TrimTrailingSlashes();

            if (!formattedPath.StartsWith("assets", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException($"'{nameof(scriptAssemblyOriginPath)}' must be either the Assets folder itself, or nested within it. " +
                    "You may provide either the full path, or the path relative to the project directory, as an argument to this method.");
            }

            string ruleSetFilePath = GetRuleSetFilePathForScriptAssembly(formattedPath);
            return String.IsNullOrEmpty(ruleSetFilePath) ? GetRuleSetFilePathInRootFolder("Default") : ruleSetFilePath;
        }
    }
}
