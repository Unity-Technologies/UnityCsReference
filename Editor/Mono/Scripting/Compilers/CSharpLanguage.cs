// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Visitors;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEditor.Modules;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;

namespace UnityEditor.Scripting.Compilers
{
    internal class CSharpLanguage : SupportedLanguage
    {
        private static Regex _crOnlyRegex = new Regex("\r(?!\n)", RegexOptions.Compiled);
        private static Regex _lfOnlyRegex = new Regex("(?<!\r)\n", RegexOptions.Compiled);

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

        public override ScriptCompilerBase CreateCompiler(ScriptAssembly scriptAssembly, EditorScriptCompilationOptions options, string tempOutputDirectory)
        {
            return new MicrosoftCSharpCompiler(scriptAssembly, options, tempOutputDirectory);
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

        static string[] GetSystemReferenceDirectories(ApiCompatibilityLevel apiCompatibilityLevel)
        {
            return MonoLibraryHelpers.GetSystemReferenceDirectories(apiCompatibilityLevel);
        }

        public override void GetClassAndNamespace(string filePath, out string outClassName, out string outNamespace)
        {
            CSharpNamespaceParser.GetClassAndNamespace(ReadAndConverteNewLines(filePath).ReadToEnd(),
                Path.GetFileNameWithoutExtension(filePath), out outClassName, out outNamespace);
        }

        // TODO: Revisit this code and switch to version 5.5.1 (or Roslyn if possible) when Editor switches to newer runtime version (on going work expected
        //       to finish around 2017.2 or 2017.3 release.
        //
        // This is a workaround for a bug in version 3.2.1 of NRefactory in which it fails to parse sources with a combination of LF / #if / #else
        // Version 5.5.1 is confirmed to not have this bug but we can't use it since it requires a newer runtime/c# version;
        static StringReader ReadAndConverteNewLines(string filePath)
        {
            var text = File.ReadAllText(filePath);

            text = _crOnlyRegex.Replace(text, "\r\n");
            text = _lfOnlyRegex.Replace(text, "\r\n");

            return new StringReader(text);
        }
    }
}
