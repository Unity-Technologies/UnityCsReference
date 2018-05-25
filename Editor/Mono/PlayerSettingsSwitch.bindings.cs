// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;

namespace UnityEditor
{
    // Player Settings is where you define various parameters for the final game that you will build in Unity. Some of these values are used in the Resolution Dialog that launches when you open a standalone game.
    public sealed partial class PlayerSettings
    {
        // Nintendo Switch specific player settings
        [NativeHeader("Runtime/Misc/PlayerSettings.h")]
        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
        public sealed partial class Switch
        {
            [NativeProperty("switchNativeFsCacheSize", TargetType.Field)]
            extern public static int nativeFsCacheSize { get; set; }
        }
    }
}
