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

        public string GetNamespaceNewRuntime(string filePath, string[] definesSymbols, string[] rspDefines)
        {
            var uniqueSymbols = definesSymbols;
            if (rspDefines != null && rspDefines.Any())
            {
                uniqueSymbols = definesSymbols.Union(rspDefines).Distinct().ToArray();
            }

            return CSharpNamespaceParser.GetNamespace(
                ReadAndConverteNewLines(filePath).ReadToEnd(),
                Path.GetFileNameWithoutExtension(filePath),
                uniqueSymbols);
        }

        public override string GetNamespace(string filePath, string definedSymbols)
        {
            var targetAssemblyFromPath = EditorCompilationInterface.Instance.GetTargetAssemblyFromPath(filePath);
            var definedSymbolsSplit = definedSymbols.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            string[] fullListOfDefines = new string[definedSymbolsSplit.Length + (targetAssemblyFromPath?.Defines?.Length ?? 0)];
            Array.Copy(definedSymbolsSplit, fullListOfDefines, definedSymbolsSplit.Length);

            if (targetAssemblyFromPath?.Defines != null)
            {
                Array.Copy(targetAssemblyFromPath.Defines, 0, fullListOfDefines, definedSymbolsSplit.Length, targetAssemblyFromPath.Defines.Length);
            }

            var rspFile = targetAssemblyFromPath?.GetResponseFiles()?.FirstOrDefault();
            ApiCompatibilityLevel compatibilityLevel = ApiCompatibilityLevel.NET_4_6;

            string[] rspDefines = null;
            if (!string.IsNullOrEmpty(rspFile))
            {
                rspDefines = ScriptCompilerBase.ParseResponseFileFromFile(
                    rspFile,
                    Application.dataPath,
                    GetSystemReferenceDirectories(compatibilityLevel)).Defines;
            }

            return GetNamespaceNewRuntime(filePath,  fullListOfDefines, rspDefines);
        }

        static StringReader ReadAndConverteNewLines(string filePath)
        {
            var text = File.ReadAllText(filePath);

            text = _crOnlyRegex.Replace(text, "\r\n");
            text = _lfOnlyRegex.Replace(text, "\r\n");

            return new StringReader(text);
        }
    }
}
