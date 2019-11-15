// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.Compilation;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.Scripting.Compilers
{
    internal class CSharpLanguage : SupportedLanguage
    {
        public override ResponseFileProvider CreateResponseFileProvider()
        {
            return new MicrosoftCSharpResponseFileProvider();
        }

        public override string GetExtensionICanCompile()
        {
            return "cs";
        }

        public override string GetLanguageName()
        {
            return "CSharp";
        }

        public override bool CompilerRequiresAdditionalReferences()
        {
            return true;
        }

        public override string[] GetCompilerDefines()
        {
            var defines = new string[]
            {
                "CSHARP_7_OR_LATER", // Incremental Compiler adds this.
                "CSHARP_7_3_OR_NEWER",
            };

            return defines;
        }
    }
}
