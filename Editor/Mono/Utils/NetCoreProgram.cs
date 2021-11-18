// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using NiceIO;
using UnityEditor.Utils;
using UnityEngine;

namespace UnityEditor.Scripting
{
    internal class NetCoreProgram : Program
    {
        public static readonly NPath DotNetRuntimePath = EditorApplication.applicationContentsPath + "/NetCoreRuntime";
        public static readonly NPath DotNetMuxerPath = DotNetRuntimePath.Combine(Application.platform == RuntimePlatform.WindowsEditor ? "dotnet.exe" : "dotnet");

        public NetCoreProgram(string executable, string arguments, Action<ProcessStartInfo> setupStartInfo)
        {
            _process.StartInfo = new ProcessStartInfo
            {
                Arguments = $"\"{executable}\" {arguments}",
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = DotNetMuxerPath.ToString(SlashMode.Native),
                WorkingDirectory = new NPath(Application.dataPath).Parent.ToString(SlashMode.Native),
            };
            _process.StartInfo.EnvironmentVariables["DOTNET_ROOT"] = DotNetRuntimePath.ToString(SlashMode.Native); // if unity is run from a process where it is already set, the
            // initialization expression we used to have would fail
            _process.StartInfo.EnvironmentVariables["DOTNET_MULTILEVEL_LOOKUP"] = "0";
            setupStartInfo?.Invoke(_process.StartInfo);
        }
    }
}
