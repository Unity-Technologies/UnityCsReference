// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace UnityEditorInternal.VR
{
    partial class VREditor
    {
        [Obsolete("Use GetVREnabledOnTargetGroup instead.")]
        public static bool GetVREnabled(BuildTargetGroup targetGroup)
        {
            return GetVREnabledOnTargetGroup(targetGroup);
        }

        [Obsolete("UseSetVREnabledOnTargetGroup instead.")]
        public static void SetVREnabled(BuildTargetGroup targetGroup, bool value)
        {
            SetVREnabledOnTargetGroup(targetGroup, value);
        }

        [Obsolete("Use GetVREnabledDevicesOnTargetGroup instead.")]
        public static string[] GetVREnabledDevices(BuildTargetGroup targetGroup)
        {
            return GetVREnabledDevicesOnTargetGroup(targetGroup);
        }

        [Obsolete("Use SetVREnabledDevicesOnTargetGroup instead.")]
        public static void SetVREnabledDevices(BuildTargetGroup targetGroup, string[] devices)
        {
            SetVREnabledDevicesOnTargetGroup(targetGroup, devices);
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
    }
}
