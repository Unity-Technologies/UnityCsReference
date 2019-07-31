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

        public abstract ScriptCompilerBase CreateCompiler(ScriptAssembly scriptAssembly, EditorScriptCompilationOptions options, string tempOutputDirectory);

        public virtual void GetClassAndNamespace(string fileName, string definedSymbols, out string outClassName,
            out string outNamespace)
        {
            outClassName = string.Empty;
            outNamespace = string.Empty;
        }

        public virtual string GetNamespace(string fileName, string definedSymbols)
        {
            return string.Empty;
        }

        public virtual bool CompilerRequiresAdditionalReferences()
        {
            return false;
        }

        public virtual string[] GetCompilerDefines()
        {
            return new string[0];
        }
    }
}
