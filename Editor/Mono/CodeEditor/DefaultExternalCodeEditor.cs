// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using System.IO;
using System.Linq;
using Unity.CodeEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    internal class DefaultExternalCodeEditor : IExternalCodeEditor
    {
        static readonly GUIContent k_ResetArguments = EditorGUIUtility.TrTextContent("Reset argument");
        static bool IsOSX => Application.platform == RuntimePlatform.OSXEditor;

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
                if (Application.platform == RuntimePlatform.OSXEditor)
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
                if (Application.platform == RuntimePlatform.OSXEditor)
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
                    m_ChosenInstallation = CodeEditor.CurrentEditorInstallation;
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
            installation = new CodeEditor.Installation
            {
                Name = Path.GetFileNameWithoutExtension(editorPath),
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

        static string[] defaultExtensions
        {
            get
            {
                var customExtensions = new[] {"json", "asmdef", "log"};
                return EditorSettings.projectGenerationBuiltinExtensions
                    .Concat(EditorSettings.projectGenerationUserExtensions)
                    .Concat(customExtensions)
                    .Distinct().ToArray();
            }
        }

        static bool SupportsExtension(string path)
        {
            var extension = Path.GetExtension(path);
            if (string.IsNullOrEmpty(extension))
                return false;
            return defaultExtensions.Contains(extension.TrimStart('.'));
        }

        public bool OpenProject(string path, int line, int column)
        {
            if (path != "" && !SupportsExtension(path)) // Assets - Open C# Project passes empty path here
            {
                return false;
            }

            string applicationPath = CodeEditor.CurrentEditorInstallation.Trim();
            if (applicationPath == CodeEditor.SystemDefaultPath)
            {
                return InternalEditorUtility.OpenFileAtLineExternal("", -1, -1);
            }

            if (IsOSX)
            {
                return CodeEditor.OSOpenFile(applicationPath, CodeEditor.ParseArgument(Arguments, path, line, column));
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = applicationPath,
                    Arguments = CodeEditor.ParseArgument(Arguments, path, line, column),
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
