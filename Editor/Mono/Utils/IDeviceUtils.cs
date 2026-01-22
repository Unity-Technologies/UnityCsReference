// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Modules;
using UnityEngine.Scripting;

namespace UnityEditor
{
    internal static class IDeviceUtils
    {
        // API for native calls
        [RequiredByNativeCode]
        internal static void StartRemoteSupport(string deviceId, out string ip, out int port)
        {
            IDevice device = ModuleManager.GetDevice(deviceId);
            device.StartRemoteSupport(out ip, out port);
        }

        // API for native calls
        [RequiredByNativeCode]
        internal static void StopRemoteSupport(string deviceId)
        {
            IDevice device = ModuleManager.GetDevice(deviceId);
            device.StopRemoteSupport();
        }

        // API for native calls
        [RequiredByNativeCode]
        internal static void StartPlayerConnectionSupport(string deviceId, out string ip, out int port)
        {
            IDevice device = ModuleManager.GetDevice(deviceId);
            device.StartPlayerConnectionSupport(out ip, out port);
        }

        // API for native calls
        [RequiredByNativeCode]
        internal static void StopPlayerConnectionSupport(string deviceId)
        {
            IDevice device = ModuleManager.GetDevice(deviceId);
            device.StopPlayerConnectionSupport();
        }
    }
}
