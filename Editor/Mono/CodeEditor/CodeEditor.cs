// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.PlatformSupport;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Scripting;

namespace Unity.CodeEditor
{
    public class CodeEditor
    {
        internal static CodeEditor Editor { get; } = new CodeEditor();
        List<IExternalCodeEditor> m_ExternalCodeEditors = new List<IExternalCodeEditor>();
        IExternalCodeEditor m_DefaultEditor = new DefaultExternalCodeEditor();

        public struct Installation
        {
            public string Name;
            public string Path;
        }

        [RequiredByNativeCode]
        static bool OpenFileAtLineColumn(string path, int line, int column)
        {
            return Editor.Current.OpenProject(path, line, column);
        }

        [OnOpenAsset]
        static bool OnOpenAsset(int instanceID, int line, int column)
        {
            var selected = EditorUtility.InstanceIDToObject(instanceID);
            var assetPath = AssetDatabase.GetAssetPath(selected);

            #pragma warning disable 618
            if (ScriptEditorUtility.GetScriptEditorFromPath(CurrentEditorInstallation) != ScriptEditorUtility.ScriptEditor.Other)
            {
                return false;
            }

            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            var assetFilePath = Path.GetFullPath(assetPath);
            return Editor.Current.OpenProject(assetFilePath, line, column);
        }

        internal Installation EditorInstallation
        {
            get
            {
                var editorPath = EditorPrefs.GetString("kScriptsDefaultApp");
                if (string.IsNullOrEmpty(editorPath))
                {
                    return new Installation
                    {
                        Name = "Internal",
                        Path = "",
                    };
                }

                foreach (var codeEditor in m_ExternalCodeEditors)
                {
                    Installation installation;
                    if (codeEditor.TryGetInstallationForPath(editorPath, out installation))
                    {
                        return installation;
                    }
                }

                // This is supporting legacy script editors until they are moved to packages
                if (!string.IsNullOrEmpty(editorPath))
                    return new Installation { Path = editorPath };

                // If no script editor is set, try to use first found supported one.
                var editorPaths = GetFoundScriptEditorPaths();

                if (editorPaths.Count > 0)
                    return new Installation { Path = editorPaths.Keys.ToArray()[0] };

                return new Installation();
            }
        }

        internal IExternalCodeEditor Current
        {
            get
            {
                var editorPath = EditorPrefs.GetString("kScriptsDefaultApp");
                if (string.IsNullOrEmpty(editorPath))
                {
                    return m_DefaultEditor;
                }

                foreach (var codeEditor in m_ExternalCodeEditors)
                {
                    Installation installation;
                    if (codeEditor.TryGetInstallationForPath(editorPath, out installation))
                    {
                        return codeEditor;
                    }
                }

                return m_DefaultEditor;
            }
        }

        internal Dictionary<string, string> GetFoundScriptEditorPaths()
        {
            var result = new Dictionary<string, string>();

            foreach (var installation in m_ExternalCodeEditors.SelectMany(codeEditor => codeEditor.Installations))
            {
                AddIfPathExists(installation.Name, installation.Path, result);
            }

            return result;
        }

        internal static void AddIfPathExists(string name, string path, Dictionary<string, string> list)
        {
            if (list.ContainsKey(path))
                return;
            if (Directory.Exists(path)) list.Add(path, name);
            else if (File.Exists(path)) list.Add(path, name);
        }

        public static void SetExternalScriptEditor(string path)
        {
            EditorPrefs.SetString("kScriptsDefaultApp", path);
            Editor.Current.Initialize(path);
        }

        public static void Register(IExternalCodeEditor externalCodeEditor)
        {
            Editor.m_ExternalCodeEditors.Add(externalCodeEditor);
        }

        public static void Unregister(IExternalCodeEditor externalCodeEditor)
        {
            Editor.m_ExternalCodeEditors.Remove(externalCodeEditor);
        }

        public static IExternalCodeEditor CurrentEditor => Editor.Current;

        public static string CurrentEditorInstallation => Editor.EditorInstallation.Path;

        public static string ParseArgument(string arguments, string path, int line, int column)
        {
            var newArgument = arguments.Replace("$(ProjectPath)", QuoteForProcessStart(Directory.GetParent(Application.dataPath).FullName));
            newArgument = newArgument.Replace("$(File)", QuoteForProcessStart(path));
            newArgument = newArgument.Replace("$(Line)", line >= 0 ? line.ToString() : "0");
            newArgument = newArgument.Replace("$(Column)", column >= 0 ? column.ToString() : "0");
            return newArgument;
        }

        /// <summary>
        /// Quote a string for passing as a single argument to Process.Start
        /// and append it to this string builder.
        /// </summary>
        /// <remarks>
        /// On Windows, quote according to the Win32 CommandLineToArgvW API scheme,
        /// used by most Windows applications (with some notable exceptions, like
        /// cmd.exe and cscript.exe). On Unix, Mono uses the entirely incompatible
        /// GLib g_shell_parse_argv function for converting the argument string to
        /// a native Unix argument list, so quote for that instead.
        ///
        /// Do not use this to quote arguments for command line shells (cmd.exe
        /// or POSIX shell), as these may use distinct quotation mechanisms.
        ///
        /// Do not append two quoted arguments without an (unquoted) separator
        /// between them: Two consecutive quotation marks triggers undocumented
        /// behavior in CommandLineToArgvW and possibly other argument processors.
        /// </remarks>
        // https://blogs.msdn.microsoft.com/twistylittlepassagesallalike/2011/04/23/everyone-quotes-command-line-arguments-the-wrong-way/
        static string QuoteForProcessStart(string argument)
        {
            var sb = new StringBuilder();
            // Quote for g_shell_parse_argv when running on Unix (under Mono).
            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                sb.Append('\'');
                sb.Append(argument.Replace("\\", "\\\\").Replace("'", "\\'"));
                sb.Append('\'');
                return sb.ToString();
            }

            sb.Append('"');
            for (int i = 0; i < argument.Length; ++i)
            {
                char c = argument[i];
                if (c == '"')
                {
                    for (int j = i - 1; j >= 0 && argument[j] == '\\'; --j)
                        sb.Append('\\');
                    sb.Append('\\');
                }
                sb.Append(c);
            }
            for (int j = argument.Length - 1; j >= 0 && argument[j] == '\\'; --j)
                sb.Append('\\');
            sb.Append('"');
            return sb.ToString();
        }
    }
}
