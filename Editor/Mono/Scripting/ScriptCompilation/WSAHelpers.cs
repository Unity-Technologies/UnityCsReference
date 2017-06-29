// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class WSAHelpers
    {
        public static bool IsCSharpAssembly(ScriptAssembly scriptAssembly)
        {
            if (scriptAssembly.Filename.ToLower().Contains("firstpass"))
                return false;

            return scriptAssembly.Language == ScriptCompilers.CSharpSupportedLanguage;
        }

        public static bool IsCSharpFirstPassAssembly(ScriptAssembly scriptAssembly)
        {
            if (!scriptAssembly.Filename.ToLower().Contains("firstpass"))
                return false;

            return scriptAssembly.Language == ScriptCompilers.CSharpSupportedLanguage;
        }

        public static bool UseDotNetCore(ScriptAssembly scriptAssembly)
        {
            var metroCompilationOverrides = PlayerSettings.WSA.compilationOverrides;
            bool dotNetCoreEnabled = scriptAssembly.BuildTarget == BuildTarget.WSAPlayer && metroCompilationOverrides != PlayerSettings.WSACompilationOverrides.None;
            bool useDotNetCore = dotNetCoreEnabled && (IsCSharpAssembly(scriptAssembly) || (metroCompilationOverrides != PlayerSettings.WSACompilationOverrides.UseNetCorePartially && IsCSharpFirstPassAssembly(scriptAssembly)));

            return useDotNetCore;
        }
    }
}
