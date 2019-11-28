// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal static class CompilationPipelineCommonHelper
    {
        const string k_UnityAssemblyPrefix = "Unity.";
        const string k_CompilerClientSuffix = ".Compiler.Client";

        public static bool ShouldAdd(string assemblyName)
        {
            var name = AssetPath.GetAssemblyNameWithoutExtension(assemblyName);

            if (name.StartsWith(k_UnityAssemblyPrefix, StringComparison.OrdinalIgnoreCase) && name.EndsWith(k_CompilerClientSuffix, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        public static void UpdateScriptAssemblyReference(ref ScriptAssembly scriptAssembly)
        {
            int referencesLength = scriptAssembly.References.Length;
            var newReferences = new string[referencesLength + 1];
            Array.Copy(scriptAssembly.References, newReferences, referencesLength);
            newReferences[referencesLength] = AssetPath.Combine(EditorApplication.applicationContentsPath, "Managed", "Unity.CompilationPipeline.Common.dll");
            scriptAssembly.References = newReferences;
        }
    }
}
