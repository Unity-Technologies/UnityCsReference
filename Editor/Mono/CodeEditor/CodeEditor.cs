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
        public struct Installation
        {
            public string Name;
            public string Path;
        }

        private readonly List<IExternalCodeEditor> m_ExternalCodeEditors = new List<IExternalCodeEditor>();
        private readonly IExternalCodeEditor m_DefaultEditor = new DefaultExternalCodeEditor();
        private IExternalCodeEditor m_CurrentEditor;
        private Installation m_CurrentInstallation;
        internal const string SystemDefaultPath = "";

        public IExternalCodeEditor CurrentCodeEditor {
            get
            {
                if(m_CurrentEditor == m_DefaultEditor && !IsCurrentEditorPathExplicitlySet)
                {
                    // try to resolve first found visual studio installation and enable it
                    try
                    {
                        var vs = m_ExternalCodeEditors.FirstOrDefault(e => e.GetType().Name == "VisualStudioEditor");
                        var installs = vs?.Installations;
                        if (installs != null && installs.Length > 0)
                        {
                            SetCodeEditor(installs[0].Path);
                        }
                    }
                    catch(Exception ex)
                    {
                        Debug.LogWarning($"Can't locate Visual Studio installation: {ex.Message}");
                    }
                }
                return m_CurrentEditor;
            }
        }
        public Installation CurrentInstallation => m_CurrentInstallation;

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static IExternalCodeEditor CurrentEditor => Editor.CurrentCodeEditor;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static string CurrentEditorInstallation => CurrentEditorPath;
        public static string CurrentEditorPath => EditorPrefs.GetString("kScriptsDefaultApp", "");
        private static bool IsCurrentEditorPathExplicitlySet => EditorPrefs.HasKey("kScriptsDefaultApp");

        public static CodeEditor Editor { get; } = new CodeEditor();

        public CodeEditor()
        {
            m_CurrentEditor = m_DefaultEditor;
            m_CurrentInstallation = GetInstallationForPath(CurrentEditorPath);
        }

        [RequiredByNativeCode]
        private static bool OpenProject(string path, int line, int column)
        {
            var didOpenProject = Editor.CurrentCodeEditor.OpenProject(path, line, column);
            if (didOpenProject)
            {
                CodeEditorAnalytics.SendCodeEditorUsage(CodeEditor.Editor.CurrentCodeEditor);
            }
            return didOpenProject;
        }

        [OnOpenAsset]
        private static bool OnOpenAsset(int instanceID, int line, int column)
        {
            var selected = EditorUtility.InstanceIDToObject(instanceID);
            var assetPath = AssetDatabase.GetAssetPath(selected);

            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            var assetFilePath = Path.GetFullPath(assetPath);
            var didOpenProject = Editor.CurrentCodeEditor.OpenProject(assetFilePath, line, column);
            if (didOpenProject)
            {
                CodeEditorAnalytics.SendCodeEditorUsage(CodeEditor.Editor.CurrentCodeEditor);
            }
            return didOpenProject;
        }

        public Installation GetInstallationForPath(string editorPath)
        {
            if (editorPath == null)
            {
                throw new ArgumentException("GetInstallationForPath: Does not allow null editorPath");
            }

            Installation installation;
            if (editorPath == CodeEditor.SystemDefaultPath)
            {
                m_DefaultEditor.TryGetInstallationForPath(editorPath, out installation);
                return installation;
            }

            foreach (var codeEditor in m_ExternalCodeEditors)
            {
                if (codeEditor.TryGetInstallationForPath(editorPath, out installation))
                {
                    return installation;
                }
            }
            m_DefaultEditor.TryGetInstallationForPath(editorPath, out installation);
            return installation;
        }

        public IExternalCodeEditor GetCodeEditorForPath(string editorPath)
        {
            if (editorPath == null)
            {
                throw new ArgumentException("GetCodeEditorForPath: Does not allow null editorPath");
            }

            if (editorPath == CodeEditor.SystemDefaultPath)
            {
                return m_DefaultEditor;
            }

            foreach (var codeEditor in m_ExternalCodeEditors)
            {
                if (codeEditor.TryGetInstallationForPath(editorPath, out _))
                {
                    return codeEditor;
                }
            }
            return m_DefaultEditor;
        }

        public Dictionary<string, string> GetFoundScriptEditorPaths()
        {
            var result = new Dictionary<string, string>();

            foreach (var installation in m_ExternalCodeEditors.SelectMany(codeEditor => codeEditor.Installations))
            {
                AddIfPathExists(installation.Name, installation.Path, result);
            }

            return result;
        }

        private static void AddIfPathExists(string name, string path, Dictionary<string, string> list)
        {
            if (list.ContainsKey(path)) return;
            if (Directory.Exists(path)) list.Add(path, name);
            else if (File.Exists(path)) list.Add(path, name);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static void SetExternalScriptEditor(string path)
        {
            Editor.SetCodeEditor(path);
        }

        public void SetCodeEditor(string editorPath)
        {
            if (editorPath == null)
            {
                throw new ArgumentException("SetCodeEditor: Does not allow null editorPath");
            }

            EditorPrefs.SetString("kScriptsDefaultApp", editorPath);
            m_CurrentEditor = ComputeCurrentEditor(editorPath);
            m_CurrentInstallation = GetInstallationForPath(editorPath);
            m_CurrentEditor.Initialize(editorPath);
        }

        private IExternalCodeEditor ComputeCurrentEditor(string editorPath)
        {
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

            return GetCodeEditorForPath(editorPath);
        }

        public static void Register(IExternalCodeEditor externalCodeEditor)
        {
            if (Editor.m_ExternalCodeEditors.Select(editor => editor.GetType()).Any(editorType => editorType == externalCodeEditor.GetType()))
                return;
            Editor.m_ExternalCodeEditors.Add(externalCodeEditor);
            if (IsCurrentEditorPathExplicitlySet)
            {
                CodeEditor.Editor.SetCodeEditor(Editor.m_CurrentInstallation.Path);
            }
        }

        public static void Unregister(IExternalCodeEditor externalCodeEditor)
        {
            Editor.m_ExternalCodeEditors.Remove(externalCodeEditor);
            CodeEditor.Editor.SetCodeEditor(Editor.m_CurrentInstallation.Path);
        }

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
        public static string QuoteForProcessStart(string argument)
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
