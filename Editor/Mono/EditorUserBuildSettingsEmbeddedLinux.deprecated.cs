// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

// wrapper class to provide similar interface as old settings. old: UnityEditor.EditorUserBuildSettings.selectedEmbeddedLinuxArchitecture   ... new: UnityEditor.EditorUserBuildSettings.EmbeddedLinux.architecture
namespace UnityEditor
{
    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    [Obsolete("EmbeddedLinuxArchitecture is deprecated. Use EmbeddedArchitecture. (UnityUpgradable) -> EmbeddedArchitecture", false)]
    public enum EmbeddedLinuxArchitecture
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
        #pragma warning disable 0618
        [Obsolete("EditorUserBuildSettings.selectedEmbeddedLinuxArchitecture is deprecated. EmbeddedLinux.Settings.architecture instead. (UnityUpgradable) -> [UnityEditor.EmbeddedLinux.Extensions] UnityEditor.EmbeddedLinux.Settings.architecture", false)]
        public static extern EmbeddedLinuxArchitecture selectedEmbeddedLinuxArchitecture
        {
            [NativeMethod("GetSelectedEmbeddedLinuxArchitecture")]
            get;
            [NativeMethod("SetSelectedEmbeddedLinuxArchitecture")]
            set;
        }
        #pragma warning restore 0618
    }
}

