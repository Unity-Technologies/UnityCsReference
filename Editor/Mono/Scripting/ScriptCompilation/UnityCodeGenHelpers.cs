// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
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

        public struct ScriptCodeGenAssemblies
        {
            public List<ScriptAssembly> ScriptAssemblies;
            public List<ScriptAssembly> CodeGenAssemblies;
        }

        public static bool IsCodeGen(string assemblyName, bool includesExtension = true)
        {
            var name = (includesExtension ? Path.GetFileNameWithoutExtension(assemblyName) : assemblyName);
            var isCodeGen = name.StartsWith(k_CodeGenPrefix, StringComparison.OrdinalIgnoreCase) && name.EndsWith(k_CodeGenSuffix, StringComparison.OrdinalIgnoreCase);
            return isCodeGen;
        }

        public static void UpdateCodeGenScriptAssembly(ref ScriptAssembly scriptAssembly)
        {
            int referencesLength = scriptAssembly.References.Length;
            var newReferences = new string[referencesLength + 1];
            Array.Copy(scriptAssembly.References, newReferences, referencesLength);
            newReferences[referencesLength] = AssetPath.Combine(EditorApplication.applicationContentsPath, "Managed", "Unity.CompilationPipeline.Common.dll");
            scriptAssembly.References = newReferences;
        }

        public static ScriptCodeGenAssemblies ToScriptCodeGenAssemblies(ScriptAssembly[] scriptAssemblies)
        {
            var result = new ScriptCodeGenAssemblies();

            result.ScriptAssemblies = new List<ScriptAssembly>(scriptAssemblies.Length);
            result.CodeGenAssemblies = new List<ScriptAssembly>(scriptAssemblies.Length);

            foreach (var scriptAssembly in scriptAssemblies)
            {
                bool isCodeGen = IsCodeGen(scriptAssembly.Filename, true);

                if (isCodeGen)
                {
                    result.CodeGenAssemblies.Add(scriptAssembly);
                }
                else
                {
                    result.ScriptAssemblies.Add(scriptAssembly);
                }
            }

            return result;
        }
    }
}
