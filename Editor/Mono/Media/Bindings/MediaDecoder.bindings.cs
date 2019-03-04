// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("VideoTesting")]
namespace UnityEditorInternal.Media
{
    internal class MediaDecoder : IDisposable
    {
        IntPtr m_Ptr;

        public MediaDecoder(string filePath)
        {
            m_Ptr = Create(filePath);
        }

        ~MediaDecoder()
        {
            Dispose();
        }

        public bool GetNextFrame(Texture2D tex, out MediaTime time)
        {
            return Internal_MediaDecoder_GetNextFrame(m_Ptr, tex, out time);
        }

        public bool SetPosition(MediaTime time)
        {
            return Internal_MediaDecoder_SetPosition(m_Ptr, time);
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Release(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        private IntPtr Create(string filePath)
        {
            IntPtr ptr = Internal_MediaDecoder_Create(filePath);
            if (ptr == IntPtr.Zero)
                throw new InvalidOperationException(
                    "MediaDecoder: Could not open " + filePath);
            return ptr;
        }

        [NativeHeader("Editor/Mono/Media/Bindings/MediaDecoder.bindings.h")]
        [FreeFunction]
        extern private static IntPtr Internal_MediaDecoder_Create(string filePath);

        [NativeHeader("Modules/Video/Public/Base/VideoClipMedia.h")]
        [FreeFunction("VideoClipMedia::Release")]
        extern private static void Internal_Release(IntPtr decoder);

        [FreeFunction]
        extern private static bool Internal_MediaDecoder_GetNextFrame(IntPtr decoder, Texture2D texture, out MediaTime time);

        [FreeFunction]
        extern private static bool Internal_MediaDecoder_SetPosition(IntPtr decoder, MediaTime time);
    }
}
