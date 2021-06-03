// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
        }
    }
}
