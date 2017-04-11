// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Utils;
using UnityEngine;

namespace UnityEditor.Scripting
{
    internal class NetCoreProgram : Program
    {
        public NetCoreProgram(string executable, string arguments, Action<ProcessStartInfo> setupStartInfo)
        {
            var dotnetExe = Paths.Combine(GetSdkRoot(), "dotnet");
            if (Application.platform == RuntimePlatform.WindowsEditor)
                dotnetExe = CommandLineFormatter.PrepareFileName(dotnetExe + ".exe");

            var startInfo = new ProcessStartInfo
            {
                Arguments = CommandLineFormatter.PrepareFileName(executable) + " " + arguments,
                CreateNoWindow = true,
                FileName = dotnetExe,
                WorkingDirectory = Application.dataPath + "/..",
            };

            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                // .NET Core needs to be able to find the newer openssl libraries that it requires on OSX
                var nativeDepsPath = Path.Combine(Path.Combine(Path.Combine(GetNetCoreRoot(), "NativeDeps"), "osx"), "lib");

                if (startInfo.EnvironmentVariables.ContainsKey("DYLD_LIBRARY_PATH"))
                    startInfo.EnvironmentVariables["DYLD_LIBRARY_PATH"] = string.Format("{0}:{1}", nativeDepsPath, startInfo.EnvironmentVariables["DYLD_LIBRARY_PATH"]);
                else
                    startInfo.EnvironmentVariables.Add("DYLD_LIBRARY_PATH", nativeDepsPath);
            }

            if (setupStartInfo != null)
                setupStartInfo(startInfo);

            _process.StartInfo = startInfo;
        }

        private static string GetSdkRoot()
        {
            return Path.Combine(GetNetCoreRoot(), "Sdk");
        }

        private static string GetNetCoreRoot()
        {
            return Path.Combine(MonoInstallationFinder.GetFrameWorksFolder(), "NetCore");
        }
    }
}
