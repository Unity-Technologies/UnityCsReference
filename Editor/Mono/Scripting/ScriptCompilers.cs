// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEditor.Utils;
using UnityEngine;

namespace UnityEditor.Scripting
{
    internal static class ScriptCompilers
    {
        [Flags]
        enum ProcessCreationFlags : uint
        {
            CREATE_NO_WINDOW = 0x08000000,
        }

        struct STARTUPINFOW
        {
            public uint cb;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpReserved;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpDesktop;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public ushort wShowWindow;
            public ushort cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CreateProcessW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpApplicationName,
            [MarshalAs(UnmanagedType.LPWStr)] string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            ProcessCreationFlags dwCreationFlags,
            IntPtr lpEnvironment,
            [MarshalAs(UnmanagedType.LPWStr)] string lpCurrentDirectory,
            ref STARTUPINFOW lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        internal static void Cleanup()
        {
            var isWindows = Application.platform == RuntimePlatform.WindowsEditor;
            if (isWindows)
            {
                // Use CreateProcessW as opposed to C# Process class to run
                // the script so that we could disable handle inheritance
                STARTUPINFOW startInfo = default;
                startInfo.cb = (uint)Marshal.SizeOf<STARTUPINFOW>();

                if (!CreateProcessW(null, $"\"{NetCoreProgram.DotNetMuxerPath}\" build-server shutdown", IntPtr.Zero, IntPtr.Zero, false, ProcessCreationFlags.CREATE_NO_WINDOW, IntPtr.Zero, null, ref startInfo, out var _))
                {
                    var lastError = Marshal.GetLastWin32Error();
                    Debug.LogError("Failed to kill csc server process: " + new Win32Exception(lastError).Message);
                }
            }
            else
            {
                // Fire-and-forget: Don't wait for exit to avoid blocking the editor
                using var _ = System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(NetCoreProgram.DotNetMuxerPath.ToString())
                    {
                        Arguments = "build-server shutdown",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
            }
        }
    }
}
