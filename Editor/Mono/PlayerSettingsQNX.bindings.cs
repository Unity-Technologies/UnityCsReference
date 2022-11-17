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
        public sealed partial class QNX
        {
            // Custom path to store data files
            [NativeProperty("HmiPlayerDataPath")]
            public static extern string playerDataPath { get; set; }

            [NativeProperty("HmiForceSRGBBlit")]
            public static extern bool forceSRGBBlit { get; set; }

            [NativeProperty("HmiCpuConfiguration")]
            public static extern int[] cpuConfiguration { get; set; }

            [NativeProperty("HmiLoadingImage")]
            public static extern Texture2D hmiLoadingImage { get; set; }

            [NativeProperty("HmiLogStartupTiming")]
            public static extern bool hmiLogStartupTiming { get; set; }
        }
    }
}
