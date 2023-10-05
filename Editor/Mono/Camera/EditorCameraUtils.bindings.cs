// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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

        public static unsafe void GetRenderersFilteringResults(ReadOnlySpan<int> rendererIDs, Span<bool> results)
        {
            if (rendererIDs.Length != results.Length)
                throw new ArgumentException("rendererIDs and results NativeArrays don't match in length.");

            GetRenderersFilteringResultsImpl(rendererIDs, results);
        }

        [FreeFunction("EditorCameraUtilsScripting::RenderToCubemap")]
        static extern int RenderToCubemapImpl(Camera camera, Texture target, [DefaultValue("63")] int faceMask, StaticEditorFlags culledFlags);

        [FreeFunction("EditorCameraUtilsScripting::GetRenderersFilteringResults")]
        static extern unsafe void GetRenderersFilteringResultsImpl(ReadOnlySpan<int> rendererIDs, Span<bool> results);

    }
}
