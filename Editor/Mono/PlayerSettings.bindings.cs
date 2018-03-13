// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Runtime/Misc/PlayerSettings.h")]
    public partial class PlayerSettings : UnityEngine.Object
    {
        // If enabled, metal API validation will be turned on in the editor
        [NativeProperty("MetalAPIValidation")]
        public extern static bool enableMetalAPIValidation
        {
            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            get;
            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
            set;
        }
    }
}
