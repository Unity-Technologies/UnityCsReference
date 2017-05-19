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
using Debug = UnityEngine.Debug;

namespace UnityEditor.Scripting
{
    internal class NetCoreProgram : Program
    {
        public NetCoreProgram(string executable, string arguments, Action<ProcessStartInfo> setupStartInfo)
        {
            if (!IsNetCoreAvailable())
            {
                Debug.LogError("Creating NetCoreProgram, but IsNetCoreAvailable() == false; fix the caller!");
                // let it happen anyway to preserve previous behaviour
            }

            var startInfo = CreateDotNetCoreStartInfoForArgs(CommandLineFormatter.PrepareFileName(executable) + " " + arguments);

            if (setupStartInfo != null)
                setupStartInfo(startInfo);

            _process.StartInfo = startInfo;
        }

        private static ProcessStartInfo CreateDotNetCoreStartInfoForArgs(string arguments)
        {
            var dotnetExe = Paths.Combine(GetSdkRoot(), "dotnet");
            if (Application.platform == RuntimePlatform.WindowsEditor)
                dotnetExe = CommandLineFormatter.PrepareFileName(dotnetExe + ".exe");

            var startInfo = new ProcessStartInfo
            {
                Arguments = arguments,
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

            return startInfo;
        }

        private static string GetSdkRoot()
        {
            return Path.Combine(GetNetCoreRoot(), "Sdk");
        }

        private static string GetNetCoreRoot()
        {
            return Path.Combine(MonoInstallationFinder.GetFrameWorksFolder(), "NetCore");
        }

        private static bool s_NetCoreAvailableChecked = false;
        private static bool s_NetCoreAvailable = false;
        public static bool IsNetCoreAvailable()
        {
            if (!s_NetCoreAvailableChecked)
            {
                s_NetCoreAvailableChecked = true;

                var startInfo = CreateDotNetCoreStartInfoForArgs("--version");
                var getVersionProg = new Program(startInfo);
                try
                {
                    getVersionProg.Start();
                }
                catch (Exception ex)
                {
                    Debug.LogWarningFormat("Disabling CoreCLR, got exception trying to run with --version: {0}", ex);
                    return false;
                }

                getVersionProg.WaitForExit(5000);
                if (!getVersionProg.HasExited)
                {
                    getVersionProg.Kill();
                    Debug.LogWarning("Disabling CoreCLR, timed out trying to run with --version");
                    return false;
                }

                if (getVersionProg.ExitCode != 0)
                {
                    Debug.LogWarningFormat("Disabling CoreCLR, got non-zero exit code: {0}, stderr: '{1}'",
                        getVersionProg.ExitCode, getVersionProg.GetErrorOutputAsString());
                    return false;
                }

                s_NetCoreAvailable = true;
            }
            return s_NetCoreAvailable;
        }
    }
}
