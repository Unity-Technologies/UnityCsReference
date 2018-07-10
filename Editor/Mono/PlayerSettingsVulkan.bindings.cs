// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using System;

namespace UnityEditor
{
    public partial class PlayerSettings : UnityEngine.Object
    {
        public static extern bool vulkanEnableSetSRGBWrite { get; set; }

        [Obsolete("Vulkan SW command buffers are deprecated, vulkanUseSWCommandBuffers will be ignored.")]
        public static bool vulkanUseSWCommandBuffers { get { return false; } set {} }
    }
}
