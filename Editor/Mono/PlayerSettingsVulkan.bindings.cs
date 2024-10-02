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

        private static extern UInt32 GetVulkanNumSwapchainBuffersImpl();
        private static extern void SetVulkanNumSwapchainBuffersImpl(UInt32 value);

        // NOTE: While in the editor, changing this value can be destructive so we force 3 swapchain buffers while running in the editor.
        public static UInt32 vulkanNumSwapchainBuffers
        {
            get
            {
                // Must match the value PlayerSettings::kFixedEditorVulkanSwapchainBufferCount in native code, 
                // explicitly report the current value being used.
                const UInt32 kFixedEditorVulkanSwapchainBufferCount = 3;
                if (EditorApplication.isPlaying)
                    return kFixedEditorVulkanSwapchainBufferCount;
                else
                return GetVulkanNumSwapchainBuffersImpl();
            }

            set => SetVulkanNumSwapchainBuffersImpl(value);
        }

        public static extern bool vulkanEnableLateAcquireNextImage { get; set; }

        [Obsolete("Vulkan SW command buffers are deprecated, vulkanUseSWCommandBuffers will be ignored.")]
        public static bool vulkanUseSWCommandBuffers { get { return false; } set {} }

        public static extern bool vulkanEnablePreTransform { get; set; }
    }
}
