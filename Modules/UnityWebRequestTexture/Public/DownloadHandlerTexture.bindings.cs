// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngineInternal;
using Unity.Collections;

namespace UnityEngine.Networking
{
    [Flags]
    public enum DownloadedTextureFlags : uint
    {
        None = 0,
        Readable = 1 << 0,
        MipmapChain = 1 << 1,
        LinearColorSpace = 1 << 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DownloadedTextureParams
    {
        public DownloadedTextureFlags flags;
        public int mipmapCount;

        public static DownloadedTextureParams Default => new DownloadedTextureParams()
        {
            flags = DownloadedTextureFlags.Readable | DownloadedTextureFlags.MipmapChain,
            mipmapCount = -1,
        };

        public bool readable
        {
            get => flags.HasFlag(DownloadedTextureFlags.Readable);
            set => SetFlags(DownloadedTextureFlags.Readable, value);
        }

        public bool mipmapChain
        {
            get => flags.HasFlag(DownloadedTextureFlags.MipmapChain);
            set => SetFlags(DownloadedTextureFlags.MipmapChain, value);
        }

        public bool linearColorSpace
        {
            get => flags.HasFlag(DownloadedTextureFlags.LinearColorSpace);
            set => SetFlags(DownloadedTextureFlags.LinearColorSpace, value);
        }

        void SetFlags(DownloadedTextureFlags flgs, bool add)
        {
            if (add)
                flags |= flgs;
            else
                flags &= ~flgs;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequestTexture/Public/DownloadHandlerTexture.h")]
    public sealed class DownloadHandlerTexture : DownloadHandler
    {
        private NativeArray<byte> m_NativeData;

        private static extern IntPtr Create([Unmarshalled] DownloadHandlerTexture obj, DownloadedTextureParams parameters);

        private void InternalCreateTexture(DownloadedTextureParams parameters)
        {
            m_Ptr = Create(this, parameters);
        }

        public DownloadHandlerTexture()
            : this(true)
        {
        }

        public DownloadHandlerTexture(bool readable)
        {
            var parameters = DownloadedTextureParams.Default;
            parameters.readable = readable;
            InternalCreateTexture(parameters);
        }

        public DownloadHandlerTexture(DownloadedTextureParams parameters)
        {
            InternalCreateTexture(parameters);
        }

        protected override NativeArray<byte> GetNativeData()
        {
            return InternalGetNativeArray(this, ref m_NativeData);
        }

        public override void Dispose()
        {
            DisposeNativeArray(ref m_NativeData);
            base.Dispose();
        }

        public Texture2D texture
        {
            get { return InternalGetTextureNative(); }
        }

        [NativeThrows]
        private extern Texture2D InternalGetTextureNative();

        public static Texture2D GetContent(UnityWebRequest www)
        {
            return GetCheckedDownloader<DownloadHandlerTexture>(www).texture;
        }
        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(DownloadHandlerTexture handler) => handler.m_Ptr;
        }

    }
}
