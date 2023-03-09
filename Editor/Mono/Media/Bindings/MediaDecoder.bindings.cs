// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Video;
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

        public MediaDecoder(VideoClip clip)
        {
            m_Ptr = Create(clip);
        }

        ~MediaDecoder()
        {
            Dispose();
        }

        public bool GetNextFrame(Texture2D tex, out MediaTime time)
        {
            ThrowIfDisposed();
            return Internal_MediaDecoder_GetNextFrame(m_Ptr, tex, out time);
        }

        public int GetNextSamples(ushort trackIndex, NativeArray<float> interleavedSamples)
        {
            ThrowIfDisposed();
            unsafe
            {
                return Internal_MediaDecoder_GetNextSamples(
                    m_Ptr, trackIndex, interleavedSamples.GetUnsafePtr(), interleavedSamples.Length);
            }
        }

        public bool SetPosition(MediaTime time)
        {
            ThrowIfDisposed();
            return Internal_MediaDecoder_SetPosition(m_Ptr, time);
        }

        public string[] GetCustomDependencies()
        {
            ThrowIfDisposed();
            return Internal_MediaDecoder_GetCustomDependencies(m_Ptr);
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

        private IntPtr Create(VideoClip clip)
        {
            IntPtr ptr = Internal_MediaDecoder_CreateFromClip(clip);
            if (ptr == IntPtr.Zero)
                throw new InvalidOperationException(
                    "MediaDecoder: Could not open clip " + clip.name);
            return ptr;
        }

        private void ThrowIfDisposed()
        {
            if (m_Ptr == IntPtr.Zero)
                throw new ObjectDisposedException("MediaDecoder");
        }

        [NativeHeader("Editor/Mono/Media/Bindings/MediaDecoder.bindings.h")]
        [FreeFunction]
        extern private static IntPtr Internal_MediaDecoder_Create(string filePath);

        [NativeHeader("Editor/Mono/Media/Bindings/MediaDecoder.bindings.h")]
        [FreeFunction]
        extern private static IntPtr Internal_MediaDecoder_CreateFromClip([NotNull] VideoClip clip);

        [NativeHeader("Modules/Video/Public/Base/VideoClipMedia.h")]
        [FreeFunction("VideoClipMedia::Release")]
        extern private static void Internal_Release(IntPtr decoder);

        [FreeFunction]
        extern private static bool Internal_MediaDecoder_GetNextFrame(IntPtr decoder, [NotNull] Texture2D texture, out MediaTime time);

        [FreeFunction]
        unsafe extern private static int Internal_MediaDecoder_GetNextSamples(IntPtr decoder, ushort trackIndex, void* buffer, int sampleCount);

        [FreeFunction]
        extern private static bool Internal_MediaDecoder_SetPosition(IntPtr decoder, MediaTime time);

        [NativeHeader("Editor/Mono/Media/Bindings/MediaDecoder.bindings.h")]
        [FreeFunction]
        extern private static string[] Internal_MediaDecoder_GetCustomDependencies(IntPtr decoder);
    }
}
