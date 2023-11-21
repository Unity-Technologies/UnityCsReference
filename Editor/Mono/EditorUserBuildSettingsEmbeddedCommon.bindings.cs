// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    // Embedded Linux && QNX Architecture
    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    public enum EmbeddedArchitecture
    {
        [UnityEngine.InspectorName("Arm64")]
        Arm64 = 0,

        [System.Obsolete("Arm32 has been removed in 2023.2")]
        Arm32 = 1,

        [UnityEngine.InspectorName("X64")]
        X64 = 2,

        [System.Obsolete("X86 has been removed in 2023.2")]
        X86 = 3,
    }

    [NativeHeader("Editor/Src/EditorUserBuildSettings.h")]
    public partial class EditorUserBuildSettings
    {
        // Embedded Linux && QNX remote device information
        public static extern bool remoteDeviceInfo { get; set; }
        public static extern string remoteDeviceAddress { get; set; }
        public static extern string remoteDeviceUsername { get; set; }
        public static extern string remoteDeviceExports { get; set; }
        public static extern string pathOnRemoteDevice { get; set; }
    }
}
