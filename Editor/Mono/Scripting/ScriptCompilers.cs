// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using UnityEditor.Utils;
using UnityEngine;

namespace UnityEditor.Scripting
{
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
