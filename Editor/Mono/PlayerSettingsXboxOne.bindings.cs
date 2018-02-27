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
        public sealed partial class XboxOne
        {
            [NativeProperty("XboxOneXTitleMemory", TargetType.Field)]
            extern public static int XTitleMemory
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }
        }
    }
}
