// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.Networking
{
    [NativeHeader("Modules/UnityWebRequestAudio/Public/DownloadHandlerAudioClip.h")]
    [NativeHeader("Modules/UnityWebRequestAudio/Public/DownloadHandlerMovieTexture.h")]
    internal static class WebRequestWWW
    {
        [FreeFunction("UnityWebRequestCreateAudioClip")]
        internal extern static AudioClip InternalCreateAudioClipUsingDH(DownloadHandler dh, string url, bool stream, bool compressed, AudioType audioType);

        [FreeFunction("UnityWebRequestCreateMovieTexture")]
        internal extern static MovieTexture InternalCreateMovieTextureUsingDH(DownloadHandler dh);
    }
}
