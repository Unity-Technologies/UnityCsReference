// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Scripting;
using UnityEditor;
using UnityEditor.Utils;

using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Unity.CodeEditor;

namespace UnityEditorInternal
{
    public class ScriptEditorUtility
    {
        // Keep in sync with enum ScriptEditorType in ExternalEditor.h
        [Obsolete("This will be removed", false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public enum ScriptEditor
        {
            SystemDefault = 0,
            MonoDevelop = 1,
            VisualStudio = 2,
            VisualStudioExpress = 3,
            Other = 32
        }

        public struct Installation
        {
            public string Name;
            public string Path;
        }

        static readonly List<Func<Installation[]>> k_PathCallbacks = new List<Func<Installation[]>>();

        [Obsolete("Use UnityEditor.ScriptEditor.Register()", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static void RegisterIde(Func<Installation[]> pathCallBack)
        {
            k_PathCallbacks.Add(pathCallBack);
        }

        [Obsolete("This functionality is going to be removed. See IExternalCodeEditor for more information", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static ScriptEditor GetScriptEditorFromPath(string path)
        {
            string lowerCasePath = path.ToLower();

            if (lowerCasePath == CodeEditor.SystemDefaultPath)
                return ScriptEditor.SystemDefault;

            return ScriptEditor.Other;
        }

        [RequiredByNativeCode]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static string GetExternalScriptEditor()
        {
            return CodeEditor.CurrentEditorPath;
        }

        [Obsolete("This method has been moved to CodeEditor.SetExternalScriptEditor", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static void SetExternalScriptEditor(string path)
        {
            CodeEditor.SetExternalScriptEditor(path);
        }

        [Obsolete("Use UnityEditor.ScriptEditor.GetCurrentEditor()", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static ScriptEditor GetScriptEditorFromPreferences()
        {
            return GetScriptEditorFromPath(CodeEditor.CurrentEditorInstallation);
        }

        [Obsolete("This method is being internalized, please use UnityEditorInternal.CodeEditorUtility.GetFoundScriptEditorPaths", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static Dictionary<string, string> GetFoundScriptEditorPaths(RuntimePlatform platform)
        {
            return CodeEditor.Editor.GetFoundScriptEditorPaths();
        }

        private static void AddIfDirectoryExists(string name, string path, Dictionary<string, string> list)
        {
            if (list.ContainsKey(path))
                return;
            if (Directory.Exists(path)) list.Add(path, name);
            else if (File.Exists(path)) list.Add(path, name);
        }
    }
}
