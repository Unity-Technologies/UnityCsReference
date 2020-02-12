// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.IO;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Utils;
using UnityEngine;

namespace UnityEditor.Scripting
{
    internal class NetCoreRunProgram : Program
    {
        static string netcoreRunPath;
        static bool? isNetCoreRunSupported;

        static NetCoreRunProgram()
        {
            netcoreRunPath = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "netcorerun", "netcorerun");

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                netcoreRunPath += ".exe";
            }
        }

        public NetCoreRunProgram(string executable, string arguments, Action<ProcessStartInfo> setupStartInfo)
        {
            if (!IsSupported())
            {
                throw new NotSupportedException("NetCoreRunProgram is not supported on this platform");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = netcoreRunPath,
                Arguments = $"{CommandLineFormatter.PrepareFileName(executable)} {arguments}",
                CreateNoWindow = true
            };

            if (setupStartInfo != null)
                setupStartInfo(startInfo);

            _process.StartInfo = startInfo;
        }

        public static bool IsSupported()
        {
            if (isNetCoreRunSupported.HasValue)
                return isNetCoreRunSupported.Value;

            if (!File.Exists(netcoreRunPath))
            {
                isNetCoreRunSupported = false;
            }
            // netcorerun.exe only works with macOS 10.13+
            else if (SystemInfo.operatingSystem.StartsWith("Mac OS X 10.", StringComparison.CurrentCulture))
            {
                var versionText = SystemInfo.operatingSystem.Substring(9);
                var version = new Version(versionText);

                if (version < new Version(10, 13))
                    isNetCoreRunSupported = false;
            }

            if (!isNetCoreRunSupported.HasValue)
                isNetCoreRunSupported = true;

            return isNetCoreRunSupported.Value;
        }
    }
}
