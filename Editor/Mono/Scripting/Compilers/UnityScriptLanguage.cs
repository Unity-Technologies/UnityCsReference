// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Scripting.Compilers
{
    internal class UnityScriptLanguage : SupportedLanguage
    {
        public override string GetExtensionICanCompile()
        {
            return "js";
        }

        public override string GetLanguageName()
        {
            return "UnityScript";
        }

        public override ScriptCompilerBase CreateCompiler(MonoIsland island, bool buildingForEditor, BuildTarget targetPlatform, bool runUpdater)
        {
            return new UnityScriptCompiler(island, runUpdater);
        }
    }
}
