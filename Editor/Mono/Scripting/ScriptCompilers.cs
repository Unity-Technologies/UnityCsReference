// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor.Utils;
using UnityEditor.Scripting.Compilers;
using UnityEngine;

namespace UnityEditor.Scripting
{
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
    }

    internal static class ScriptCompilers
    {
        internal static void Cleanup()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                var startInfo = new ProcessStartInfo()
                {
                    CreateNoWindow = true,
                    FileName = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "RoslynScripts", "kill_csc_server.bat")
                };

                var p = new Program(startInfo);
                p.Start();
            }
        }
    }
}
