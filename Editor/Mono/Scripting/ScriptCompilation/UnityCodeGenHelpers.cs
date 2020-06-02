// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal static class UnityCodeGenHelpers
    {
        const string k_CompilerSuffix = ".Compiler";
        const string k_CodeGenSuffix = ".CodeGen";
        const string k_CompilerTestsSuffix = ".Compiler.Tests";
        const string k_CodeGenTestsSuffix = ".CodeGen.Tests";
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

        public static bool IsCodeGen(string assemblyName)
        {
            var name = AssetPath.GetAssemblyNameWithoutExtension(assemblyName);

            if (name.StartsWith(k_CodeGenPrefix, StringComparison.OrdinalIgnoreCase) && name.EndsWith(k_CompilerSuffix, StringComparison.OrdinalIgnoreCase))
                return true;

            if (name.StartsWith(k_CodeGenPrefix, StringComparison.OrdinalIgnoreCase) && name.EndsWith(k_CodeGenSuffix, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        public static bool IsCodeGenTest(string assemblyName)
        {
            var name = AssetPath.GetAssemblyNameWithoutExtension(assemblyName);

            if (name.StartsWith(k_CodeGenPrefix, StringComparison.OrdinalIgnoreCase) && name.EndsWith(k_CompilerTestsSuffix, StringComparison.OrdinalIgnoreCase))
                return true;

            if (name.StartsWith(k_CodeGenPrefix, StringComparison.OrdinalIgnoreCase) && name.EndsWith(k_CodeGenTestsSuffix, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        public static ScriptCodeGenAssemblies ToScriptCodeGenAssemblies(ScriptAssembly[] scriptAssemblies)
        {
            var result = new ScriptCodeGenAssemblies();

            result.ScriptAssemblies = new List<ScriptAssembly>(scriptAssemblies.Length);
            result.CodeGenAssemblies = new List<ScriptAssembly>(scriptAssemblies.Length);

            foreach (var scriptAssembly in scriptAssemblies)
            {
                bool isCodeGen = IsCodeGen(scriptAssembly.Filename);

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
