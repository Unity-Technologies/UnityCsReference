// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Modules;


namespace UnityEditor
{
    internal static class IDeviceUtils
    {
        // API for native calls
        internal static RemoteAddress StartRemoteSupport(string deviceId)
        {
            IDevice device = ModuleManager.GetDevice(deviceId);
            return device.StartRemoteSupport();
        }

        // API for native calls
        internal static void StopRemoteSupport(string deviceId)
        {
            IDevice device = ModuleManager.GetDevice(deviceId);
            device.StopRemoteSupport();
        }

        // API for native calls
        internal static RemoteAddress StartPlayerConnectionSupport(string deviceId)
        {
            IDevice device = ModuleManager.GetDevice(deviceId);
            return device.StartPlayerConnectionSupport();
        }

        // API for native calls
        internal static void StopPlayerConnectionSupport(string deviceId)
        {
            IDevice device = ModuleManager.GetDevice(deviceId);
            device.StopPlayerConnectionSupport();
        }
    }
}
