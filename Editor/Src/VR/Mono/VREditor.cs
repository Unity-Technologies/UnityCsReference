// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal.VR;

namespace UnityEditorInternal.VR
{
    partial class VREditor
    {
        private static Dictionary<BuildTargetGroup, bool> dirtyDeviceLists = new Dictionary<BuildTargetGroup, bool>();

        public static bool IsDeviceListDirty(BuildTargetGroup targetGroup)
        {
            if (dirtyDeviceLists.ContainsKey(targetGroup))
                return dirtyDeviceLists[targetGroup];

            return false;
        }

        private static void SetDeviceListDirty(BuildTargetGroup targetGroup)
        {
            if (dirtyDeviceLists.ContainsKey(targetGroup))
                dirtyDeviceLists[targetGroup] = true;
            else
                dirtyDeviceLists.Add(targetGroup, true);
        }

        public static void ClearDeviceListDirty(BuildTargetGroup targetGroup)
        {
            if (dirtyDeviceLists.ContainsKey(targetGroup))
                dirtyDeviceLists[targetGroup] = false;
        }

        public static VRDeviceInfoEditor[] GetEnabledVRDeviceInfo(BuildTargetGroup targetGroup)
        {
            string[] enabledVRDevices = GetVREnabledDevicesOnTargetGroup(targetGroup);
            return GetAllVRDeviceInfo(targetGroup).Where(d => enabledVRDevices.Contains(d.deviceNameKey)).ToArray();
        }

        public static VRDeviceInfoEditor[] GetEnabledVRDeviceInfo(BuildTarget target)
        {
            string[] enabledVRDevices = GetVREnabledDevicesOnTarget(target);
            return GetAllVRDeviceInfoByTarget(target).Where(d => enabledVRDevices.Contains(d.deviceNameKey)).ToArray();
        }

        public static bool IsVRDeviceEnabledForBuildTarget(BuildTarget target, string deviceName)
        {
            string[] vrDevices = GetVREnabledDevicesOnTarget(target);
            foreach (string device in vrDevices)
            {
                if (device == deviceName)
                    return true;
            }
            return false;
        }

        public static string[] GetAvailableVirtualRealitySDKs(BuildTargetGroup targetGroup)
        {
            VRDeviceInfoEditor[] deviceInfos = GetAllVRDeviceInfo(targetGroup);
            string[] sdks = new string[deviceInfos.Length];

            for (int i = 0; i < deviceInfos.Length; ++i)
            {
                sdks[i] = deviceInfos[i].deviceNameKey;
            }

            return sdks;
        }

        // APIs Exposed to PlayerSettings for Scripting Reference
        public static string[] GetVirtualRealitySDKs(BuildTargetGroup targetGroup)
        {
            return GetVREnabledDevicesOnTargetGroup(targetGroup);
        }

        public static void SetVirtualRealitySDKs(BuildTargetGroup targetGroup, string[] sdks)
        {
            SetVREnabledDevicesOnTargetGroup(targetGroup, sdks);
            SetDeviceListDirty(targetGroup);
        }
    }
}

namespace UnityEditor
{
    partial class PlayerSettings
    {
        public static string[] GetAvailableVirtualRealitySDKs(BuildTargetGroup targetGroup)
        {
            return VREditor.GetAvailableVirtualRealitySDKs(targetGroup);
        }

        public static bool GetVirtualRealitySupported(BuildTargetGroup targetGroup)
        {
            return VREditor.GetVREnabledOnTargetGroup(targetGroup);
        }

        public static void SetVirtualRealitySupported(BuildTargetGroup targetGroup, bool value)
        {
            VREditor.SetVREnabledOnTargetGroup(targetGroup, value);
        }

        public static string[] GetVirtualRealitySDKs(BuildTargetGroup targetGroup)
        {
            return VREditor.GetVirtualRealitySDKs(targetGroup);
        }

        public static void SetVirtualRealitySDKs(BuildTargetGroup targetGroup, string[] sdks)
        {
            VREditor.SetVirtualRealitySDKs(targetGroup, sdks);
        }
    }
}
