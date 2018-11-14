// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.Scripting.Compilers
{
    internal abstract class SupportedLanguage
    {
        public virtual ResponseFileProvider CreateResponseFileProvider()
        {
            return null;
        }

        public abstract string GetExtensionICanCompile();
        public abstract string GetLanguageName();
        public abstract ScriptCompilerBase CreateCompiler(ScriptAssembly scriptAssembly, MonoIsland island, bool buildingForEditor, BuildTarget targetPlatform, bool runUpdater);
        public virtual string GetNamespace(string fileName, string definedSymbols)
        {
            return string.Empty;
        }

        public virtual bool CompilerRequiresAdditionalReferences()
        {
            return false;
        }

        public virtual string[] GetCompilerDefines(BuildTarget targetPlatform, bool buildingForEditor,
            ScriptAssembly scriptAssembly)
        {
            return new string[0];
        }
    }
}
