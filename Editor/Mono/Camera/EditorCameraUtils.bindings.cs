// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEditor.Rendering
{
    [NativeHeader("Editor/Src/Camera/EditorCameraUtils.h")]
    [RequiredByNativeCode]
    public static class EditorCameraUtils
    {
        public static bool RenderToCubemap(this Camera camera, Texture target, int faceMask, StaticEditorFlags culledFlags)
            => RenderToCubemapImpl(camera, target, faceMask, culledFlags) == 1;

        [Obsolete("Obsolete. Use GetRenderersFilteringResults(ReadOnlySpan<EntityId>, Span<bool>) instead.")]
        public static unsafe void GetRenderersFilteringResults(ReadOnlySpan<int> rendererIDs, Span<bool> results)
        {
            Debug.Assert(UnsafeUtility.SizeOf<EntityId>() == sizeof(int), "EntityId and int should have the same size for this conversion to work correctly.");
            GetRenderersFilteringResults(MemoryMarshal.Cast<int, EntityId>(rendererIDs), results);
        }

        public static unsafe void GetRenderersFilteringResults(ReadOnlySpan<EntityId> rendererIDs, Span<bool> results)
        {
            if (rendererIDs.Length != results.Length)
                throw new ArgumentException("rendererIDs and results NativeArrays don't match in length.");

            GetRenderersFilteringResultsImpl(rendererIDs, results);
        }

        [Obsolete("Obsolete. Use GetRenderersHiddenResultBits(ReadOnlySpan<EntityId>, Span<ulong>) instead.")]
        public static unsafe void GetRenderersHiddenResultBits(ReadOnlySpan<int> rendererIDs, Span<ulong> resultBits)
        {
            Debug.Assert(UnsafeUtility.SizeOf<EntityId>() == sizeof(int), "EntityId and int should have the same size for this conversion to work correctly.");
            GetRenderersHiddenResultBits(MemoryMarshal.Cast<int, EntityId>(rendererIDs), resultBits);
        }

        public static unsafe void GetRenderersHiddenResultBits(ReadOnlySpan<EntityId> rendererIDs, Span<ulong> resultBits)
        {
            const int kBitsPerChunkCount = sizeof(ulong) * 8;

            int resultBitsLengthExpected = (rendererIDs.Length + kBitsPerChunkCount - 1) / kBitsPerChunkCount;

            if (resultBitsLengthExpected != resultBits.Length)
                throw new ArgumentException("Unexpected resultBits Span length. The expected length of resultBits Span should be equal to the formula: (rendererIDs.Length + 63) / 64");

            GetRenderersHiddenResultBitsImpl(rendererIDs, resultBits);
        }

        [FreeFunction("EditorCameraUtilsScripting::RenderToCubemap")]
        static extern int RenderToCubemapImpl(Camera camera, Texture target, [DefaultValue("63")] int faceMask, StaticEditorFlags culledFlags);

        [FreeFunction("EditorCameraUtilsScripting::GetRenderersFilteringResults")]
        static extern unsafe void GetRenderersFilteringResultsImpl(ReadOnlySpan<EntityId> rendererIDs, Span<bool> results);

        [FreeFunction("EditorCameraUtilsScripting::GetRenderersHiddenResultBits")]
        static extern unsafe void GetRenderersHiddenResultBitsImpl(ReadOnlySpan<EntityId> rendererIDs, Span<ulong> resultBits);
    }
}
