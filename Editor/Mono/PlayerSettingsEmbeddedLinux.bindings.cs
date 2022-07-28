// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
    public partial class PlayerSettings : UnityObject
    {
        [NativeHeader("Runtime/Misc/PlayerSettings.h")]
        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        public sealed partial class EmbeddedLinux
        {
            // Custom path to store data files
            [NativeProperty("PlayerDataPath")]
            public static extern string playerDataPath { get; set; }

            [NativeProperty("ForceSRGBBlit")]
            public static extern bool forceSRGBBlit { get; set; }

            [NativeProperty("EnableGamepadInput")]
            public static extern bool enableGamepadInput { get; set; }

            [NativeProperty("CpuConfiguration")]
            public static extern int[] cpuConfiguration { get; set; }

            [NativeProperty("HmiLoadingImage")]
            public static extern Texture2D hmiLoadingImage { get; set; }
        }
    }
}
