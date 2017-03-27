// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor.Scripting.Compilers;

namespace UnityEditor.Scripting
{
    internal struct SupportedLanguageStruct
    {
        public string extension;
        public string languageName;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MonoIsland
    {
        public readonly BuildTarget _target;
        public readonly bool _development_player;
        public readonly bool _editor;
        public readonly ApiCompatibilityLevel _api_compatibility_level;
        public readonly string[] _files;
        public readonly string[] _references;
        public readonly string[] _defines;
        public readonly string _output;

        public MonoIsland(BuildTarget target, ApiCompatibilityLevel api_compatibility_level, string[] files, string[] references, string[] defines, string output)
        {
            _target = target;
            _development_player = false;
            _editor = false;
            _api_compatibility_level = api_compatibility_level;
            _files = files;
            _references = references;
            _defines = defines;
            _output = output;
        }

        public MonoIsland(BuildTarget target, bool editor, bool development_player, ApiCompatibilityLevel api_compatibility_level, string[] files, string[] references, string[] defines, string output)
        {
            _target = target;
            _development_player = development_player;
            _editor = editor;
            _api_compatibility_level = api_compatibility_level;
            _files = files;
            _references = references;
            _defines = defines;
            _output = output;
        }

        public string GetExtensionOfSourceFiles()
        {
            return _files.Length > 0 ? ScriptCompilers.GetExtensionOfSourceFile(_files[0]) : "NA";
        }
    }

    internal static class ScriptCompilers
    {
        internal static readonly List<SupportedLanguage> SupportedLanguages;
        internal static readonly SupportedLanguage CSharpSupportedLanguage;
        static ScriptCompilers()
        {
            SupportedLanguages = new List<SupportedLanguage>();

            var types = new List<Type>();
            types.Add(typeof(CSharpLanguage));
            types.Add(typeof(BooLanguage));
            types.Add(typeof(UnityScriptLanguage));

            foreach (var t in types)
            {
                SupportedLanguages.Add((SupportedLanguage)Activator.CreateInstance(t));
            }

            CSharpSupportedLanguage = SupportedLanguages.Single(l => l.GetType() == typeof(CSharpLanguage));
        }

        internal static SupportedLanguageStruct[] GetSupportedLanguageStructs()
        {
            //we communicate with the runtime by xforming our SupportedLaanguage class to a struct, because that's
            //just a lot easier to marshall between native and managed code.
            return SupportedLanguages.Select(lang => new SupportedLanguageStruct
            {
                extension = lang.GetExtensionICanCompile(),
                languageName = lang.GetLanguageName()
            }).ToArray();
        }

        internal static string GetNamespace(string file, string definedSymbols)
        {
            if (string.IsNullOrEmpty(file)) throw new ArgumentException("Invalid file");

            string extension = GetExtensionOfSourceFile(file);
            foreach (var lang in SupportedLanguages)
            {
                if (lang.GetExtensionICanCompile() == extension)
                    return lang.GetNamespace(file, definedSymbols);
            }

            throw new ApplicationException("Unable to find a suitable compiler");
        }

        internal static SupportedLanguage GetLanguageFromName(string name)
        {
            foreach (var lang in SupportedLanguages)
            {
                if (String.Equals(name, lang.GetLanguageName(), StringComparison.OrdinalIgnoreCase))
                    return lang;
            }

            throw new ApplicationException(string.Format("Script language '{0}' is not supported", name));
        }

        internal static SupportedLanguage GetLanguageFromExtension(string extension)
        {
            foreach (var lang in SupportedLanguages)
            {
                if (String.Equals(extension, lang.GetExtensionICanCompile(), StringComparison.OrdinalIgnoreCase))
                    return lang;
            }

            throw new ApplicationException(string.Format("Script file extension '{0}' is not supported", extension));
        }

        internal static ScriptCompilerBase CreateCompilerInstance(MonoIsland island, bool buildingForEditor, BuildTarget targetPlatform, bool runUpdater)
        {
            if (island._files.Length == 0) throw new ArgumentException("Cannot compile MonoIsland with no files");

            foreach (var lang in SupportedLanguages)
            {
                if (lang.GetExtensionICanCompile() == island.GetExtensionOfSourceFiles())
                    return lang.CreateCompiler(island, buildingForEditor, targetPlatform, runUpdater);
            }

            throw new ApplicationException(string.Format("Unable to find a suitable compiler for sources with extension '{0}' (Output assembly: {1})", island.GetExtensionOfSourceFiles(), island._output));
        }

        public static string GetExtensionOfSourceFile(string file)
        {
            var ext = Path.GetExtension(file).ToLower();
            ext = ext.Substring(1); //strip dot
            return ext;
        }
    }
}
