// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal static class UnityCodeGenHelpers
    {
        const string k_CodeGenSuffix = ".CodeGen";
        const string k_CodeGenPrefix = "Unity.";

        const string k_UnityEngineModules = "UnityEngine";
        const string k_UnityEngineModulesLower = "unityengine";

        const string k_UnityEditorModules = "UnityEditor";
        const string k_UnityEditorModulesLower = "unityeditor";

        public static bool IsCodeGen(string assemblyName, bool includesExtension = true)
        {
            var name = (includesExtension ? Path.GetFileNameWithoutExtension(assemblyName) : assemblyName);
            var isCodeGen = name.StartsWith(k_CodeGenPrefix) && name.EndsWith(k_CodeGenSuffix, StringComparison.OrdinalIgnoreCase);
            return isCodeGen;
        }

        public static void UpdateCodeGenScriptAssembly(ref ScriptAssembly scriptAssembly)
        {
            scriptAssembly.ScriptAssemblyReferences = new ScriptAssembly[0];

            int newReferenceCount = 0;
            var references = new string[scriptAssembly.References.Length];

            foreach (var reference in scriptAssembly.References)
            {
                var name = AssetPath.GetFileName(reference);
                if (!Utility.FastStartsWith(name, k_UnityEngineModules, k_UnityEngineModulesLower)
                    && !Utility.FastStartsWith(name, k_UnityEditorModules, k_UnityEditorModulesLower))
                {
                    references[newReferenceCount] = reference;
                    newReferenceCount++;
                }
            }
            var result = new string[newReferenceCount + 1];
            Array.Copy(references, result, newReferenceCount);
            result[newReferenceCount] = AssetPath.Combine(EditorApplication.applicationContentsPath, "Managed", "Unity.CompilationPipeline.Common.dll");
            scriptAssembly.References = result;
        }
    }
}
