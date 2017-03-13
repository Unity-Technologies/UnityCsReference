// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using System.Linq;
using UnityEditor.Scripting.Compilers;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class WSAHelpers
    {
        public static bool IsCSharpAssembly(string assemblyName, EditorBuildRules.TargetAssembly[] customTargetAssemblies)
        {
            if (assemblyName.ToLower().Contains("firstpass"))
                return false;

            var csLang = ScriptCompilers.CSharpSupportedLanguage;
            var assemblies = EditorBuildRules.GetTargetAssemblies(csLang, customTargetAssemblies).Where(a => a.Flags != AssemblyFlags.EditorOnly);

            return assemblies.Any(a => a.Filename == assemblyName);
        }

        public static bool IsCSharpFirstPassAssembly(string assemblyName, EditorBuildRules.TargetAssembly[] customTargetAssemblies)
        {
            if (!assemblyName.ToLower().Contains("firstpass"))
                return false;

            var csLang = ScriptCompilers.CSharpSupportedLanguage;
            var assemblies = EditorBuildRules.GetTargetAssemblies(csLang, customTargetAssemblies).Where(a => a.Flags != AssemblyFlags.EditorOnly);

            return assemblies.Any(a => a.Filename == assemblyName);
        }

        public static bool UseDotNetCore(string path, BuildTarget buildTarget, EditorBuildRules.TargetAssembly[] customTargetAssemblies)
        {
            var metroCompilationOverrides = PlayerSettings.WSA.compilationOverrides;
            bool dotNetCoreEnabled = buildTarget == BuildTarget.WSAPlayer && metroCompilationOverrides != PlayerSettings.WSACompilationOverrides.None;
            var assemblyName = Path.GetFileName(path);
            bool useDotNetCore = dotNetCoreEnabled && (IsCSharpAssembly(path, customTargetAssemblies) || (metroCompilationOverrides != PlayerSettings.WSACompilationOverrides.UseNetCorePartially && IsCSharpFirstPassAssembly(assemblyName, customTargetAssemblies)));

            return useDotNetCore;
        }
    }
}
