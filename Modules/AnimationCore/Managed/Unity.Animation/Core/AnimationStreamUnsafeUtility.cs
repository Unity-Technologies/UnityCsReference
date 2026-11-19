// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Collections;

namespace Unity.Animations.LowLevel.Unsafe
{
    [GenerateTestsForBurstCompatibility]
    unsafe static class AnimationStreamUnsafeUtility
    {
        /// <summary>
        /// Returns the void pointer at the start of the AnimatedData.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* GetUnsafePtr(this in AnimationStream stream) => stream.m_LocalTranslationData.m_Ptr;

        /// <summary>
        /// Returns the float4 pointer at the start of the interpolated data chunks.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float4* GetInterpolatedDataChunkUnsafePtr(this in AnimationStream stream) => (float4*)stream.m_LocalTranslationData.m_Ptr;

        /// <summary>
        /// Returns the int4 pointer at the start of the discrete data chunks.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int4* GetDiscreteDataChunkUnsafePtr(this in AnimationStream stream) => (int4*)stream.m_IntData.m_Ptr;

        /// <summary>
        /// Returns the quaternion4 pointer at the start of the rotation data chunks.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static quaternion4* GetRotationDataChunkUnsafePtr(this in AnimationStream stream) => stream.m_LocalRotationData.m_Ptr;
    }
}
