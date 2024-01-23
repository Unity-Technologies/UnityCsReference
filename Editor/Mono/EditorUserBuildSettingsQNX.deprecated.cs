// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
//using UnityEditor.QNX;

// wrapper class to provide similar interface as old settings. old: UnityEditor.EditorUserBuildSettings.selectedQnxArchitecture   ... new: UnityEditor.EditorUserBuildSettings.QNX.architecture
namespace UnityEditor
{
    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    [Obsolete("QNXArchitecture is deprecated. Use EmbeddedArchitecture. (UnityUpgradable) -> EmbeddedArchitecture", false)]
    public enum QNXArchitecture
    {
        [UnityEngine.InspectorName("Arm64")]
        Arm64 = EmbeddedArchitecture.Arm64,

        [System.Obsolete("Arm32 has been removed in 2023.2")]
        Arm32 = EmbeddedArchitecture.Arm32,

        [UnityEngine.InspectorName("X64")]
        X64 = EmbeddedArchitecture.X64,

        [System.Obsolete("X86 has been removed in 2023.2")]
        X86 = EmbeddedArchitecture.X86,
    }

    public partial class EditorUserBuildSettings
    {
        [Obsolete("EditorUserBuildSettings.selectedQnxOsVersion is deprecated. Use QNX.Settings.osVersion instead. (UnityUpgradable) -> [UnityEditor.QNX.Extensions] UnityEditor.QNX.Settings.osVersion", false)]
        public static extern QNXOsVersion selectedQnxOsVersion
        {
            [NativeMethod("GetSelectedQNXOsVersion")]
            get;
            [NativeMethod("SetSelectedQNXOsVersion")]
            set;
        }

        #pragma warning disable 0618
        [Obsolete("EditorUserBuildSettings.selectedQnxArchitecture is deprecated. Use QNX.Settings.architecture instead. (UnityUpgradable) -> [UnityEditor.QNX.Extensions] UnityEditor.QNX.Settings.architecture", false)]
        public static extern QNXArchitecture selectedQnxArchitecture
        {
            [NativeMethod("GetSelectedQNXArchitecture")]
            get;
            [NativeMethod("SetSelectedQNXArchitecture")]
            set;
        }
        #pragma warning restore 0618
    }
}

