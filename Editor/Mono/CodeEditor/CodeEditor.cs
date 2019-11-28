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
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Scripting;
using static UnityEditor.TypeCache;

namespace Unity.CodeEditor
{
    public class CodeEditor
    {
        internal static CodeEditor Editor { get; } = new CodeEditor();
        List<IExternalCodeEditor> m_ExternalCodeEditors = new List<IExternalCodeEditor>();
        IExternalCodeEditor m_DefaultEditor = new DefaultExternalCodeEditor();
        internal const string SystemDefaultPath = "";

        public struct Installation
        {
            public string Name;
            public string Path;
        }

        [RequiredByNativeCode]
        static bool OpenProject(string path, int line, int column)
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
                var editorPath = CurrentEditorInstallation.Trim();
                if (editorPath == CodeEditor.SystemDefaultPath)
                {
                    // If no script editor is set, try to use first found supported one.
                    var editorPaths = GetFoundScriptEditorPaths();
                    if (editorPaths.Count > 0)
                    {
                        return new Installation { Path = editorPaths.Keys.First() };
                    }

                    return new Installation
                    {
                        Name = "Internal",
                        Path = editorPath,
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

                return new Installation { Path = editorPath };
            }
        }

        internal IExternalCodeEditor Current
        {
            get
            {
                var editorPath = CurrentEditorInstallation.Trim();
                if (editorPath == CodeEditor.SystemDefaultPath)
                {
                    return m_DefaultEditor;
                }

                if (m_ExternalCodeEditors.Count() == 0)
                {
                    TypeCollection collection = TypeCache.GetTypesDerivedFrom<IExternalCodeEditor>();
                    for (int i = 0; i < collection.Count(); i++)
                    {
                        var codeEditorType = collection[i];
                        if (codeEditorType == typeof(DefaultExternalCodeEditor))
                            continue;

                        if (codeEditorType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) != null)
                        {
                            IExternalCodeEditor codeEditor = (IExternalCodeEditor)Activator.CreateInstance(codeEditorType);
                            m_ExternalCodeEditors.Add(codeEditor);
                        }
                    }
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

            #pragma warning disable 618
            if (ScriptEditorUtility.GetScriptEditorFromPath(CurrentEditorInstallation) != ScriptEditorUtility.ScriptEditor.Other && Application.platform == RuntimePlatform.OSXEditor)
            {
                AddIfPathExists("Visual Studio", "/Applications/Visual Studio.app", result);
                AddIfPathExists("Visual Studio (Preview)", "/Applications/Visual Studio (Preview).app", result);
            }

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
            if (Editor.m_ExternalCodeEditors.Select(editor => editor.GetType()).Where(editorType => editorType == externalCodeEditor.GetType()).Any())
                return;
            Editor.m_ExternalCodeEditors.Add(externalCodeEditor);
        }

        public static void Unregister(IExternalCodeEditor externalCodeEditor)
        {
            Editor.m_ExternalCodeEditors.Remove(externalCodeEditor);
        }

        public static IExternalCodeEditor CurrentEditor => Editor.Current;

        public static string CurrentEditorInstallation => EditorPrefs.GetString("kScriptsDefaultApp");

        public static bool OSOpenFile(string appPath, string arguments)
        {
            return ExternalEditor.OSOpenFileWithArgument(appPath, arguments);
        }

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
