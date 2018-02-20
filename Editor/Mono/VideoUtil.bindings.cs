// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Modules/Video/Public/VideoManager.h")]
    [NativeHeader("Runtime/Graphics/Texture.h")]
    [StaticAccessor("VideoManager::GetInstance()", StaticAccessorType.Dot)]
    internal static class VideoUtil
    {
        public static extern GUID StartPreview(VideoClip clip);

        public static extern void StopPreview(GUID id);

        public static extern void PlayPreview(GUID id, bool loop);

        public static extern void PausePreview(GUID id);

        public static extern bool IsPreviewPlaying(GUID id);

        public static extern Texture GetPreviewTexture(GUID id);
    }
}

