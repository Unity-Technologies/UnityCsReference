// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.CodeEditor;
using UnityEditor.Utils;
using UnityEditor.VisualStudioIntegration;

namespace UnityEditorInternal
{
    public class ScriptEditorUtility
    {
        // Keep in sync with enum ScriptEditorType in ExternalEditor.h
        public enum ScriptEditor { SystemDefault = 0, MonoDevelop = 1, VisualStudio = 2, VisualStudioExpress = 3, Other = 32 }

        public struct Installation
        {
            public string Name;
            public string Path;
        }

        static readonly List<Func<Installation[]>> k_PathCallbacks = new List<Func<Installation[]>>();

        [Obsolete("Use UnityEditor.ScriptEditor.Register()", false)]
        public static void RegisterIde(Func<Installation[]> pathCallBack)
        {
            k_PathCallbacks.Add(pathCallBack);
        }

        [Obsolete("This functionality is going to be removed. See IExternalCodeEditor for more information", false)]
        public static ScriptEditor GetScriptEditorFromPath(string path)
        {
            string lowerCasePath = path.ToLower();

            if (lowerCasePath == CodeEditor.SystemDefaultPath)
                return ScriptEditor.SystemDefault;

            // Disable internal Visual Studio if the package is loaded.
            if (!UnityVSSupport.IsDefaultExternalCodeEditor())
                return ScriptEditor.Other;

            if (lowerCasePath.Contains("monodevelop") || lowerCasePath.Contains("xamarinstudio") || lowerCasePath.Contains("xamarin studio"))
                return ScriptEditor.MonoDevelop;

            if (lowerCasePath.EndsWith("devenv.exe"))
                return ScriptEditor.VisualStudio;

            if (lowerCasePath.EndsWith("vcsexpress.exe") || lowerCasePath.EndsWith("wdexpress.exe"))
                return ScriptEditor.VisualStudioExpress;

            // Visual Studio for Mac is based on MonoDevelop
            if (IsVisualStudioForMac(path))
                return ScriptEditor.MonoDevelop;

            return ScriptEditor.Other;
        }

        internal static bool IsVisualStudioForMac(string path)
        {
            var lowerCasePath = path.ToLower();
            var filename = Path.GetFileName(Paths.UnifyDirectorySeparator(lowerCasePath)).Replace(" ", "");
            return filename.StartsWith("visualstudio") && !filename.Contains("code") && filename.EndsWith(".app");
        }

        public static string GetExternalScriptEditor()
        {
            return CodeEditor.CurrentEditorInstallation;
        }

        [Obsolete("This method has been moved to CodeEditor.SetExternalScriptEditor", false)]
        public static void SetExternalScriptEditor(string path)
        {
            CodeEditor.SetExternalScriptEditor(path);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This functionality has been moved to the IExternalCodeEditor packages", true)]
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
            throw new NotSupportedException("This functionality has been moved to the IExternalCodeEditor packages");
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This functionality has been moved to the IExternalCodeEditor packages", true)]
        public static void SetExternalScriptEditorArgs(string args)
        {
            throw new NotSupportedException("This functionality has been moved to the IExternalCodeEditor packages");
        }

        [Obsolete("Use UnityEditor.ScriptEditor.GetCurrentEditor()", false)]
        public static ScriptEditor GetScriptEditorFromPreferences()
        {
            return GetScriptEditorFromPath(CodeEditor.CurrentEditorInstallation);
        }

        [Obsolete("This method is being internalized, please use UnityEditorInternal.CodeEditorUtility.GetFoundScriptEditorPaths", false)]
        public static Dictionary<string, string> GetFoundScriptEditorPaths(RuntimePlatform platform)
        {
            return CodeEditor.Editor.GetFoundScriptEditorPaths();
        }

        static void AddIfDirectoryExists(string name, string path, Dictionary<string, string> list)
        {
            if (list.ContainsKey(path))
                return;
            if (Directory.Exists(path)) list.Add(path, name);
            else if (File.Exists(path)) list.Add(path, name);
        }
    }
}
