// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Multiplayer.PlayMode.Editor;

[NativeHeader("Editor/Platform/Interface/EditorUtility.h")]
static class ProcessWindowUtility
{
    const int k_SwRestore = 9;

    // Only macOS and Windows can bring another process's window to the front; on Linux the native
    // implementation only accepts the editor's own GTK windows.
    internal static bool IsSupported =>
        Application.platform is RuntimePlatform.OSXEditor or RuntimePlatform.WindowsEditor;

    internal static bool TryBringToFront(int processId)
    {
        if (!IsSupported)
            return false;

        using var process = ProcessSystem.GetProcessById(processId);
        if (process == null || process.HasExited)
            return false;

        // The native call takes a process id on macOS and a window handle on Windows.
        var handle = Application.platform == RuntimePlatform.WindowsEditor
            ? GetMainWindowHandle(process)
            : processId;
        if (handle == 0)
            return false;

        // SetForegroundWindow does not restore a minimized window.
        if (Application.platform == RuntimePlatform.WindowsEditor && IsIconic(new IntPtr(handle)))
            ShowWindowAsync(new IntPtr(handle), k_SwRestore);

        SetWindowInFront(handle);
        return true;
    }

    static int GetMainWindowHandle(Process process)
    {
        process.Refresh();
        // Window handles are guaranteed to fit in 32 bits for interop between 32 and 64-bit processes.
        return process.MainWindowHandle.ToInt32();
    }

    // The native parameter is a C++ long, which is 32 bits on Windows, so an int is passed to avoid narrowing.
    [FreeFunction("SetWindowInFront")]
    static extern void SetWindowInFront(int handle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsIconic(IntPtr windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool ShowWindowAsync(IntPtr windowHandle, int command);
}
