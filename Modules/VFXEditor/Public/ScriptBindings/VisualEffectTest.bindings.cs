// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace UnityEditor.Experimental.VFX
{
    [NativeHeader("Modules/VFXEditor/Public/VisualEffectTest.h")]
    internal static class VisualEffectTest
    {
        [FreeFunction(Name = "VisualEffectTest::DebugCopyBufferComputeTest")]
        extern public static bool DebugCopyBufferComputeTest();
    }
}
