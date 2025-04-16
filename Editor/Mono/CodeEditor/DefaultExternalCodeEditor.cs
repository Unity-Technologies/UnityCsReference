// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using System.IO;
using System.Linq;
using Unity.CodeEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using NiceIO;

namespace UnityEditor
{
    internal class DefaultExternalCodeEditor : IExternalCodeEditor
    {
        static readonly GUIContent k_ResetArguments = EditorGUIUtility.TrTextContent("Reset argument");
        static readonly string[] supportedExtensions = { "json", "asmdef", "log", "cs", "uxml", "uss", "shader", "compute", "cginc", "hlsl", "glslinc", "template", "raytrace" };
        static bool IsOSX => Application.platform == RuntimePlatform.OSXEditor;
        static bool IsWindows => Application.platform == RuntimePlatform.WindowsEditor;
        static bool IsLinux => Application.platform == RuntimePlatform.LinuxEditor;

        string m_ChosenInstallation;

        const string k_ArgumentKey = "kScriptEditorArgs";
        const string k_DefaultArgument = "$(File)";

        string Arguments
        {
            get
            {
                // Starting in Unity 5.5, we support setting script editor arguments on OSX and
                // use then when opening the script editor.
                // Before Unity 5.5, we would still save the default script editor args in EditorPrefs,
                // even though we never used them. This means that the user potentially has some
                // script editor args saved and once he upgrades to 5.5, they will be used when
                // open the script editor. Which unintended and causes a regression in behaviour.
                // So on OSX we change the key for per application for script editor args,
                // to avoid reading the one from previous versions.
                // The year 2021: Delete mac hack.
                if (IsOSX)
                {
                    var oldMac = EditorPrefs.GetString("kScriptEditorArgs_" + Installation);
                    if (!string.IsNullOrEmpty(oldMac))
                    {
                        EditorPrefs.SetString(k_ArgumentKey, oldMac);
                    }
                }

                return EditorPrefs.GetString(k_ArgumentKey + Installation, k_DefaultArgument);
            }
            set
            {
                if (IsOSX)
                {
                    EditorPrefs.SetString("kScriptEditorArgs_" + Installation, value);
                }

                EditorPrefs.SetString(k_ArgumentKey + Installation, value);
            }
        }

        string Installation
        {
            get
            {
                if (m_ChosenInstallation == null)
                    m_ChosenInstallation = CodeEditor.CurrentEditorPath;
                return m_ChosenInstallation;
            }
            set
            {
                m_ChosenInstallation = value;
            }
        }

        public CodeEditor.Installation[] Installations { get; }
        public bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation)
        {
            if (string.IsNullOrEmpty(editorPath))
            {
                installation = new CodeEditor.Installation
                {
                    Name = "",
                    Path = ""
                };
                return false;
            }
            installation = new CodeEditor.Installation
            {
                Name = OSUtil.GetAppFriendlyName(editorPath) + " (internal)",
                Path = editorPath
            };
            return true;
        }

        public void OnGUI()
        {
            Arguments = EditorGUILayout.TextField("External Script Editor Args", Arguments);
            if (GUILayout.Button(k_ResetArguments, GUILayout.Width(120)))
            {
                Arguments = k_DefaultArgument;
            }
        }

        public void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
        {
        }

        public void SyncAll()
        {
        }

        public void Initialize(string editorInstallationPath)
        {
        }

        static string[] DefaultExtensions =>
            EditorSettings.projectGenerationUserExtensions
                .Concat(supportedExtensions)
                .Distinct().ToArray();

        static bool SupportsExtension(string path)
        {
            var extension = Path.GetExtension(path);
            if (string.IsNullOrEmpty(extension))
                return false;
            return DefaultExtensions.Contains(extension.TrimStart('.'));
        }

        public bool OpenProject(string path, int line, int column)
        {
            if (path != CodeEditor.SystemDefaultPath && !SupportsExtension(path)) // Assets - Open C# Project passes empty path here
            {
                return false;
            }

            var applicationPath = CodeEditor.CurrentEditorPath.Trim();
            var doesNotExistWarning =
                $"External Code Editor application path does not exist ({applicationPath})! Please select a different application.";

            var npath = new NPath(applicationPath);
            if (applicationPath == null || !npath.Exists())
            {
                UnityEngine.Debug.LogWarning(doesNotExistWarning);
                return false;
            }

            if (applicationPath == CodeEditor.SystemDefaultPath)
            {
                return InternalEditorUtility.OpenFileAtLineExternal(path, -1, -1);
            }

            string fileName = "";
            string arguments = "";
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor:
                    return CodeEditor.OSOpenFile(applicationPath, CodeEditor.ParseArgument(Arguments, path, line, column));
                case RuntimePlatform.LinuxEditor:
                    fileName = applicationPath;
                    arguments = CodeEditor.ParseArgument(Arguments, path, line, column);
                    break;
                case RuntimePlatform.WindowsEditor:
                    fileName = "cmd.exe";
                    arguments = "/C \"" + CodeEditor.QuoteForProcessStart(applicationPath) +
                                " " + CodeEditor.ParseArgument(Arguments, path, line, column) + "\"";
                    break;
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = true,
                }
            };
            var result = process.Start();
            return result;
        }
    }
}
