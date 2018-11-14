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
            if (EditorApplication.scriptingRuntimeVersion == ScriptingRuntimeVersion.Latest)
                return new MicrosoftCSharpResponseFileProvider();
            return new MonoCSharpResponseFileProvider();
        }

        public override string GetExtensionICanCompile()
        {
            return "cs";
        }

        public override string GetLanguageName()
        {
            return "CSharp";
        }

        public static CSharpCompiler GetCSharpCompiler(BuildTarget targetPlatform, bool buildingForEditor,
            ScriptAssembly scriptAssembly)
        {
            var target = ModuleManager.GetTargetStringFromBuildTarget(targetPlatform);
            var extension = ModuleManager.GetCompilationExtension(target);
            return extension.GetCsCompiler(buildingForEditor, scriptAssembly);
        }

        public override ScriptCompilerBase CreateCompiler(ScriptAssembly scriptAssembly, MonoIsland island, bool buildingForEditor, BuildTarget targetPlatform, bool runUpdater)
        {
            switch (GetCSharpCompiler(targetPlatform, buildingForEditor, scriptAssembly))
            {
                case CSharpCompiler.Microsoft:
                    return new MicrosoftCSharpCompiler(island, runUpdater);
                case CSharpCompiler.Mono:
                default:
                    return new MonoCSharpCompiler(island, runUpdater);
            }
        }

        public override bool CompilerRequiresAdditionalReferences()
        {
            return true;
        }

        public override string[] GetCompilerDefines(BuildTarget targetPlatform, bool buildingForEditor,
            ScriptAssembly scriptAssembly)
        {
            var compiler = GetCSharpCompiler(targetPlatform, buildingForEditor, scriptAssembly);

            if (compiler == CSharpCompiler.Microsoft)
            {
                var defines = new string[]
                {
                    "CSHARP_7_OR_LATER", // Incremental Compiler adds this.
                    "CSHARP_7_3_OR_NEWER",
                };

                return defines;
            }

            return new string[0];
        }

        static string[] GetSystemReferenceDirectories(ApiCompatibilityLevel apiCompatibilityLevel)
        {
            return MonoLibraryHelpers.GetSystemReferenceDirectories(apiCompatibilityLevel);
        }

        public string GetNamespaceNewRuntime(string filePath, string definedSymbols, string[] defines)
        {
            var definedSymbolSplit = definedSymbols.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var uniqueSymbols = defines.Union(definedSymbolSplit).Distinct().ToArray();
            return CSharpNamespaceParser.GetNamespace(
                ReadAndConverteNewLines(filePath).ReadToEnd(),
                Path.GetFileNameWithoutExtension(filePath),
                uniqueSymbols);
        }

        public string GetNamespaceOldRuntime(string filePath, string definedSymbols, string[] defines)
        {
            var definedSymbolSplit = definedSymbols.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var uniqueSymbols = defines.Union(definedSymbolSplit).Distinct().ToArray();
            using (var parser = ParserFactory.CreateParser(ICSharpCode.NRefactory.SupportedLanguage.CSharp, ReadAndConverteNewLines(filePath)))
            {
                foreach (var symbol in uniqueSymbols)
                {
                    parser.Lexer.ConditionalCompilationSymbols.Add(symbol, string.Empty);
                }

                parser.Lexer.EvaluateConditionalCompilation = true;
                parser.Parse();
                try
                {
                    var visitor = new NamespaceVisitor();
                    var data = new VisitorData { TargetClassName = Path.GetFileNameWithoutExtension(filePath) };
                    parser.CompilationUnit.AcceptVisitor(visitor, data);
                    return string.IsNullOrEmpty(data.DiscoveredNamespace) ? string.Empty : data.DiscoveredNamespace;
                }
                catch
                {
                    // Don't care; all we want is the namespace
                }
            }
            return string.Empty;
        }

        public override string GetNamespace(string filePath, string definedSymbols)
        {
            var responseFilePath = Path.Combine("Assets", MonoCSharpCompiler.ResponseFilename);
            if (EditorApplication.scriptingRuntimeVersion == ScriptingRuntimeVersion.Latest)
            {
                var responseFileData = ScriptCompilerBase.ParseResponseFileFromFile(
                    responseFilePath,
                    Application.dataPath,
                    GetSystemReferenceDirectories(ApiCompatibilityLevel.NET_4_6));
                return GetNamespaceNewRuntime(filePath, definedSymbols, responseFileData.Defines);
            }
            else
            {
                var responseFileData = ScriptCompilerBase.ParseResponseFileFromFile(
                    responseFilePath,
                    Application.dataPath,
                    GetSystemReferenceDirectories(ApiCompatibilityLevel.NET_2_0));
                return GetNamespaceOldRuntime(filePath, definedSymbols, responseFileData.Defines);
            }
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

        class VisitorData
        {
            public VisitorData()
            {
                CurrentNamespaces = new Stack<string>();
            }

            public string TargetClassName;
            public Stack<string> CurrentNamespaces;
            public string DiscoveredNamespace;
        }
        class NamespaceVisitor : AbstractAstVisitor
        {
            public override object VisitNamespaceDeclaration(ICSharpCode.NRefactory.Ast.NamespaceDeclaration namespaceDeclaration, object data)
            {
                var visitorData = (VisitorData)data;
                visitorData.CurrentNamespaces.Push(namespaceDeclaration.Name);
                // Visit children (E.g. TypeDcelarion objects)
                namespaceDeclaration.AcceptChildren(this, visitorData);
                visitorData.CurrentNamespaces.Pop();
                return null;
            }

            public override object VisitTypeDeclaration(ICSharpCode.NRefactory.Ast.TypeDeclaration typeDeclaration, object data)
            {
                var visitorData = (VisitorData)data;
                if (typeDeclaration.Name == visitorData.TargetClassName)
                {
                    var fullNamespace = string.Empty;
                    foreach (var ns in visitorData.CurrentNamespaces)
                    {
                        if (fullNamespace == string.Empty)
                            fullNamespace = ns;
                        else
                            fullNamespace = ns + "." + fullNamespace;
                    }
                    visitorData.DiscoveredNamespace = fullNamespace;
                }
                return null;
            }
        }
    }
}
