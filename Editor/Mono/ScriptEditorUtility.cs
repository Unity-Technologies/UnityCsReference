// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor.Utils;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditorInternal
{
    public class ScriptEditorUtility
    {
        // Keep in sync with enum ScriptEditorType in ExternalEditor.h
        public enum ScriptEditor { Internal = 0, MonoDevelop = 1, VisualStudio = 2, VisualStudioExpress = 3, VisualStudioCode = 4, Rider = 5, Other = 32 }

        public static ScriptEditor GetScriptEditorFromPath(string path)
        {
            string lowerCasePath = path.ToLower();

            if (lowerCasePath == "internal")
                return ScriptEditor.Internal;

            if (lowerCasePath.Contains("monodevelop") || lowerCasePath.Contains("xamarinstudio") || lowerCasePath.Contains("xamarin studio"))
                return ScriptEditor.MonoDevelop;

            if (lowerCasePath.EndsWith("devenv.exe"))
                return ScriptEditor.VisualStudio;

            if (lowerCasePath.EndsWith("vcsexpress.exe"))
                return ScriptEditor.VisualStudioExpress;

            string filename = Path.GetFileName(Paths.UnifyDirectorySeparator(lowerCasePath)).Replace(" ", "");

            // Visual Studio for Mac is based on MonoDevelop
            if (filename == "visualstudio.app")
                return ScriptEditor.MonoDevelop;

            if (filename == "code.exe" || filename == "visualstudiocode.app" || filename == "vscode.app" || filename == "code.app" || filename == "code")
                return ScriptEditor.VisualStudioCode;

            if (filename == "rider.exe" || filename == "rider64.exe" || filename == "rider32.exe"
                || (filename.StartsWith("rider") && filename.EndsWith(".app")) || filename == "rider.sh")
                return ScriptEditor.Rider;

            return ScriptEditor.Other;
        }

        public static bool IsScriptEditorSpecial(string path)
        {
            return GetScriptEditorFromPath(path) != ScriptEditor.Other;
        }

        public static string GetExternalScriptEditor()
        {
            return EditorPrefs.GetString("kScriptsDefaultApp");
        }

        public static void SetExternalScriptEditor(string path)
        {
            EditorPrefs.SetString("kScriptsDefaultApp", path);
        }

        static string GetScriptEditorArgsKey(string path)
        {
            // Starting in Unity 5.5, we support setting script editor arguments on OSX and
            // use then when opening the script editor.
            // Before Unity 5.5, we would still save the default script editor args in EditorPrefs,
            // even though we never used them. This means that the user potentially has some
            // script editor args saved and once he upgrades to 5.5, they will be used when
            // open the script editor. Which unintended and causes a regression in behaviour.
            // So on OSX we change the key for per application for script editor args,
            // to avoid reading the one from previous versions.
            if (Application.platform == RuntimePlatform.OSXEditor)
                return "kScriptEditorArgs_" + path;

            return "kScriptEditorArgs" + path;
        }

        static string GetDefaultStringEditorArgs()
        {
            // On OSX there is a built-in mechanism for opening files in apps.
            // We use this mechanism when the external script editor args are not set.
            // Which was the only support behaviour before Unity 5.5. We therefor
            // default to this behavior.
            // If the script editor args are set, we only launch the script editor with args
            // specified and do not use the built-in mechanism for opening script files.
            if (Application.platform == RuntimePlatform.OSXEditor)
                return "";

            return "\"$(File)\"";
        }

        public static string GetExternalScriptEditorArgs()
        {
            string editor = GetExternalScriptEditor();

            if (IsScriptEditorSpecial(editor))
                return "";

            return EditorPrefs.GetString(GetScriptEditorArgsKey(editor), GetDefaultStringEditorArgs());
        }

        public static void SetExternalScriptEditorArgs(string args)
        {
            string editor = GetExternalScriptEditor();

            EditorPrefs.SetString(GetScriptEditorArgsKey(editor), args);
        }

        public static ScriptEditor GetScriptEditorFromPreferences()
        {
            return GetScriptEditorFromPath(GetExternalScriptEditor());
        }

        public static string[] GetFoundScriptEditorPaths(RuntimePlatform platform)
        {
            var result = new List<string>();

            if (platform == RuntimePlatform.OSXEditor)
            {
                AddIfDirectoryExists("/Applications/Visual Studio.app", result);
            }

            return result.ToArray();
        }

        static void AddIfDirectoryExists(string path, List<string> list)
        {
            if (Directory.Exists(path))
                list.Add(path);
        }
    }
}
