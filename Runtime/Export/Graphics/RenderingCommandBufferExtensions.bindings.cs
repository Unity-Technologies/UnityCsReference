// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using UnityEngine;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Export/Graphics/RenderingCommandBufferExtensions.bindings.h")]
    [UsedByNativeCode]
    public static class CommandBufferExtensions
    {
        // Extension calls into the RenderCommandBufferExtensions_Bindings
        [FreeFunction("RenderingCommandBufferExtensions_Bindings::Internal_SwitchIntoFastMemory")]
        extern private static void Internal_SwitchIntoFastMemory(CommandBuffer cmd, ref UnityEngine.Rendering.RenderTargetIdentifier rt, UnityEngine.Rendering.FastMemoryFlags fastMemoryFlags, float residency, bool copyContents);

        [FreeFunction("RenderingCommandBufferExtensions_Bindings::Internal_SwitchOutOfFastMemory")]
        extern private static void Internal_SwitchOutOfFastMemory(CommandBuffer cmd, ref UnityEngine.Rendering.RenderTargetIdentifier rt, bool copyContents);


        // API functions
        // SwitchIntoFastMemory is only relevant on XboxOne, on other platforms it is an empty stub
        [NativeConditional("UNITY_XBOXONE")]
        public static void SwitchIntoFastMemory(this CommandBuffer cmd, RenderTargetIdentifier rid, FastMemoryFlags fastMemoryFlags, float residency, bool copyContents)
        {
            Internal_SwitchIntoFastMemory(cmd, ref rid, fastMemoryFlags, residency, copyContents);
        }

        // SwitchOutOfFastMemory is only relevant on XboxOne, on other platforms it is an empty stub
        [NativeConditional("UNITY_XBOXONE")]
        public static void SwitchOutOfFastMemory(this CommandBuffer cmd, RenderTargetIdentifier rid, bool copyContents)
        {
            Internal_SwitchOutOfFastMemory(cmd, ref rid, copyContents);
        }
    }
}
