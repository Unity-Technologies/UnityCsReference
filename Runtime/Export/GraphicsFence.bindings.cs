// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using uei = UnityEngine.Internal;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace UnityEngine.Rendering
{
    public enum SynchronisationStageFlags
    {
        VertexProcessing = 1,
        PixelProcessing = 2,
        ComputeProcessing = 4,
        AllGPUOperations = VertexProcessing | PixelProcessing | ComputeProcessing,
    }

    // The type of GraphicsFence to create. CPUSynchronization one can only be used to check whether the GPU has passed the fence.
    // AsyncQueueSynchronisation can be used to synchronise between the main thread and the async queue
    public enum GraphicsFenceType
    {
        AsyncQueueSynchronisation = 0,
//        CPUSynchronisation = 0,
    }


    [NativeHeader("Runtime/Graphics/GPUFence.h")]
    [UsedByNativeCode]
    public struct GraphicsFence
    {
        internal IntPtr m_Ptr;
        internal int m_Version;
        internal GraphicsFenceType m_FenceType;

        internal static SynchronisationStageFlags TranslateSynchronizationStageToFlags(SynchronisationStage s)
        {
            return s == SynchronisationStage.VertexProcessing ? SynchronisationStageFlags.VertexProcessing : SynchronisationStageFlags.PixelProcessing;
        }

        public bool passed
        {
            get
            {
                Validate();

                if (!SystemInfo.supportsGraphicsFence)
                    throw new System.NotSupportedException("Cannot determine if this GraphicsFence has passed as this platform has not implemented GraphicsFences.");

                if (!IsFencePending())
                    return true;

                return HasFencePassed_Internal(m_Ptr);
            }
        }

        [FreeFunction("GPUFenceInternals::HasFencePassed_Internal")]
        extern private static bool HasFencePassed_Internal(IntPtr fencePtr);

        internal void InitPostAllocation()
        {
            if (m_Ptr == IntPtr.Zero)
            {
                if (SystemInfo.supportsGraphicsFence)
                {
                    throw new System.NullReferenceException("The internal fence ptr is null, this should not be possible for fences that have been correctly constructed using Graphics.CreateGraphicsFence() or CommandBuffer.CreateGraphicsFence()");
                }
                m_Version = GetPlatformNotSupportedVersion();
                return;
            }

            m_Version = GetVersionNumber(m_Ptr);
        }

        internal bool IsFencePending()
        {
            if (m_Ptr == IntPtr.Zero)
                return false;

            return m_Version == GetVersionNumber(m_Ptr);
        }

        internal void Validate()
        {
            if (m_Version == 0 || (SystemInfo.supportsGraphicsFence && m_Version == GetPlatformNotSupportedVersion()))
                throw new System.InvalidOperationException("This GraphicsFence object has not been correctly constructed see Graphics.CreateGraphicsFence() or CommandBuffer.CreateGraphicsFence()");
        }

        private int GetPlatformNotSupportedVersion()
        {
            return -1;
        }

        [NativeThrows]
        [FreeFunction("GPUFenceInternals::GetVersionNumber")]
        extern private static int GetVersionNumber(IntPtr fencePtr);
    }
}
