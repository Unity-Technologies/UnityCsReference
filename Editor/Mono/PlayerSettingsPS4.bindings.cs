// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using System.Runtime.CompilerServices;

namespace UnityEditor
{
    public sealed partial class PlayerSettings
    {
        [NativeHeader("Runtime/Misc/PlayerSettings.h")]
        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
        public sealed partial class PS4
        {
            [NativeProperty(TargetType = TargetType.Field)] extern public static bool enableApplicationExit { get; set; }
        }
    }
}
