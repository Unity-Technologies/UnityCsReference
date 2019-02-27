// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

        [FreeFunction("EditorCameraUtilsScripting::RenderToCubemap")]
        static extern int RenderToCubemapImpl(Camera camera, Texture target, [DefaultValue("63")] int faceMask, StaticEditorFlags culledFlags);
    }
}
