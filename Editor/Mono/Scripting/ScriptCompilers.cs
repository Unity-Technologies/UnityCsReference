// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Scripting.ScriptCompilation;

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
        public readonly bool _allowUnsafeCode;
        public readonly ApiCompatibilityLevel _api_compatibility_level;
        public readonly string[] _files;
        public readonly string[] _references;
        public readonly string[] _defines;
        public readonly string[] _responseFiles;
        public readonly string _output;

        public MonoIsland(BuildTarget target, ApiCompatibilityLevel api_compatibility_level, bool allowUnsafeCode, string[] files, string[] references, string[] defines, string output)
        {
            _target = target;
            _development_player = false;
            _editor = false;
            _allowUnsafeCode = allowUnsafeCode;
            _api_compatibility_level = api_compatibility_level;
            _files = files;
            _references = references;
            _defines = defines;
            _output = output;
            _responseFiles = null;
        }

        public MonoIsland(BuildTarget target, bool editor, bool development_player, bool allowUnsafeCode, ApiCompatibilityLevel api_compatibility_level, string[] files, string[] references, string[] defines, string output)
        {
            _target = target;
            _development_player = development_player;
            _editor = editor;
            _allowUnsafeCode = allowUnsafeCode;
            _api_compatibility_level = api_compatibility_level;
            _files = files;
            _references = references;
            _defines = defines;
            _output = output;
            _responseFiles = null;
        }

        public MonoIsland(BuildTarget target, bool editor, bool development_player, bool allowUnsafeCode, ApiCompatibilityLevel api_compatibility_level, string[] files, string[] references, string[] defines, string output, string[] responseFiles)
            : this(target, editor, development_player, allowUnsafeCode, api_compatibility_level, files, references, defines, output)
        {
            _responseFiles = responseFiles;
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

        public static string GetExtensionOfSourceFile(string file)
        {
            var ext = Path.GetExtension(file).ToLower();
            ext = ext.Substring(1); //strip dot
            return ext;
        }
    }
}
