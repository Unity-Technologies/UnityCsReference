// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using System.Runtime.InteropServices;
using Unity.Bindings;
using UnityEngine.Scripting;

namespace UnityEditorInternal.VR
{
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeStruct(GenerateMarshallingType = NativeStructGenerateOption.UseCustomStruct)]
    public partial struct VRDeviceInfoEditor
    {
        public string deviceNameKey;
        public string deviceNameUI;
        public string externalPluginName;
        public bool supportsEditorMode;
        public bool inListByDefault;
    }

    [NativeType(Header = "./Editor/Src/VR/VREditor.bindings.h")]
    public sealed partial class VREditor
    {
        extern public static  VRDeviceInfoEditor[] GetAllVRDeviceInfo(BuildTargetGroup targetGroup);

        extern public static  VRDeviceInfoEditor[] GetAllVRDeviceInfoByTarget(BuildTarget target);

        extern public static  bool GetVREnabledOnTargetGroup(BuildTargetGroup targetGroup);

        extern public static  void SetVREnabledOnTargetGroup(BuildTargetGroup targetGroup, bool value);

        extern public static  string[] GetVREnabledDevicesOnTargetGroup(BuildTargetGroup targetGroup);

        extern public static  string[] GetVREnabledDevicesOnTarget(BuildTarget target);

        extern public static  void SetVREnabledDevicesOnTargetGroup(BuildTargetGroup targetGroup, string[] devices);
    }
}
